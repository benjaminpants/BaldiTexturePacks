using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks.Components
{
    public class HardcodedTexturePackReplacements : MonoBehaviour
    {
        public static HardcodedTexturePackReplacements Instance;
        public bool BSODAShouldRotate = true;
        public bool UseClassicDetentionText = false;
        public Color ItemSlotBackgroundColor = Color.white;
        public Color ItemSlotHighlightColor = Color.red;
        public string DetentionText = "You get detention!\n{0} seconds remain.";

        void Awake()
        {
            Instance = this;
        }
    }
}
