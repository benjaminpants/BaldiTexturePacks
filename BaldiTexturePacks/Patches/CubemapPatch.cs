using HarmonyLib;
using UnityEngine;
namespace BaldiTexturePacks.Patches
{
    [HarmonyPatch(typeof(Shader))]
    [HarmonyPatch("SetGlobalTexture", typeof(string), typeof(Texture))]
    class ReplaceCubemap
    {
        static void Prefix(string name, ref Texture value)
        {
            if (name == "_Skybox" && value is Cubemap cubemap &&
                TexturePacksPlugin.currentCubemapReplacements.ContainsKey(cubemap))
            {
                value = TexturePacksPlugin.currentCubemapReplacements[cubemap];
            }
        }
    }
}