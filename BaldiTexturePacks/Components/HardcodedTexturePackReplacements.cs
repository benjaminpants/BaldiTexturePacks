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
        public Color ItemSlotBackgroundColor = Color.white;
        public Color ItemSlotHighlightColor = Color.red;
        //public string NotebookText = "{0}/{1}";

        void Awake()
        {
            Instance = this;
        }
    }
}
