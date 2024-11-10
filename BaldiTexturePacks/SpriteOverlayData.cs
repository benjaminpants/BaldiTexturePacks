using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks
{
    public class SpriteOverlayData
    {
        [JsonProperty("Sprite")]
        public string sprite;

        [JsonProperty("PixelsPerUnit")]
        public float pixelsPerUnit;

        [JsonProperty("Pivot")]
        public float[] pivot;

        public Sprite GenerateSprite(string imagePath)
        {
            return AssetLoader.SpriteFromFile(Path.Combine(imagePath, sprite), new Vector2(pivot[0], pivot[1]), pixelsPerUnit);
        }
    }
}
