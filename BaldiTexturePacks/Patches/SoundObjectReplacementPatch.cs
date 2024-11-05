using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks.Patches
{

    [HarmonyPatch(typeof(AudioManager))]
    [HarmonyPatch("PlaySingle")]
    class SoundReplacementPlaySinglePatch
    {
        static void Prefix(SoundObject file, out AudioClip __state)
        {
            __state = file.soundClip;
            if (TexturePacksPlugin.currentSoundReplacements.ContainsKey(file))
            {
                file.soundClip = TexturePacksPlugin.currentSoundReplacements[file].GetClip();
            }
        }

        static void Postfix(SoundObject file, AudioClip __state)
        {
            file.soundClip = __state;
        }
    }

    [HarmonyPatch(typeof(AudioManager))]
    [HarmonyPatch("PlayQueue")]
    class SoundReplacementQueuePatch
    {
        static void Prefix(AudioManager __instance, out (SoundObject, AudioClip) __state)
        {
            if (__instance.soundQueue.Length != 0 && __instance.soundQueue[0] != null)
            {
                if (TexturePacksPlugin.currentSoundReplacements.ContainsKey(__instance.soundQueue[0]))
                {
                    __state = (__instance.soundQueue[0], __instance.soundQueue[0].soundClip);
                    __instance.soundQueue[0].soundClip = TexturePacksPlugin.currentSoundReplacements[__instance.soundQueue[0]].GetClip();
                    return;
                }
            }
            __state = (null, null);
        }

        static void Postfix((SoundObject, AudioClip) __state)
        {
            if (__state.Item1 == null) return;
            __state.Item1.soundClip = __state.Item2;
        }
    }
}
