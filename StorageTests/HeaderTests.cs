using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace StorageTests;

[TestClass]
public class HeaderTests
{
    [TestMethod("Header: Add Single Header")]
    public void TestHeaderAddSingle()
    {
        var header = new Header();
        header.Add("Content-Type", "application/json");

        var headers = header.Get();
        Assert.AreEqual(1, headers.Count);
        Assert.IsTrue(headers.ContainsKey("content-type"));
        Assert.AreEqual("application/json", headers["content-type"]);
    }

    [TestMethod("Header: Add Multiple Headers")]
    public void TestHeaderAddMultiple()
    {
        var header = new Header();
        var dictionary = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "X-Custom-Header", "Value" },
        };

        header.Add(dictionary);

        var headers = header.Get();
        Assert.AreEqual(2, headers.Count);
        Assert.IsTrue(headers.ContainsKey("content-type"));
        Assert.IsTrue(headers.ContainsKey("x-custom-header"));
        Assert.AreEqual("application/json", headers["content-type"]);
        Assert.AreEqual("Value", headers["x-custom-header"]);
    }

    [TestMethod("Header: Update Existing Header")]
    public void TestHeaderUpdateExisting()
    {
        var header = new Header();
        header.Add("Content-Type", "application/json");
        header.Add("CONTENT-TYPE", "text/plain");

        var headers = header.Get();
        Assert.AreEqual(1, headers.Count);
        Assert.IsTrue(headers.ContainsKey("content-type"));
        Assert.AreEqual("text/plain", headers["content-type"]);
    }

    [TestMethod("Header: Update Existing Header with Different Case")]
    public void TestHeaderUpdateExistingDifferentCase()
    {
        var header = new Header();
        header.Add("X-Custom", "value1");
        header.Add("x-custom", "value2");

        var headers = header.Get();
        Assert.AreEqual(1, headers.Count);
        Assert.AreEqual("value2", headers["x-custom"]);
    }
}
