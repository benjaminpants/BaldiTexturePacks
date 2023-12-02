using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;
using HarmonyLib;
using System.Linq;
using MTM101BaldAPI.AssetManager;
using System.Reflection;
using MonoMod.Utils;
using AlmostEngine;

namespace BaldiTexturePacks
{
    [Flags]
    public enum SupportedTPFeatures
    {
        Textures = 1,
        Audio = 2,
        Midi = 4,
        Language = 8
    }

    public class TexturePack
    {
        public TexturePack(string name, string author, int version, string fp)
        {
            Name = name;
            Author = author;
            Version = version;
            filePath = fp;
        }

        public void UpdateFile()
        {
            File.WriteAllText(Path.Combine(filePath, "pack.json"), JsonConvert.SerializeObject(this));
        }

        private static FieldInfo lm_localizedText = AccessTools.Field(typeof(LocalizationManager), "localizedText");
        public void Apply()
        {
            if (!enabled) return;
            TPPlugin.Log.LogDebug(String.Format("[{0}] Loading...", Name));
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Textures))
            {
                foreach (KeyValuePair<int, Texture2D> kvp in textures)
                {
                    Graphics.CopyTexture(kvp.Value, TPPlugin.Instance.allTextures.Where(x => x.GetHashCode() == kvp.Key).First());
                }
                TPPlugin.Log.LogDebug(String.Format("[{0}] Copied {1} textures.", Name, textures.Count));
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Language) && langData != null)
            {
                Dictionary<string, string> dict = (Dictionary<string, string>)lm_localizedText.GetValue(Singleton<LocalizationManager>.Instance);
                langData.items.Do(x =>
                {
                    dict[x.key] = x.value; //this is stupid but oh well
                });
                lm_localizedText.SetValue(Singleton<LocalizationManager>.Instance, dict);
                TPPlugin.Log.LogDebug(String.Format("[{0}] Loaded {1} language elements.", Name, langData.items.Length));
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Audio))
            {
                // replace all the clips
                foreach (KeyValuePair<SoundObject, AudioClip> kvp in TPPlugin.Instance.originalSoundClips)
                {
                    if (clipsToReplace.ContainsKey(kvp.Value))
                    {
                        kvp.Key.soundClip = clipsToReplace[kvp.Value];
                    }
                }
                foreach (KeyValuePair<AudioSource, AudioClip> kvp in TPPlugin.Instance.originalSourceClips)
                {
                    if (clipsToReplace.ContainsKey(kvp.Value))
                    {
                        if (kvp.Key.clip != null)
                        {
                            kvp.Key.clip = clipsToReplace[kvp.Value];
                        }
                    }
                }
                TPPlugin.Log.LogDebug(String.Format("[{0}] Loaded {1} audio replacements.", Name, clipsToReplace.Count));
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Midi))
            {
                foreach (KeyValuePair<string, string> kvp in midiOverrides)
                {
                    TPPlugin.Instance.midiOverrides[kvp.Key] = kvp.Value;
                }
                TPPlugin.Log.LogDebug(String.Format("[{0}] Added {1} midis to midi replacement patch.", Name, midiOverrides.Count));
            }
        }
        public string GetDesc()
        {
            string warning = "";
            if (langData != null)
            {
                if (langData.items.Length != 0)
                {
                    warning = "\n*This pack is not guranteed to instantly unload when disabled!";
                }
            }
            if (clipsToReplace.Count != 0)
            {
                warning += "\n*This pack is using experimental features!(Audio replacement!)";
            }
            return String.Format("{0}{1}{2}\n{3}", Description, (warning != "") ? "*" : "", warning, "Author:" + Author);
        }

        public void LoadTextures()
        {
            if (!Directory.Exists(Path.Combine(filePath, "Textures"))) return;
            string[] pngs = Directory.GetFiles(Path.Combine(filePath, "Textures"), "*.png");
            for (int i = 0; i < pngs.Length; i++)
            {
                Texture2D targetTex = TPPlugin.Instance.allTextures.Where(x => x.name == Path.GetFileNameWithoutExtension(pngs[i])).First();
                Texture2D generatedTex = AssetManager.AttemptConvertTo(AssetManager.TextureFromFile(pngs[i]), targetTex.format);
                generatedTex.name = Path.GetFileName(filePath) + "_" + generatedTex.name;
                textures.Add(targetTex.GetHashCode(), generatedTex);
            }
        }

        public void LoadAudios()
        {
            AudioClip[] allclips = Resources.FindObjectsOfTypeAll<AudioClip>();
            if (internalName == "core")
            {
                allclips.Do(clip =>
                {
                    clipsToReplace.Add(clip, clip);
                });
                return;
            }
            if (!Directory.Exists(Path.Combine(filePath, "Audio"))) return;
            string[] sounds = Directory.GetFiles(Path.Combine(filePath, "Audio"));
            for (int i = 0; i < sounds.Length; i++)
            {
                AudioClip clip = AssetManager.AudioClipFromFile(sounds[i]);
                clipsToReplace.Add(allclips.Where(x => x.name == Path.GetFileNameWithoutExtension(sounds[i])).First(), clip);
            }
        }

        public void LoadMidis()
        {
            if (!Directory.Exists(Path.Combine(filePath, "Midi"))) return;
            string[] mids = Directory.GetFiles(Path.Combine(filePath, "Midi"), "*.mid");
            for (int i = 0; i < mids.Length; i++)
            {
                string targetMidi = Path.GetFileNameWithoutExtension(mids[i]);
                midiOverrides.Add(targetMidi, AssetManager.MidiFromFile(mids[i], internalName + "_" + Path.GetFileNameWithoutExtension(targetMidi)));
            }
        }

        public void LoadAllNeeded()
        {
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Textures))
            {
                LoadTextures();
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Language))
            {
                LoadLocalization();
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Audio))
            {
                LoadAudios();
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Midi))
            {
                LoadMidis();
            }
        }

        public void LoadLocalization()
        {
            string subPath = Path.Combine(filePath, "Subtitles.json");
            if (!File.Exists(subPath)) return;
            langData = JsonUtility.FromJson<LocalizationData>(File.ReadAllText(subPath));
        }

        public string Name = "none";
        public string Description = "A resource pack";
        public string Author = "none";
        public int Version = 0;
        [JsonIgnore]
        public bool enabled = false;

        [JsonIgnore]
        public SupportedTPFeatures supportedFeatures
        {
            get
            {
                return TPPlugin.Instance.configTextureOnly.Value ? SupportedTPFeatures.Textures : supportedFeaturesInternal;
            }
        }


        [JsonIgnore]
        protected SupportedTPFeatures supportedFeaturesInternal
        {
            get
            {
                switch (Version)
                {
                    case -1: //for core resource pack use
                        return SupportedTPFeatures.Textures | SupportedTPFeatures.Audio;
                    case 1:
                        return SupportedTPFeatures.Textures;
                    case 2:
                        return SupportedTPFeatures.Textures | SupportedTPFeatures.Language;
                    case 3:
                        return SupportedTPFeatures.Textures | SupportedTPFeatures.Language | SupportedTPFeatures.Audio;
                    case 4:
                        return SupportedTPFeatures.Textures | SupportedTPFeatures.Language | SupportedTPFeatures.Audio | SupportedTPFeatures.Midi;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        [JsonIgnore]
        public string filePath;
        [JsonIgnore]
        public string internalName => Path.GetFileName(filePath);
        [JsonIgnore]
        public Dictionary<int, Texture2D> textures = new Dictionary<int, Texture2D>();
        [JsonIgnore]
        public LocalizationData langData = null;
        [JsonIgnore]
        public Dictionary<AudioClip, AudioClip> clipsToReplace = new Dictionary<AudioClip, AudioClip>();
        [JsonIgnore]
        public Dictionary<string, string> midiOverrides = new Dictionary<string, string>();
    }

}
