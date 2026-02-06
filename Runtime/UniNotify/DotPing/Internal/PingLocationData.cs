using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UniCore.Notify.DotPing.Internal
{
    [Serializable]
    internal class PingLocationData
    {
        [JsonProperty] public Dictionary<string, PingLocationNode> nodes;
        [JsonProperty] public Dictionary<string, List<string>> childrenMap;
    }
}