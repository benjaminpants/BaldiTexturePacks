﻿using BepInEx.Logging;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;
using System.IO;
using System.Collections;
using MTM101BaldAPI.Registers;
using System.Linq;
using MTM101BaldAPI.AssetTools;

namespace BaldiTexturePacks
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.baldiplus.texturepacks", "Texture Packs", "3.0.0.0")]
    public partial class TexturePacksPlugin : BaseUnityPlugin
    {
        public static TexturePacksPlugin Instance;

        internal static ManualLogSource Log;

        public static List<TexturePack> packs = new List<TexturePack>();

        public static string[] manualExclusions => new string[]
        {
            "LightMap",
            "Large01",
            "Large02",
            "Medium01",
            "Medium02",
            "Medium03",
            "Medium04",
            "Medium05",
            "Medium06",
            "Thin01",
            "Thin02",
        };

        public static List<Texture2D> validTexturesForReplacement = new List<Texture2D>();

        public static List<SoundObject> validSoundObjectsForReplacement = new List<SoundObject>();

        string packsPath => Path.Combine(Application.streamingAssetsPath, "Texture Packs");

        string corePackPath => Path.Combine(packsPath, "core");

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.texturepacks");
            LoadingEvents.RegisterOnAssetsLoaded(this.Info, OnLoad(), true);
            harmony.PatchAllConditionals();
            Log = this.Logger;
            Instance = this;
        }

        // thanks to 
        int GetStableHashCode(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        IEnumerator OnLoad()
        {
            yield return 4;
            yield return "Dumping Resources...";
            if (!Directory.Exists(packsPath))
            {
                Directory.CreateDirectory(packsPath);
            }
            if (!Directory.Exists(corePackPath))
            {
                Directory.CreateDirectory(corePackPath);
            }
            string texturesPath = Path.Combine(corePackPath, "Textures");
            if (!Directory.Exists(texturesPath))
            {
                Directory.CreateDirectory(texturesPath);
            }
            Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>()
                .Where(x => x.GetInstanceID() >= 0)
                .Where(x => (x.name != "") && (x.name != null))
                .Where(x => !manualExclusions.Contains(x.name))
                .Where(x => !x.name.StartsWith("LDR_LLL"))
                .ToArray();

            int coreTexturesHash = allTextures.Length;
            bool shouldRegenerateDump = true;
            string dumpCachePath = Path.Combine(corePackPath, "dumpCache.txt");
            Log.LogInfo(coreTexturesHash);
            if (File.Exists(dumpCachePath))
            {
                if (File.ReadAllText(dumpCachePath) == coreTexturesHash.ToString())
                {
                    shouldRegenerateDump = false;
                }
            }
            for (int i = 0; i < allTextures.Length; i++)
            {
                Texture2D readableCopy = allTextures[i].MakeReadableCopy(false);
                if (shouldRegenerateDump)
                {
                    File.WriteAllBytes(Path.Combine(texturesPath, allTextures[i].name + ".png"), readableCopy.EncodeToPNG());
                }
                originalTextures.Add(allTextures[i], readableCopy);
            }
            if (shouldRegenerateDump)
            {
                File.WriteAllText(dumpCachePath, coreTexturesHash.ToString());
            }
            validTexturesForReplacement.AddRange(allTextures);
            yield return "Getting all valid replaceables...";
            validSoundObjectsForReplacement = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.GetInstanceID() >= 0).ToList();
            yield return "Adding packs...";
            // find all valid packs and add them to the list
            string[] pathDirectories = Directory.GetDirectories(packsPath);
            for (int i = 0; i < pathDirectories.Length; i++)
            {
                if (Path.GetFileNameWithoutExtension(pathDirectories[i]) == "core") continue; // core is not an actual texture pack
                packs.Add(new TexturePack(pathDirectories[i]));
            }
            yield return "Loading packs...";
            for (int i = 0; i < packs.Count; i++)
            {
                packs[i].LoadInstantly();
            }
            yield break;
        }
    }
}
