using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks
{
    public class MiscOverrides
    {
        [JsonIgnore]
        public static MiscOverrides OverrideTemplate = new MiscOverrides();

        public SerializableColor UnderwaterColor = (SerializableColor)Color.cyan;
        public SerializableColor FogColor = (SerializableColor)Color.white;
        public SerializableColor TestFogColor = (SerializableColor)Color.black;
        public SerializableColor ItemBackgroundColor = (SerializableColor)Color.white;
        public SerializableColor ItemSelectColor = (SerializableColor)Color.red;
        public SerializableColor DetentionTextColor = (SerializableColor)Color.red;
        public SerializableColor ElevatorFloorColor = (SerializableColor)Color.red;
        public SerializableColor ElevatorSeedColor = (SerializableColor)Color.red;
        public bool BSODAShouldRotate = true;
        public bool UseClassicDetentionText = false;
        public string DetentionText = "You get detention!\r\n{0} seconds remain.";
    }
}
