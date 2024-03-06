using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;
using HarmonyLib;
using System.Linq;
using MTM101BaldAPI.AssetTools;
using System.Reflection;
using MonoMod.Utils;
using AlmostEngine;
using MTM101BaldAPI;

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
                    try
                    {
                        Graphics.CopyTexture(kvp.Value, TPPlugin.Instance.allTextures.Where(x => x.GetHashCode() == kvp.Key).First());
                    }
                    catch (Exception)
                    {
                        throw new TexturePackLoadException(this, "Failed to load/copy " + kvp.Value.name + "!");
                    }
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
            if (TPPlugin.Instance.allTextures.Length == 0) throw new Exception("allTextures is of length 0!");
            string[] pngs = Directory.GetFiles(Path.Combine(filePath, "Textures"), "*.png");
            for (int i = 0; i < pngs.Length; i++)
            {
                string nameToSearch = Path.GetFileNameWithoutExtension(pngs[i]).Trim();
                IEnumerable<Texture2D> potentialTextures = TPPlugin.Instance.allTextures.Where(x => x.name == nameToSearch);
                if (potentialTextures.ToArray().Length == 0)
                {
                    throw new TexturePackLoadException(this, "Unable to find texture with name: " + nameToSearch + "!");
                }
                Texture2D targetTex = potentialTextures.First();
                Texture2D generatedTex = AssetLoader.AttemptConvertTo(AssetLoader.TextureFromFile(pngs[i]), targetTex.format);
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
                string extension = Path.GetExtension(sounds[i]).ToLower().Remove(0, 1).Trim();
                Debug.Log(extension);
                if (extension == "dummy") throw new TexturePackLoadException(this, "Attempted to load dummy file! Please rename to proper format! " + Name + "/" + Path.GetFileNameWithoutExtension(sounds[i]));
                AudioClip clip = AssetLoader.AudioClipFromFile(sounds[i]);
                AudioClip[] possibleClips = allclips.Where(x => x.name == Path.GetFileNameWithoutExtension(sounds[i])).ToArray();
                if (possibleClips.Length == 0) throw new TexturePackLoadException(this, "Can't find audioclip with name: " + possibleClips + "!");
                clipsToReplace.Add(possibleClips.First(), clip);
            }
        }

        public void LoadMidis()
        {
            if (!Directory.Exists(Path.Combine(filePath, "Midi"))) return;
            string[] mids = Directory.GetFiles(Path.Combine(filePath, "Midi"), "*.mid");
            for (int i = 0; i < mids.Length; i++)
            {
                string targetMidi = Path.GetFileNameWithoutExtension(mids[i]);
                midiOverrides.Add(targetMidi, AssetLoader.MidiFromFile(mids[i], internalName + "_" + Path.GetFileNameWithoutExtension(targetMidi)));
            }
        }

        public void LoadAllNeeded()
        {
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Textures))
            {
                try
                {
                    LoadTextures();
                }
                catch (Exception E)
                {
                    MTM101BaldiDevAPI.CauseCrash(TPPlugin.Instance.Info, E);
                }
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Language))
            {
                try
                {
                    LoadLocalization();
                }
                catch (Exception E)
                {
                    MTM101BaldiDevAPI.CauseCrash(TPPlugin.Instance.Info, E);
                }
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Audio))
            {
                try
                {
                    LoadAudios();
                }
                catch (Exception E)
                {
                    MTM101BaldiDevAPI.CauseCrash(TPPlugin.Instance.Info, E);
                }
            }
            if (supportedFeatures.HasFlag(SupportedTPFeatures.Midi))
            {
                try
                {
                    LoadMidis();
                }
                catch (Exception E)
                {
                    MTM101BaldiDevAPI.CauseCrash(TPPlugin.Instance.Info, E);
                }
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
