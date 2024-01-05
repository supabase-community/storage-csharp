using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Interfaces;

namespace StorageTests;

[TestClass]
public class StorageFileTests
{
    Client Storage => Helpers.GetClient();

    private string _bucketId = string.Empty;
    private IStorageFileApi<FileObject> _bucket = null!;

    [TestInitialize]
    public async Task InitializeTest()
    {
        _bucketId = Guid.NewGuid().ToString();

        if (_bucket == null && await Storage.GetBucket(_bucketId) == null)
        {
            await Storage.CreateBucket(_bucketId, new BucketUpsertOptions { Public = true });
        }

        _bucket = Storage.From(_bucketId);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        if (_bucket != null)
        {
            var files = await _bucket.List();

            Assert.IsNotNull(files);

            foreach (var file in files)
            {
                if (file.Name is not null)
                    await _bucket.Remove(new List<string> { file.Name });
            }

            await Storage.DeleteBucket(_bucketId);
        }
    }

    [TestMethod("File: Upload File")]
    public async Task UploadFile()
    {
        var didTriggerProgress = new TaskCompletionSource<bool>();

        var asset = "supabase-csharp.png";
        var name = $"{Guid.NewGuid()}.png";
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace("file:", "");

        Assert.IsNotNull(basePath);

        var imagePath = Path.Combine(basePath, "Assets", asset);

        await _bucket.Upload(imagePath, name, null, (_, _) => { didTriggerProgress.TrySetResult(true); });

        var list = await _bucket.List();

        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNotNull(existing);

        var sentProgressEvent = await didTriggerProgress.Task;
        Assert.IsTrue(sentProgressEvent);

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Upload Arbitrary Byte Array")]
    public async Task UploadArbitraryByteArray()
    {
        var tsc = new TaskCompletionSource<bool>();

        var name = $"{Guid.NewGuid()}.bin";

        await _bucket.Upload(new Byte[] { 0x0, 0x0, 0x0 }, name, null, (_, _) => tsc.TrySetResult(true));

        var list = await _bucket.List();
        Assert.IsNotNull(list);

        var existing = list.Find(item => item.Name == name);
        Assert.IsNotNull(existing);

        var sentProgressEvent = await tsc.Task;
        Assert.IsTrue(sentProgressEvent);

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Download")]
    public async Task DownloadFile()
    {
        var tsc = new TaskCompletionSource<bool>();

        var asset = "supabase-csharp.png";
        var name = $"{Guid.NewGuid()}.png";
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace("file:", "");
        Assert.IsNotNull(basePath);

        var imagePath = Path.Combine(basePath, "Assets", asset);

        await _bucket.Upload(imagePath, name);

        var downloadPath = Path.Combine(basePath, name);
        await _bucket.Download(name, downloadPath, (_, _) => tsc.TrySetResult(true));

        var sentProgressEvent = await tsc.Task;
        Assert.IsTrue(sentProgressEvent);

        Assert.IsTrue(File.Exists(downloadPath));

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Download Bytes")]
    public async Task DownloadBytes()
    {
        var tsc = new TaskCompletionSource<bool>();

        var data = new Byte[] { 0x0 };
        var name = $"{Guid.NewGuid()}.bin";

        await _bucket.Upload(data, name);

        var result = await _bucket.Download(name, (_, _) => tsc.TrySetResult(true));

        var sentProgressEvent = await tsc.Task;

        Assert.IsTrue(sentProgressEvent);
        Assert.IsTrue(data.SequenceEqual(result));

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Rename")]
    public async Task Move()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
        await _bucket.Move(name, "new-file.bin");
        var items = await _bucket.List();

        Assert.IsNotNull(items);

        Assert.IsNotNull(items.Find((f) => f.Name == "new-file.bin"));
        Assert.IsNull(items.Find((f) => f.Name == name));
    }

    [TestMethod("File: Get Public Link")]
    public async Task GetPublicLink()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);
        var url = _bucket.GetPublicUrl(name);
        await _bucket.Remove(new List<string> { name });

        Assert.IsNotNull(url);
    }

    [TestMethod("File: Get Signed Link")]
    public async Task GetSignedLink()
    {
        var name = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name);

        var url = await _bucket.CreateSignedUrl(name, 3600);
        Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));

        await _bucket.Remove(new List<string> { name });
    }

    [TestMethod("File: Get Multiple Signed Links")]
    public async Task GetMultipleSignedLinks()
    {
        var name1 = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name1);

        var name2 = $"{Guid.NewGuid()}.bin";
        await _bucket.Upload(new Byte[] { 0x0, 0x1 }, name2);

        var urls = await _bucket.CreateSignedUrls(new List<string> { name1, name2 }, 3600);

        Assert.IsNotNull(urls);

        foreach (var response in urls)
        {
            Assert.IsTrue(Uri.IsWellFormedUriString(response.SignedUrl, UriKind.Absolute));
        }

        await _bucket.Remove(new List<string> { name1 });
    }

    [TestMethod("File: Can Create Signed Upload Url")]
    public async Task CanCreateSignedUploadUrl()
    {
        var result = await _bucket.CreateUploadSignedUrl("test.png");
        Assert.IsTrue(Uri.IsWellFormedUriString(result.SignedUrl.ToString(), UriKind.Absolute));
    }
}