using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiTexturePacks
{

    // todo: investigate chalkles
    public class SpriteOverlay : MonoBehaviour
    {
        SpriteRenderer toCopy;
        SpriteRenderer myRenderer;
        string lastSpriteName = "";
        void Awake()
        {
            toCopy = GetComponent<SpriteRenderer>();
            toCopy.forceRenderingOff = true; // nothing else uses this
            GameObject child = new GameObject("FakeRenderer");
            child.transform.SetParent(transform);
            child.transform.localScale = Vector3.one;
            child.transform.localPosition = Vector3.zero;
            child.transform.rotation = Quaternion.identity;
            child.layer = LayerMask.NameToLayer("Billboard");
            myRenderer = child.AddComponent<SpriteRenderer>();
            myRenderer.material = toCopy.material;
            if (TexturePacksPlugin.currentSpriteReplacements.ContainsKey(toCopy.sprite.name))
            {
                myRenderer.sprite = TexturePacksPlugin.currentSpriteReplacements[toCopy.sprite.name];
            }
            else
            {
                myRenderer.sprite = toCopy.sprite;
            }
        }

        void Update()
        {
            myRenderer.flipX = toCopy.flipX;
            myRenderer.flipY = toCopy.flipY;
            if (toCopy.sprite.name != lastSpriteName)
            {
                if (TexturePacksPlugin.currentSpriteReplacements.ContainsKey(toCopy.sprite.name))
                {
                    myRenderer.sprite = TexturePacksPlugin.currentSpriteReplacements[toCopy.sprite.name];
                }
                else
                {
                    myRenderer.sprite = toCopy.sprite;
                }
                lastSpriteName = toCopy.sprite.name;
            }
            myRenderer.color = toCopy.color;
            myRenderer.enabled = toCopy.enabled;
        }
    }
}
