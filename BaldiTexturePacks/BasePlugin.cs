using BepInEx.Logging;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using MTM101BaldAPI;

namespace BaldiTexturePacks
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.baldiplus.texturepacks", "Texture Packs", "3.0.0.0")]
    public class TexturePacksPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.texturepacks");
            harmony.PatchAllConditionals();
        }
    }
}
