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
using BaldiTexturePacks.Components;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.SaveSystem;
using UnityEngine.UI;

namespace BaldiTexturePacks
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.baldiplus.texturepacks", "Texture Packs", "3.0.0.0")]
    public partial class TexturePacksPlugin : BaseUnityPlugin
    {
        public static List<(string, bool)> packOrder = new List<(string, bool)>();

        public static TexturePacksPlugin Instance;

        internal static ManualLogSource Log;

        public static ElevatorScreen baseElevatorScreen;

        public static List<TexturePack> packs = new List<TexturePack>();

        public static bool allPacksReady = false;

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
            "Font Texture",
            "BasicallyGames_Logo_Color_2019"
        };

        public static Dictionary<string, List<Component>> validManualReplacementTargets = new Dictionary<string, List<Component>>();

        public static T[] AddManualReplacementTargetsFromResources<T>() where T : UnityEngine.Component
        {
            T[] found = Resources.FindObjectsOfTypeAll<T>().Where(x => x.GetInstanceID() >= 0 && x.gameObject.scene.name == null).ToArray();
            if (found.Length < 1) return found;
            found.Do(x => x.MarkAsNeverUnload());
            if (!validManualReplacementTargets.ContainsKey(typeof(T).Name))
            {
                validManualReplacementTargets[typeof(T).Name] = new List<Component>();
            }
            validManualReplacementTargets[typeof(T).Name].AddRange(found);
            return found;
        }

        public static List<SpriteOverlay> spriteOverlays = new List<SpriteOverlay>();

        public static SpriteOverlay[] AddOverlaysToTransform(Transform t)
        {
            List<SpriteOverlay> overlays = new List<SpriteOverlay>();
            SpriteRenderer[] renderers = GetAllIncludingDisabled<SpriteRenderer>(t);
            renderers.Do(x =>
            {
                overlays.Add(x.gameObject.AddComponent<SpriteOverlay>());
            });
            spriteOverlays.AddRange(overlays);
            return overlays.ToArray();
        }

        public static void AddReplacementTarget(Component c)
        {
            if (!validManualReplacementTargets.ContainsKey(c.GetType().Name))
            {
                validManualReplacementTargets[c.GetType().Name] = new List<Component>();
            }
            validManualReplacementTargets[c.GetType().Name].Add(c);
        }

        static T[] GetAllIncludingDisabled<T>(Transform b) where T : UnityEngine.Component
        {
            List<T> t = new List<T>();
            for (int i = 0; i < b.childCount; i++)
            {
                t.AddRange(GetAllIncludingDisabled<T>(b.GetChild(i)));
            }
            T comp = b.GetComponent<T>();
            if (comp != null)
            {
                t.Add(comp);
            }
            return t.ToArray();
        }

        public static void AddAllChildrenToMovables(Transform t)
        {
            validMovableComponents.AddRange(GetAllIncludingDisabled<RectTransform>(t));
        }

        // a list of components (transform components) that are allowed to be moved
        public static List<Component> validMovableComponents = new List<Component>();

        // if a type is in this list it must be part of validMovableComponents to have its properties changed
        public static List<Type> typesThatMustBeValidMovables = new List<Type>()
        {
            typeof(RectTransform),
            typeof(Transform)
        };

        public static Dictionary<Type, string[]> validFieldChanges = new Dictionary<Type, string[]>()
        {
            {
                typeof(RectTransform),
                new string[]
                {
                    "anchoredPosition",
                    "anchoredPostion3D",
                    "sizeDelta",
                    "pivot",
                    "offsetMin",
                    "offsetMax",
                    "anchorMin",
                    "anchorMax",
                    "localPosition",
                    "position",
                    "localScale",
                    "rotation",
                    "eulerAngles",
                    "localEulerAngles",
                    "localRotation"
                }
            },
            {
                typeof(Transform),
                new string[]
                {
                    "localPosition",
                    "position",
                    "localScale",
                    "rotation",
                    "eulerAngles",
                    "localEulerAngles",
                    "localRotation"
                }
            },
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
                typeof(LineRenderer),
                new string[]
                {
                "widthMultiplier",
                "textureMode",
                }
            },
            {
                typeof(TMP_Text),
                new string[]
                {
                "color",
                "fontStyle",
                "text",
                "alignment"
                }
            },
            {
                typeof(TextLocalizer),
                new string[]
                {
                "key",
                }
            },
            {
                typeof(TextMeshPro),
                new string[]
                {
                "color",
                "fontStyle",
                "text",
                "alignment"
                }
            },
            {
                typeof(TextMeshProUGUI),
                new string[]
                {
                "color",
                "fontStyle",
                "text",
                "alignment"
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
                typeof(HardcodedTexturePackReplacements),
                new string[]
                {
                "BSODAShouldRotate",
                "ItemSlotBackgroundColor",
                "ItemSlotHighlightColor",
                }
            },
            {
                typeof(CursorInitiator),
                new string[]
                {
                "cursorColor"
                }
            },
            {
                typeof(SpriteRenderer),
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
                        result[itm.key] = itm.value;
                    }
                }
            }
            return result;
        }

        static string DisplayHierachy(Component cmp)
        {
            string result = cmp.name + " (" + cmp.GetType().Name + ")";
            Transform p = cmp.transform.parent;
            while (p != null)
            {
                result = p.name + "->" + result;
                p = p.transform.parent;
            }
            return result;
        }

        static string DisplayOverlayHierachy(SpriteOverlay cmp)
        {
            string result = cmp.name;
            Transform p = cmp.transform.parent;
            while (p != null)
            {
                result = p.name + "->" + result;
                p = p.transform.parent;
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
            AddReplacementTarget(gameObject.AddComponent<HardcodedTexturePackReplacements>());
            CustomOptionsCore.OnMenuInitialize += AddCategory;
            ModdedSaveSystem.AddSaveLoadAction(this, SaveHandler);
        }

        public void SaveHandler(bool isSave, string path)
        {
            string filePath = Path.Combine(path, "packs.txt");
            // save code
            if (isSave)
            {
                StringBuilder stb = new StringBuilder();
                if (isSave)
                {
                    for (int i = 0; i < packOrder.Count; i++)
                    {
                        stb.AppendLine(packOrder[i].Item1 + ":" + (packOrder[i].Item2 ? "enabled" : "disabled"));
                    }
                }
                File.WriteAllText(filePath, stb.ToString());
                return;
            }
            packOrder.Clear();
            if (!File.Exists(filePath)) return;
            // load code
            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] split = lines[i].Split(':');
                if (split.Length != 2) continue; //invalid, ignore.
                packOrder.Add((split[0], split[1] == "enabled"));
            }
            if (allPacksReady)
            {
                packOrder.RemoveAll(x => (packs.Where(z => z.internalId == x.Item1).Count() == 0));
            }
        }

        void AddCategory(OptionsMenu __instance, CustomOptionsHandler handler)
        {
            if (Singleton<CoreGameManager>.Instance != null) return;
            handler.AddCategory<PackManagerScreen>("Texture\nPacks");
        }

        IEnumerator OnLoad()
        {
            yield return 11;
            yield return "Getting base objects...";
            AddManualReplacementTargetsFromResources<MathMachine>().Do(x =>
            {
                validMovableComponents.AddRange(x.GetComponentsInChildren<TMP_Text>().Select(z => (Component)z.transform));
            });
            AddManualReplacementTargetsFromResources<FloodEvent>();
            AddManualReplacementTargetsFromResources<FogEvent>();
            AddManualReplacementTargetsFromResources<HudManager>().Do(x => AddAllChildrenToMovables(x.transform));
            AddManualReplacementTargetsFromResources<LookAtGuy>();
            AddManualReplacementTargetsFromResources<DigitalClock>().Do(x =>
            {
                validMovableComponents.AddRange(x.GetComponentsInChildren<SpriteRenderer>());
                validMovableComponents.AddRange(x.GetComponentsInChildren<Image>());
            });
            AddManualReplacementTargetsFromResources<ElevatorScreen>().Do(x => AddAllChildrenToMovables(x.transform));
            AddManualReplacementTargetsFromResources<Item>().Do(x =>
            {
                if (x.transform.Find("RendererBase"))
                {
                    validMovableComponents.Add(x.transform.Find("RendererBase").GetComponentInChildren<SpriteRenderer>());
                }
            });
            AddManualReplacementTargetsFromResources<BaldiBG>().Do(x =>
            {
                AddAllChildrenToMovables(x.transform);
                if (x.GetComponent<CursorInitiator>())
                {
                    AddReplacementTarget(x.GetComponent<CursorInitiator>());
                }
            });
            baseElevatorScreen = Resources.FindObjectsOfTypeAll<ElevatorScreen>().First(x => x.GetInstanceID() >= 0 && x.gameObject.scene.name == null);
            yield return "Setting up file structures...";
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
            if (!File.Exists(Path.Combine(corePackPath, "README.txt")))
            {
                File.WriteAllText(Path.Combine(corePackPath, "README.txt"), "Hello! You should not copy this folder to make your texture pack.\nThis folder does contain useful information and dumped textures(which you should only copy the ones you plan to modify), but is not a texture pack base.\nIf you are looking to make a Texture Pack, please look inside the install zip file, as there should be a \"TemplatePack\".\nFor more information, please go to: https://github.com/benjaminpants/BaldiTexturePacks/wiki");
            }
            yield return "Fetching Textures...";
            Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>()
                .Where(x => x.GetInstanceID() >= 0)
                .Where(x => (x.name != "") && (x.name != null))
                .Where(x => !manualExclusions.Contains(x.name))
                .Where(x => !x.name.StartsWith("LDR_LLL"))
                .ToArray();

            // create grapple hook dummy texture
            Texture2D grappleLine = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            grappleLine.filterMode = FilterMode.Point;
            grappleLine.name = "GrappleLine";
            Color[] colors = new Color[256 * 256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            grappleLine.SetPixels(0, 0, 256, 256, colors);
            grappleLine.Apply();
            validTexturesForReplacement.Add(grappleLine);
            validTexturesForReplacement.AddRange(allTextures);
            allTextures = allTextures.AddToArray(grappleLine);

            int coreTexturesHash = allTextures.Length;
            bool shouldRegenerateDump = true;
            string dumpCachePath = Path.Combine(corePackPath, "dumpCache.txt");
            if (File.Exists(dumpCachePath))
            {
                if (File.ReadAllText(dumpCachePath) == coreTexturesHash.ToString())
                {
                    shouldRegenerateDump = false;
                }
            }

            yield return (shouldRegenerateDump ? "Dumping textures..." : "Creating readable texture copies...");
            for (int i = 0; i < allTextures.Length; i++)
            {
                Texture2D readableCopy = allTextures[i].MakeReadableCopy(true);
                if (shouldRegenerateDump)
                {
                    File.WriteAllBytes(Path.Combine(texturesPath, allTextures[i].name + ".png"), readableCopy.EncodeToPNG());
                }
                originalTextures.Add(allTextures[i], readableCopy);
            }
            yield return "Dumping field changes...";
            if (shouldRegenerateDump)
            {
                File.WriteAllText(dumpCachePath, coreTexturesHash.ToString());
                
                // replacements 'dump'
                if (!Directory.Exists(Path.Combine(corePackPath, "Replacements")))
                {
                    Directory.CreateDirectory(Path.Combine(corePackPath, "Replacements"));
                }

                StringBuilder rStb = new StringBuilder();

                rStb.AppendLine("This README contains all of the valid replacements you could do!");

                rStb.AppendLine("Valid classes and fields:");
                foreach (KeyValuePair<Type, string[]> validField in validFieldChanges)
                {
                    rStb.AppendLine(validField.Key.Name + ":");
                    for (int i = 0; i < validField.Value.Length; i++)
                    {
                        rStb.AppendLine("\t" + validField.Value[i]);
                    }
                }
                rStb.AppendLine("Valid Root Objects (Objects at the top/root of the replacement file):");
                foreach (KeyValuePair<string, List<Component>> kvp in validManualReplacementTargets)
                {
                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        rStb.AppendLine("\t" + kvp.Key + ":" + kvp.Value[i].name);
                    }
                }

                rStb.AppendLine("Valid Transformable Objects (Objects that can have their transform positions modified):");
                foreach (Component comp in validMovableComponents)
                {
                    rStb.AppendLine("\t" + DisplayHierachy(comp));
                }

                File.WriteAllText(Path.Combine(corePackPath, "Replacements", "README.txt"), rStb.ToString());
            }
            yield return "Fetching Soundobjects and Audioclips...";

            validSoundObjectsForReplacement = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.GetInstanceID() >= 0 && x.name != "Silence").ToList();
            validClipsForReplacement = Resources.FindObjectsOfTypeAll<AudioSource>().Where(x => x.GetInstanceID() >= 0).Where(x => x.clip != null).Select(x => x.clip).Distinct().ToList();
            // handle annoying things like the outdoors ambience
            Resources.FindObjectsOfTypeAll<AudioSource>().Where(x => x.GetInstanceID() >= 0).Where(x => x.clip != null).Where(x => x.playOnAwake).Do(x =>
            {
                x.playOnAwake = false;
                x.gameObject.AddComponent<AudioPlayOnAwake>().source = x;
            });

            yield return "Editing grappling hook...";
            Material newMat = new Material(Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "White" && (x.GetInstanceID() >= 0)));
            newMat.name = "TexturePackGrappleMaterial";
            newMat.color = Color.white;
            newMat.SetMainTexture(grappleLine);
            ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).itemObjects.Do(x =>
            {
                LineRenderer renderer = x.item.GetComponentInChildren<LineRenderer>();
                if (renderer)
                {
                    renderer.material = newMat;
                }
            });

            yield return "Adding sprite overlays...";

            NPCMetaStorage.Instance.All().Where(x => x.info.Metadata.GUID == "mtm101.rulerp.bbplus.baldidevapi").Do(c =>
            {
                c.prefabs.Do(kvp => AddOverlaysToTransform(kvp.Value.transform));
            });

            Resources.FindObjectsOfTypeAll<Gum>().Where(x => x.GetInstanceID() >= 0).Do(x => AddOverlaysToTransform(x.transform));
            Resources.FindObjectsOfTypeAll<TapePlayer>().Where(x => x.GetInstanceID() >= 0).Do(x => AddOverlaysToTransform(x.transform));
            Resources.FindObjectsOfTypeAll<HappyBaldi>().Where(x => x.GetInstanceID() >= 0).Do(x => AddOverlaysToTransform(x.transform));

            yield return "Dumping all other data...";
            // handle all other dumps
            if (shouldRegenerateDump)
            {

                // soundObject 'dump'
                if (!Directory.Exists(Path.Combine(corePackPath, "SoundObjects")))
                {
                    Directory.CreateDirectory(Path.Combine(corePackPath, "SoundObjects"));
                }

                StringBuilder stb = new StringBuilder();

                stb.AppendLine("No audio dumping is currently available.");
                stb.AppendLine("Below is a list of valid SoundObjects that you can replace(on the left)");
                stb.AppendLine("And their associated subtitle and subtitle key.");

                foreach (SoundObject sObj in validSoundObjectsForReplacement)
                {
                    stb.AppendLine(sObj.name + "->" + Singleton<LocalizationManager>.Instance.GetLocalizedText(sObj.soundKey) + "(" + sObj.soundKey + ")");
                }

                File.WriteAllText(Path.Combine(corePackPath, "SoundObjects", "README.txt"), stb.ToString());

                // audioclip 'dump'
                if (!Directory.Exists(Path.Combine(corePackPath, "AudioClips")))
                {
                    Directory.CreateDirectory(Path.Combine(corePackPath, "AudioClips"));
                }

                stb = new StringBuilder();

                stb.AppendLine("No audio dumping is currently available.");
                stb.AppendLine("These sounds are not played using a SoundObject, meaning they have no subtitle.");
                stb.AppendLine("Below is a list of valid AudioClips and what they are attached to.");

                foreach (AudioClip clip in validClipsForReplacement)
                {
                    AudioSource[] sources = Resources.FindObjectsOfTypeAll<AudioSource>().Where(x => x.GetInstanceID() >= 0 && x.clip == clip).ToArray();
                    stb.Append(clip.name + " (");
                    for (int i = 0; i < sources.Length; i++)
                    {
                        stb.Append(sources[i].name + (i < (sources.Length - 1) ? "," : ""));
                    }
                    stb.AppendLine(")");
                }

                File.WriteAllText(Path.Combine(corePackPath, "AudioClips", "README.txt"), stb.ToString());

                // spriteswaps 'dump
                if (!Directory.Exists(Path.Combine(corePackPath, "SpriteSwaps")))
                {
                    Directory.CreateDirectory(Path.Combine(corePackPath, "SpriteSwaps"));
                }

                stb = new StringBuilder();

                stb.AppendLine("Below is a list of valid SpriteRenderers that sprite swaps will work on:");

                foreach (SpriteOverlay overlay in spriteOverlays)
                {
                    stb.AppendLine("\t" + DisplayOverlayHierachy(overlay));
                }

                Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.GetInstanceID() >= 0).ToArray();

                string[] texturesToSearchFor = new string[]
                {
                    "HammerCycle_0_Sheet",
                    "HammerCycle_1_Sheet",
                    "HammerCycle_2_Sheet",
                    "HammerCycle_3_Sheet",
                    "HammerCycle_4_Sheet",
                    "HammerCycle_5_Sheet",
                    "HammerCycle_6_Sheet",
                    "HammerCycle_7_Sheet",
                    "HammerSwing_Frame_0",
                    "HammerSwing_Frame_1",
                    "HammerSwing_Frame_2",
                    "HammerSwing_Frame_3",
                    "HammerSwing_Frame_4",
                    "HammerSwing_Frame_5",
                    "HammerSwing_Frame_6",
                    "HammerSwing_Frame_7",
                    "HammerSwing_Frame_8",
                    "WalkCycle_0_Sheet",
                    "WalkCycle_1_Sheet",
                    "WalkCycle_3_Sheet",
                    "WalkCycle_4_Sheet",
                    "WalkCycle_5_Sheet",
                    "WalkCycle_6_Sheet",
                    "WalkCycle_7_Sheet",
                    "ChaseCycle_0_Sheet",
                    "ChaseCycle_1_Sheet",
                    "ChaseCycle_2_Sheet",
                    "ChaseCycle_3_Sheet",
                    "ChaseCycle_4_Sheet",
                    "ChaseCycle_5_Sheet",
                    "ChaseCycle_6_Sheet",
                    "ChaseCycle_7_Sheet",
                    "OpenHands_Dark_Sheet",
                    "OpenHands_Light_Sheet",
                    "CraftersSprites",
                    "DanceSheet",
                    "CampingTalkSheet",
                    "FarmTalkSheet",
                    "CloudyCopter",
                    "bully_final",
                    "Banana_Entity",
                    "BAL_Countdown_Sheet",
                    "AlarmClock_Sheet",
                    "BaldiApple",
                    "BsodaSprite",
                    "Gotta Sweep Sprite",
                    "GrapplingHookSprite",
                    "Head",
                    "NoL_Sheet",
                    "Principal",
                    "Slap_Sheet",
                    "Slap_Sheet_Broken",
                    "ChalkFace",
                    "beans_gumwad",
                    "beans_enemywad",
                    "BAL_SmileIdle",
                    "1PrizeSprites",
                    "Beans_SpriteSheet",
                    "OhNoSheet",
                    "Playtime",
                    "TapePlayerClosed",
                    "TapePlayerOpen",
                    "Phoneog"
                };

                List<Sprite> foundSprites = new List<Sprite>();

                for (int i = 0; i < texturesToSearchFor.Length; i++)
                {
                    foundSprites.AddRange(allSprites.Where(x => x.texture.name == texturesToSearchFor[i]));
                }
                // im not adding all 100 baldi wave frames you cant make me
                foundSprites.AddRange(allSprites.Where(x => x.texture.name.StartsWith("Baldi_Wave")));

                foundSprites.Sort((a, b) => a.name.CompareTo(b.name));

                stb.AppendLine("Below is a list of sprites you can replace + some extra info about them\n(You aren't limited to replacing these! A replacement with a sprite not listed here will still work if it occurs in an object with sprite swaps enabled!):");

                for (int i = 0; i < foundSprites.Count; i++)
                {
                    stb.AppendLine("\t" + foundSprites[i].name + " | pixelsPerUnit: " + foundSprites[i].pixelsPerUnit + " | pivot: " + ("(" + foundSprites[i].pivot.x / foundSprites[i].rect.width + ", " + foundSprites[i].pivot.y / foundSprites[i].rect.height + ")") + " | textureRect: " + foundSprites[i].textureRect.ToString() + " | texture: " + foundSprites[i].texture.name);
                }

                File.WriteAllText(Path.Combine(corePackPath, "SpriteSwaps", "README.txt"), stb.ToString());
            }

            yield return "Adding packs...";
            bool packOrderChanged = false;
            // find all valid packs and add them to the list
            string[] pathDirectories = Directory.GetDirectories(packsPath);
            for (int i = 0; i < pathDirectories.Length; i++)
            {
                if (Path.GetFileNameWithoutExtension(pathDirectories[i]) == "core") continue; // core is not an actual texture pack
                packs.Add(new TexturePack(pathDirectories[i]));
                // add pack to order list if it doesn't exist
                int foundIndex = packOrder.FindIndex(x => x.Item1 == packs[packs.Count - 1].internalId);
                if (foundIndex == -1)
                {
                    packOrderChanged = true;
                    packOrder.Add((packs[packs.Count - 1].internalId, false));
                }
            }
            yield return "Loading packs...";
            for (int i = 0; i < packOrder.Count; i++)
            {
                TexturePack foundPack = packs.Find(x => x.internalId == packOrder[i].Item1);
                if (foundPack == null) continue;
                if (packOrder[i].Item2)
                {
                    foundPack.LoadInstantly();
                }
            }
            allPacksReady = true;
            if (packOrderChanged)
            {
                SaveHandler(true, ModdedSaveSystem.GetCurrentSaveFolder(this));
            }
            //packOrder.RemoveAll(x => (packs.Where(z => z.internalId == x.Item1).Count() == 0));
            yield break;
        }
    }
}
