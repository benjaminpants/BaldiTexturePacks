using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks
{
    /// <summary>
    /// The class representing a standard Texture Pack
    /// </summary>
    public class TexturePack
    {
        protected string _path;
        public string path => _path;

        public Dictionary<Texture2D, string> texturesToReplacementsPaths = new Dictionary<Texture2D, string>();

        public Dictionary<SoundObject, SoundReplacement> soundReplacements = new Dictionary<SoundObject, SoundReplacement>();

        public Dictionary<AudioClip, string> clipReplacements = new Dictionary<AudioClip, string>();

        private List<AudioClip> createdClips = new List<AudioClip>();

        public TexturePack(string path)
        {
            string texturesPath = Path.Combine(path, "Textures");
            string soundPath = Path.Combine(path, "SoundObjects");
            string clipsPath = Path.Combine(path, "AudioClips");
            _path = path;
            if (Directory.Exists(texturesPath))
            {
                string[] textures = Directory.GetFiles(texturesPath, "*.png");
                for (int i = 0; i < textures.Length; i++)
                {
                    texturesToReplacementsPaths.Add(TexturePacksPlugin.validTexturesForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(textures[i])), textures[i]);
                }
            }
            if (Directory.Exists(soundPath))
            {
                string[] audio = Directory.GetFiles(soundPath, "*.wav");
                for (int i = 0; i < audio.Length; i++)
                {
                    soundReplacements.Add(TexturePacksPlugin.validSoundObjectsForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(audio[i])), new SoundReplacement()
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
                    clipReplacements.Add(TexturePacksPlugin.validClipsForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(clips[i])), clips[i]);
                }
            }

        }

        public void LoadInstantly()
        {
            LoadAll().MoveUntilDone();
        }

        public IEnumerator LoadAll()
        {
            foreach (AudioClip clip in createdClips)
            {
                UnityEngine.Object.Destroy(clip);
            }
            createdClips.Clear();
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
            yield break;
        }
    }
}
