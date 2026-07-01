using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Storage;

namespace StorageTests;

[TestClass]
public class SearchOptionsTests
{
    [TestMethod("SearchOptions: Test Default Sort Values")]
    public void TestDefaultSortValues()
    {
        var options = new SearchOptions();
        
        Assert.AreEqual(options.SortBy.Column, "name");
        Assert.AreEqual(options.SortBy.Order, "asc");
    }
    
    
    [TestMethod("SearchOptions: Test Default Sort Column Value")]
    public void TestDefaultSortColumnValue()
    {
        var options = new SearchOptions()
        {
           
        };
        
        Assert.AreEqual(options.SortBy.Column, "name");
        Assert.AreEqual(options.SortBy.Order, "asc");
    }
}