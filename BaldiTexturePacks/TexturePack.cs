using BaldiTexturePacks.ReplacementSystem;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks
{

    public enum PackFlags
    {
        Legacy = 0,
        TexturesAndSounds = 1
    }

    public class PackMeta
    {
        [JsonProperty("Name")]
        public string name;

        [JsonProperty("Description")]
        public string description;

        [JsonProperty("Author")]
        public string author;

        [JsonProperty("Version")]
        public int versionNumber;

        public PackMeta()
        {
            name = "Unnamed";
            description = "No description.";
            author = "Nobody";
            versionNumber = 6;
        }
    }

    /// <summary>
    /// The class representing a standard Texture Pack
    /// </summary>
    public class TexturePack
    {
        protected string _path;
        public string path => _path;
        public string localizationPath => Path.Combine(path, (flags == PackFlags.Legacy) ? "Subtitles.json" : "Subtitles_English.json");
        public string overlaysPath => Path.Combine(path, "SpriteSwaps");
        public Dictionary<Texture2D, string> texturesToReplacementsPaths = new Dictionary<Texture2D, string>();
        public Dictionary<SoundObject, SoundReplacement> soundReplacements = new Dictionary<SoundObject, SoundReplacement>();
        public Dictionary<AudioClip, string> clipReplacements = new Dictionary<AudioClip, string>();
        private List<AudioClip> createdClips = new List<AudioClip>();
        public PackMeta metaData = new PackMeta();
        public LocalizationData localizationData = null;

        public List<string> manualReplacementPaths = new List<string>();
        public List<ReplaceNode> manualReplacements = new List<ReplaceNode>();

        public List<string> spriteOverlayPaths = new List<string>();
        public List<Sprite> createdSprites = new List<Sprite>();

        public PackFlags flags
        {
            get
            {
                if (metaData.versionNumber <= 5)
                {
                    return PackFlags.Legacy; // legacy pack
                }
                switch (metaData.versionNumber)
                {
                    default:
                    case 6:
                        return PackFlags.TexturesAndSounds;
                }
            }
        }

        public TexturePack(string path)
        {
            metaData = JsonConvert.DeserializeObject<PackMeta>(File.ReadAllText(Path.Combine(path, "pack.json")));
            _path = path;
            string texturesPath = Path.Combine(path, "Textures");
            string soundPath = Path.Combine(path, "SoundObjects");
            string clipsPath = Path.Combine(path, "AudioClips");
            string replacementsPath = Path.Combine(path, "Replacements");
            // allow legacy packs to load somewhat
            if (flags == PackFlags.Legacy)
            {
                soundPath = Path.Combine(path, "Audio");
                clipsPath = Path.Combine(path, "Audio");
            }
            if (Directory.Exists(texturesPath))
            {
                string[] textures = Directory.GetFiles(texturesPath, "*.png");
                for (int i = 0; i < textures.Length; i++)
                {
                    Texture2D textureToReplace = TexturePacksPlugin.validTexturesForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(textures[i]));
                    if (textureToReplace == null) continue;
                    texturesToReplacementsPaths.Add(textureToReplace, textures[i]);
                }
            }
            if (Directory.Exists(soundPath))
            {
                string[] audio = Directory.GetFiles(soundPath, "*.wav");
                for (int i = 0; i < audio.Length; i++)
                {
                    SoundObject objectToReplace = TexturePacksPlugin.validSoundObjectsForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(audio[i]));
                    if (objectToReplace == null) continue;
                    soundReplacements.Add(objectToReplace, new SoundReplacement()
                    {
                        clipPath = audio[i]
                    });
                }
            }
            if (Directory.Exists(clipsPath))
            {
                string[] clips = Directory.GetFiles(clipsPath, "*.wav");
                for (int i = 0; i < clips.Length; i++)
                {
                    AudioClip clipToReplace = TexturePacksPlugin.validClipsForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(clips[i]));
                    if (clipToReplace == null) continue;
                    clipReplacements.Add(clipToReplace, clips[i]);
                }
            }
            if (Directory.Exists(replacementsPath))
            {
                manualReplacementPaths = Directory.GetFiles(replacementsPath, "*.json").ToList();
            }
            if (Directory.Exists(overlaysPath))
            {
                spriteOverlayPaths = Directory.GetFiles(overlaysPath, "*.json").ToList();
            }
        }

        public void LoadInstantly()
        {
            LoadAll().MoveUntilDone();
        }

        public void Unload()
        {
            foreach (AudioClip clip in createdClips)
            {
                UnityEngine.Object.Destroy(clip);
            }
            foreach (Sprite sprite in createdSprites)
            {
                UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
            }
            createdClips.Clear();
            manualReplacements.Clear();
            createdSprites.Clear();
            localizationData = null;
        }

        public IEnumerator LoadAll()
        {
            Unload();
            foreach (KeyValuePair<Texture2D, string> toReplacePath in texturesToReplacementsPaths)
            {
                yield return "Loading: " + toReplacePath.Value;
                Texture2D toDo = AssetLoader.AttemptConvertTo(AssetLoader.TextureFromFile(toReplacePath.Value), toReplacePath.Key.format);
                AssetLoader.ReplaceTexture(toReplacePath.Key, toDo);
                UnityEngine.GameObject.Destroy(toDo);
            }
            foreach (KeyValuePair<SoundObject, SoundReplacement> replacement in soundReplacements)
            {
                yield return "Loading: " + replacement.Value.clipPath;
                replacement.Value.Load();
                TexturePacksPlugin.currentSoundReplacements[replacement.Key] = replacement.Value;
            }
            foreach (KeyValuePair<AudioClip, string> replacement in clipReplacements)
            {
                yield return "Loading: " + replacement.Value;
                AudioClip audClip = AssetLoader.AudioClipFromFile(replacement.Value);
                audClip.name += "_Pack"; //todo: update
                createdClips.Add(audClip);
                TexturePacksPlugin.currentClipReplacements[replacement.Key] = audClip;
            }
            if (manualReplacementPaths.Count > 0)
            {
                foreach (string replacementPath in manualReplacementPaths)
                {
                    yield return "Loading: " + replacementPath;
                    manualReplacements.Add(JsonConvert.DeserializeObject<ReplaceNode>(File.ReadAllText(replacementPath)));
                }
                yield return "Traversing replacement trees...";
                foreach (ReplaceNode rpNode in manualReplacements)
                {
                    rpNode.GoThroughTree(TexturePacksPlugin.validManualReplacementTargets, null).Do(x => TexturePacksPlugin.AddUndo(x));
                }
            }
            foreach (string overlayPath in spriteOverlayPaths)
            {
                yield return "Loading Sprite Swap: " + overlayPath;
                Dictionary<string, SpriteOverlayData> data = JsonConvert.DeserializeObject<Dictionary<string, SpriteOverlayData>>(File.ReadAllText(overlayPath));
                foreach (KeyValuePair<string, SpriteOverlayData> kvp in data)
                {
                    Sprite generatedSprite = kvp.Value.GenerateSprite(overlaysPath);
                    createdSprites.Add(generatedSprite);
                    TexturePacksPlugin.currentSpriteReplacements[kvp.Key] = generatedSprite;
                }
            }
            if (File.Exists(localizationPath))
            {
                yield return "Reloading Localization...";
                localizationData = JsonConvert.DeserializeObject<LocalizationData>(File.ReadAllText(localizationPath));
                // todo: store this via AccessTools
                Singleton<LocalizationManager>.Instance.ReflectionInvoke("Start", null);
            }
            yield break;
        }
    }
}
