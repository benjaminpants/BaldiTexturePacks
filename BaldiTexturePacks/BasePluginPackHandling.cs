﻿using BaldiTexturePacks.ReplacementSystem;
using BepInEx;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
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

        // created at the end of the loading process
        public static Dictionary<SoundObject, SoundObject> createdSoundObjectDummies = new Dictionary<SoundObject, SoundObject>();

        // created during pack loading
        public static Dictionary<SoundObject, SubtitleOverrideData> currentSubtitleOverrides = new Dictionary<SoundObject, SubtitleOverrideData>();

        public static Dictionary<string, Sprite> currentSpriteReplacements = new Dictionary<string, Sprite>();

        public static Dictionary<string, string> currentMidiReplacements = new Dictionary<string, string>();

        public static Dictionary<Cubemap, Cubemap> currentCubemapReplacements = new Dictionary<Cubemap, Cubemap>();

        // finalize the pack loading by generating all the dummy SoundObjects we need for subtitle replacements
        public static void FinalizePackLoading()
        {
            foreach (KeyValuePair<SoundObject, SubtitleOverrideData> overrideData in currentSubtitleOverrides)
            {
                SoundObject dummyObject = ScriptableObject.CreateInstance<SoundObject>();
                if (overrideData.Value.subDurationOverride == -1f)
                {
                    dummyObject.subDuration = overrideData.Key.subDuration;
                }
                else
                {
                    dummyObject.subDuration = overrideData.Value.subDurationOverride;
                }

                if (overrideData.Value.keyOverride == "")
                {
                    dummyObject.soundKey = overrideData.Key.soundKey;
                }
                else
                {
                    dummyObject.soundKey = overrideData.Value.keyOverride;
                }

                dummyObject.additionalKeys = overrideData.Value.timedKeys;
                dummyObject.name = overrideData.Key.name + "_PackDummy";
                dummyObject.soundClip = overrideData.Key.soundClip; // just incase
                createdSoundObjectDummies[overrideData.Key] = dummyObject;
            }

            Singleton<LocalizationManager>.Instance.ReflectionInvoke("Start", null);
        }

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
            currentSpriteReplacements.Clear();
            currentSubtitleOverrides.Clear();
            currentMidiReplacements.Clear();
            currentCubemapReplacements.Clear();

            foreach (SoundObject so in createdSoundObjectDummies.Values)
            {
                Destroy(so); //free memory
            }
            createdSoundObjectDummies.Clear();

            foreach (TexturePack pack in packs)
            {
                pack.Unload(); //unload all audio clips, sprites, midis and cubemaps
            }

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

    public class SoundReplacement
    {
        protected bool loaded;

        public AudioClip[] clips;

        public string clipPath;

        public AudioClip GetClip()
        {
            if (clips.Length == 1)
            {
                return clips[0];
            }
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        public void Load()
        {
            if (loaded)
            {
                Unload();
            }
            List<AudioClip> foundClips = new List<AudioClip>
            {
                AssetLoader.AudioClipFromFile(clipPath)
            };
            string[] foundAltClips = Directory.GetFiles(Path.GetDirectoryName(clipPath), Path.GetFileNameWithoutExtension(clipPath) + ".*.wav");
            foreach (string path in foundAltClips)
            {
                foundClips.Add(AssetLoader.AudioClipFromFile(path));
            }
            clips = foundClips.ToArray();
            loaded = true;
        }

        public void Unload()
        {
            foreach (AudioClip clip in clips)
            {
                UnityEngine.Object.Destroy(clip);
            }
            loaded = false;
        }
    }
}
