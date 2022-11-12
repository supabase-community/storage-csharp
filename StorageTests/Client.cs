using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace StorageTests
{
    [TestClass]
    public class Client
    {
        [TestMethod("Can use gettable headers")]
        public void CanUseGettableHeaders()
        {
            var client = new Supabase.Storage.Client("http://localhost:5000", new Dictionary<string, string> { { "Testing", "1234" } });

            client.GetHeaders = () => new Dictionary<string, string> { { "Dynamic", "4567" }, { "Testing", "4567" } };

            Assert.AreEqual("1234", client.Headers["Testing"]);
            Assert.AreEqual("4567", client.Headers["Dynamic"]);
        }
    }
}
