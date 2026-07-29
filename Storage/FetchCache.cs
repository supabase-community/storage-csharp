using System.Runtime.Serialization;
using Supabase.Core.Attributes;

namespace Supabase.Storage;

public enum FetchCache
{
    [MapTo("cache"), EnumMember(Value = "cache")]
    Cache,
    [MapTo("default"), EnumMember(Value = "default")]
    Default,
    [MapTo("no-store"), EnumMember(Value = "no-store")]
    NoStore,
    [MapTo("reload"), EnumMember(Value = "reload")]
    Reload,
    [MapTo("no-cache"), EnumMember(Value = "no-cache")]
    NoCache,
    [MapTo("force-cache"), EnumMember(Value = "force-cache")]
    ForceCache,
    [MapTo("only-if-cached"), EnumMember(Value = "only-if-cached")] 
    OnlyIfCached,
}