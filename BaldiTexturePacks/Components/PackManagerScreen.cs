using MTM101BaldAPI.OptionsAPI;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiTexturePacks
{

    public class PackEntryUI : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public MenuToggle toggle;
        public StandardMenuButton top;
        public StandardMenuButton bottom;
    }

    public class PackManagerScreen : CustomOptionsCategory
    {

        int page = 0;

        bool changesMade = false;

        int maxPages;
        int startIndex => page * entriesPerPage;

        const int entriesPerPage = 3;

        public PackEntryUI[] entries = new PackEntryUI[entriesPerPage];

        public override void Build()
        {
            Vector3 originVec = new Vector3(100f,32f,0f);
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = BuildPackMoveButton("Pack" + i, originVec + new Vector3(0f,i * -40f,0f), i);
                entries[i].gameObject.SetActive(false);
            }
            CreateApplyButton(() => { ApplyPacks(true); });
            UpdatePage();
            maxPages = Mathf.FloorToInt((float)(TexturePacksPlugin.packOrder.Count - 1) / entriesPerPage);
            if (maxPages > 0)
            {
                AdjustmentBars bars = null;
                bars = CreateBars(() => 
                {
                    page = bars.GetRaw();
                }, "PageBar", (originVec + new Vector3(0f, entriesPerPage * -40f, 0f)), maxPages);
            }
        }

        void ApplyPacks(bool instant)
        {
            changesMade = false;
            if (instant)
            {
                TexturePacksPlugin.ClearAllModifications(); //revert everything to normal before beginning
                for (int i = 0; i < TexturePacksPlugin.packOrder.Count; i++)
                {
                    TexturePack foundPack = TexturePacksPlugin.packs.Find(x => x.internalId == TexturePacksPlugin.packOrder[i].Item1);
                    if (foundPack == null) continue;
                    if (TexturePacksPlugin.packOrder[i].Item2)
                    {
                        foundPack.LoadInstantly();
                    }
                }
                return;
            }
        }

        void OnDisable()
        {
            if (changesMade)
            {
                ApplyPacks(true);
            }
        }

        public void UpdatePage()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                int p = startIndex + i;
                PackEntryUI entry = entries[i];
                if (p >= TexturePacksPlugin.packOrder.Count)
                {
                    entry.gameObject.SetActive(false);
                    continue;
                }
                TexturePack pack = TexturePacksPlugin.packs.Find(x => x.internalId == TexturePacksPlugin.packOrder[p].Item1);
                entry.gameObject.SetActive(true);
                entry.text.text = pack.metaData.name;
                entry.toggle.Set(TexturePacksPlugin.packOrder[p].Item2);
            }
        }

        public void MoveElement(int i, int amount)
        {
            changesMade = true;
            if (i + amount < 0) return;
            if (i + amount >= TexturePacksPlugin.packOrder.Count) return;
            (string, bool) order = TexturePacksPlugin.packOrder[i];
            TexturePacksPlugin.packOrder.RemoveAt(i);
            TexturePacksPlugin.packOrder.Insert(i + amount, order);
            UpdatePage();
        }

        public PackEntryUI BuildPackMoveButton(string name, Vector3 position, int pos)
        {
            MenuToggle toggle = CreateToggle("Toggle", "Modded Title Screen", false, Vector3.zero, 250f);
            Destroy(toggle.GetComponentInChildren<TextLocalizer>());
            toggle.GetComponentInChildren<StandardMenuButton>().OnPress.AddListener(() =>
            {
                (string, bool) packOrd = TexturePacksPlugin.packOrder[startIndex + pos];
                TexturePacksPlugin.packOrder[startIndex + pos] = (packOrd.Item1, toggle.Value);
            });
            PackEntryUI ui = new GameObject().AddComponent<PackEntryUI>();
            StandardMenuButton butTop = CreateButton(() => {
                MoveElement(startIndex + pos, -1);
            }, menuArrowRight, menuArrowRightHighlight, name + "HighlightButtonU", new Vector3(64f,10f,0f));
            butTop.transform.localEulerAngles = new Vector3(0f,0f,90f);
            StandardMenuButton butBot = CreateButton(() => {
                MoveElement(startIndex + pos, 1);
            }, menuArrowLeft, menuArrowLeftHighlight, name + "HighlightButtonD", new Vector3(64f,-10f, 0f));
            butBot.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            ui.transform.SetParent(toggle.transform.parent);
            ui.transform.localScale = Vector3.one;

            toggle.transform.SetParent(ui.transform, false);
            butTop.transform.SetParent(ui.transform, false);
            butBot.transform.SetParent(ui.transform, false);

            ui.toggle = toggle;
            ui.text = toggle.GetComponentInChildren<TextMeshProUGUI>();
            ui.top = butTop;
            ui.bottom = butBot;
            ui.transform.localPosition = position;
            ui.name = name;

            return ui;
        }
    }
}
