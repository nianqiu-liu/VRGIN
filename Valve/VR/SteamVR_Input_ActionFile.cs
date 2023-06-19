using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace Valve.VR
{
    [Serializable]
    public class SteamVR_Input_ActionFile
    {
        public List<SteamVR_Input_ActionFile_Action> actions = new List<SteamVR_Input_ActionFile_Action>();

        public List<SteamVR_Input_ActionFile_ActionSet> action_sets = new List<SteamVR_Input_ActionFile_ActionSet>();

        public List<SteamVR_Input_ActionFile_DefaultBinding> default_bindings = new List<SteamVR_Input_ActionFile_DefaultBinding>();

        public List<Dictionary<string, string>> localization = new List<Dictionary<string, string>>();

        [JsonIgnore] public string filePath;

        [JsonIgnore] public List<SteamVR_Input_ActionFile_LocalizationItem> localizationHelperList = new List<SteamVR_Input_ActionFile_LocalizationItem>();

        private const string findString_appKeyStart = "\"app_key\"";

        private const string findString_appKeyEnd = "\",";

        public void InitializeHelperLists()
        {
            foreach (var actionset in action_sets)
            {
                actionset.actionsInList = new List<SteamVR_Input_ActionFile_Action>(actions.Where((SteamVR_Input_ActionFile_Action action) =>
                                                                                                      action.path.StartsWith(actionset.name) &&
                                                                                                      SteamVR_Input_ActionFile_ActionTypes.listIn.Contains(action.type)));
                actionset.actionsOutList = new List<SteamVR_Input_ActionFile_Action>(actions.Where((SteamVR_Input_ActionFile_Action action) =>
                                                                                                       action.path.StartsWith(actionset.name) &&
                                                                                                       SteamVR_Input_ActionFile_ActionTypes.listOut.Contains(action.type)));
                actionset.actionsList = new List<SteamVR_Input_ActionFile_Action>(actions.Where((SteamVR_Input_ActionFile_Action action) => action.path.StartsWith(actionset.name)));
            }

            foreach (var item in localization) localizationHelperList.Add(new SteamVR_Input_ActionFile_LocalizationItem(item));
        }

        public void SaveHelperLists()
        {
            foreach (var action_set in action_sets)
            {
                action_set.actionsList.Clear();
                action_set.actionsList.AddRange(action_set.actionsInList);
                action_set.actionsList.AddRange(action_set.actionsOutList);
            }

            actions.Clear();
            foreach (var action_set2 in action_sets)
            {
                actions.AddRange(action_set2.actionsInList);
                actions.AddRange(action_set2.actionsOutList);
            }

            localization.Clear();
            foreach (var localizationHelper in localizationHelperList)
            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("language_tag", localizationHelper.language);
                foreach (var item in localizationHelper.items) dictionary.Add(item.Key, item.Value);
                localization.Add(dictionary);
            }
        }

        public static string GetShortName(string name)
        {
            var text = name;
            var num = text.LastIndexOf('/');
            if (num != -1)
            {
                if (num == text.Length - 1)
                {
                    text = text.Remove(num);
                    num = text.LastIndexOf('/');
                    if (num == -1) return GetCodeFriendlyName(text);
                }

                return GetCodeFriendlyName(text.Substring(num + 1));
            }

            return GetCodeFriendlyName(text);
        }

        public static string GetCodeFriendlyName(string name)
        {
            name = name.Replace('/', '_').Replace(' ', '_');
            if (!char.IsLetter(name[0])) name = "_" + name;
            for (var i = 0; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                {
                    name = name.Remove(i, 1);
                    name = name.Insert(i, "_");
                }
            }

            return name;
        }

        public string[] GetFilesToCopy(bool throwErrors = false)
        {
            var list = new List<string>();
            var fullName = new FileInfo(filePath).Directory.FullName;
            list.Add(filePath);
            foreach (var default_binding in default_bindings)
            {
                var text = Path.Combine(fullName, default_binding.binding_url);
                if (File.Exists(text))
                    list.Add(text);
                else if (throwErrors) Debug.LogError("<b>[SteamVR]</b> Could not bind binding file specified by the actions.json manifest: " + text);
            }

            return list.ToArray();
        }

        public void CopyFilesToPath(string toPath, bool overwrite)
        {
            var filesToCopy = SteamVR_Input.actionFile.GetFilesToCopy(false);
            foreach (var text in filesToCopy)
            {
                var fileInfo = new FileInfo(text);
                var text2 = Path.Combine(toPath, fileInfo.Name);
                var flag = false;
                if (File.Exists(text2)) flag = true;
                if (flag)
                {
                    if (overwrite)
                    {
                        var fileInfo2 = new FileInfo(text2);
                        fileInfo2.IsReadOnly = false;
                        fileInfo2.Delete();
                        File.Copy(text, text2);
                        RemoveAppKey(text2);
                        Debug.Log("<b>[SteamVR]</b> Copied (overwrote) SteamVR Input file at path: " + text2);
                    }
                    else
                        Debug.Log("<b>[SteamVR]</b> Skipped writing existing file at path: " + text2);
                }
                else
                {
                    File.Copy(text, text2);
                    RemoveAppKey(text2);
                    Debug.Log("<b>[SteamVR]</b> Copied SteamVR Input file to folder: " + text2);
                }
            }
        }

        private static void RemoveAppKey(string newFilePath)
        {
            if (!File.Exists(newFilePath)) return;
            var text = File.ReadAllText(newFilePath);
            var value = "\"app_key\"";
            var num = text.IndexOf(value);
            if (num != -1)
            {
                var num2 = text.IndexOf("\",", num);
                if (num2 != -1)
                {
                    num2 += "\",".Length;
                    var count = num2 - num;
                    var contents = text.Remove(num, count);
                    new FileInfo(newFilePath).IsReadOnly = false;
                    File.WriteAllText(newFilePath, contents);
                }
            }
        }

        public static SteamVR_Input_ActionFile Open(string path)
        {
            if (File.Exists(path))
            {
                var steamVR_Input_ActionFile = JsonConvert.DeserializeObject<SteamVR_Input_ActionFile>(File.ReadAllText(path));
                steamVR_Input_ActionFile.filePath = path;
                steamVR_Input_ActionFile.InitializeHelperLists();
                return steamVR_Input_ActionFile;
            }

            return null;
        }

        public void Save(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists) fileInfo.IsReadOnly = false;
            var contents = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            File.WriteAllText(path, contents);
        }
    }
}
