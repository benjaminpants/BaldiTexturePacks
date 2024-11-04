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

        public TexturePack(string path)
        {
            string texturesPath = Path.Combine(path, "Textures");
            string audioPath = Path.Combine(path, "SoundObjects");
            _path = path;
            if (Directory.Exists(texturesPath))
            {
                string[] textures = Directory.GetFiles(texturesPath);
                for (int i = 0; i < textures.Length; i++)
                {
                    texturesToReplacementsPaths.Add(TexturePacksPlugin.validTexturesForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(textures[i])), textures[i]);
                }
            }
            if (Directory.Exists(audioPath))
            {
                string[] audio = Directory.GetFiles(audioPath);
                for (int i = 0; i < audio.Length; i++)
                {
                    soundReplacements.Add(TexturePacksPlugin.validSoundObjectsForReplacement.Find(x => x.name == Path.GetFileNameWithoutExtension(audio[i])), new SoundReplacement()
                    {
                        clipPath = audio[i]
                    });
                }
            }

        }

        public void LoadInstantly()
        {
            LoadAll().MoveUntilDone();
        }

        public IEnumerator LoadAll()
        {
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
            yield break;
        }
    }
}
