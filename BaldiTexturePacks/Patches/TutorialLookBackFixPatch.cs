using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;

namespace BaldiTexturePacks.Patches
{
    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Sprite Swaps")]
    [HarmonyPatch(typeof(TutorialGameManager), "BeginPlay")]
    class LookBackFix
    {
        static void Postfix(AudioManager[] ___lookBackBaldi, ref SpriteRenderer[] ___lookBackRenderer)
        {
            for (int i = 0; i < ___lookBackRenderer.Length; i++)
            {
                ___lookBackBaldi[i].gameObject.SetActive(true);
                ___lookBackRenderer[i] = ___lookBackRenderer[i].transform.Find("FakeRenderer").GetComponent<SpriteRenderer>();
                ___lookBackBaldi[i].gameObject.SetActive(false);
                ___lookBackRenderer[i].gameObject.SetActive(false);
            }
        }
    }
}
