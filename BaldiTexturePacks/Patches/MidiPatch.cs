using HarmonyLib;

namespace BaldiTexturePacks.Patches
{
    [HarmonyPatch(typeof(MusicManager))]
    [HarmonyPatch("PlayMidi")]
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
