using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaldiTexturePacks
{
    public class SubtitleOverrideData
    {
        [JsonProperty("DurationOverride")]
        public float subDurationOverride = -1f;

        [JsonProperty("KeyOverride")]
        public string keyOverride = "";

        [JsonProperty("TimedKeys")]
        public SubtitleTimedKey[] timedKeys;
    }
}
