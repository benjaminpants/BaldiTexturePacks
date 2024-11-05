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

        // todo: do this
        public static void ClearAllModifications()
        {

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
