using Newtonsoft.Json;

namespace Supabase.Storage;

public class FetchParameter
{
    [JsonProperty("cache")]
    public FetchCache? Cache { get; set; }
}