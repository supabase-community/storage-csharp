using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Supabase.Storage
{
    /// <summary>
    /// Represents a file object in Supabase Storage with its associated metadata and properties.
    /// This class is used for version 2 of the Storage API.
    /// </summary>
    public class FileObjectV2
    {
        
        /// <summary>
        /// The unique identifier of the file.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The version of the file.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; }

        /// <summary>
        /// The name of the file.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The identifier of the bucket containing the file.
        /// </summary>
        [JsonPropertyName("bucket_id")]
        public string? BucketId { get; set; }

        /// <summary>
        /// The timestamp when the file was last updated.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// The timestamp when the file was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// The timestamp when the file was last accessed.
        /// </summary>
        [JsonPropertyName("last_accessed_at")]
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public int? Size { get; set; }

        /// <summary>
        /// The cache control directives for the file.
        /// </summary>
        [JsonPropertyName("cache_control")]
        public string? CacheControl { get; set; }

        /// <summary>
        /// The MIME type of the file.
        /// </summary>
        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        /// <summary>
        /// The ETag of the file for caching purposes.
        /// </summary>
        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        /// <summary>
        /// The timestamp when the file was last modified.
        /// </summary>
        [JsonPropertyName("last_modified")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// The custom metadata associated with the file.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; } 
    }
}
