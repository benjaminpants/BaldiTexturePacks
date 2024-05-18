using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiTexturePacks
{
    public class TexturePacksMenu : MonoBehaviour
    {
        public OptionsMenu opMen;
        public MenuToggle[] toggles = new MenuToggle[4];
        public Dictionary<string, bool> currentValues = new Dictionary<string, bool>();
        public List<string> currentOrder = new List<string>();
        public int offset = 0;
        public AdjustmentBars pageBar;
        void Start()
        {
            currentOrder = TPPlugin.Instance.packOrder.ToList();
            RebuildMenu();
        }

        public void BarUpdate()
        {
            UpdateCurrentPageValues();
            offset = pageBar.GetRaw() * 4;
            RebuildMenu();
        }

        void OnEnable()
        {
            currentOrder = TPPlugin.Instance.packOrder.ToList();
            currentValues.Clear();
            RebuildMenu();
        }

        public void UpdateCurrentPageValues()
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                if ((offset + i) > (currentOrder.Count - 1))
                {
                    continue;
                }
                string textP = currentOrder[offset + i];
                currentValues[textP] = toggles[i].Value;
            }
        }

        public void UpdatePacks()
        {
            TPPlugin.Instance.packOrder = currentOrder.ToList();
            UpdateCurrentPageValues();
            foreach (KeyValuePair<string, bool> kvp in currentValues)
            {
                TPPlugin.Instance.packs[kvp.Key].enabled = kvp.Value;
            }
        }

        void MoveItem(int positionAt, int by)
        {
            if (positionAt + by >= currentOrder.Count) return;
            UpdateCurrentPageValues();
            string toMove = currentOrder[positionAt];
            currentOrder.RemoveAt(positionAt);
            currentOrder.Insert(positionAt + by, toMove);
            RebuildMenu();
        }

        public void RebuildMenu()
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                // delete the old one if it exists
                if (toggles[i] != null)
                {
                    GameObject.Destroy(toggles[i].gameObject);
                }
                if ((offset + i) > (currentOrder.Count - 1))
                {
                    continue;
                }
                string textP = currentOrder[offset + i];
                TexturePack pack = TPPlugin.Instance.packs[textP];
                if (!currentValues.ContainsKey(textP))
                {
                    currentValues.Add(textP, pack.enabled);
                }
                MenuToggle ch = CustomOptionsCore.CreateToggleButton(opMen, new Vector2(70f, -40f * (i - 1)), pack.Name, currentValues[textP], pack.GetDesc());
                RectTransform rt = ch.transform.Find("ToggleText").gameObject.GetComponent<RectTransform>();
                rt.offsetMin = new Vector2(-308, rt.offsetMin.y);
                ch.transform.SetParent(transform, false);
                toggles[i] = ch;
                if (pack.internalName == "core")
                {
                    ch.Disable(true);
                    ((GameObject)ch.ReflectionGetVariable("disableCover")).transform.localPosition -= new Vector3(8f,0f,0f);
                }
                else
                {
                    int curDex = (offset + i);
                    Image downArrow = UIHelpers.CreateImage(TPPlugin.menuArrows[0], ch.transform, new Vector2(70f, -9f), false);
                    downArrow.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));
                    StandardMenuButton downButton = downArrow.gameObject.ConvertToButton<StandardMenuButton>();
                    downButton.highlightedSprite = TPPlugin.menuArrows[1];
                    downButton.swapOnHigh = true;
                    downButton.unhighlightedSprite = TPPlugin.menuArrows[0];
                    downButton.OnPress.AddListener(() =>
                    {
                        MoveItem(curDex, 1);
                    });

                    if (curDex - 1 == 0) continue;

                    Image upArrow = UIHelpers.CreateImage(TPPlugin.menuArrows[0], ch.transform, new Vector2(70f, 9f), false);
                    upArrow.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, -90f));
                    StandardMenuButton upButton = upArrow.gameObject.ConvertToButton<StandardMenuButton>();
                    upButton.highlightedSprite = TPPlugin.menuArrows[1];
                    upButton.swapOnHigh = true;
                    upButton.unhighlightedSprite = TPPlugin.menuArrows[0];
                    upButton.OnPress.AddListener(() =>
                    {
                        MoveItem(curDex, -1);
                    });
                }
            }
        }
    }
}
