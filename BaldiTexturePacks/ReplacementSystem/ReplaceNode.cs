using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
            TexturePacksPlugin.Log.LogInfo("targ");
            TexturePacksPlugin.Log.LogInfo(target);
            List<Replacement> undos = new List<Replacement>();
            if (children.Length > 0) throw new NotImplementedException("Non-Component objects having children is NOT supported atm!");
            object toReplace;
            if (AccessTools.GetFieldNames(target.GetType()).Contains(name))
            {
                toReplace = AccessTools.Field(target.GetType(), name).GetValue(target);
            }
            else
            {
                toReplace = AccessTools.Property(target.GetType(), name).GetValue(target);
            }
            TexturePacksPlugin.Log.LogInfo("replace");
            TexturePacksPlugin.Log.LogInfo(toReplace);

            foreach (KeyValuePair<string, string> replacement in replacements)
            {
                if (!TexturePacksPlugin.validFieldChanges.ContainsKey(toReplace.GetType()))
                {
                    TexturePacksPlugin.Log.LogWarning("Attempted to change property on invalid type: " + toReplace.GetType().Name + "!");
                    continue;
                }
                if (!TexturePacksPlugin.validFieldChanges[toReplace.GetType()].Contains(replacement.Key))
                {
                    TexturePacksPlugin.Log.LogWarning("Attempted to change " + replacement.Key + " on " + toReplace.GetType() + " which is a field not on the whitelist!");
                    continue;
                }
                Replacement rp = new Replacement(toReplace, replacement.Key);
                undos.Add(rp);
                rp.SetValue(StringToField(rp.replacementType, replacement.Value));
            }

            return undos.ToArray();
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
                    return ReplaceNonGameObject(rootObject);
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
                    if (validToSearch.ContainsKey(nameParts[0]))
                    {
                        componentsToApplyReplacementsTo.AddRange(ScanForName(validToSearch[nameParts[0]].ToArray(), nameParts[1]));
                    }
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
                    if (TexturePacksPlugin.typesThatMustBeValidMovables.Contains(comp.GetType()))
                    {
                        if (!TexturePacksPlugin.validMovableComponents.Contains(comp)) continue;
                    }
                    if (!TexturePacksPlugin.validFieldChanges[comp.GetType()].Contains(replacement.Key))
                    {
                        TexturePacksPlugin.Log.LogWarning("Attempted to change " + replacement.Key + " on " + comp.GetType() + " which is a field not on the whitelist!");
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
            if (obj.GetType().IsValueType)
            {
                return obj.ToString();
            }
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
                case "Vector3":
                    Vector3 vec3 = (Vector3)obj;
                    return FieldToString(vec3.x) + " " + FieldToString(vec3.y) + " " + FieldToString(vec3.z);
                case "Quaternion":
                    Quaternion qat = (Quaternion)obj;
                    return FieldToString(qat.eulerAngles);
                case "Vector2":
                    Vector2 vec2 = (Vector2)obj;
                    return FieldToString(vec2.x) + " " + FieldToString(vec2.y);
                case "System.String":
                    return (string)obj;
                default:
                    throw new NotImplementedException("Unknown primative type: " + obj.GetType().Name);
            }
        }

        public static object StringToField(Type type, string str)
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type, str);
            }
            switch (type.Name)
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
                case "Vector3":
                    string[] splitv3 = str.Split(' ');
                    return new Vector3(float.Parse(splitv3[0]), float.Parse(splitv3[1]), float.Parse(splitv3[2]));
                case "Vector2":
                    string[] splitv2 = str.Split(' ');
                    return new Vector3(float.Parse(splitv2[0]), float.Parse(splitv2[1]));
                case "String":
                    return str;
                case "Quaternion":
                    return StringToField(typeof(Vector3), str);
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
        public Type replacementType;

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
                replacementType = infoField.FieldType;
            }
            else
            {
                infoProperty = AccessTools.Property(instance.GetType(), field);
                value = ReplaceNode.FieldToString(infoProperty.GetValue(instance));
                replacementType = infoProperty.PropertyType;
            }
        }

        public void Undo()
        {
            if (infoField != null)
            {
                infoField.SetValue(instance, ReplaceNode.StringToField(infoField.FieldType, value));
            }
            else
            {
                infoProperty.SetValue(instance, ReplaceNode.StringToField(infoProperty.PropertyType, value));
            }
        }
    }
}
