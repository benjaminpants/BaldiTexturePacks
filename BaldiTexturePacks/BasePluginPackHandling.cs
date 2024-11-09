using BaldiTexturePacks.ReplacementSystem;
using BepInEx;
using MTM101BaldAPI.AssetTools;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks
{
    public partial class TexturePacksPlugin : BaseUnityPlugin
    {
        public static Dictionary<Texture2D, Texture2D> originalTextures = new Dictionary<Texture2D, Texture2D>();

        public static Dictionary<SoundObject, SoundReplacement> currentSoundReplacements = new Dictionary<SoundObject, SoundReplacement>();

        public static Dictionary<AudioClip, AudioClip> currentClipReplacements = new Dictionary<AudioClip, AudioClip>();

        public static List<Replacement> doneReplacements = new List<Replacement>();

        public static void AddUndo(Replacement toUndo)
        {
            // make sure there are no exact duplicates
            if (doneReplacements.Find(x => x.instance == toUndo.instance && x.field == toUndo.field) == null)
            {
                doneReplacements.Add(toUndo);
            }
        }

        public static void ClearAllModifications()
        {
            currentSoundReplacements.Clear();
            currentClipReplacements.Clear();

            foreach (KeyValuePair<Texture2D, Texture2D> textureToRevert in originalTextures)
            {
                AssetLoader.ReplaceTexture(textureToRevert.Key, textureToRevert.Value);
            }

            foreach (Replacement r in doneReplacements)
            {
                r.Undo();
            }
            doneReplacements.Clear();
        }
    }

    // in the future this will have more features
    public class SoundReplacement
    {
        protected bool loaded;

        public AudioClip clip;

        public string clipPath;

        public AudioClip GetClip()
        {
            return clip;
        }

        public void Load()
        {
            if (loaded)
            {
                Unload();
            }
            clip = AssetLoader.AudioClipFromFile(clipPath);
            loaded = true;
        }

        public void Unload()
        {
            UnityEngine.Object.Destroy(clip);
            loaded = false;
        }
    }
}
