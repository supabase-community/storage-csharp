using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;
using Supabase.Storage.Interfaces;

namespace StorageTests.Files;

/// <summary>
/// Covers <see cref="StorageFileApi.GetPublicUrl"/>, the pure URL builder: it targets the public
/// object path by default, switches to the image-render path when transform options are supplied,
/// and appends the download attribute when download options ask for it.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class PublicUrlTests
{
    private const string BaseUrl = "http://localhost/storage/v1";

    private static IStorageFileApi<FileObject> Bucket() =>
        new Client(BaseUrl, new Dictionary<string, string>()).From("bucket");

    [TestMethod]
    public void GetPublicUrl_ShouldTargetThePublicObjectPath()
    {
        var url = Bucket().GetPublicUrl("a.png", null);
        url.Should().StartWith($"{BaseUrl}/object/public/bucket/a.png");
    }

    [TestMethod]
    public void GetPublicUrl_ShouldUseTheRenderPath_GivenTransformOptions()
    {
        var url = Bucket().GetPublicUrl("a.png", new TransformOptions { Width = 100, Height = 100 });
        url.Should().Contain("/render/image/public/bucket/a.png").And.Contain("width=100");
    }

    [TestMethod]
    public void GetPublicUrl_ShouldAppendDownloadName_GivenDownloadOptions()
    {
        var url = Bucket().GetPublicUrl("a.png", null, new DownloadOptions { FileName = "custom.png" });
        url.Should().Contain("download=custom.png");
    }
}