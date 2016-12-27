using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace CognitiveVR
{
    public class CognitiveVR_SceneKeyConfigurationWindow : EditorWindow
    {
        public static void Init()
        {
            CognitiveVR_SceneKeyConfigurationWindow window = (CognitiveVR_SceneKeyConfigurationWindow)EditorWindow.GetWindow(typeof(CognitiveVR_SceneKeyConfigurationWindow));
            window.Show();
        }

        private static void ReadNames()
        {
            //save these to a temp list
            List<CognitiveVR_Preferences.SceneKeySetting> oldSettings = new List<CognitiveVR_Preferences.SceneKeySetting>();
            foreach (var v in CognitiveVR_Preferences.Instance.SceneKeySettings)
            {
                oldSettings.Add(v);
            }


            //clear then rebuild the list in preferences
            CognitiveVR_Preferences.Instance.SceneKeySettings.Clear();

            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                CognitiveVR_Preferences.Instance.SceneKeySettings.Add(new CognitiveVR_Preferences.SceneKeySetting(name, path));
            }

            //match up dictionary keys from temp list
            foreach (var oldSetting in oldSettings)
            {
                foreach (var newSetting in CognitiveVR_Preferences.Instance.SceneKeySettings)
                {
                    if (newSetting.SceneName == oldSetting.SceneName)
                    {
                        newSetting.SceneKey = oldSetting.SceneKey;
                        newSetting.Track = oldSetting.Track;
                        newSetting.LastRevision = oldSetting.LastRevision;
                        newSetting.SceneName = oldSetting.SceneName;
                        newSetting.ScenePath = oldSetting.ScenePath;
                    }
                }
            }
        }

        int toggleWidth = 60;
        int sceneWidth = 140;
        int keyWidth = 400;

        Vector2 canvasPos;
        bool loadedScenes;
        string searchString = "";
        void OnGUI()
        {
            CognitiveVR_Preferences prefs = CognitiveVR_Settings.GetPreferences();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Scenes"))
            {
                loadedScenes = false;
                //scenes = null;
            }

            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(prefs);
                AssetDatabase.SaveAssets();
            }

            GUILayout.EndHorizontal();
            searchString = EditorGUILayout.TextField("Search", searchString);

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            if (!loadedScenes)
            {
                ReadNames();
                loadedScenes = true;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Record", GUILayout.Width(toggleWidth));
            GUILayout.Label("Scene Name", GUILayout.Width(sceneWidth));
            GUILayout.Label("SceneID", GUILayout.Width(keyWidth));
            GUILayout.EndHorizontal();

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            canvasPos = GUILayout.BeginScrollView(canvasPos);

            foreach (var v in prefs.SceneKeySettings)
            {
                if (!string.IsNullOrEmpty(searchString) && !v.SceneName.ToLower().Contains(searchString.ToLower())) { continue; }
                DisplaySceneKeySettings(v);
            }

            GUILayout.EndScrollView();
        }

        void DisplaySceneKeySettings(CognitiveVR_Preferences.SceneKeySetting settings)
        {
            GUILayout.BeginHorizontal();

            settings.Track = GUILayout.Toggle(settings.Track, "", GUILayout.Width(toggleWidth));
            GUILayout.Label(settings.SceneName, GUILayout.Width(sceneWidth));

            string startSceneName = settings.SceneKey;

            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(settings.SceneKey))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                EditorGUILayout.TextField("a12345b6-78c9-01d2-3456-78e9f0ghi123", style);
            }
            else
            {
                settings.SceneKey = EditorGUILayout.TextField(settings.SceneKey, GUILayout.Width(keyWidth));
            }

            if (!string.IsNullOrEmpty(settings.SceneKey) && string.IsNullOrEmpty(startSceneName))
            {
                //new key!
                settings.Track = true;
            }

            if (settings.Track)
            {
                bool validKey = KeyIsValid(settings.SceneKey);

                if (!validKey)
                {
                    if (settings.SceneKey.Contains("http://sceneexplorer.com/scene/"))
                    {
                        settings.SceneKey = settings.SceneKey.Replace("http://sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }
                    if (settings.SceneKey.Contains("https://sceneexplorer.com/scene/"))
                    {
                        settings.SceneKey = settings.SceneKey.Replace("https://sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }
                    else if (settings.SceneKey.Contains("sceneexplorer.com/scene/"))
                    {
                        settings.SceneKey = settings.SceneKey.Replace("sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }

                    GUI.color = Color.red;
                    GUILayout.Button(new GUIContent("!", "ID is invalid! Should be format:\na12345b6-78c9-01d2-3456-78e9f0ghi123"), GUILayout.Width(14), GUILayout.Height(14));
                    GUI.color = Color.white;
                }
            }
#if UNITY_EDITOR_OSX
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button(new GUIContent("Export Scene", "Exporting scenes is not available on Mac at this time"));
            EditorGUI.EndDisabledGroup();
#else
            if (GUILayout.Button(new GUIContent("Export Scene", "Load this scene and begin exporting with current export settings")))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(settings.ScenePath);
                var prefs = CognitiveVR_Settings.GetPreferences();
                CognitiveVR.CognitiveVR_SceneExportWindow.ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality);
            }
#endif
            GUILayout.EndHorizontal();
        }

        bool KeyIsValid(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }

            //a12345b6-78c9-01d2-3456-78e9f0ghi123

            string pattern = @"[A-Za-z0-9\-+]{" + key.Length + "}";
            bool regexPass = System.Text.RegularExpressions.Regex.IsMatch(key, pattern);
            return regexPass;
        }
    }
}