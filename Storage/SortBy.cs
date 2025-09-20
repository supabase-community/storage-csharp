using System.Text.Json.Serialization;

namespace Supabase.Storage
{
    /// <summary>
    /// Represents sorting configuration for Storage queries.
    /// </summary>
    public class SortBy
    {
        /// <summary>
        /// The column name to sort by.
        /// </summary>
        [JsonPropertyName("column")]
        public string? Column { get; set; }

        /// <summary>
        /// The sort order direction.
        /// </summary>
        [JsonPropertyName("order")]
        public string? Order { get; set; }
    }
}
