using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Exceptions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace StorageTests
{
    /// <summary>
    /// Verifies that a non-JSON error body (e.g. a gateway or plain-text error) surfaces a
    /// <see cref="SupabaseStorageException"/> carrying the real status and body, rather than a
    /// Newtonsoft JSON parse error escaping from <c>ErrorResponse.TryParse</c>.
    /// </summary>
    [TestClass]
    public class ErrorResponseTests
    {
        private const string Bucket = "bucket";

        private WireMockServer server = null!;
        private Client client = null!;

        [TestInitialize]
        public void Initialize()
        {
            server = WireMockServer.Start();
            client = new Client($"{server.Url}/storage/v1", new Dictionary<string, string>
            {
                { "Authorization", "Bearer test-key" }
            });
        }

        [TestCleanup]
        public void Cleanup() => server.Stop();

        [TestMethod(DisplayName = "TryParse returns null for a non-JSON error body")]
        public void TryParseReturnsNullForNonJsonBody() =>
            Assert.IsNull(ErrorResponse.TryParse("Upload failed: gateway error"));

        [TestMethod(DisplayName = "TryParse reads the status and message from a JSON error body")]
        public void TryParseReadsJsonErrorBody()
        {
            var parsed = ErrorResponse.TryParse("{\"statusCode\":404,\"message\":\"Not found\"}");
            Assert.AreEqual(404, parsed?.StatusCode);
            Assert.AreEqual("Not found", parsed?.Message);
        }

        [TestMethod(DisplayName = "A non-JSON error response surfaces a SupabaseStorageException with the real status and body")]
        public async Task NonJsonErrorResponseSurfacesStorageException()
        {
            const string body = "502 Bad Gateway";
            server.Given(Request.Create().WithPath($"/storage/v1/object/list/{Bucket}").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(502).WithBody(body));

            var exception = await Assert.ThrowsAsync<SupabaseStorageException>(
                () => client.From(Bucket).List());

            Assert.AreEqual(502, exception.StatusCode);
            Assert.AreEqual(body, exception.Content);
        }
    }
}
