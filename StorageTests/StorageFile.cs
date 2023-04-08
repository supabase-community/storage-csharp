using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Interfaces;

namespace StorageTests
{
    [TestClass]
    public class StorageFile
    {
        Supabase.Storage.Client storage => Helpers.GetClient();

        private string bucketId;
        private IStorageFileApi<FileObject> bucket;

        [TestInitialize]
        public async Task InitializeTest()
        {
            bucketId = Guid.NewGuid().ToString();

            if (bucket == null && await storage.GetBucket(bucketId) == null)
            {
                await storage.CreateBucket(bucketId, new BucketUpsertOptions { Public = true });
            }

            bucket = storage.From(bucketId);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            if (bucket != null)
            {
                var files = await bucket.List();

                foreach (var file in files)
                    await bucket.Remove(new List<string> { file.Name });

                await storage.DeleteBucket(bucketId);
            }
        }

        [TestMethod("File: Upload File")]
        public async Task UploadFile()
        {
            var didTriggerProgress = new TaskCompletionSource<bool>();

            var asset = "supabase-csharp.png";
            var name = $"{Guid.NewGuid()}.png";
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("file:", "");
            var imagePath = Path.Combine(basePath, "Assets", asset);

            EventHandler<float> onProgress = (sender, args) =>
            {
                didTriggerProgress.TrySetResult(true);
            };

            await bucket.Upload(imagePath, name, null, onProgress);

            var list = await bucket.List();

            var existing = list.Find(item => item.Name == name);
            Assert.IsNotNull(existing);

            var sentProgressEvent = await didTriggerProgress.Task;
            Assert.IsTrue(sentProgressEvent);

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Upload Arbitrary Byte Array")]
        public async Task UploadArbitraryByteArray()
        {
            var tsc = new TaskCompletionSource<bool>();

            var name = $"{Guid.NewGuid()}.bin";
            EventHandler<float> onProgress = (sender, args) =>
            {
                tsc.TrySetResult(true);
            };

            await bucket.Upload(new Byte[] { 0x0, 0x0, 0x0 }, name, null, onProgress);

            var list = await bucket.List();

            var existing = list.Find(item => item.Name == name);
            Assert.IsNotNull(existing);

            var sentProgressEvent = await tsc.Task;
            Assert.IsTrue(sentProgressEvent);

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Download")]
        public async Task DownloadFile()
        {
            var tsc = new TaskCompletionSource<bool>();

            var asset = "supabase-csharp.png";
            var name = $"{Guid.NewGuid()}.png";
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("file:", "");
            var imagePath = Path.Combine(basePath, "Assets", asset);

            await bucket.Upload(imagePath, name);

            EventHandler<float> onProgress = (sender, args) =>
            {
                tsc.TrySetResult(true);
            };

            var downloadPath = Path.Combine(basePath, name);
            await bucket.Download(name, downloadPath, onProgress);

            var sentProgressEvent = await tsc.Task;
            Assert.IsTrue(sentProgressEvent);

            Assert.IsTrue(File.Exists(downloadPath));

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Download Bytes")]
        public async Task DownloadBytes()
        {
            var tsc = new TaskCompletionSource<bool>();

            var data = new Byte[] { 0x0 };
            var name = $"{Guid.NewGuid()}.bin";
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("file:", "");

            await bucket.Upload(data, name);

            EventHandler<float> onProgress = (sender, args) =>
            {
                tsc.TrySetResult(true);
            };

            var downloadPath = Path.Combine(basePath, name);
            var result = await bucket.Download(name, onProgress);

            var sentProgressEvent = await tsc.Task;
            Assert.IsTrue(sentProgressEvent);

            Assert.IsTrue(data.SequenceEqual(result));

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Rename")]
        public async Task Move()
        {
            var name = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
            await bucket.Move(name, "new-file.bin");
            var items = await bucket.List();

            Assert.IsNotNull(items.Find((f) => f.Name == "new-file.bin"));
            Assert.IsNull(items.Find((f) => f.Name == name));
        }

        [TestMethod("File: Get Public Link")]
        public async Task GetPublicLink()
        {
            var name = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
            var url = bucket.GetPublicUrl(name);
            await bucket.Remove(new List<string> { name });

            Assert.IsNotNull(url);
        }

        [TestMethod("File: Get Signed Link")]
        public async Task GetSignedLink()
        {
            var name = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name);

            var url = await bucket.CreateSignedUrl(name, 3600);
            Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));

            await bucket.Remove(new List<string> { name });
        }

        [TestMethod("File: Get Multiple Signed Links")]
        public async Task GetMultipleSignedLinks()
        {
            var name1 = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name1);

            var name2 = $"{Guid.NewGuid()}.bin";
            await bucket.Upload(new Byte[] { 0x0, 0x1 }, name2);

            var urls = await bucket.CreateSignedUrls(new List<string> { name1, name2 }, 3600);

            foreach (var response in urls)
            {
                Assert.IsTrue(Uri.IsWellFormedUriString(response.SignedUrl, UriKind.Absolute));
            }

            await bucket.Remove(new List<string> { name1 });
        }

        [TestMethod("File: Can Create Signed Upload Url")]
        public async Task CanCreateSignedUploadUrl()
        {
            var result = await bucket.CreateUploadSignedUrl("test.png");
            Assert.IsTrue(Uri.IsWellFormedUriString(result.SignedUrl.ToString(), UriKind.Absolute));
        }

    }
}
