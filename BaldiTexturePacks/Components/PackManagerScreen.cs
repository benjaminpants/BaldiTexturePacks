using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.OptionsAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public TexturePack currentPack;
    }

    public class PackManagerScreen : CustomOptionsCategory
    {

        int page = 0;

        bool changesMade = false;

        int maxPages;
        int startIndex => page * entriesPerPage;

        const int entriesPerPage = 3;

        public TextMeshProUGUI pageText;

        public PackEntryUI[] entries = new PackEntryUI[entriesPerPage];

        public override void Build()
        {
            Vector3 originVec = new Vector3(100f,32f,0f);
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = BuildPackMoveButton("Pack" + i, originVec + new Vector3(0f,i * -40f,0f), i);
                entries[i].gameObject.SetActive(false);
            }
            CreateApplyButton(() => {
                ApplyPacks(true);
                SoundObject obj = TexturePacksPlugin.validSoundObjectsForReplacement.First(x => x.name == "NotebookCollect");
                optionsMenu.GetComponent<AudioManager>().PlaySingle(obj);
            });
            maxPages = Mathf.FloorToInt((float)(TexturePacksPlugin.packOrder.Count - 1) / entriesPerPage);
            CreateButton(() => { page--; UpdatePage(); }, menuArrowLeft, menuArrowLeftHighlight, "PreviousPage", new Vector3(-40f, entriesPerPage * -40f, 0f));
            CreateButton(() => { page++; UpdatePage(); }, menuArrowRight, menuArrowRightHighlight, "Next", new Vector3(40f, entriesPerPage * -40f, 0f));
            pageText = CreateText("PageNumber","1/1", new Vector3(0f, entriesPerPage * -40f, 0f), MTM101BaldAPI.UI.BaldiFonts.ComicSans24, TextAlignmentOptions.Center, new Vector2(80f,48f), Color.black, false);
            UpdatePage();
        }

        void ApplyPacks(bool instant)
        {
            changesMade = false;
            if (instant)
            {
                TexturePacksPlugin.ClearAllModifications(); //revert everything to normal before beginning
                try
                {
                    for (int i = 0; i < TexturePacksPlugin.packOrder.Count; i++)
                    {
                        TexturePack foundPack = TexturePacksPlugin.packs.Find(x => x.internalId == TexturePacksPlugin.packOrder[i].Item1);
                        if (foundPack == null) continue;
                        if (TexturePacksPlugin.packOrder[i].Item2)
                        {
                            foundPack.LoadInstantly();
                        }
                    }
                }
                catch (Exception E)
                {
                    MTM101BaldiDevAPI.CauseCrash(TexturePacksPlugin.Instance.Info, E);
                }
                TexturePacksPlugin.FinalizePackLoading();
                return;
            }
            throw new NotImplementedException();
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
            page = Mathf.Clamp(page, 0, maxPages);
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
                entry.currentPack = pack;
            }
            pageText.text = (page + 1) + "/" + (maxPages + 1);
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

        static FieldInfo _hotspot = AccessTools.Field(typeof(MenuToggle), "hotspot");

        public PackEntryUI BuildPackMoveButton(string name, Vector3 position, int pos)
        {
            PackEntryUI ui = new GameObject().AddComponent<PackEntryUI>();
            MenuToggle toggle = CreateToggle("Toggle", "Modded Title Screen", false, Vector3.zero, 250f);
            StandardMenuButton buttonToAddTipTo = ((GameObject)_hotspot.GetValue(toggle)).GetComponent<StandardMenuButton>();
            buttonToAddTipTo.eventOnHigh = true;
            buttonToAddTipTo.OnHighlight.AddListener(() => {
                tooltipController.UpdateTooltip(ui.currentPack.metaData.description + "\nAuthor: " + ui.currentPack.metaData.author + (ui.currentPack.flags == PackFlags.Legacy ? "\n(Legacy Pack!)" : ""));
            });
            buttonToAddTipTo.OffHighlight.AddListener(() => { tooltipController.CloseTooltip(); });
            Destroy(toggle.GetComponentInChildren<TextLocalizer>());
            toggle.GetComponentInChildren<StandardMenuButton>().OnPress.AddListener(() =>
            {
                (string, bool) packOrd = TexturePacksPlugin.packOrder[startIndex + pos];
                TexturePacksPlugin.packOrder[startIndex + pos] = (packOrd.Item1, toggle.Value);
            });
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
