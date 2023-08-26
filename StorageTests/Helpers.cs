using System;
using System.Collections.Generic;

namespace StorageTests
{
    public static class Helpers
    {
        public static string SupabaseUrl => Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "http://localhost:3000";
        public static string ServiceKey => Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY");
        public static string PublicKey => Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");

        public static string StorageUrl => $"{SupabaseUrl}/storage/v1";
        
        public static Supabase.Storage.Client GetClient()
        {
            return new Supabase.Storage.Client(StorageUrl, new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {ServiceKey}" },
            });
        }
        
        public static Supabase.Storage.Client GetPublicClient()
        {
            return new Supabase.Storage.Client(StorageUrl, new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {PublicKey}" },
            });
        }
    }
}

