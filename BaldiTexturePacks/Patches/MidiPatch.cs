using HarmonyLib;
using System;

namespace BaldiTexturePacks.Patches
{
    [HarmonyPatch(typeof(MusicManager))]
    [HarmonyPatch("PlayMidi")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(bool) })]
    class ReplaceMidi
    {
        static void Prefix(ref string song)
        {
            if (TexturePacksPlugin.currentMidiReplacements.ContainsKey(song))
            {
                song = TexturePacksPlugin.currentMidiReplacements[song];
            }
        }
    }

}
