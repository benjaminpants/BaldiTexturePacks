using BaldiTexturePacks.Components;
using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiTexturePacks.Patches
{
    [HarmonyPatch(typeof(HudManager))]
    [HarmonyPatch("SetItemSelect")]
    class SetItemSelectPatch
    {
        static void Postfix(HudManager __instance, int value, string key, ref RawImage[] ___itemBackgrounds, int ___previousSelectedItem)
        {
            if (___itemBackgrounds[value] != null)
            {
                for (int i = 0; i < ___itemBackgrounds.Length; i++)
                {
                    ___itemBackgrounds[i].color = HardcodedTexturePackReplacements.Instance.ItemSlotBackgroundColor;
                }
                ___itemBackgrounds[___previousSelectedItem].color = HardcodedTexturePackReplacements.Instance.ItemSlotBackgroundColor;
                ___itemBackgrounds[value].color = HardcodedTexturePackReplacements.Instance.ItemSlotHighlightColor;
            }
        }
    }

    [HarmonyPatch(typeof(HudManager))]
    [HarmonyPatch("Awake")]
    class SetItemBackgrounds
    {
        static void Postfix(HudManager __instance, ref RawImage[] ___itemBackgrounds)
        {
            for (int i = 0; i < ___itemBackgrounds.Length; i++)
            {
                ___itemBackgrounds[i].color = HardcodedTexturePackReplacements.Instance.ItemSlotBackgroundColor;
            }
        }
    }

    [HarmonyPatch(typeof(ITM_BSODA))]
    [HarmonyPatch("Use")]
    class BSODARotationPatch
    {
        static void Postfix(SpriteRenderer ___spriteRenderer)
        {
            if (!HardcodedTexturePackReplacements.Instance.BSODAShouldRotate)
            {
                ___spriteRenderer.SetSpriteRotation(0f);
            }
        }
    }
}
