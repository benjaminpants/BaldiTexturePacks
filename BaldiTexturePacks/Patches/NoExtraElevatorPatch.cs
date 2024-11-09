using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks.Patches
{
    [HarmonyPatch(typeof(GameLoader))]
    [HarmonyPatch("AssignElevatorScreen")]
    class NoExtraElevatorPatch
    {
        static void Prefix(ref ElevatorScreen screen)
        {
            // this will only trigger if its the standard elevator screen from the main menu
            if (screen.name == "ElevatorScreen" && screen.gameObject.scene.name == "MainMenu")
            {
                screen = GameObject.Instantiate<ElevatorScreen>(TexturePacksPlugin.baseElevatorScreen);
            }
        }
    }
}
