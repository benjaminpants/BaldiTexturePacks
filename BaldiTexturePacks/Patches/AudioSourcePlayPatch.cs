using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks.Patches
{

    [HarmonyPatch(typeof(AudioSource))]
    [HarmonyPatch("PlayHelper")]
    class AudioSourcePlayPatch
    {
        // so usually, i would undo this .clip change to avoid messing with things. However, changing the clip causes Play to reset.
        // so for now, I will leave this as is, and see hope it doesn't cause any problems
        static void Prefix(AudioSource source)
        {
            if (source.clip == null) return;
            //__state = source.clip;
            if (TexturePacksPlugin.currentClipReplacements.ContainsKey(source.clip))
            {
                source.clip = TexturePacksPlugin.currentClipReplacements[source.clip];
            }
        }
    }
    [HarmonyPatch(typeof(AudioSource))]
    [HarmonyPatch("PlayOneShot")]
    [HarmonyPatch(new Type[] { typeof(AudioClip), typeof(float)})]
    class AudioSourcePlayOneShotPatch
    {
        static void Prefix(ref AudioClip clip)
        {
            if (TexturePacksPlugin.currentClipReplacements.ContainsKey(clip))
            {
                clip = TexturePacksPlugin.currentClipReplacements[clip];
            }
        }
    }
}
