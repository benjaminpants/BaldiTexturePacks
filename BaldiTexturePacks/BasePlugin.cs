using System;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using MTM101BaldAPI;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.AssetTools;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Unity.Collections;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using BepInEx.Logging;
using System.Collections;
using BepInEx.Configuration;

namespace BaldiTexturePacks
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.baldiplus.texturepacks", "Texture Packs", "1.2.0.0")]
    public class TPPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public readonly string[] toIgnore = {
            "UnityBlack",
            "UnityGrey",
            "UnityNormalMap",
            "UnityRandomRotation",
            "UnityLinearGrey",
            "LightMap",
            "Large01",
            "Large02"
        };
        public static TPPlugin Instance;
        public string packRootFolder => AssetLoader.GetModPath(this);
        public string packFolder => Path.Combine(packRootFolder, "Texture Packs");
        public string basePackPath => Path.Combine(packFolder, "core");
        public Dictionary<string, TexturePack> packs = new Dictionary<string, TexturePack>();
        public List<string> packOrder = new List<string>();
        public Texture2D[] allTextures;
        bool packsLoaded = false;
        public static Sprite[] menuArrows = new Sprite[2];
        public static SoundObject UpdatePackSound;
        public Dictionary<SoundObject, AudioClip> originalSoundClips = new Dictionary<SoundObject, AudioClip>();
        public Dictionary<AudioSource, AudioClip> originalSourceClips = new Dictionary<AudioSource, AudioClip>();
        public Dictionary<string, string> midiOverrides = new Dictionary<string, string>();
        internal ConfigEntry<bool> configTextureOnly;
        internal ConfigEntry<bool> configAutoDump;

        void AddPack(TexturePack pack, bool autoApply)
        {
            packs.Add(pack.internalName, pack);
            packOrder.Add(pack.internalName);
            if (autoApply)
            {
                ApplyPacks(false);
            }
        }

        void OnMen(OptionsMenu __instance)
        {
            GameObject ob = CustomOptionsCore.CreateNewCategory(__instance, "Texture Packs");
            TexturePacksMenu tpm = ob.AddComponent<TexturePacksMenu>();
            tpm.opMen = __instance;
            StandardMenuButton b = CustomOptionsCore.CreateApplyButton(__instance, "Applies the texture packs", () =>
            {
                tpm.UpdatePacks();
                ApplyPacks(true);
                __instance.GetComponent<AudioManager>().PlaySingle(UpdatePackSound);
            });
            tpm.pageBar = CustomOptionsCore.CreateAdjustmentBar(__instance, new Vector2(-80f,-140f), "PackPage", Mathf.CeilToInt((float)packOrder.Count / 4f) - 1, "The current page.", 0, tpm.BarUpdate);
            tpm.pageBar.transform.SetParent(ob.transform, false);
            b.transform.SetParent(ob.transform, false);
        }
        void ApplyPacks(bool applyCore)
        {
            if (!configTextureOnly.Value)
            {
                ReloadSourceClips();
                midiOverrides.Clear();
                // tell the game to reload all localization
                Singleton<LocalizationManager>.Instance.LoadLocalizedText("Subtitles_En.json", Language.English); //placeholder
            }
            // tell all packs to apply in the order they were mounted in
            for (int i = 0; i < packOrder.Count; i++)
            {
                if ((!applyCore) && (packs[packOrder[i]].internalName == "core"))
                {
                    continue;
                }
                try
                {
                    packs[packOrder[i]].Apply();
                }
                catch (Exception E)
                {
                    MTM101BaldiDevAPI.CauseCrash(TPPlugin.Instance.Info, E);
                }
            }
            if (!configTextureOnly.Value)
            {
                // tell all text localizers to update
                GameObject.FindObjectsOfType<TextLocalizer>(true).Do(x =>
                {
                    x.Invoke("Awake", 0f);
                    x.Invoke("Start", 0f);
                });
            }
        }

        void DumpAllTextures(string dumpTo, bool exportDummy, out Dictionary<int, Texture2D> dummyTextures)
        {
            Dictionary<int, Texture2D> dumTextures = new Dictionary<int, Texture2D>();
            allTextures.Do(x =>
            {
                int texCount = allTextures.Where(z => z.name == x.name).Count();
                if (texCount > 1)
                {
                    Logger.LogWarning(x.name + " has " + texCount + " textures with the same name! Ignoring...");
                    return;
                }
                if (x.mipmapCount > 1)
                {
                    Logger.LogWarning(x.name + " has " + x.mipmapCount + " mipmaps! Ignoring...");
                    return;
                }
                RenderTexture dummyTex = RenderTexture.GetTemporary(x.width, x.height, 24);
                Texture2D toDump = x;

                if ((x.name == null) || (x.name == ""))
                {
                    RenderTexture.ReleaseTemporary(dummyTex);
                    Logger.LogDebug("Found unnamed texture, ignoring.");
                    return;
                }
                string pathToUse = Path.Combine(dumpTo, x.name + ".png");
                toDump = new Texture2D(toDump.width, toDump.height, toDump.format, toDump.mipmapCount > 1);
                toDump.name = x.name;
                Graphics.Blit(x, dummyTex);
                toDump.ReadPixels(new Rect(0, 0, dummyTex.width, dummyTex.height), 0, 0);
                File.WriteAllBytes(pathToUse, toDump.EncodeToPNG());
                if (exportDummy)
                {
                    toDump.Apply();
                    dumTextures.Add(x.GetHashCode(), toDump);
                }
                else
                {
                    UnityEngine.Object.Destroy(toDump); //we don't need you anymore...
                }
                RenderTexture.ReleaseTemporary(dummyTex);
            });
            dummyTextures = dumTextures;
        }

        void LoadPacks()
        {
            string[] paths = Directory.GetDirectories(packFolder);
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                string dirName = Path.GetFileName(path);
                if (packs.ContainsKey(dirName))
                {
                    continue;
                }
                Logger.LogDebug(dirName);
                TexturePack pack = JsonConvert.DeserializeObject<TexturePack>(File.ReadAllText(Path.Combine(paths[i],"pack.json")));
                pack.filePath = path;
                AddPack(pack, false);
                pack.LoadAllNeeded();
            }
        }

        public void ReloadSourceClips()
        {
            Resources.FindObjectsOfTypeAll<AudioSource>().Do(src =>
            {
                originalSourceClips.Clear();
                if (src.clip == null) return;
                originalSourceClips.Add(src, src.clip);
            });
        }

        void OnResourcesLoad()
        {
            if (MTM101BaldiDevAPI.Instance.Info.Metadata.Version < new Version("3.2.1.0"))
            {
                MTM101BaldiDevAPI.CauseCrash(this.Info, new Exception("Texturepacks mod requires a version of the BB+ API at or above 3.2.1.0!\nIf you have updated and are still receiving this error, go to BepInEx/cache and delete every file in there!"));
                return;
            }
            allTextures = Resources.FindObjectsOfTypeAll<Texture2D>().Where(x => !(toIgnore.Contains(x.name)))
                .Where(x => !x.name.StartsWith("LDR_LLL"))
                .ToArray(); //time to load all textures and keep them in memory foreverr
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            menuArrows[0] = allSprites.Where(x => x.name == "MenuArrowSheet_2").First();
            menuArrows[1] = allSprites.Where(x => x.name == "MenuArrowSheet_0").First();
            SoundObject[] allSoundObjects = Resources.FindObjectsOfTypeAll<SoundObject>();
            UpdatePackSound = allSoundObjects.Where(x => x.name == "Xylophone").First();
            allSoundObjects.Do(snd =>
            {
                originalSoundClips.Add(snd,snd.soundClip);
            });
            ReloadSourceClips();
            // initialize key directories
            if (!Directory.Exists(packRootFolder)) Directory.CreateDirectory(packRootFolder);
            if (!Directory.Exists(packFolder)) Directory.CreateDirectory(packFolder);
            int texCount = 0;
            int currentTexCount = allTextures.Where(x => x.name != null).Count();
            string ctPath = Path.Combine(packRootFolder, "coreTextureCache.txt");
            if (File.Exists(ctPath))
            {
                if (!configAutoDump.Value)
                {
                    texCount = currentTexCount;
                }
                else
                {
                    texCount = int.Parse(File.ReadAllText(ctPath));
                }
            }
            if (texCount != currentTexCount)
            {
                Logger.LogDebug(String.Format("Core texture size mismatch! Redumping textures! ({0}, {1})", texCount, currentTexCount));
                TexturePack baseTexturePack = new TexturePack("Core", "mystman12", -1, basePackPath);
                baseTexturePack.Description = "The core textures for Baldi's Basics Plus.";
                if (Directory.Exists(basePackPath))
                {
                    new DirectoryInfo(Path.Combine(basePackPath, "Textures")).GetFiles().Do(x => x.Delete());
                    new DirectoryInfo(Path.Combine(basePackPath, "Audio")).GetFiles().Do(x => x.Delete());
                    new DirectoryInfo(Path.Combine(basePackPath, "Midi")).GetFiles().Do(x => x.Delete());
                }
                Directory.CreateDirectory(Path.Combine(basePackPath, "Textures"));
                DumpAllTextures(Path.Combine(basePackPath, "Textures"), true, out Dictionary<int, Texture2D> oldTex);
                // convert tex array to kvp
                oldTex.Do(x =>
                {
                    baseTexturePack.textures = oldTex;
                    x.Value.name = "core_" + x.Value.name;
                });
                File.WriteAllText(ctPath, currentTexCount.ToString());
                AddPack(baseTexturePack, false);
                baseTexturePack.UpdateFile();

                // Dump All Audio
                Directory.CreateDirectory(Path.Combine(basePackPath, "Audio"));
                AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
                File.WriteAllText(Path.Combine(basePackPath, "Audio", "README.txt"), "Sorry, no audio dumping (yet)! You'll need a tool such as AssetStudio for that!\nThis is still a useful resource however!\nOpen up a dummy file to see its corresponding subtitle(if it has one)");
                clips.Do(x =>
                {
                    SoundObject obj = allSoundObjects.Where(z => z.soundClip == x).FirstOrDefault();
                    string contents = obj ? (obj.soundKey + "\n" + Singleton<LocalizationManager>.Instance.GetLocalizedText(obj.soundKey)) : "No soundobject found.";
                    File.WriteAllText(Path.Combine(basePackPath, "Audio", x.name + ".dummy"), contents);
                });

                baseTexturePack.LoadAudios();

                // dump the midis
                Directory.CreateDirectory(Path.Combine(basePackPath, "Midi"));
                File.WriteAllText(Path.Combine(basePackPath, "Midi", "README.txt"),
@"Sorry! No midi dumping (yet!)
However, here is a list of midi's included in the game by default that you might find helpful!(Note these are case sensitive!)
DanceV0_5
Elevator
school
titleFixed
");

            }

            LoadPacks();

            packsLoaded = true;

            Singleton<PlayerFileManager>.Instance.Load(); //reload data so we load the pack order as we skipped it last time

            ApplyPacks(false);
        }

        void SaveLoad(bool isSave, string myPath)
        {
            if (!packsLoaded) return;
            packs["core"].enabled = true;
            if (isSave)
            {
                File.WriteAllText(Path.Combine(myPath, "packorder.json"), JsonConvert.SerializeObject(packOrder));
                Dictionary<string, bool> enabledPacks = new Dictionary<string, bool>();
                for (int i = 0; i < packOrder.Count; i++)
                {
                    enabledPacks.Add(packOrder[i], packs[packOrder[i]].enabled);
                }
                File.WriteAllText(Path.Combine(myPath, "enabledpacks.json"), JsonConvert.SerializeObject(enabledPacks));
            }
            else
            {
                string[] oldOrder = packOrder.ToArray();
                if (!File.Exists(Path.Combine(myPath, "packorder.json")))
                {
                    SaveLoad(true, myPath);
                }
                packOrder = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(myPath, "packorder.json")));
                // add back any packs that might've been obliterated by the old order
                for (int i = 0; i < oldOrder.Length; i++)
                {
                    if (packOrder.Find(x => x == oldOrder[i]) == null)
                    {
                        packOrder.Add(oldOrder[i]);
                    }
                }
                // remove any packs that no longer exist
                for (int i = (packOrder.Count - 1); i >= 0; i--)
                {
                    if (!packs.ContainsKey(packOrder[i]))
                    {
                        packOrder.RemoveAt(i);
                    }
                }
                Dictionary<string, bool> enabledPacks = JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(Path.Combine(myPath, "enabledpacks.json")));
                foreach (KeyValuePair<string, bool> kvp in enabledPacks)
                {
                    if (packs.ContainsKey(kvp.Key))
                    {
                        packs[kvp.Key].enabled = kvp.Value;
                    }
                }
                ApplyPacks(true);
            }
        }

        void Awake()
        {
            Instance = this;
            MTM101BaldAPI.SaveSystem.ModdedSaveSystem.AddSaveLoadAction(this, SaveLoad);
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.texturepacks");

            //load texture packs here
            LoadingEvents.RegisterOnAssetsLoaded(OnResourcesLoad,true);

            Log = base.Logger;

            CustomOptionsCore.OnMenuInitialize += OnMen;

            harmony.PatchAll();

            configTextureOnly = Config.Bind("General",     
                                         "Textures Only",
                                         false,
                                         "Enabling this makes texture packs only able to change textures. This is good for mod compatability.");
            configAutoDump = Config.Bind("General",
                                         "Auto Dump",
                                         true,
                                         "If the mod should auto-dump textures when it thinks a change has occured. This is ignored if there is no core pack already present. If you turn it off, it is suggested you turn it on when the game updates for best compatibility.");
        }
    }
}
