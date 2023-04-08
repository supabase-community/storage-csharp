using System;
using System.Collections.Generic;

namespace StorageTests
{
    public static class Helpers
    {
        public static Supabase.Storage.Client GetClient()
        {
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY");

            return new Supabase.Storage.Client(string.Format("{0}/storage/v1", url), new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {key}" },
            });
        }
    }
}

