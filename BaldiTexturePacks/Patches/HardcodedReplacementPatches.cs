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
    [HarmonyPatch(typeof(DetentionUi))]
    [HarmonyPatch("Initialize")]
    class DetentionUIPatch
    {
        static void Postfix(DetentionUi __instance, ref TMP_Text ___timer)
        {
            TMP_Text mainText = __instance.transform.Find("MainText").GetComponent<TMP_Text>();
            if (mainText == null)
            {
                return;
            }
            mainText.text = String.Format(HardcodedTexturePackReplacements.Instance.DetentionText, " ");
            if (HardcodedTexturePackReplacements.Instance.UseClassicDetentionText)
            {
                ___timer.color = Color.clear; //hide the timer
                ___timer = mainText;
            }
        }
    }

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

    [HarmonyPatch(typeof(DetentionUi))]
    [HarmonyPatch("Update")]
    class DetentionUIUpdatePatch
    {
        static void Postfix(TMP_Text ___timer, int ___roundedTime)
        {
            if (HardcodedTexturePackReplacements.Instance.UseClassicDetentionText)
            {
                ___timer.text = String.Format(HardcodedTexturePackReplacements.Instance.DetentionText, ___roundedTime.ToString());
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
