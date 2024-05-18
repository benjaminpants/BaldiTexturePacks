using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiTexturePacks
{
    [HarmonyPatch(typeof(MusicManager))]
    [HarmonyPatch("PlayMidi")]
    class ReplaceMidi
    {
        static void Prefix(ref string song)
        {
            if (TPPlugin.Instance.midiOverrides.ContainsKey(song))
            {
                song = TPPlugin.Instance.midiOverrides[song];
            }
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(FogEvent))]
    [HarmonyPatch("Begin")]
    class ReplaceFogColor
    {
        static void Prefix(FogEvent __instance, ref UnityEngine.Color ___fogColor)
        {
            ___fogColor = TPPlugin.Instance.generalOverrides.FogColor;
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(FloodEvent))]
    [HarmonyPatch("Initialize")]
    class ReplaceUnderwaterColor
    {
        static void Postfix(FloodEvent __instance, Fog ___underwaterFog)
        {
            ___underwaterFog.color = TPPlugin.Instance.generalOverrides.UnderwaterColor;
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(LookAtGuy))]
    [HarmonyPatch("Blind")]
    class LookAtGuyColor
    {
        static void Prefix(LookAtGuy __instance)
        {
            __instance.fog.color = TPPlugin.Instance.generalOverrides.TestFogColor;
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
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
                    ___itemBackgrounds[i].color = TPPlugin.Instance.generalOverrides.ItemBackgroundColor;
                }
                ___itemBackgrounds[___previousSelectedItem].color = TPPlugin.Instance.generalOverrides.ItemBackgroundColor;
                ___itemBackgrounds[value].color = TPPlugin.Instance.generalOverrides.ItemSelectColor;
            }
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(HudManager))]
    [HarmonyPatch("Awake")]
    class SetItemBackgrounds
    {
        static void Postfix(HudManager __instance, ref RawImage[] ___itemBackgrounds)
        {
            for (int i = 0; i < ___itemBackgrounds.Length; i++)
            {
                ___itemBackgrounds[i].color = TPPlugin.Instance.generalOverrides.ItemBackgroundColor;
            }
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(DetentionUi))]
    [HarmonyPatch("Initialize")]
    class DetentionUIPatch
    {
        static void Postfix(DetentionUi __instance, ref TMP_Text ___timer)
        {
            TMP_Text mainText = __instance.transform.Find("MainText").GetComponent<TMP_Text>();
            if (mainText == null)
            {
                TPPlugin.Log.LogWarning("Unable To Find Maintext!");
                return;
            }
            if (!mainText.text.StartsWith("You get detention!")) return; //we reached a modded DetentionUI component, best not to change anything for mod compat
            mainText.text = String.Format(TPPlugin.Instance.generalOverrides.DetentionText, " ");
            mainText.color = TPPlugin.Instance.generalOverrides.DetentionTextColor;
            ___timer.color = TPPlugin.Instance.generalOverrides.DetentionTextColor;
            if (TPPlugin.Instance.generalOverrides.UseClassicDetentionText)
            {
                ___timer.color = UnityEngine.Color.clear; //hide the timer
                ___timer = mainText;
            }
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(DetentionUi))]
    [HarmonyPatch("Update")]
    class DetentionUIUpdatePatch
    {
        static void Postfix(TMP_Text ___timer, int ___roundedTime)
        {
            if (TPPlugin.Instance.generalOverrides.UseClassicDetentionText)
            {
                ___timer.text = String.Format(TPPlugin.Instance.generalOverrides.DetentionText, ___roundedTime.ToString());
            }
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(ElevatorScreen))]
    [HarmonyPatch("UpdateFloorDisplay")]
    class UpdateFloorDisplayPatch
    {
        static void Postfix(ElevatorScreen __instance, TMP_Text ___floorText, TMP_Text ___seedText)
        {
            ___floorText.color = TPPlugin.Instance.generalOverrides.ElevatorFloorColor;
            ___seedText.color = TPPlugin.Instance.generalOverrides.ElevatorSeedColor;
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(ITM_BSODA))]
    [HarmonyPatch("Use")]
    class BSODARotationPatch
    {
        static void Postfix(SpriteRenderer ___spriteRenderer)
        {
            if (TPPlugin.Instance.generalOverrides.BSODAShouldRotate)
            {
                ___spriteRenderer.SetSpriteRotation(0f);
            }
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.texturepacks", "General", "Use Overrides")]
    [HarmonyPatch(typeof(StoreScreen))]
    [HarmonyPatch("Start")]
    class StoreScreenFixSlotColors
    {
        static void Postfix(StoreScreen __instance, Image[] ___inventoryImage)
        {
            for (int i = 0; i < 6; i++)
            {
                ___inventoryImage[i].transform.parent.GetComponent<RawImage>().color = TPPlugin.Instance.generalOverrides.ItemBackgroundColor;
            }
        }
    }
}
