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

        bool waiting = false;
        IEnumerator WaitForSource()
        {
            waiting = true;
            while (source == null)
            {
                yield return null;
            }
            while (source.enabled == false)
            {
                yield return null;
            }
            waiting = false;
            source.Play();
        }

        void Awake()
        {
            if (waiting) return;
            if (source == null) { StartCoroutine(WaitForSource()); return; }
            if (!source.enabled) { StartCoroutine(WaitForSource()); return; }
            source.Play();
        }

        void OnEnable()
        {
            if (waiting) return;
            if (source == null) { StartCoroutine(WaitForSource()); return; }
            if (!source.enabled) { StartCoroutine(WaitForSource()); return; }
            source.Play();
        }
    }
}
