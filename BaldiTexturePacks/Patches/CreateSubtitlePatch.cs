using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaldiTexturePacks.Patches
{
    [HarmonyPatch(typeof(SubtitleManager))]
    [HarmonyPatch("CreateSub")]
    class CreateSubtitlePatch
    {
        static void Prefix(ref SoundObject file)
        {
            if (TexturePacksPlugin.createdSoundObjectDummies.ContainsKey(file))
            {
                file = TexturePacksPlugin.createdSoundObjectDummies[file];
            }
        }
    }
}