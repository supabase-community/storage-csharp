using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace StorageTests;

[TestClass]
public class SortByTests
{
    [TestMethod("SortBy: Test Default Sort Values")]
    public void TestDefaultSortValues()
    {
        var options = new SortBy();
        
        Assert.AreEqual(options.Column, "name");
        Assert.AreEqual(options.Order, "asc");
    }
    
    
    [TestMethod("SortBy: Test Default Sort Column Value")]
    public void TestDefaultSortColumnValue()
    {
        var options = new SortBy()
        {
           Column = "status"
        };
        
        Assert.AreEqual(options.Column, "status");
        Assert.AreEqual(options.Order, "asc");
    }
    
    [TestMethod("SortBy: Test Default Sort Order Value")]
    public void TestDefaultSortOrderValue()
    {
        var options = new SortBy()
        {
            Order = "desc"
        };
        
        Assert.AreEqual(options.Column, "name");
        Assert.AreEqual(options.Order, "desc");
    }
    
    [TestMethod("SortBy: Test SortBy")]
    public void TestSortByValue()
    {
        var options = new SortBy()
        {
            Order = "desc",
            Column = "updated_at"
        };
        
        Assert.AreEqual(options.Column, "updated_at");
        Assert.AreEqual(options.Order, "desc");
    }
}