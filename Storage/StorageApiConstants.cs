namespace Supabase.Storage
{
    /// <summary>
    /// Internal constants used by the Storage API.
    /// </summary>
    internal static class StorageConstants
    {
        /// <summary>
        /// HTTP header names used in Storage API requests.
        /// </summary>
        public static class Headers
        {
            public const string Authorization = "Authorization";
            public const string CacheControl = "cache-control";
            public const string ContentType = "content-type";
            public const string Upsert = "x-upsert";
            public const string Metadata = "x-metadata";
            public const string Duplex = "x-duplex";
        }

        /// <summary>
        /// API endpoint paths.
        /// </summary>
        public static class Endpoints
        {
            public const string Object = "/object";
            public const string ObjectPublic = "/object/public";
            public const string ObjectSign = "/object/sign";
            public const string ObjectList = "/object/list";
            public const string ObjectInfo = "/object/info";
            public const string ObjectMove = "/object/move";
            public const string ObjectCopy = "/object/copy";
            public const string RenderImageAuthenticated = "/render/image/authenticated";
            public const string RenderImagePublic = "/render/image/public";
            public const string UploadResumable = "/upload/resumable";
            public const string UploadSign = "/object/upload/sign";
        }

        /// <summary>
        /// Default values.
        /// </summary>
        public static class Defaults
        {
            public const int CacheControlMaxAge = 3600;
            public const int UploadChunkSize = 6 * 1024 * 1024; // 6MB
        }
    }
}
