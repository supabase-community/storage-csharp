using System.Text.Json.Serialization;

namespace Supabase.Storage
{
    /// <summary>
    /// Represents the response received when creating a signed URL for file access through Supabase Storage.
    /// </summary>
    public class CreateSignedUrlResponse
    {
        /// <summary>
        /// Represents the signed URL returned as part of a response when requesting access to a file
        /// stored in Supabase Storage. This URL can be used to access the file directly with
        /// the defined expiration and optional transformations or download options applied.
        /// </summary>
        [JsonPropertyName("signedURL")]
        public string? SignedUrl { get; set; }
    }

    /// <summary>
    /// Represents the extended response received when creating multiple signed URLs
    /// for file access through Supabase Storage. In addition to the signed URL, it includes
    /// the associated file path.
    /// </summary>
    public class CreateSignedUrlsResponse: CreateSignedUrlResponse
    {
        /// <summary>
        /// Represents the file path associated with a signed URL in the response.
        /// This property indicates the specific file path for which the signed URL
        /// was generated, allowing identification of the file within the storage bucket.
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
