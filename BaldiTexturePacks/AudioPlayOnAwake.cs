using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks
{
    public class AudioPlayOnAwake : MonoBehaviour
    {
        public AudioSource source;
        IEnumerator WaitForSource()
        {
            while (source == null)
            {
                yield return null;
            }
            while (source.enabled == false)
            {
                yield return null;
            }
            source.Play();
        }

        void Awake()
        {
            if (source == null) { StartCoroutine(WaitForSource()); return; }
            if (!source.enabled) { StartCoroutine(WaitForSource()); return; }
            source.Play();
        }
    }
}
