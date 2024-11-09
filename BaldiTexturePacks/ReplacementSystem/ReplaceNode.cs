using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BaldiTexturePacks.ReplacementSystem
{
    public class ReplaceNode
    {
        [JsonProperty("Name")]
        public string name;
        [JsonProperty("Children")]
        public ReplaceNode[] children = new ReplaceNode[0];
        [JsonProperty("Replacements")]
        public Dictionary<string, string> replacements = new Dictionary<string, string>();

        // https://stackoverflow.com/questions/30299671/matching-strings-with-wildcard
        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

        public Component[] ScanForName(Component[] array, string search)
        {
            string regEx = WildCardToRegular(search);
            return array.Where(x => Regex.IsMatch(x.gameObject.name, regEx)).ToArray();
        }

        public Replacement[] ReplaceNonGameObject(Component target)
        {
            throw new NotImplementedException("Replacing non-gameobject fields hasn't been implemented yet!");
        }

        public Replacement[] GoThroughTree(Dictionary<string, List<Component>> validToSearch, Component rootObject = null)
        {
            List<Replacement> undos = new List<Replacement>();

            List<Component> componentsToApplyReplacementsTo = new List<Component>();

            // parse name
            string[] nameParts = name.Split(':');
            if (nameParts.Length > 2)
            {
                throw new Exception("More than 2 parts found in name!");
            }
            if (nameParts.Length == 1)
            {
                if (rootObject == null) // if we don't have a rootObject this must be the initial call
                {
                    throw new Exception("Uppermost node must contain type!"); //we need an object to start at!
                }
                else
                {
                    // if this isn't a gameobject, such as attempting to replace variables belong to Fog
                    // go through all of them and perform the replacement logic for those
                    for (int i = 0; i < children.Length; i++)
                    {
                        undos.AddRange(children[i].ReplaceNonGameObject(rootObject));
                    }
                }
            }
            else
            {
                // if we have a rootObject, we must've been triggered through the children search
                if (rootObject != null)
                {
                    TexturePacksPlugin.Log.LogDebug("We have root object " + rootObject + "!");
                    string regEx = WildCardToRegular(nameParts[1]);
                    for (int i = 0; i < rootObject.transform.childCount; i++)
                    {
                        GameObject gamObj = rootObject.transform.GetChild(i).gameObject;
                        if (Regex.IsMatch(gamObj.name, regEx))
                        {
                            Component comp = gamObj.GetComponent(nameParts[0]);
                            if (comp == null) continue;
                            TexturePacksPlugin.Log.LogInfo("fo:" + nameParts[0]);
                            componentsToApplyReplacementsTo.Add(comp);
                        }
                    }
                }
                else
                {
                    componentsToApplyReplacementsTo.AddRange(ScanForName(validToSearch[nameParts[0]].ToArray(), nameParts[1]));
                }
            }

            for (int i = 0; i < children.Length; i++)
            {
                for (int j = 0; j < componentsToApplyReplacementsTo.Count; j++)
                {
                    undos.AddRange(children[i].GoThroughTree(validToSearch, componentsToApplyReplacementsTo[j]));
                }
            }

            TexturePacksPlugin.Log.LogDebug(componentsToApplyReplacementsTo.Count);
            // now that we have everything we need, apply our replacements
            foreach (KeyValuePair<string, string> replacement in replacements)
            {
                TexturePacksPlugin.Log.LogDebug(replacement.Key + ":" + replacement.Value);
                foreach (Component comp in componentsToApplyReplacementsTo)
                {
                    if (!TexturePacksPlugin.validFieldChanges.ContainsKey(comp.GetType()))
                    {
                        TexturePacksPlugin.Log.LogWarning("Attempted to change property on invalid type: " + comp.GetType().Name + "!");
                        continue;
                    }
                    Replacement rp = new Replacement(comp, replacement.Key);
                    undos.Add(rp);
                    rp.SetValue(StringToField(rp.replacementType, replacement.Value));
                }
            }
            return undos.ToArray();
        }

        public static string FieldToString(object obj)
        {
            switch (obj.GetType().Name)
            {
                case "Single":
                case "UInt32":
                case "Double":
                case "Int32":
                    return obj.ToString();
                case "Color":
                    Color c = (Color)obj;
                    return FieldToString(c.r) + " " + FieldToString(c.g) + " " + FieldToString(c.b) + " " + FieldToString(c.a);
                case "Color32":
                    Color32 c32 = (Color32)obj;
                    return FieldToString(c32.r) + " " + FieldToString(c32.g) + " " + FieldToString(c32.b) + " " + FieldToString(c32.a);
                case "System.String":
                    return (string)obj;
                default:
                    throw new NotImplementedException("Unknown primative type: " + obj.GetType().Name);
            }
        }

        public static object StringToField(string type, string str)
        {
            switch (type)
            {
                case "Single":
                    return float.Parse(str);
                case "UInt32":
                    return uint.Parse(str);
                case "Int32":
                    return int.Parse(str);
                case "Color":
                    string[] split = str.Split(' ');
                    return new Color(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
                case "Color32":
                    string[] split32 = str.Split(' ');
                    return new Color32(byte.Parse(split32[0]), byte.Parse(split32[1]), byte.Parse(split32[2]), byte.Parse(split32[3]));
                case "String":
                    return str;
                default:
                    throw new NotImplementedException("Unknown str passed: " + str);
            }
        }
    }


    public class Replacement
    {
        public object instance;
        FieldInfo infoField;
        PropertyInfo infoProperty;
        public string value;
        public string field;
        public string replacementType;

        public void SetValue(object v)
        {
            if (infoField != null)
            {
                infoField.SetValue(instance, v);
            }
            else
            {
                infoProperty.SetValue(instance, v);
            }
        }

        public Replacement(object instance, string field)
        {
            this.field = field;
            this.instance = instance;
            if (AccessTools.GetFieldNames(instance.GetType()).Contains(field))
            {
                infoField = AccessTools.Field(instance.GetType(), field);
                value = ReplaceNode.FieldToString(infoField.GetValue(instance));
                replacementType = infoField.FieldType.Name;
            }
            else
            {
                infoProperty = AccessTools.Property(instance.GetType(), field);
                value = ReplaceNode.FieldToString(infoProperty.GetValue(instance));
                replacementType = infoProperty.PropertyType.Name;
            }
        }

        public void Undo()
        {
            if (infoField != null)
            {
                infoField.SetValue(instance, ReplaceNode.StringToField(infoField.FieldType.Name, value));
            }
            else
            {
                infoProperty.SetValue(instance, ReplaceNode.StringToField(infoProperty.PropertyType.Name, value));
            }
        }
    }
}
