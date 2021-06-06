using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Reactor.GameProvider
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RuntimeType
    {
        [EnumMember(Value = "mono")]
        Mono,

        [EnumMember(Value = "il2cpp")]
        IL2CPP
    }

    public abstract class BuildInfo
    {
        [JsonConstructor]
        protected BuildInfo(RuntimeType runtimeType, bool isObfuscated)
        {
            RuntimeType = runtimeType;
            IsObfuscated = isObfuscated;
        }

        [JsonProperty("runtimeType")]
        public RuntimeType RuntimeType { get; }

        [JsonProperty("isObfuscated")]
        public bool IsObfuscated { get; }
    }

    public class SteamBuildInfo : BuildInfo
    {
        [JsonConstructor]
        public SteamBuildInfo(RuntimeType runtimeType, bool isObfuscated, ulong manifestId) : base(runtimeType, isObfuscated)
        {
            ManifestId = manifestId;
        }

        [JsonProperty("manifestId")]
        public ulong ManifestId { get; }
    }

    public class ItchBuildInfo : BuildInfo
    {
        [JsonConstructor]
        public ItchBuildInfo(RuntimeType runtimeType, bool isObfuscated, int buildId) : base(runtimeType, isObfuscated)
        {
            BuildId = buildId;
        }

        [JsonProperty("buildId")]
        public int BuildId { get; }
    }
}
