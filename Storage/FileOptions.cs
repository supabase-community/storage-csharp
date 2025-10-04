using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Supabase.Storage
{
    /// <summary>
    /// Represents configuration options for file operations in Supabase Storage.
    /// </summary>
    public class FileOptions
    {
        /// <summary>
        /// Controls caching behavior for the file. Default value is "3600".
        /// </summary>
        [JsonPropertyName("cacheControl")]
        public string CacheControl { get; set; } = "3600";

        /// <summary>
        /// Specifies the content type of the file. Default value is "text/plain;charset=UTF-8".
        /// </summary>
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "text/plain;charset=UTF-8";

        /// <summary>
        /// Determines whether to perform an upsert operation (update if exists, insert if not).
        /// </summary>
        [JsonPropertyName("upsert")]
        public bool Upsert { get; set; }

        /// <summary>
        /// Specifies the duplex mode for the file operation.
        /// </summary>
        [JsonPropertyName("duplex")]
        public string? Duplex { get; set; }

        /// <summary>
        /// Additional metadata associated with the file.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Custom headers to be included with the file operation.
        /// </summary>
        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }
    }
}
