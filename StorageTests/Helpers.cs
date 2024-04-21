using System;
using System.Collections.Generic;

namespace StorageTests
{
    public static class Helpers
    {
        public static string SupabaseUrl => "http://localhost:5000";
        public static string ServiceKey => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoic2VydmljZV9yb2xlIiwiaWF0IjoxNjEzNTMxOTg1LCJleHAiOjE5MjkxMDc5ODV9.th84OKK0Iz8QchDyXZRrojmKSEZ-OuitQm_5DvLiSIc";
        public static string PublicKey => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJhdXRoZW50aWNhdGVkIiwic3ViIjoiMzE3ZWFkY2UtNjMxYS00NDI5LWEwYmItZjE5YTdhNTE3YjRhIiwiZW1haWwiOiJpbmlhbit0ZXN0MUBzdXBhYmFzZS5pbyIsImV4cCI6MTkzOTEwNzk4NSwiYXBwX21ldGFkYXRhIjp7InByb3ZpZGVyIjoiZW1haWwifSwidXNlcl9tZXRhZGF0YSI6e30sInJvbGUiOiJhdXRoZW50aWNhdGVkIn0.E-x3oYcHIjFCdUO1M3wKDl1Ln32mik0xdHT2PjrvN70";

        public static string StorageUrl => $"{SupabaseUrl}";
        
        public static Supabase.Storage.Client GetServiceClient()
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

