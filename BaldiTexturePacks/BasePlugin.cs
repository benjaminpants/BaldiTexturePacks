using BepInEx.Logging;
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
using TMPro;

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
            "Font Texture"
        };

        public static Dictionary<string, List<Component>> validManualReplacementTargets = new Dictionary<string, List<Component>>();

        public static void AddManualReplacementTargetsFromResources<T>() where T : UnityEngine.Component
        {
            T[] found = Resources.FindObjectsOfTypeAll<T>().Where(x => x.GetInstanceID() >= 0).ToArray();
            if (found.Length < 1) return;
            found.Do(x => x.MarkAsNeverUnload());
            if (!validManualReplacementTargets.ContainsKey(typeof(T).Name))
            {
                validManualReplacementTargets[typeof(T).Name] = new List<Component>();
            }
            validManualReplacementTargets[typeof(T).Name].AddRange(found);
        }

        public static Dictionary<Type, string[]> validFieldChanges = new Dictionary<Type, string[]>()
        {
            { 
                typeof(Fog), 
                new string[]
                {
                "color",
                "maxDist",
                "startDist",
                "strength"
                }
            },
            {
                typeof(TMP_Text),
                new string[]
                {
                "color",
                "fontStyle"
                }
            },
            {
                typeof(TextMeshPro),
                new string[]
                {
                "color",
                "fontStyle"
                }
            },
            {
                typeof(TextMeshProUGUI),
                new string[]
                {
                "color",
                "fontStyle"
                }
            },
            {
                typeof(UnityEngine.UI.Image),
                new string[]
                {
                "color"
                }
            },
            {
                typeof(UnityEngine.UI.RawImage),
                new string[]
                {
                "color"
                }
            },
            {
                typeof(FloodEvent),
                new string[]
                {
                "underwaterFog",
                }
            },
            {
                typeof(FogEvent),
                new string[]
                {
                "fogColor",
                }
            },
            {
                typeof(LookAtGuy),
                new string[]
                {
                "fog",
                }
            }
        };


        public static List<Texture2D> validTexturesForReplacement = new List<Texture2D>();

        public static List<SoundObject> validSoundObjectsForReplacement = new List<SoundObject>();

        public static List<AudioClip> validClipsForReplacement = new List<AudioClip>();

        string packsPath => Path.Combine(Application.streamingAssetsPath, "Texture Packs");

        string corePackPath => Path.Combine(packsPath, "core");

        Dictionary<string, string> LoadAllPackLocalization(Language lang)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (TexturePack pack in packs)
            {
                if (pack.localizationData != null)
                {
                    foreach (LocalizationItem itm in pack.localizationData.items)
                    {
                        result.Add(itm.key, itm.value);
                    }
                }
            }
            return result;
        }


        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.texturepacks");
            LoadingEvents.RegisterOnAssetsLoaded(this.Info, OnLoad(), true);
            harmony.PatchAllConditionals();
            Log = this.Logger;
            Instance = this;
            AssetLoader.LocalizationFromFunction(LoadAllPackLocalization);
        }

        IEnumerator OnLoad()
        {
            yield return 5;
            yield return "Getting base objects...";
            AddManualReplacementTargetsFromResources<MathMachine>();
            AddManualReplacementTargetsFromResources<FloodEvent>();
            AddManualReplacementTargetsFromResources<FogEvent>();
            AddManualReplacementTargetsFromResources<HudManager>();
            AddManualReplacementTargetsFromResources<LookAtGuy>();
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
                Texture2D readableCopy = allTextures[i].MakeReadableCopy(true);
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
            validClipsForReplacement = Resources.FindObjectsOfTypeAll<AudioSource>().Where(x => x.GetInstanceID() >= 0).Where(x => x.clip != null).Select(x => x.clip).Distinct().ToList();
            // handle annoying things like the outdoors ambience
            Resources.FindObjectsOfTypeAll<AudioSource>().Where(x => x.GetInstanceID() >= 0).Where(x => x.clip != null).Where(x => x.playOnAwake).Do(x =>
            {
                x.playOnAwake = false;
                x.gameObject.AddComponent<AudioPlayOnAwake>().source = x;
            });
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
