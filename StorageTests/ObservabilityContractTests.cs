using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace StorageTests
{
    /// <summary>
    /// Contract tests for the diagnostics the SDK emits through System.Diagnostics
    /// (ActivitySource/Meter "Supabase.Storage") and for the sanitization rule: telemetry must
    /// never contain a query string, a signed-URL token, or file contents. Also covers the
    /// transfer-size metric that distinguishes uploads/downloads from control-plane requests.
    /// </summary>
    [TestClass]
    public class ObservabilityContractTests
    {
        private const string Bucket = "bucket";
        private const string SecretToken = "secret-signed-url-token-42";

        private readonly List<Activity> activities = new();
        private readonly List<(string Name, double Value, Dictionary<string, object?> Tags)> measurements = new();
        private ActivityListener activityListener = null!;
        private MeterListener meterListener = null!;
        private WireMockServer server = null!;
        private Client client = null!;

        [TestInitialize]
        public void TestInitializer()
        {
            server = WireMockServer.Start();
            client = new Client($"{server.Url}/storage/v1", new Dictionary<string, string>
            {
                { "Authorization", "Bearer test-key" }
            });

            activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == StorageDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => activities.Add(activity)
            };
            ActivitySource.AddActivityListener(activityListener);

            meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == StorageDiagnostics.SourceName)
                        listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
                Capture(instrument.Name, value, tags));
            meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
                Capture(instrument.Name, value, tags));
            meterListener.Start();
        }

        private void Capture(string name, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var tagValues = new Dictionary<string, object?>();
            foreach (var tag in tags)
                tagValues[tag.Key] = tag.Value;
            measurements.Add((name, value, tagValues));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            activityListener.Dispose();
            meterListener.Dispose();
            server.Stop();
        }

        [TestMethod(DisplayName = "A control-plane request emits an HTTP span and the request-duration metric, with no transfer direction")]
        public async Task ControlPlaneRequestIsInstrumented()
        {
            server.Given(Request.Create().WithPath($"/storage/v1/object/list/{Bucket}").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("[]"));

            await client.From(Bucket).List();

            var span = SingleSpan($"POST /storage/v1/object/list/{Bucket}");
            Assert.AreEqual("POST", span.GetTagItem("http.request.method"));
            Assert.AreEqual(200, span.GetTagItem("http.response.status_code"));
            Assert.IsNull(span.GetTagItem("storage.transfer.direction"));
            Assert.IsTrue(measurements.Any(m => m.Name == "supabase.storage.http.request.duration"));
        }

        [TestMethod(DisplayName = "A download emits a span tagged as a download and records the transfer size in bytes")]
        public async Task DownloadRecordsTransferSize()
        {
            var payload = Encoding.UTF8.GetBytes("hello-world-download-payload");
            server.Given(Request.Create().WithPath($"/storage/v1/object/{Bucket}/file.txt").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(payload));

            var bytes = await client.From(Bucket).Download("file.txt", (EventHandler<float>?)null);

            Assert.AreEqual(payload.Length, bytes.Length);
            var span = SingleSpan($"GET /storage/v1/object/{Bucket}/file.txt");
            Assert.AreEqual("download", span.GetTagItem("storage.transfer.direction"));

            var size = measurements.Single(m => m.Name == "supabase.storage.transfer.size");
            Assert.AreEqual((double)payload.Length, size.Value);
            Assert.AreEqual("download", size.Tags["storage.transfer.direction"]);
            Assert.IsTrue(measurements.Any(m => m.Name == "supabase.storage.transfer.duration"));
        }

        [TestMethod(DisplayName = "An upload emits a span tagged as an upload and records the transfer size in bytes")]
        public async Task UploadRecordsTransferSize()
        {
            var payload = Encoding.UTF8.GetBytes("some-bytes-to-upload");
            server.Given(Request.Create().WithPath($"/storage/v1/object/{Bucket}/file.bin").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"Key\":\"x\"}"));

            await client.From(Bucket).Upload(payload, "file.bin");

            var span = SingleSpan($"POST /storage/v1/object/{Bucket}/file.bin");
            Assert.AreEqual("upload", span.GetTagItem("storage.transfer.direction"));

            var size = measurements.Single(m => m.Name == "supabase.storage.transfer.size");
            Assert.AreEqual((double)payload.Length, size.Value);
            Assert.AreEqual("upload", size.Tags["storage.transfer.direction"]);
        }

        [TestMethod(DisplayName = "A failed control-plane request marks the span as an error")]
        public async Task FailedRequestMarksTheSpanAsError()
        {
            server.Given(Request.Create().WithPath($"/storage/v1/object/list/{Bucket}").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(500).WithBody("{\"message\":\"boom\"}"));

            await Assert.ThrowsAsync<Supabase.Storage.Exceptions.SupabaseStorageException>(
                () => client.From(Bucket).List());

            var span = SingleSpan($"POST /storage/v1/object/list/{Bucket}");
            Assert.AreEqual(ActivityStatusCode.Error, span.Status);
            Assert.AreEqual(500, span.GetTagItem("http.response.status_code"));
        }

        [TestMethod(DisplayName = "Telemetry never contains a signed-URL token from the query string")]
        public async Task TelemetryDoesNotLeakSignedUrlToken()
        {
            var signedUrl = new UploadSignedUrl(
                new Uri($"{server.Url}/storage/v1/object/upload/sign/{Bucket}/file.bin?token={SecretToken}"),
                SecretToken,
                "file.bin");
            server.Given(Request.Create().WithPath($"/storage/v1/object/upload/sign/{Bucket}/file.bin").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"Key\":\"x\"}"));

            await client.From(Bucket).UploadToSignedUrl(Encoding.UTF8.GetBytes("data"), signedUrl);

            var span = SingleSpan($"POST /storage/v1/object/upload/sign/{Bucket}/file.bin");
            Assert.AreEqual($"{server.Url}/storage/v1/object/upload/sign/{Bucket}/file.bin", span.GetTagItem("url.full"),
                "the signed-URL token lives in the query string and must never be recorded");

            var recorded = activities
                .SelectMany(a => a.TagObjects)
                .Select(tag => tag.Value?.ToString() ?? "")
                .Concat(measurements.SelectMany(m => m.Tags.Values).Select(v => v?.ToString() ?? ""));
            Assert.IsFalse(recorded.Any(value => value.Contains(SecretToken)),
                "no span name, tag, or metric dimension may contain the signed-URL token");
        }

        private Activity SingleSpan(string operationName) =>
            activities.Single(a => a.OperationName == operationName);
    }
}
