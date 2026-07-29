using Newtonsoft.Json;

namespace Supabase.Storage;

public class PurgeCacheOptions
{
    /// <summary>
    /// If true, purges only the transformations (resized/formatted variants) for the object or bucket,
    /// leaving the original cached file intact. If omitted, purges all cached versions
    /// </summary>
    [JsonProperty("transformations")]
    public bool? Transformations { get; set; } = true;
}