using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine.UI;

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

    [HarmonyPatch(typeof(FogEvent))]
    [HarmonyPatch("Begin")]
    class ReplaceFogColor
    {
        static void Prefix(FogEvent __instance, ref UnityEngine.Color ___fogColor)
        {
            ___fogColor = TPPlugin.Instance.generalOverrides.FogColor;
        }
    }

    [HarmonyPatch(typeof(FloodEvent))]
    [HarmonyPatch("Initialize")]
    class ReplaceUnderwaterColor
    {
        static void Postfix(FloodEvent __instance, Fog ___underwaterFog)
        {
            ___underwaterFog.color = TPPlugin.Instance.generalOverrides.UnderwaterColor;
        }
    }

    [HarmonyPatch(typeof(LookAtGuy))]
    [HarmonyPatch("Blind")]
    class LookAtGuyColor
    {
        static void Prefix(LookAtGuy __instance)
        {
            __instance.fog.color = TPPlugin.Instance.generalOverrides.TestFogColor;
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
                    ___itemBackgrounds[i].color = TPPlugin.Instance.generalOverrides.ItemBackgroundColor;
                }
                ___itemBackgrounds[___previousSelectedItem].color = TPPlugin.Instance.generalOverrides.ItemBackgroundColor;
                ___itemBackgrounds[value].color = TPPlugin.Instance.generalOverrides.ItemSelectColor;
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
                ___itemBackgrounds[i].color = TPPlugin.Instance.generalOverrides.ItemBackgroundColor;
            }
        }
    }

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

    [HarmonyPatch(typeof(ITM_BSODA))]
    [HarmonyPatch("Use")]
    class BSODARotationPatch
    {
        static void Postfix(Entity ___entity)
        {
            if (TPPlugin.Instance.generalOverrides.BSODAShouldRotate)
            {
                ___entity.SetBaseRotation(0f);
            }
        }
    }

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
