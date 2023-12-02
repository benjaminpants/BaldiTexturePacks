using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaldiTexturePacks
{
    [HarmonyPatch(typeof(MusicManager))]
    [HarmonyPatch("PlayMidi")]
    class ReplaceMidi
    {
        static void Prefix(MusicManager __instance, ref string song)
        {
            if (TPPlugin.Instance.midiOverrides.ContainsKey(song))
            {
                song = TPPlugin.Instance.midiOverrides[song];
            }
        }
    }
}
