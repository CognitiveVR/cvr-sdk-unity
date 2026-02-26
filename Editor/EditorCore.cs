using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEditor.SceneManagement;

//contains functions for button/label styles
//references to editor prefs
//set editor define symbols
//check for sdk updates
//pre/post build inferfaces

namespace Cognitive3D
{
    public enum DisplayKey
    {
        FullName,
        ShortName,
        ViewerName,
        Other,
        ManagerName,
        GatewayURL,
        DashboardURL,
        ViewerURL,
        DocumentationURL,
    }

    [InitializeOnLoad]
    internal class EditorCore
    {
        static EditorCore()
        {
            //check sdk versions
            CheckForUpdates();

            string savedSDKVersion = EditorPrefs.GetString("cognitive_sdk_version", "");
            if (string.IsNullOrEmpty(savedSDKVersion) || Cognitive3D_Manager.SDK_VERSION != savedSDKVersion)
            {
                Debug.Log("Cognitive3D SDK version " + Cognitive3D_Manager.SDK_VERSION);
                EditorPrefs.SetString("cognitive_sdk_version", Cognitive3D_Manager.SDK_VERSION);
            }

            if (!Cognitive3D_Preferences.Instance.EditorHasDisplayedPopup && !EditorCore.HasC3DDefine())
            {
                Cognitive3D_Preferences.Instance.EditorHasDisplayedPopup = true;
                EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorApplication.update += UpdateInitWizard;
            }

            if (Cognitive3D_Preferences.Instance.LocalStorage && Cognitive3D_Preferences.Instance.UploadCacheOnEndPlay)
            {
                EditorApplication.playModeStateChanged -= ModeChanged;
                EditorApplication.playModeStateChanged += ModeChanged;
            }

            EditorUtils.Init();
        }

        //there's some new bug in 2021.1.15ish. creating editor window in constructor gets BaseLiveReloadAssetTracker. delay to avoid that
        static int initDelay = 4;
        static void UpdateInitWizard()
        {
            if (initDelay > 0) { initDelay--; return; }
            EditorApplication.update -= UpdateInitWizard;
            ProjectSetupWindow.Init();
        }

        static void ModeChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredEditMode)
            {
                if (Cognitive3D_Preferences.Instance.LocalStorage && Cognitive3D_Preferences.Instance.UploadCacheOnEndPlay)
                    EditorApplication.update += DelayUploadCache;
                EditorApplication.playModeStateChanged -= ModeChanged;
                uploadDelayFrames = 10;
            }
        }

        static int uploadDelayFrames = 0;
        private static void DelayUploadCache()
        {
            uploadDelayFrames--;
            if (uploadDelayFrames < 0)
            {
                EditorApplication.update -= DelayUploadCache;
                ICache ic = new DualFileCache(Application.persistentDataPath + "/c3dlocal/");
                if (ic.HasContent())
                    new EditorDataUploader(ic);
            }
        }

        public static DynamicObjectIdPool[] _cachedPoolAssets;
        /// <summary>
        /// search the project database and return an array of all DynamicObjectPool assets
        /// </summary>
        public static DynamicObjectIdPool[] GetDynamicObjectPoolAssets
        {
            get
            {
                if (_cachedPoolAssets == null)
                {
                    _cachedPoolAssets = new DynamicObjectIdPool[0];
                    string[] guids = AssetDatabase.FindAssets("t:dynamicobjectidpool");
                    foreach (var guid in guids)
                    {
                        ArrayUtility.Add<DynamicObjectIdPool>(ref _cachedPoolAssets, AssetDatabase.LoadAssetAtPath<DynamicObjectIdPool>(AssetDatabase.GUIDToAssetPath(guid)));
                    }
                }
                return _cachedPoolAssets;
            }
        }

        public static bool IsDeveloperKeyValid
        {
            get
            {
                return EditorPrefs.HasKey("c3d_developerkey") && !string.IsNullOrEmpty(EditorPrefs.GetString("c3d_developerkey"));
            }
        }
        public static string DeveloperKey
        {
            get
            {
                return EditorPrefs.GetString("c3d_developerkey");
            }
            internal set
            {
                EditorPrefs.SetString("c3d_developerkey", value);
            }
        }

        public static List<string> GetPlayerDefines()
        {
            string s = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            string[] ExistingSymbols = s.Split(';');
            return new List<string>(ExistingSymbols);
        }

        public static void AddDefine(string symbol)
        {
            var defines = GetPlayerDefines();
            if (defines.Contains(symbol))
            {
                return;
            }
            defines.Add(symbol);

            //rebuild symbols
            string alldefines = "";
            for (int i = 0; i < defines.Count; i++)
            {
                if (!string.IsNullOrEmpty(defines[i]))
                {
                    alldefines += defines[i] + ";";
                }
            }
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), alldefines);
        }

        public static void RemoveDefine(string symbol)
        {
            string s = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            string[] ExistingSymbols = s.Split(';');
            List<string> finalDefines = new List<string>();
            for(int i = 0; i<ExistingSymbols.Length;i++)
            {
                if (ExistingSymbols[i] != symbol)
                {
                    finalDefines.Add(ExistingSymbols[i]);
                }
            }

            //rebuild symbols
            string alldefines = "";
            for (int i = 0; i < finalDefines.Count; i++)
            {
                if (!string.IsNullOrEmpty(finalDefines[i]))
                {
                    alldefines += finalDefines[i] + ";";
                }
            }
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), alldefines);
        }

        public static bool HasC3DDefine()
        {
            List<string> ExistingSymbols = GetPlayerDefines();

            foreach (var v in ExistingSymbols)
            {
                if (v.StartsWith("C3D_"))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasC3DDefine(out List<string> C3DSymbols)
        {
            List<string> ExistingSymbols = GetPlayerDefines();
            C3DSymbols = new List<string>();

            foreach (var v in ExistingSymbols)
            {
                if (v.StartsWith("C3D_"))
                {
                    C3DSymbols.Add(v);
                    return true;
                }
            }
            return false;
        }

        public static void SetPlayerDefine(List<string> C3DSymbols)
        {
            //get all scripting define symbols
            string s = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            string[] ExistingSymbols = s.Split(';');

            //categorizing definition symbols
            List<string> ExistingNonC3DSymbols = new List<string>();
            foreach (var v in ExistingSymbols)
            {
                if (!v.StartsWith("C3D_"))
                {
                    ExistingNonC3DSymbols.Add(v);
                }
            }

            //combine symbols
            List<string> finalDefines = new List<string>();
            foreach (var v in ExistingNonC3DSymbols)
                finalDefines.Add(v);
            foreach (var v in C3DSymbols)
                finalDefines.Add(v);

            //rebuild symbols
            string alldefines = "";
            for (int i = 0; i < finalDefines.Count; i++)
            {
                if (!string.IsNullOrEmpty(finalDefines[i]))
                {
                    alldefines += finalDefines[i] + ";";
                }
            }
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), alldefines);
        }

        public static void SetPlayerDefine(string C3DSymbol)
        {
            //get all scripting define symbols
            string s = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            string[] ExistingSymbols = s.Split(';');

            //categorizing definition symbols
            List<string> ExistingNonC3DSymbols = new List<string>();
            foreach (var v in ExistingSymbols)
            {
                if (!v.StartsWith("C3D_"))
                {
                    ExistingNonC3DSymbols.Add(v);
                }
            }

            //combine symbols
            List<string> finalDefines = new List<string>();
            foreach (var v in ExistingNonC3DSymbols)
                finalDefines.Add(v);
            
            // Add C3D define
            finalDefines.Add(C3DSymbol);

            //rebuild symbols
            string alldefines = "";
            for (int i = 0; i < finalDefines.Count; i++)
            {
                if (!string.IsNullOrEmpty(finalDefines[i]))
                {
                    alldefines += finalDefines[i] + ";";
                }
            }

            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), alldefines);
        }

        /// <summary>
        /// Creates a new Cognitive3D_Preferences asset at the specified path and returns it.
        /// If the path is invalid or empty, returns the existing preferences.
        /// </summary>
        public static Cognitive3D_Preferences CreatePreferences(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var newAsset = ScriptableObject.CreateInstance<Cognitive3D_Preferences>();
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = newAsset;
                return newAsset;
            }

            return GetPreferences();
        }

        /// <summary>
        /// Creates a copy of an existing Cognitive3D_Preferences asset and saves it at the specified path.
        /// If the path is invalid, returns the existing preferences.
        /// </summary>
        public static Cognitive3D_Preferences CopyPreferences(string path, Cognitive3D_Preferences preferencesToCopy)
        {
            if (!string.IsNullOrEmpty(path))
            {
                // Duplicate the asset
                string originalPath = AssetDatabase.GetAssetPath(preferencesToCopy);
                AssetDatabase.CopyAsset(originalPath, path);
                AssetDatabase.Refresh();

                // Load and set the new copied asset
                var copiedPrefs = AssetDatabase.LoadAssetAtPath<Cognitive3D_Preferences>(path);
                if (copiedPrefs != null)
                {
                    return copiedPrefs;
                }
            }

            return GetPreferences();
        }

        /// <summary>
        /// Sets the current active Cognitive3D_Preferences instance used by the editor.
        /// </summary>
        internal static void SetPreferences(Cognitive3D_Preferences newPreferences)
        {
            _prefs = newPreferences;
        }

        /// <summary>
        /// Copies all data from the given preferences object into the main preferences asset in the Resources folder.
        /// If the main asset does not exist, it will be created.
        /// </summary>
        public static void SaveToPreference(Cognitive3D_Preferences newPref)
        {
            var mainPrefs = Resources.Load<Cognitive3D_Preferences>("Cognitive3D_Preferences");
            if (mainPrefs == null)
            {
                mainPrefs = ScriptableObject.CreateInstance<Cognitive3D_Preferences>();
                string filepath = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(filepath))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                AssetDatabase.CreateAsset(mainPrefs, $"{filepath}/Cognitive3D_Preferences.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (mainPrefs != null)
            {
                // Save the original name to restore after copying
                string originalName = mainPrefs.name;

                // Copy serialized data
                EditorUtility.CopySerialized(newPref, mainPrefs);

                // Restore the original name to avoid asset mismatch warnings
                mainPrefs.name = originalName;

                EditorUtility.SetDirty(mainPrefs);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("Applied current preferences to main asset.");
            }
            else
            {
                Debug.LogError("Failed to load or create main preferences asset.");
            }
        }

        static Cognitive3D_Preferences _prefs;
        /// <summary>
        /// Gets the Cognitive3D_preferences or creates and returns new default preferences
        /// </summary>
        public static Cognitive3D_Preferences GetPreferences()
        {
            if (_prefs == null)
            {
                _prefs = Resources.Load<Cognitive3D_Preferences>("Cognitive3D_Preferences");
                if (_prefs == null)
                {
                    _prefs = ScriptableObject.CreateInstance<Cognitive3D_Preferences>();
                    string filepath = "Assets/Resources";
                    if (!AssetDatabase.IsValidFolder(filepath))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    AssetDatabase.CreateAsset(_prefs, filepath + System.IO.Path.DirectorySeparatorChar + "Cognitive3D_Preferences.asset");
                    EditorUtility.SetDirty(EditorCore.GetPreferences());
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            return _prefs;
        }

        /// <summary>
        /// used to search through project directories to find cognitive resources
        /// </summary>
        public static bool RecursiveDirectorySearch(string directory, out string filepath, string searchDir)
        {
            if (directory.EndsWith(searchDir))
            {
                filepath = "Assets" + directory.Substring(Application.dataPath.Length);
                return true;
            }
            foreach (var dir in Directory.GetDirectories(Path.Combine(Application.dataPath, directory)))
            {
                RecursiveDirectorySearch(dir, out filepath, searchDir);
                if (filepath != "") { return true; }
            }
            filepath = "";
            return false;
        }

        /// <summary>
        /// return a list of all scene names and paths from the project
        /// </summary>
        /// <param name="names"></param>
        /// <param name="paths"></param>
        public static void GetAllScenes(List<string> names, List<string> paths)
        {
            string[] guidList = AssetDatabase.FindAssets("t:scene");
            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);
                names.Add(name);
                paths.Add(path);
            }
        }

        internal static System.Action RefreshSceneVersionComplete;
        /// <summary>
        /// make a get request for all scene versions of this scene
        /// </summary>
        /// <param name="refreshSceneVersionComplete"></param>
        public static void RefreshSceneVersion(Action refreshSceneVersionComplete)
        {
            //Debug.Log("refresh scene version");
            //gets the scene version from api and sets it to the current scene
            string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScenePath);
            if (currentSettings != null)
            {
                if (!IsDeveloperKeyValid) { Debug.Log("Developer key invalid"); return; }
                if (currentSettings == null)
                {
                    Debug.Log("SendSceneVersionRequest no scene settings!");
                    return;
                }
                if (string.IsNullOrEmpty(currentSettings.SceneId))
                {
                    if (refreshSceneVersionComplete != null)
                        refreshSceneVersionComplete.Invoke();
                    return;
                }

                RefreshSceneVersionComplete = refreshSceneVersionComplete;
                string url = CognitiveStatics.GetScenes();
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "APIKEY:DEVELOPER " + DeveloperKey);
                EditorNetwork.Get(url, GetSceneVersionResponse, headers, true, "Get Scene Version");//AUTH
            }
            else
            {
                Debug.Log("No scene versions for scene: " + currentScenePath);
            }
        }

        /// <summary>
        /// Sends version refresh requests for all Cognitive3D setting scenes.  
        /// Handles each request asynchronously and tracks completion using a counter.  
        /// Skips scenes with invalid developer keys or missing scene IDs.
        /// </summary>
        /// <param name="refreshSceneVersionComplete">Callback invoked after all valid version requests complete.</param>
        public static void RefreshAllScenesVersion(Action refreshSceneVersionComplete)
        {
            if (Cognitive3D_Preferences.Instance.sceneSettings.Count == 0)
            {
                refreshSceneVersionComplete?.Invoke();
                return;
            }

            if (!IsDeveloperKeyValid)
            {
                Debug.Log("Developer key invalid");
                refreshSceneVersionComplete?.Invoke();
                return;
            }

            // Make a single API call to get all scenes (same as RefreshSceneVersion)
            string url = CognitiveStatics.GetScenes();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "APIKEY:DEVELOPER " + DeveloperKey);

            EditorNetwork.Get(url, (responseCode, error, text) =>
            {
                GetAllScenesVersionResponse(responseCode, error, text);
                refreshSceneVersionComplete?.Invoke();
            }, headers, true, "Get All Scene Versions");
        }

        /// <summary>
        /// Handles the response from getting all scenes and updates all scene settings in preferences.
        /// </summary>
        private static void GetAllScenesVersionResponse(int responseCode, string error, string text)
        {
            if (responseCode != 200)
            {
                Debug.LogError("GetAllScenesVersionResponse [CODE] " + responseCode + " [ERROR] " + error);
                EditorUtility.DisplayDialog("Error Getting Scene Versions",
                    "There was an error getting scene data from the Cognitive3D Dashboard. Response code was " + responseCode + ".\n\nSee Console for more details", "Ok");
                return;
            }

            // Parse the response containing all scenes (same format as RefreshSceneVersion)
            var wrappedJson = "{\"scenes\":" + text + "}";
            var collection = JsonUtility.FromJson<ScenesCollectionList>(wrappedJson);

            if (collection == null || collection.scenes == null)
            {
                Debug.LogWarning("Failed to parse scenes collection from response");
                return;
            }

            // Update each scene setting with its latest version
            bool hasUpdates = false;
            foreach (var sceneSetting in Cognitive3D_Preferences.Instance.sceneSettings)
            {
                if (sceneSetting == null || string.IsNullOrEmpty(sceneSetting.SceneId))
                    continue;

                // Find matching scene in the response
                foreach (var sceneCollection in collection.scenes)
                {
                    if (sceneCollection.sdkFacingId == sceneSetting.SceneId)
                    {
                        var latestVersion = sceneCollection.GetLatestVersion();
                        if (latestVersion != null)
                        {
                            sceneSetting.VersionId = latestVersion.id;
                            sceneSetting.VersionNumber = latestVersion.versionNumber;
                            sceneSetting.backendSceneId = sceneCollection.id;
                            hasUpdates = true;
                        }
                        break;
                    }
                }
            }

            if (hasUpdates)
            {
                EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Fetches scene data from the API and updates only the scenes that already exist in preferences.
        /// Matches by SceneId and VersionNumber, then fills in missing fields such as VersionId.
        /// Does not add or remove any scene settings entries.
        /// </summary>
        /// <param name="callback">Callback invoked after the request completes.</param>
        public static void UpdateExistingSceneVersions(EditorNetwork.Response callback)
        {
            if (Cognitive3D_Preferences.Instance.sceneSettings.Count == 0)
            {
                // No API call needed. Treat as success so buttons remain usable
                callback?.Invoke(200, null, null);
                return;
            }

            if (!IsDeveloperKeyValid)
            {
                Debug.Log("Developer key invalid");
                callback?.Invoke(0, "Developer key invalid", null);
                return;
            }

            string url = CognitiveStatics.GetScenes();
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", "APIKEY:DEVELOPER " + DeveloperKey }
            };

            EditorNetwork.Get(url, callback, headers, true, "Update Existing Scene Versions");
        }

        internal static void GetSceneVersionResponse(int responsecode, string error, string text)
        {
            if (responsecode != 200)
            {
                RefreshSceneVersionComplete = null;
                Debug.LogError("GetSettingsResponse [CODE] " + responsecode + " [ERROR] " + error);
                EditorUtility.DisplayDialog("Error Getting Scene Version", "There was an error getting data about this scene from the Cognitive3D Dashboard. Response code was " + responsecode + ".\n\nSee Console for more details", "Ok");
                return;
            }
            var settings = Cognitive3D_Preferences.FindCurrentScene();
            if (settings == null)
            {
                //this should be impossible, but might happen if changing scenes at exact time
                RefreshSceneVersionComplete = null;
                Debug.LogError("Scene version request returned 200, but current scene cannot be found");
                return;
            }

            //receive and apply scene version data to preferences
            var wrappedJson = $"{{\"scenes\":{text}}}";
            var collection = JsonUtility.FromJson<ScenesCollectionList>(wrappedJson);
            if (collection != null)
            {
                foreach (var sceneCollection in collection.scenes)
                {
                    if (sceneCollection.sdkFacingId == settings.SceneId)
                    {
                        settings.VersionId = sceneCollection.GetLatestVersion().id;
                        settings.VersionNumber = sceneCollection.GetLatestVersion().versionNumber;
                        EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
                        AssetDatabase.SaveAssets();
                        break;
                    }
                }
            }
            if (RefreshSceneVersionComplete != null)
            {
                RefreshSceneVersionComplete.Invoke();
            }
        }

        #region GUI
        internal class Styles
        {
            private const float SmallIconSize = 16.0f;
            private const float MediumButtonWidth = 64.0f;
            private const float LargeButtonWidth = 90.0f;
            private const float IconButtonWidth = 30.0f;
            internal const float GroupSelectionWidth = 244.0f;
            internal const float LabelWidth = 96f;
            internal const float TitleLabelWidth = 196f;
            private const float IconSize = 16f;

            internal readonly GUIStyle DetailContainer = new GUIStyle
            {
                padding = new RectOffset(10, 10, 5, 5)
            };

            internal readonly GUIStyle FeatureButtonTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };

            internal readonly GUIStyle FeatureButtonDescription = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
            };

            internal readonly GUIStyle FeatureTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(0, 5, 0, 5)
            };

            internal readonly GUIStyle ContextPadding = new GUIStyle
            {
                padding = new RectOffset(10, 10, 5, 3),
                margin = new RectOffset(4, 4, 4, 5)
            };

            internal readonly GUIStyle DescriptionPadding = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                padding = new RectOffset(15, 15, 5, 5)
            };

            internal readonly GUIStyle HelpBoxPadding = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(15, 15, 5, 5)
            };

            internal readonly GUIStyle HelpBoxLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true,
                margin = new RectOffset(0, 0, 15, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            internal readonly GUIStyle ExternalLink = new GUIStyle(GUI.skin.label)
            {
                fixedWidth = 18,
                fixedHeight = 18,
                margin = new RectOffset(0, 0, 12, 4)
            };

            internal readonly GUIStyle ListBoxPadding = new GUIStyle(GUI.skin.box)
            {
                wordWrap = true,
                margin = new RectOffset(15, 15, 5, 5)
            };

            internal readonly GUIStyle ListBoxPadding2 = new GUIStyle(GUI.skin.scrollView)
            {
                wordWrap = true,
                margin = new RectOffset(15, 15, 5, 5)
            };

            internal readonly GUIStyle LeftPaddingBoldLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                padding = new RectOffset(15, 0, 0, 0)
            };

            internal readonly GUIStyle LeftPaddingLabel = new GUIStyle(GUI.skin.label)
            {
                padding = new RectOffset(15, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
            };


            internal readonly GUIStyle FeatureButton = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 100,
                fontSize = 30,
                alignment = TextAnchor.MiddleLeft,
                imagePosition = ImagePosition.ImageLeft,
                margin = new RectOffset(5, 5, 5, 5)
            };

            internal readonly GUIStyle FeatureSmallButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 30
            };

            internal readonly GUIStyle CompleteIcon = new GUIStyle
            {
                fixedWidth = 18,
                fixedHeight = 18,
                margin = new RectOffset(0, 0, 2, 2),
                padding = new RectOffset(2, 0, 2, 2),
            };

            internal readonly GUIStyle IncompleteIcon = new GUIStyle
            {
                fixedWidth = 18,
                fixedHeight = 18,
                margin = new RectOffset(0, 0, 2, 2),
                padding = new RectOffset(0, 0, 0, 0),
            };

            internal readonly GUIStyle CenteredLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };

            internal readonly GUIStyle CodeSnippet = new GUIStyle(WizardGUISkin.GetStyle("code_snippet"));

            internal readonly GUIStyle ListLabel = new GUIStyle("TV Selection")
            {
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(5, 5, 5, 3),
                margin = new RectOffset(4, 4, 4, 5)
            };

            internal readonly GUIStyle IssuesTitleBoldLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                wordWrap = false,
                stretchWidth = false,
                fontStyle = FontStyle.Bold,
            };

            internal readonly GUIStyle IssuesTitleLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                wordWrap = false,
                stretchWidth = false,
                padding = new RectOffset(10, 10, 0, 0)
            };

            internal readonly GUIStyle InlinedIconStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = SmallIconSize,
                fixedHeight = SmallIconSize
            };

            internal readonly GUIStyle IconStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(5, 5, 4, 5),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = SmallIconSize,
                fixedHeight = SmallIconSize
            };

            internal readonly GUIStyle MediumButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
                fixedWidth = MediumButtonWidth,
            };

            internal readonly GUIStyle LargeButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
                fixedWidth = LargeButtonWidth,
            };

            internal readonly GUIStyle IconButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 5, 0, 0),
                fixedWidth = IconButtonWidth,
                fixedHeight = 25
            };

            internal readonly GUIStyle InfoButton = new GUIStyle
            {
                padding = new RectOffset(0, 0, 5, 0)
            };

            internal readonly GUIStyle SubtitleHelpText = new GUIStyle(EditorStyles.miniLabel)
            {
                margin = new RectOffset(10, 0, 0, 0),
                wordWrap = true
            };

            internal readonly GUIStyle List = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(10, 10, 10, 10),
                padding = new RectOffset(5, 5, 5, 5),
            };

            internal readonly GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            internal readonly GUIStyle ItemDescription = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
            };
        }

        private static Styles _styles;
        // Delays instantiation of the Styles object until it is first accessed
        public static Styles styles
        {
            get
            {
                if (_styles == null)
                    _styles = new Styles();
                return _styles;
            }
        }

        public static Color GreenButton = new Color(0.4f, 1f, 0.4f);
        public static Color BlueishGrey = new Color32(0xE8, 0xEB, 0xFF, 0xFF);
        public static Color CognitiveBlue = new Color32(98, 180, 243, 255);

        static GUIStyle headerStyle;
        public static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle(EditorStyles.largeLabel);
                    headerStyle.fontSize = 14;
                    headerStyle.alignment = TextAnchor.UpperCenter;
                    headerStyle.fontStyle = FontStyle.Bold;
                    headerStyle.richText = true;
                }
                return headerStyle;
            }
        }

        private static GUISkin _wizardGuiSkin;
        public static GUISkin WizardGUISkin
        {
            get
            {
                if (_wizardGuiSkin == null)
                {
                    _wizardGuiSkin = Resources.Load<GUISkin>("WizardGUISkin");
                }
                return _wizardGuiSkin;
            }
        }

        public static void GUIHorizontalLine(float size = 10)
        {
            GUILayout.Space(size);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(size);
        }

        #endregion

        #region Icons and Textures
        private static Texture2D _logoDone;
        public static Texture2D LogoDone
        {
            get
            {
                if (_logoDone == null)
                    _logoDone = Resources.Load<Texture2D>("Icons/logo-done");
                return _logoDone;
            }
        }

        private static Texture2D _logoWarning;
        public static Texture2D LogoWarning
        {
            get
            {
                if (_logoWarning == null)
                    _logoWarning = Resources.Load<Texture2D>("Icons/logo-warning");
                return _logoWarning;
            }
        }

        private static Texture2D _logoError;
        public static Texture2D LogoError
        {
            get
            {
                if (_logoError == null)
                    _logoError = Resources.Load<Texture2D>("Icons/logo-error");
                return _logoError;
            }
        }

        private static Texture2D _logo;
        public static Texture2D LogoTexture
        {
            get
            {
                if (_logo == null)
                    _logo = Resources.Load<Texture2D>("C3D-Primary-Logo");
                return _logo;
            }
        }

        private static Texture2D _logoIcon;
        public static Texture2D LogoIcon
        {
            get
            {
                if (_logoIcon == null)
                    _logoIcon = Resources.Load<Texture2D>("C3D-Icon");
                return _logoIcon;
            }
        }

        private static Texture2D _logoCheckmark;
        public static Texture2D LogoCheckmark
        {
            get
            {
                if (_logoCheckmark == null)
                    _logoCheckmark = Resources.Load<Texture2D>("logo-checkmark");
                return _logoCheckmark;
            }
        }

        private static Texture2D _sceneGeometryIcon;
        public static Texture2D SceneGeometryIcon
        {
            get
            {
                if (_sceneGeometryIcon == null)
                    _sceneGeometryIcon = Resources.Load<Texture2D>("Icons/scene-geometry-icon");
                return _sceneGeometryIcon;
            }
        }

        private static Texture2D _exploreFeaturesIcon;
        public static Texture2D ExploreFeaturesIcon
        {
            get
            {
                if (_exploreFeaturesIcon == null)
                    _exploreFeaturesIcon = Resources.Load<Texture2D>("Icons/explore-features-icon");
                return _exploreFeaturesIcon;
            }
        }

        private static Texture2D _background;
        public static Texture2D BackgroundTexture
        {
            get
            {
                if (_background == null)
                    _background = Resources.Load<Texture2D>("cognitive3d-background");
                return _background;
            }
        }

        private static Texture2D _plusIcon;
        public static Texture2D PlusIcon
        {
            get
            {
                if (_plusIcon == null)
                    _plusIcon = Resources.Load<Texture2D>("Features/Icons/plus");
                return _plusIcon;
            }
        }

        private static Texture2D _externalLinkIcon;
        public static Texture2D ExternalLinkIcon
        {
            get
            {
                if (_externalLinkIcon == null)
                {
                    _externalLinkIcon = Resources.Load<Texture2D>("Icons/external-link");
                }
                return _externalLinkIcon;
            }
        }

        private static Texture2D _dynamicsIcon;
        public static Texture2D DynamicsIcon
        {
            get
            {
                if (_dynamicsIcon == null)
                    _dynamicsIcon = Resources.Load<Texture2D>("Features/Icons/dynamics");
                return _dynamicsIcon;
            }
        }

        private static Texture2D _customEventIcon;
        public static Texture2D CustomEventIcon
        {
            get
            {
                if (_customEventIcon == null)
                    _customEventIcon = Resources.Load<Texture2D>("Features/Icons/custom-events");
                return _customEventIcon;
            }
        }

        private static Texture2D _sensorIcon;
        public static Texture2D SensorIcon
        {
            get
            {
                if (_sensorIcon == null)
                    _sensorIcon = Resources.Load<Texture2D>("Features/Icons/sensors");
                return _sensorIcon;
            }
        }

        private static Texture2D _exitpollIcon;
        public static Texture2D ExitpollIcon
        {
            get
            {
                if (_exitpollIcon == null)
                    _exitpollIcon = Resources.Load<Texture2D>("Features/Icons/exitpoll");
                return _exitpollIcon;
            }
        }

        private static Texture2D _remoteControlsIcon;
        public static Texture2D RemoteControlsIcon
        {
            get
            {
                if (_remoteControlsIcon == null)
                    _remoteControlsIcon = Resources.Load<Texture2D>("Features/Icons/remote-controls");
                return _remoteControlsIcon;
            }
        }

        private static Texture2D _audioRecordingIcon;
        public static Texture2D AudioRecordingIcon
        {
            get
            {
                if (_audioRecordingIcon == null)
                    _audioRecordingIcon = Resources.Load<Texture2D>("Features/Icons/audio-recording");
                return _audioRecordingIcon;
            }
        }

        private static Texture2D _socialPlatformIcon;
        public static Texture2D SocialPlatformIcon
        {
            get
            {
                if (_socialPlatformIcon == null)
                    _socialPlatformIcon = Resources.Load<Texture2D>("Features/Icons/social-platform");
                return _socialPlatformIcon;
            }
        }

        private static Texture2D _multiplayerIcon;
        public static Texture2D MultiplayerIcon
        {
            get
            {
                if (_multiplayerIcon == null)
                    _multiplayerIcon = Resources.Load<Texture2D>("Features/Icons/multiplayer");
                return _multiplayerIcon;
            }
        }

        private static Texture2D _mediaIcon;
        public static Texture2D MediaIcon
        {
            get
            {
                if (_mediaIcon == null)
                    _mediaIcon = Resources.Load<Texture2D>("Features/Icons/media");
                return _mediaIcon;
            }
        }

        private static Texture2D _completeCheckmark;
        public static Texture2D CompleteCheckmark
        {
            get
            {
                if (_completeCheckmark == null)
                {
                    _completeCheckmark = Resources.Load<Texture2D>("Icons/circle-checkmark");
                }
                return _completeCheckmark;
            }
        }

        private static Texture2D _circleWarning;
        public static Texture2D CircleWarning
        {
            get
            {
                if (_circleWarning == null)
                {
                    _circleWarning = Resources.Load<Texture2D>("Icons/circle-warning");
                }
                return _circleWarning;
            }
        }

        private static Texture2D _circleCheckmark;
        public static Texture2D CircleCheckmark
        {
            get
            {
                if (_circleCheckmark == null)
                {
                    _circleCheckmark = Resources.Load<Texture2D>("Icons/circle check");
                }
                return _circleCheckmark;
            }
        }

        [System.Obsolete("Use EditorCore.CircleCheckmark instead")]
        public static Texture2D Checkmark
        {
            get
            {
                return CircleCheckmark;
            }
        }

        private static Texture2D _emptyCircle;
        public static Texture2D CircleEmpty
        {
            get
            {
                if (_emptyCircle == null)
                {
                    _emptyCircle = Resources.Load<Texture2D>("Icons/circle grey empty");
                }
                return _emptyCircle;
            }
        }

        private static Texture2D _boxCheckmark;
        public static Texture2D BoxCheckmark
        {
            get
            {
                if (_boxCheckmark == null)
                {
                    _boxCheckmark = Resources.Load<Texture2D>("Icons/box check");
                }
                return _boxCheckmark;
            }
        }

        private static Texture2D _boxEmpty;
        public static Texture2D BoxEmpty
        {
            get
            {
                if (_boxEmpty == null)
                {
                    _boxEmpty = Resources.Load<Texture2D>("Icons/box empty");
                }
                return _boxEmpty;
            }
        }


        private static Texture2D _alert;
        public static Texture2D Alert
        {
            get
            {
                if (_alert == null)
                {
                    _alert = Resources.Load<Texture2D>("Icons/alert");
                }
                return _alert;
            }
        }

        private static Texture2D _error;
        public static Texture2D Error
        {
            get
            {
                if (_error == null)
                {
                    _error = Resources.Load<Texture2D>("Icons/error");
                }
                return _error;
            }
        }

        private static Texture2D _info;
        public static Texture2D Info
        {
            get
            {
                if (_info == null)
                {
                    _info = Resources.Load<Texture2D>("Icons/info");
                }
                return _info;
            }
        }

        private static Texture2D _infoGrey;
        public static Texture2D InfoGrey
        {
            get
            {
                if (_infoGrey == null)
                {
                    _infoGrey = Resources.Load<Texture2D>("Icons/info grey");
                }
                return _infoGrey;
            }
        }

        private static Texture2D _searchIcon;
        public static Texture2D SearchIcon
        {
            get
            {
                if (_searchIcon == null)
                {
                    _searchIcon = Resources.Load<Texture2D>("Icons/search");
                }
                return _searchIcon;
            }
        }

        private static Texture2D _searchIconwhite;
        public static Texture2D SearchIconWhite
        {
            get
            {
                if (_searchIconwhite == null)
                {
                    _searchIconwhite = Resources.Load<Texture2D>("Icons/search white");
                }
                return _searchIconwhite;
            }
        }

        private static Texture2D exitpollFeature;
        internal static Texture2D ExitPollFeature
        {
            get
            {
                if (exitpollFeature == null)
                {
                    exitpollFeature = Resources.Load<Texture2D>("FeatureImages/ExitPoll");
                }
                return exitpollFeature;
            }
        }

        private static Texture2D sceneFeature;
        internal static Texture2D SceneFeature
        {
            get
            {
                if (sceneFeature == null)
                {
                    sceneFeature = Resources.Load<Texture2D>("FeatureImages/SceneExplorer");
                }
                return sceneFeature;
            }
        }

        private static Texture2D dynamicsFeature;
        internal static Texture2D DynamicsFeature
        {
            get
            {
                if (dynamicsFeature == null)
                {
                    dynamicsFeature = Resources.Load<Texture2D>("FeatureImages/Dynamics");
                }
                return dynamicsFeature;
            }
        }

        private static Texture2D sensorsFeature;
        internal static Texture2D SensorsFeature
        {
            get
            {
                if (sensorsFeature == null)
                {
                    sensorsFeature = Resources.Load<Texture2D>("FeatureImages/Sensors");
                }
                return sensorsFeature;
            }
        }

        private static Texture2D mediaFeature;
        internal static Texture2D MediaFeature
        {
            get
            {
                if (mediaFeature == null)
                {
                    mediaFeature = Resources.Load<Texture2D>("FeatureImages/Media");
                }
                return mediaFeature;
            }
        }

        private static Texture2D readyRoomFeature;
        internal static Texture2D ReadyRoomFeature
        {
            get
            {
                if (readyRoomFeature == null)
                {
                    readyRoomFeature = Resources.Load<Texture2D>("FeatureImages/Ready Room");
                }
                return readyRoomFeature;
            }
        }

        private static Texture2D onboardingVideo;
        internal static Texture2D OnboardingVideo
        {
            get
            {
                if (onboardingVideo == null)
                {
                    onboardingVideo = Resources.Load<Texture2D>("FeatureImages/Onboarding Video");
                }
                return onboardingVideo;
            }
        }

        private static Texture2D _settingsIcon;
        public static Texture2D SettingsIcon
        {
            get
            {
                if (_settingsIcon == null)
                {
                    _settingsIcon = Resources.Load<Texture2D>("Icons/gear blue");
                }
                return _settingsIcon;
            }
        }

        private static Texture2D _settingsIconWhite;
        private static Texture2D _settingsIconBlack;
        public static Texture2D SettingsIcon2
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    if (_settingsIconWhite == null)
                    {
                        _settingsIconWhite = Resources.Load<Texture2D>("Icons/gear white");
                    }
                    return _settingsIconWhite;
                }
                else
                {
                    if (_settingsIconBlack == null)
                    {
                        _settingsIconBlack = Resources.Load<Texture2D>("Icons/gear black");
                    }
                    return _settingsIconBlack;
                }
            }
        }

        private static Texture2D _filterIcon;
        public static Texture2D FilterIcon
        {
            get
            {
                if (_filterIcon == null)
                {
                    _filterIcon = Resources.Load<Texture2D>("Icons/group");
                }
                return _filterIcon;
            }
        }

        private static Texture2D _clearIcon;
        public static Texture2D ClearIcon
        {
            get
            {
                if (_clearIcon == null)
                {
                    _clearIcon = Resources.Load<Texture2D>("Icons/clear");
                }
                return _clearIcon;
            }
        }

        private static Texture2D externalIcon;
        public static Texture2D ExternalIcon
        {
            get
            {
                if (externalIcon == null)
                {
                    externalIcon = Resources.Load<Texture2D>("Icons/external");
                }
                return externalIcon;
            }
        }

        private static Texture2D clouduploadIcon;
        public static Texture2D CloudUploadIcon
        {
            get
            {
                if (clouduploadIcon == null)
                {
                    clouduploadIcon = Resources.Load<Texture2D>("Icons/cloud upload");
                }
                return clouduploadIcon;
            }
        }

        private static Texture2D _refreshIconWhite;
        private static Texture2D _refreshIconBlack;
        public static Texture2D RefreshIcon
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    if (_refreshIconWhite == null)
                    {
                        _refreshIconWhite = Resources.Load<Texture2D>("Icons/refresh white");
                    }
                    return _refreshIconWhite;
                }
                else
                {
                    if (_refreshIconBlack == null)
                    {
                        _refreshIconBlack = Resources.Load<Texture2D>("Icons/refresh black");
                    }
                    return _refreshIconBlack;
                }
            }
        }

#endregion

#region ExitPoll
        /// <summary>
        /// This method retrieves the ExitPoll hooks. It checks if the developer key is valid.
        /// If successful, it sends a request to fetch the ExitPoll hooks and processes the response.
        /// </summary>
        public static void RefreshExitPollHooks()
        {
            Debug.Log("Refresh exitpoll hooks");
            //gets the scene version from api and sets it to the current scene

            if (!IsDeveloperKeyValid) { Debug.Log("Developer key invalid"); return; }

            string url = CognitiveStatics.GetExitpollHooks();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            EditorNetwork.Get(url, GetExitPollHooksResponse, headers, true);//AUTH
        }

        /// <summary>
        /// Handles the response from the GetExitPollHooks API request.
        /// </summary>
        private static void GetExitPollHooksResponse(int responsecode, string error, string text)
        {
            if (responsecode != 200)
            {
                RefreshSceneVersionComplete = null;
                //internal server error
                Debug.LogError("GetExitPollHooksResponse Error [CODE] " + responsecode + " [ERROR] " + error);
                return;
            }

            ExitPollHookData[] hooks = Util.GetJsonArray<ExitPollHookData>(text);
            Util.logDevelopment("Response contains " + hooks.Length + " exitpoll hooks");
            if (hooks.Length > 0)
            {
                ExitPollHooks = hooks;
            }
        }

        [System.Serializable]
        internal class ExitPollHookData
        {
            public bool active;
            public string description;
            public string name;
            public string questionSetId;
        }

        internal static ExitPollHookData[] ExitPollHooks = new ExitPollHookData[] { };
#endregion

#region Media

        /// <summary>
        /// make a get request to get media available to this scene
        /// </summary>
        public static void RefreshMediaSources()
        {
            Debug.Log("refresh media sources");
            //gets the scene version from api and sets it to the current scene
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScenePath);
            if (currentSettings != null)
            {
                if (!IsDeveloperKeyValid) { Debug.Log("Developer key invalid"); return; }

                if (currentSettings == null)
                {
                    Debug.Log("SendSceneVersionRequest no scene settings!");
                    return;
                }
                string url = CognitiveStatics.GetMediaSourceList();
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                EditorNetwork.Get(url, GetMediaSourcesResponse, headers, true, "Get Scene Version");//AUTH
            }
            else
            {
                Debug.Log("No scene versions for scene: " + currentScenePath);
            }
        }

        /// <summary>
        /// response from get media sources
        /// </summary>
        private static void GetMediaSourcesResponse(int responsecode, string error, string text)
        {
            if (responsecode != 200)
            {
                RefreshSceneVersionComplete = null;
                //internal server error
                Debug.LogError("GetMediaSourcesResponse Error [CODE] " + responsecode + " [ERROR] " + error);
                return;
            }

            MediaSource[] sources = Util.GetJsonArray<MediaSource>(text);
            Util.logDevelopment("Response contains " + sources.Length + " media sources");
            if (sources.Length > 0)
            {
                ArrayUtility.Insert<MediaSource>(ref sources, 0, new MediaSource());
                MediaSources = sources;
            }
        }

        [Serializable]
        public class MediaSource
        {
            public string name;
            public string uploadId;
            public string description;
        }

        public static MediaSource[] MediaSources = new MediaSource[] { };
#endregion

#region Scene Setup Display Names
        static TextAsset DisplayNameAsset;
        static bool HasLoadedDisplayNames;
        public static string DisplayValue(DisplayKey key)
        {
            if (DisplayNameAsset == null && !HasLoadedDisplayNames)
            {
                HasLoadedDisplayNames = true;
                DisplayNameAsset = Resources.Load<TextAsset>("DisplayNames");
            }
            if (DisplayNameAsset == null)
            {
                //default
                switch (key)
                {
                    case DisplayKey.GatewayURL: return Cognitive3D_Preferences.Instance.Gateway;
                    case DisplayKey.DashboardURL: return Cognitive3D_Preferences.Instance.Dashboard;
                    case DisplayKey.ViewerName: return "Scene Explorer";
                    case DisplayKey.ViewerURL: return Cognitive3D_Preferences.Instance.Viewer;
                    case DisplayKey.DocumentationURL: return Cognitive3D_Preferences.Instance.Documentation;
                    case DisplayKey.FullName: return "Cognitive3D";
                    case DisplayKey.ShortName: return "Cognitive3D";
                    case DisplayKey.ManagerName: return "Cognitive3D_Manager";
                    default: return "unknown";
                }
            }
            else
            {
                var lines = DisplayNameAsset.text.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Length == 0) { continue; }
                    if (line.StartsWith("//")) { continue; }
                    string replacement = System.Text.RegularExpressions.Regex.Replace(line, @"\t|\n|\r", "");
                    var split = replacement.Split('|');
                    foreach (var keyvalue in (DisplayKey[])Enum.GetValues(typeof(DisplayKey)))
                    {
                        if (split[0].ToUpper() == key.ToString().ToUpper())
                        {
                            return split[1];
                        }
                    }
                }
            }
            return "unknown";
        }

        internal static void SetMainCamera(GameObject camera)
        {
            if (camera == null) return;

            if (camera.CompareTag("MainCamera") == false)
            {
                camera.tag = "MainCamera";
            }
        }

        internal static void SetTrackingSpace(GameObject trackingSpace)
        {
            if (trackingSpace == null) return;

            if (!trackingSpace.GetComponent<RoomTrackingSpace>())
            {
                trackingSpace.AddComponent<RoomTrackingSpace>();
            }
        }

        private static GameObject _leftController;
        public static GameObject leftController {
            get {
                return _leftController;
            }
            internal set {
                _leftController = value;
            }
        }
        private static GameObject _rightController;
        public static GameObject rightController {
            get {
                return _rightController;
            }
            internal set {
                _rightController = value;
            }
        }

        /// <summary>
        /// Sets controllers from Scene Setup window
        /// </summary>
        /// <param name="isRight"></param>
        /// <param name="controller"></param>
        internal static void SetControllers(bool isRight, GameObject controller)
        {
            if (isRight)
            {
                rightController = controller;
            }
            else
            {
                leftController = controller;
            }
        }

        internal static void SetController(bool isRight, GameObject controller)
        {
            if (controller == null) return;
            if (!controller.GetComponent<DynamicObject>())
            {
                controller.AddComponent<DynamicObject>();
            }

            InputUtil.ControllerType controllerType = InputUtil.ControllerType.Quest2;
#if C3D_STEAMVR2
            controllerType = InputUtil.ControllerType.ViveWand;
#elif C3D_OCULUS
            controllerType = InputUtil.ControllerType.Quest2;
#elif C3D_PICOXR
            controllerType = InputUtil.ControllerType.PicoNeo3;
#elif C3D_VIVEWAVE
            controllerType = InputUtil.ControllerType.ViveFocus;
#endif

            var dyn = controller.GetComponent<DynamicObject>();
            dyn.IsRight = isRight;
            dyn.IsController = true;
            dyn.inputType = InputUtil.InputType.Controller;
            dyn.SyncWithPlayerGazeTick = true;
            dyn.FallbackControllerType = controllerType;
            dyn.idSource = DynamicObject.IdSourceType.GeneratedID;
        }

        /// <summary>
        /// Checks if left controller is valid and properly setup in Scene Setup window
        /// </summary>
        /// This is used in project validation to check if controllers are setup properly
        internal static bool IsLeftControllerValid()
        {
            return leftController ? true : false;
        }

        /// <summary>
        /// Checks if right controller is valid and properly setup in Scene Setup window
        /// </summary>
        internal static bool IsRightControllerValid()
        {
            return rightController ? true : false;
        }
#endregion

#region Cognitive3DPrefabUtility

        private const string packagePrefabPath = "Packages/com.cognitive3d.c3d-sdk/Runtime/Resources/Cognitive3D_Manager.prefab";
        private const string projectPrefabPath = "Assets/Resources/Cognitive3D_Manager.prefab";

        /// <summary>
        /// Returns the prefab that should be used by the project,
        /// preferring the copy in Assets/Resources. 
        /// If the copy doesn't exist, it is created from the package prefab.
        /// </summary>
        public static GameObject GetCognitive3DManagerPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectPrefabPath);
            if (prefab != null) return prefab;

            CreatePrefabCopy();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectPrefabPath);
            if (prefab != null) return prefab;

            // Fallback to package prefab if something went wrong
            return AssetDatabase.LoadAssetAtPath<GameObject>(packagePrefabPath);
        }
        
        /// <summary>
        /// Creates a copy of the Cognitive3D Manager prefab from the package into Assets/Resources.
        /// Does nothing if the prefab already exists in Assets/Resources.
        /// </summary>
        public static void CreatePrefabCopy()
        {
            if (File.Exists(projectPrefabPath))
            {
                return;
            }

            // Load from package
            GameObject packagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(packagePrefabPath);
            if (packagePrefab == null)
            {
                Debug.LogError("Could not find Cognitive3D Manager prefab in package.");
                return;
            }

            // Ensure Resources folder exists
            if (!Directory.Exists("Assets/Resources"))
            {
                Directory.CreateDirectory("Assets/Resources");
            }

            // Copy prefab into Assets
            AssetDatabase.CopyAsset(packagePrefabPath, projectPrefabPath);
            AssetDatabase.SaveAssets();

            Util.logDebug("Created Cognitive3D Manager prefab copy in Assets/Resources.");
        }

        /// <summary>
        /// Checks if a Cognitive3D Manager prefab exists in the current scene.
        /// </summary>
        public static bool IsManagerPrefabInScene()
        {
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScene.path);

            if (currentSettings != null)
            {
                foreach (var root in currentScene.GetRootGameObjects())
                {
                    var manager = root.GetComponentInChildren<Cognitive3D_Manager>();
                    if (manager != null)
                    {
                        return true; // Found a Cognitive3D_Manager in the scene
                    }
                }
            }

            return false; // No manager found
        }

        /// <summary>
        /// Checks if the Cognitive3D Manager in the current scene is using the old prefab from the package.
        /// </summary>
        public static bool IsUsingOldManagerPrefab()
        {
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScene.path);
            if (currentSettings != null)
            {
                foreach (var root in currentScene.GetRootGameObjects())
                {
                    var oldManager = root.GetComponentInChildren<Cognitive3D_Manager>();
                    if (oldManager != null)
                    {
                        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(oldManager.gameObject) as GameObject;
                        string prefabPath = sourcePrefab != null ? AssetDatabase.GetAssetPath(sourcePrefab) : "";

                        if (prefabPath == packagePrefabPath)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Starts the update process to replace old prefab instances in tracked scenes.
        /// </summary>
        public static void PrefabUpdater()
        {
            EditorApplication.update += RunUpdateCheck;
        }

        /// <summary>
        /// Iterates through all tracked scenes, replaces any old package prefab instances
        /// with the project prefab, and adds a manager if none exists.
        /// </summary>
        static void RunUpdateCheck()
        {
            EditorApplication.update -= RunUpdateCheck;

            var scenes = Cognitive3D_Preferences.Instance.sceneSettings;
            foreach (var scene in scenes)
            {
                string path = scene.ScenePath;
                if (!System.IO.File.Exists(path))
                {
                    Util.logWarning($"Scene path not found: {path}");
                    continue;
                }

                var _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                bool modified = false;
                bool foundManager = false;

                foreach (var root in _scene.GetRootGameObjects())
                {
                    var oldManager = root.GetComponentInChildren<Cognitive3D_Manager>();
                    if (oldManager != null)
                    {
                        foundManager = true;
                        // Get prefab source path
                        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(oldManager.gameObject) as GameObject;
                        string prefabPath = sourcePrefab != null ? AssetDatabase.GetAssetPath(sourcePrefab) : "";

                        // Only replace if it's from the old package path
                        if (prefabPath == packagePrefabPath)
                        {
                            var newPrefab = PrefabUtility.InstantiatePrefab(GetCognitive3DManagerPrefab(), _scene) as GameObject;
                            if (newPrefab != null)
                            {
                                newPrefab.name = "Cognitive3D_Manager";
                                UnityEngine.Object.DestroyImmediate(oldManager.gameObject);
                                modified = true;
                            }
                        }
                    }
                }

                // If no manager exists, add one
                if (!foundManager)
                {
                    var newPrefab = PrefabUtility.InstantiatePrefab(GetCognitive3DManagerPrefab(), _scene) as GameObject;
                    if (newPrefab != null)
                    {
                        newPrefab.name = "Cognitive3D_Manager";
                        modified = true;
                    }
                }

                if (modified)
                {
                    EditorSceneManager.MarkSceneDirty(_scene);
                    EditorSceneManager.SaveScene(_scene);
                    Util.logDebug($"Saved updated scene: {path}");
                }
            }
        }
#endregion

        #region Packages

        static Action<UnityEditor.PackageManager.PackageCollection> GetPackageResponseAction;
        static UnityEditor.PackageManager.Requests.ListRequest GetPackageListRequest;
        public static void GetPackages(Action<UnityEditor.PackageManager.PackageCollection> responseAction)
        {
            GetPackageResponseAction = responseAction;
            GetPackageListRequest = UnityEditor.PackageManager.Client.List();
            EditorApplication.update += WaitGetPackageList;
        }

        //TODO consider merging this with WaitList below. need to refactor to handle various actions from the result
        static void WaitGetPackageList()
        {
            if (!GetPackageListRequest.IsCompleted) { return; }
            EditorApplication.update -= WaitGetPackageList;

            if (GetPackageListRequest.Error != null)
            {
                Debug.LogError("Checking current version Cognitive3D package. " + GetPackageListRequest.Error);
                return;
            }

            GetPackageResponseAction.Invoke(GetPackageListRequest.Result);
        }

        #endregion

        #region Assembly Definition
        /// <summary>
        /// Checks if 'allowUnsafeCode' is enabled in the Cognitive3D.asmdef file.
        /// Logs an error if the asmdef file cannot be found.
        /// </summary>
        internal static bool IsUnsafeCodeEnabled()
        {
            string path = "Packages/com.cognitive3d.c3d-sdk/Runtime/Cognitive3D.asmdef";

            if (!File.Exists(path))
            {
                Util.LogOnce($"ASMDEF not found at {path}", LogType.Error);
                return false;
            }

            string json = File.ReadAllText(path);

            // Check if allowUnsafeCode is explicitly set to true
            return json.Contains("\"allowUnsafeCode\": true");
        }

        /// <summary>
        /// Enables 'allowUnsafeCode' in the Cognitive3D.asmdef file by either updating its value
        /// or inserting the property if it does not already exist. 
        /// Triggers AssetDatabase refresh after modification. 
        /// Logs an error if the asmdef file cannot be found.
        /// </summary>
        internal static void EnableUnsafeCode()
        {
            string path = "Packages/com.cognitive3d.c3d-sdk/Runtime/Cognitive3D.asmdef";

            if (!File.Exists(path))
            {
                Util.LogOnce($"ASMDEF not found at {path}", LogType.Error);
                return;
            }

            string json = File.ReadAllText(path);

            // Use regex to replace or insert the allowUnsafeCode field
            if (json.Contains("\"allowUnsafeCode\""))
            {
                json = System.Text.RegularExpressions.Regex.Replace(json, "\"allowUnsafeCode\"\\s*:\\s*(false|true)", "\"allowUnsafeCode\": true");
            }
            else
            {
                // insert before the final closing }
                int insertIndex = json.LastIndexOf('}');
                if (insertIndex > 0)
                {
                    // Add a comma before inserting if not already at the end of an object
                    json = json.Insert(insertIndex, ",\n  \"allowUnsafeCode\": true\n");
                }
            }

            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }
        #endregion

        #region SDK Updates
        //data about the last sdk release on github
        public class ReleaseInfo
        {
            public string tag_name;
            public string body;
            public string created_at;
        }

        private static void SaveEditorVersion()
        {
            if (EditorPrefs.GetString("c3d_version") != Cognitive3D_Manager.SDK_VERSION)
            {
                EditorPrefs.SetString("c3d_version", Cognitive3D_Manager.SDK_VERSION);
                EditorPrefs.SetString("c3d_updateDate", DateTime.UtcNow.ToString("dd-MM-yyyy"));
            }
        }

        public static void ForceCheckUpdates()
        {
            EditorPrefs.SetString("c3d_skipVersion", "");
            EditorApplication.update -= UpdateCheckForUpdates;
            EditorPrefs.SetString("c3d_updateRemindDate", DateTime.UtcNow.AddDays(1).ToString("dd-MM-yyyy"));
            SaveEditorVersion();

#if USE_ATTRIBUTION
            //check package manager for current version
            listRequest = UnityEditor.PackageManager.Client.List();
            EditorApplication.update += WaitList;
#else
            //check github releases for current version
            checkForUpdatesRequest = UnityEngine.Networking.UnityWebRequest.Get(CognitiveStatics.GITHUB_SDKVERSION);
            checkForUpdatesRequest.SendWebRequest();
            EditorApplication.update += UpdateCheckForUpdates;
#endif
        }

        static UnityEngine.Networking.UnityWebRequest checkForUpdatesRequest;
        static UnityEditor.PackageManager.Requests.ListRequest listRequest;
        static void CheckForUpdates()
        {
            System.DateTime remindDate; //current date must be this or beyond to show popup window

            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.DateTimeStyles styles = System.Globalization.DateTimeStyles.None;

            if (DateTime.TryParseExact(EditorPrefs.GetString("c3d_updateRemindDate", "01/01/1971"), "dd-MM-yyyy", culture, styles, out remindDate))
            {
                if (DateTime.UtcNow > remindDate)
                {
                    EditorPrefs.SetString("c3d_updateRemindDate", DateTime.UtcNow.AddDays(1).ToString("dd-MM-yyyy"));
                    SaveEditorVersion();

#if USE_ATTRIBUTION
                    //check package manager for current version
                    listRequest = UnityEditor.PackageManager.Client.List();
                    EditorApplication.update += WaitList;
#else
                    //check github releases for current version
                    checkForUpdatesRequest = UnityEngine.Networking.UnityWebRequest.Get(CognitiveStatics.GITHUB_SDKVERSION);
                    checkForUpdatesRequest.SendWebRequest();
                    EditorApplication.update += UpdateCheckForUpdates;
#endif
                }
            }
            else
            {
                EditorPrefs.SetString("c3d_updateRemindDate", DateTime.UtcNow.AddDays(1).ToString("dd-MM-yyyy"));
            }
        }

        //wait for package manager 
        static void WaitList()
        {
            if (!listRequest.IsCompleted) { return; }
            EditorApplication.update -= WaitList;

            if (listRequest.Error != null)
            {
                Debug.LogError("Checking current version Cognitive3D package. " + listRequest.Error);
                return;
            }

            UnityEditor.PackageManager.PackageInfo c3dpackage = null;
            foreach (var v in listRequest.Result)
            {
                if (v.name == "com.cognitive3d.c3d-sdk")
                {
                    c3dpackage = v;
                    break;
                }
            }
            if (c3dpackage == null) { Debug.LogError("Checking current version Cognitive3D package. com.cognitive3d.c3d-sdk package not found!"); return; }

            System.Version installedVersion = null;
            System.Version packageManagerVersion = null;
            try
            {
                installedVersion = new Version(c3dpackage.version);
                packageManagerVersion = new Version(c3dpackage.versions.latest);
                if (packageManagerVersion > installedVersion)
                {
                    UpdateSDKWindow.InitPackageManager(c3dpackage.versions.latest);
                }
                else
                {
                    Debug.Log("Cognitive3D Version: " + installedVersion + ". Up to date!");
                }
            }
            catch
            {
                Debug.LogWarning("Checking current version Cognitive3D package. Invalid version found");
                UpdateSDKWindow.InitPackageManager("");
            }
        }

        //wait for github release response
        static void UpdateCheckForUpdates()
        {
            if (!checkForUpdatesRequest.isDone)
            {
                //IMPROVEMENT check for timeout
            }
            else
            {
                if (!string.IsNullOrEmpty(checkForUpdatesRequest.error))
                {
                    Debug.Log("Check for Cognitive3D SDK version update error: " + checkForUpdatesRequest.error);
                }

                if (!string.IsNullOrEmpty(checkForUpdatesRequest.downloadHandler.text))
                {
                    var info = JsonUtility.FromJson<ReleaseInfo>(checkForUpdatesRequest.downloadHandler.text);
                    var version = info.tag_name;
                    string summary = info.body;

                    if (!string.IsNullOrEmpty(version))
                    {
                        string skipVersion = EditorPrefs.GetString("c3d_skipVersion");

                        if (version != skipVersion) //new version, not the skipped one
                        {
                            Version installedVersion = new Version(Cognitive3D_Manager.SDK_VERSION);
                            Version githubVersion = new Version(version);
                            if (githubVersion > installedVersion)
                            {
                                UpdateSDKWindow.Init(version, summary);
                            }
                            else
                            {
                                Debug.Log("Cognitive3D Version: " + installedVersion + ". Up to date!");
                            }
                        }
                        else if (skipVersion == version) //skip this version. limit this check to once a day
                        {
                            EditorPrefs.SetString("c3d_updateRemindDate", DateTime.UtcNow.AddDays(1).ToString("dd-MM-yyyy"));
                        }
                    }
                }
                EditorApplication.update -= UpdateCheckForUpdates;
            }
        }

#endregion

#region Files and Directories

        internal static bool HasDynamicExportFiles(string meshname)
        {
            string dynamicExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + meshname;
            var SceneExportDirExists = Directory.Exists(dynamicExportDirectory);
            return SceneExportDirExists && Directory.GetFiles(dynamicExportDirectory).Length > 0;
        }

        internal static bool HasDynamicObjectThumbnail(string meshname)
        {
            string dynamicExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + meshname;
            var SceneExportDirExists = Directory.Exists(dynamicExportDirectory);

            if (!SceneExportDirExists) return false;

            var files = Directory.GetFiles(dynamicExportDirectory);
            for (int i = 0;i<files.Length; i++)
            {
                if (files[i].EndsWith("cvr_object_thumbnail.png"))
                { return true; }
            }
            return false;
        }

        internal static bool HasSceneThumbnail(Cognitive3D_Preferences.SceneSettings sceneSettings)
        {
            if (sceneSettings == null) { return false; }

            string sceneScreenshotDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + sceneSettings.SceneName + "screenshot" + Path.DirectorySeparatorChar;
            var sceneScreenshotDirExists = Directory.Exists(sceneScreenshotDirectory);

            if (!sceneScreenshotDirExists) { return false; }

            //look in screenshot subdirectory

            var files = Directory.GetFiles(sceneScreenshotDirectory);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith("screenshot.png"))
                { return true; }
            }
            return false;
        }

        class CachedSceneThumbnail
        {
            public Cognitive3D_Preferences.SceneSettings SceneSettings;
            public Texture2D SceneThumbnail;
            public CachedSceneThumbnail(Cognitive3D_Preferences.SceneSettings sceneSettings, Texture2D sceneThumbnail)
            {
                SceneSettings = sceneSettings;
                SceneThumbnail = sceneThumbnail;
            }
        }
        static CachedSceneThumbnail cachedSceneThumbnail;

        internal static bool GetSceneThumbnail(Cognitive3D_Preferences.SceneSettings sceneSettings, ref Texture2D thumbnail, bool refresh)
        {
            //clear cached thumbnail if necessary
            if (refresh)
            {
                cachedSceneThumbnail = null;
            }
            if (cachedSceneThumbnail != null && sceneSettings == cachedSceneThumbnail.SceneSettings)
            {
                thumbnail = cachedSceneThumbnail.SceneThumbnail;
                return true;
            }

            //check if scene export directory exists
            if (sceneSettings == null) { return false; }
            string sceneScreenshotDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneSettings.SceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar;
            var sceneScreenshotDirExists = Directory.Exists(sceneScreenshotDirectory);
            if (!sceneScreenshotDirExists) { return false; }

            //look in screenshot subdirectory
            var files = Directory.GetFiles(sceneScreenshotDirectory);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith("screenshot.png"))
                {
                    var bytes = File.ReadAllBytes(files[i]);
                    thumbnail = new Texture2D(1,1);
                    if (thumbnail.LoadImage(bytes, false))
                    {                        
                        cachedSceneThumbnail = new CachedSceneThumbnail(sceneSettings, thumbnail);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public static bool CreateTargetFolder(string fullName)
        {
            try
            {
                Directory.CreateDirectory("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName);
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Failed to create folder: Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName, "Ok");
                return false;
            }
            return true;
        }

        /// <summary>
        /// return path to Cognitive3D_SceneExplorerExport/NAME. create if it doesn't exist
        /// </summary>
        public static string GetSubDirectoryPath(string directoryName)
        {
            CreateTargetFolder(directoryName);
            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + directoryName + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// returns path to Cognitive3D_SceneExplorerExport
        /// </summary>
        public static string GetBaseDirectoryPath()
        {
            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// has scene export directory AND files for scene
        /// </summary>
        public static bool HasSceneExportFiles(Cognitive3D_Preferences.SceneSettings currentSceneSettings)
        {
            if (currentSceneSettings == null) { return false; }
            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);
            if (!SceneExportDirExists) { return false; }

            var files = Directory.GetFiles(sceneExportDirectory);
            bool hasBin = false;
            bool hasGltf = false;
            foreach (var f in files)
            {
                if (f.EndsWith("scene.bin"))
                    hasBin = true;
                if (f.EndsWith("scene.gltf"))
                    hasGltf = true;
            }

            return SceneExportDirExists && files.Length > 0 && hasBin && hasGltf;
        }

        /// <summary>
        /// get scene export directory. returns "" if no directory exists
        /// </summary>
        public static string GetSceneExportDirectory(Cognitive3D_Preferences.SceneSettings currentSceneSettings)
        {
            if (currentSceneSettings == null) { return ""; }
            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);
            if (!SceneExportDirExists) { return ""; }
            return sceneExportDirectory;
        }

        public static string GetDynamicExportDirectory()
        {
            string dynamicExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar;
            var ExportDirExists = Directory.Exists(dynamicExportDirectory);
            if (!ExportDirExists) { return ""; }
            return dynamicExportDirectory;
        }

        /// <summary>
        /// has scene export directory. returns true even if there are no files in directory
        /// </summary>
        public static bool HasSceneExportFolder(Cognitive3D_Preferences.SceneSettings currentSceneSettings)
        {
            if (currentSceneSettings == null) { return false; }
            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);

            return SceneExportDirExists;
        }

        /// <summary>
        /// returns size of scene export folder in MB
        /// </summary>
        public static float GetSceneFileSize(Cognitive3D_Preferences.SceneSettings scene)
        {
            if (string.IsNullOrEmpty(GetSceneExportDirectory(scene)))
            {
                return 0;
            }
            float size = GetDirectorySize(GetSceneExportDirectory(scene)) / 1048576f;
            return size;
        }

        /// <summary>
        /// returns directory size by full string path in bytes
        /// </summary>
        public static long GetDirectorySize(string fullPath)
        {
            string[] a = System.IO.Directory.GetFiles(fullPath, "*.*", System.IO.SearchOption.AllDirectories);
            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            return b;
        }

        public static List<string> ExportedDynamicObjects;
        /// <summary>
        /// returns list of exported dynamic objects. refreshes on init window focused
        /// </summary>
        public static List<string> GetExportedDynamicObjectNames()
        {
            //read dynamic object mesh names from directory
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic";
            Directory.CreateDirectory(path);
            var subdirectories = Directory.GetDirectories(path);
            List<string> ObjectNames = new List<string>();
            foreach (var subdir in subdirectories)
            {
                var dirname = new DirectoryInfo(subdir).Name;
                ObjectNames.Add(dirname);
            }
            ExportedDynamicObjects = ObjectNames;
            return ObjectNames;
        }

#endregion

#region Scene Screenshot

        static RenderTexture sceneRT = null;
        public static RenderTexture GetSceneRenderTexture()
        {
            if (sceneRT == null)
                sceneRT = new RenderTexture(1024, 432, 24);

            var cameras = SceneView.GetAllSceneCameras();
            if (cameras != null && cameras.Length > 0 && cameras[0] != null)
            {
                cameras[0].targetTexture = sceneRT;
                cameras[0].Render();
            }
            return sceneRT;
        }

        public static void UploadSceneThumbnail(Cognitive3D_Preferences.SceneSettings settings)
        {
            string path = "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + settings.SceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png";

            if (!File.Exists(path))
            {
                Debug.Log("Cannot upload Scene Thumbnail, thumbnail asset doesn't exist");
                return;
            }

            string url = CognitiveStatics.PostScreenshot(settings.SceneId, settings.VersionNumber);
            var bytes = File.ReadAllBytes(path);
            WWWForm form = new WWWForm();
            form.AddBinaryData("screenshot", bytes, "screenshot.png");
            var headers = new Dictionary<string, string>();
            foreach (var v in form.headers)
            {
                headers[v.Key] = v.Value;
            }
            headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            EditorNetwork.Post(url, form, UploadCustomScreenshotResponse, headers, false);
        }

        public static void UploadCustomScreenshot()
        {
            //check that scene has been uploaded
            var currentScene = Cognitive3D_Preferences.FindCurrentScene();
            if (currentScene == null)
            {
                Debug.LogError("Could not find current scene");
                return;
            }
            if (string.IsNullOrEmpty(currentScene.SceneId))
            {
                Debug.LogError(currentScene.SceneName + " scene has not been uploaded!");
                return;
            }
            if (!IsDeveloperKeyValid)
            {
                Debug.LogError("Developer Key is invalid!");
                return;
            }

            //file select popup
            string path = EditorUtility.OpenFilePanel("Select Screenshot", "", "png");
            if (path.Length == 0)
            {
                return;
            }
            string filename = Path.GetFileName(path);

            //confirm popup and upload
            if (EditorUtility.DisplayDialog("Upload Screenshot", "Upload " + filename + " to " + currentScene.SceneName + " version " + currentScene.VersionNumber + "?", "Upload", "Cancel"))
            {
                string url = CognitiveStatics.PostScreenshot(currentScene.SceneId, currentScene.VersionNumber);
                var bytes = File.ReadAllBytes(path);
                WWWForm form = new WWWForm();
                form.AddBinaryData("screenshot", bytes, "screenshot.png");
                var headers = new Dictionary<string, string>();
                foreach (var v in form.headers)
                {
                    headers[v.Key] = v.Value;
                }
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                EditorNetwork.Post(url, form, UploadCustomScreenshotResponse, headers, false);
            }
        }

        /// <summary>
        /// response from posting custom screenshot
        /// </summary>
        static void UploadCustomScreenshotResponse(int responsecode, string error, string text)
        {
            if (responsecode == 200)
            {
                EditorUtility.DisplayDialog("Upload Complete", "Screenshot uploaded successfully", "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Upload Fail", "Screenshot was not uploaded successfully. See console for details", "Ok");
                Debug.LogError("Failed to upload screenshot. [CODE] "+responsecode+" [ERROR] " + error);
            }
        }

        static Texture2D cachedScreenshot;
        /// <summary>
        /// get a screenshot from directory
        /// returns true if screenshot exists
        /// </summary>
        public static bool LoadScreenshot(string sceneName, out Texture2D returnTexture)
        {
            if (cachedScreenshot)
            {
                returnTexture = cachedScreenshot;
                return true;
            }
            //if file exists
            if (Directory.Exists("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot"))
            {
                if (File.Exists("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png"))
                {
                    //load texture from file
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(File.ReadAllBytes("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png"));
                    returnTexture = tex;
                    cachedScreenshot = returnTexture;
                    return true;
                }
            }
            returnTexture = null;
            return false;
        }

        public static void SaveScreenshot(RenderTexture renderTexture, string sceneName, Action completeScreenshot)
        {
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            Directory.CreateDirectory("Cognitive3D_SceneExplorerExport");
            Directory.CreateDirectory("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName);
            Directory.CreateDirectory("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot");

            //save file
            File.WriteAllBytes("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png", tex.EncodeToPNG());
            //use editor update to delay teh screenshot 1 frame?

            if (completeScreenshot != null)
                completeScreenshot.Invoke();
            completeScreenshot = null;
        }

        public static void SceneViewCameraScreenshot(Camera cam, string sceneName, System.Action saveCameraScreenshot)
        {
            //create render texture
            RenderTexture rt = new RenderTexture(512, 512, 0);
            cam.targetTexture = rt;
            cam.Render();

            Texture2D tex = new Texture2D(512, 512);
            RenderTexture.active = cam.targetTexture;
            tex.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            cam.targetTexture.Release();

            Directory.CreateDirectory("Cognitive3D_SceneExplorerExport");
            Directory.CreateDirectory("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName);
            Directory.CreateDirectory("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot");

            //save file
            File.WriteAllBytes("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png", tex.EncodeToPNG());
            //use editor update to delay teh screenshot 1 frame?

            if (saveCameraScreenshot != null)
                saveCameraScreenshot.Invoke();
            saveCameraScreenshot = null;
        }

        public static void DeleteSceneThumbnail(string sceneName)
        {
            string filePath = "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png";
            if (File.Exists(filePath))
                File.Delete(filePath);
            else
                Debug.Log("can't delete file, doesn't exist " + filePath);
        }

#endregion

        #region Dynamic Object Thumbnails
        //returns unused layer int. returns -1 if no unused layer is found
        public static int FindUnusedLayer()
        {
            for (int i = 31; i > 0; i--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// save a screenshot to a dynamic object's export folder from the current sceneview
        /// </summary>
        public static void SaveDynamicThumbnailSceneView(GameObject target)
        {
            SaveDynamicThumbnail(target, SceneView.lastActiveSceneView.camera.transform.position, SceneView.lastActiveSceneView.camera.transform.rotation);
        }

        /// <summary>
        /// save a screenshot to a dynamic object's export folder from a generated position
        /// </summary>
        public static void SaveDynamicThumbnailAutomatic(GameObject target)
        {
            Vector3 pos;
            Quaternion rot;
            CalcCameraTransform(target, out pos, out rot);
            SaveDynamicThumbnail(target, pos, rot);
        }

        public static Texture2D GetSceneIsometricThumbnail()
        {
            //center on the player if a prefab exists in the scene. otherwise origin
            Vector3 pos = new Vector3(20,17,20);
            Quaternion rot = Quaternion.Euler(30, -135, 0);

            if (Camera.main != null)
            {
                pos += Camera.main.transform.position;
            }

            return GenerateSceneIsoThumbnail(pos, rot);
        }

        /// <summary>
        /// recursively get child transforms, skipping nested dynamic objects
        /// </summary>
        public static void RecursivelyGetChildren(List<Transform> transforms, Transform current)
        {
            transforms.Add(current);
            for (int i = 0; i < current.childCount; i++)
            {
                var dyn = current.GetChild(i).GetComponent<DynamicObject>();
                if (!dyn)
                {
                    RecursivelyGetChildren(transforms, current.GetChild(i));
                }
            }
        }

        /// <summary>
        /// Gets a readable copy of a texture for thumbnails
        /// </summary>
        static Texture2D GetReadableTextureThumbnail(Texture2D source)
        {
            if (source == null) return null;

            try
            {
                RenderTexture tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                readable.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);
                return readable;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Exception in GetReadableTextureThumbnail: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Resizes a texture to target dimensions
        /// </summary>
        static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        /// <summary>
        /// set layer mask on dynamic object, create temporary camera
        /// render to texture and save to file
        /// </summary>
        static void SaveDynamicThumbnail(GameObject target, Vector3 position, Quaternion rotation)
        {
            Dictionary<GameObject, int> originallayers = new Dictionary<GameObject, int>();
            var dynamic = target.GetComponent<Cognitive3D.DynamicObject>();

            // Special handling for UI Images - use sprite texture directly
            var uiImage = target.GetComponent<UnityEngine.UI.Image>();
            if (uiImage != null && uiImage.sprite != null)
            {
                Texture2D thumbnail = GetReadableTextureThumbnail(uiImage.sprite.texture);
                if (thumbnail != null)
                {
                    // Resize to 512x512 for consistency
                    Texture2D resized = ResizeTexture(thumbnail, 512, 512);
                    File.WriteAllBytes("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + dynamic.MeshName + Path.DirectorySeparatorChar + "cvr_object_thumbnail.png", resized.EncodeToPNG());
                    return;
                }
            }

#if C3D_TMPRO
            // Special handling for TextMeshProUGUI - render using canvas baking
            var tmpUI = target.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpUI != null)
            {
                RectTransform rectTransform = target.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float width = rectTransform.rect.width * rectTransform.lossyScale.x;
                    float height = rectTransform.rect.height * rectTransform.lossyScale.y;
                    Texture2D thumbnail = ExportUtility.TextureBakeCanvasUIElement(target.transform, width, height, 512);
                    if (thumbnail != null)
                    {
                        File.WriteAllBytes("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + dynamic.MeshName + Path.DirectorySeparatorChar + "cvr_object_thumbnail.png", thumbnail.EncodeToPNG());
                        return;
                    }
                }
            }
#endif

            //choose layer
            int layer = FindUnusedLayer();
            if (layer == -1) { Debug.LogWarning("couldn't find layer, don't set layers"); }

            //create camera stuff
            GameObject go = new GameObject("temp dynamic camera_" + target.name);
            var renderCam = go.AddComponent<Camera>();
            renderCam.clearFlags = CameraClearFlags.Color;
            renderCam.backgroundColor = Color.clear;
            renderCam.nearClipPlane = 0.01f;
            if (layer != -1)
            {
                renderCam.cullingMask = 1 << layer;
            }
            RenderTexture rt = RenderTexture.GetTemporary(512, 512, 16);
            renderCam.targetTexture = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height);

            //position camera
            go.transform.position = position;
            go.transform.rotation = rotation;

            //do this recursively, skipping nested dynamic objects
            List<Transform> relayeredTransforms = new List<Transform>();
            RecursivelyGetChildren(relayeredTransforms, target.transform);
            //set dynamic gameobject layers
            try
            {
                if (layer != -1)
                {
                    foreach (var v in relayeredTransforms)
                    {
                        originallayers.Add(v.gameObject, v.gameObject.layer);
                        v.gameObject.layer = layer;
                    }
                }
                //render to texture
                renderCam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (layer != -1)
            {
                //reset dynamic object layers
                foreach (var v in originallayers)
                {
                    v.Key.layer = v.Value;
                }
            }

            //remove camera
            GameObject.DestroyImmediate(renderCam.gameObject);

            //save
            File.WriteAllBytes("Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + dynamic.MeshName + Path.DirectorySeparatorChar + "cvr_object_thumbnail.png", tex.EncodeToPNG());
        }

        static Texture2D GenerateSceneIsoThumbnail(Vector3 position, Quaternion rotation)
        {
            GameObject go = new GameObject("temp scene iso camera");
            var renderCam = go.AddComponent<Camera>();
            renderCam.fieldOfView = 15;
            renderCam.nearClipPlane = 0.5f;
            var rt = new RenderTexture(512, 512, 16);
            renderCam.targetTexture = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height);

            //position camera
            go.transform.position = position;
            go.transform.rotation = rotation;

            //set dynamic gameobject layers
            try
            {
                //render to texture
                renderCam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            GameObject.DestroyImmediate(renderCam.gameObject);
            return tex;
        }

        /// <summary>
        /// generate position and rotation for a dynamic object, based on its bounding box
        /// </summary>
        static void CalcCameraTransform(GameObject target, out Vector3 position, out Quaternion rotation)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds largestBounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    largestBounds.Encapsulate(renderer.bounds);
                }

                if (largestBounds.size.magnitude <= 0)
                {
                    largestBounds = new Bounds(target.transform.position, Vector3.one * 5);
                }

                //IMPROVEMENT include target's rotation
                position = TransformPointUnscaled(target.transform, new Vector3(-1, 1, -1) * largestBounds.size.magnitude * 3 / 4);
                rotation = Quaternion.LookRotation(largestBounds.center - position, Vector3.up);
            }
            else //canvas dynamic objects
            {
                position = TransformPointUnscaled(target.transform, new Vector3(-1, 1, -1) * 3 / 4);
                rotation = Quaternion.LookRotation(target.transform.position - position, Vector3.up);
            }
        }
#endregion

#region Axis Input

        public class Axis
        {
            public string Name = String.Empty;
            public float Gravity = 0.0f;
            public float Deadzone = 0.001f;
            public float Sensitivity = 1.0f;
            public bool Snap = false;
            public bool Invert = false;
            public int InputType = 2;
            public int AxisNum = 0;
            public int JoystickNum = 0;
            public Axis(string name, int axis, bool inverted)
            {
                Name = name;
                AxisNum = axis;
                Invert = inverted;
            }

            /// <summary>
            /// Overloaded constructor
            /// </summary>
            /// <param name="name"></param>
            /// <param name="axis"></param>
            public Axis(string name, int axis)
            {
                Name = name;
                AxisNum = axis;
                Invert = false;
            }
        }

        /// <summary>
        /// appends axis input data into input manager. used for steamvr2 inputs
        /// </summary>
        public static void BindAxis(Axis axis)
        {
            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

            SerializedProperty axisIter = axesProperty.Copy(); //m_axes
            axisIter.Next(true); //m_axes.array
            axisIter.Next(true); //m_axes.array.size
            while (axisIter.Next(false))
            {
                if (axisIter.FindPropertyRelative("m_Name").stringValue == axis.Name)
                {
                    return;
                }
            }

            axesProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();

            SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);
            axisProperty.FindPropertyRelative("m_Name").stringValue = axis.Name;
            axisProperty.FindPropertyRelative("gravity").floatValue = axis.Gravity;
            axisProperty.FindPropertyRelative("dead").floatValue = axis.Deadzone;
            axisProperty.FindPropertyRelative("sensitivity").floatValue = axis.Sensitivity;
            axisProperty.FindPropertyRelative("snap").boolValue = axis.Snap;
            axisProperty.FindPropertyRelative("invert").boolValue = axis.Invert;
            axisProperty.FindPropertyRelative("type").intValue = axis.InputType;
            axisProperty.FindPropertyRelative("axis").intValue = axis.AxisNum;
            axisProperty.FindPropertyRelative("joyNum").intValue = axis.JoystickNum;
            serializedObject.ApplyModifiedProperties();
        }

        public static void CheckForExpiredDeveloperKey(EditorNetwork.Response callback)
        {
            if (IsDeveloperKeyValid)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorPrefs.GetString("c3d_developerkey"));
                EditorNetwork.Get("https://" + EditorCore.DisplayValue(DisplayKey.GatewayURL) + "/v0/apiKeys/verify", callback, headers, true);
            }
            else
            {
                callback.Invoke(0, "Invalid Developer Key", "");
            }
        }

        public static void CheckForExpiredDeveloperKey(string devKey, EditorNetwork.Response callback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "APIKEY:DEVELOPER " + devKey);
            EditorNetwork.Get("https://" + EditorCore.DisplayValue(DisplayKey.GatewayURL) + "/v0/apiKeys/verify", callback, headers, true);
        }

        internal static void CheckForApplicationKey(string developerKey, EditorNetwork.Response callback)
        {
            if (!string.IsNullOrEmpty(developerKey))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "APIKEY:DEVELOPER " + developerKey);
                EditorNetwork.Get("https://" + EditorCore.DisplayValue(DisplayKey.GatewayURL) + "/v0/applicationKey", callback, headers, true);
            }
            else
            {
                callback.Invoke(0, "Invalid Developer Key", "");
            }
        }

        internal static bool IsCurrentSceneValid()
        {
            var currentScene = Cognitive3D_Preferences.FindCurrentScene();
            return !(currentScene == null || string.IsNullOrEmpty(currentScene.SceneId));
        }

        internal static void CheckSubscription(string developerKey, EditorNetwork.Response callback)
        {
            if (!string.IsNullOrEmpty(developerKey))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "APIKEY:DEVELOPER " + developerKey);
                EditorNetwork.Get("https://" + EditorCore.DisplayValue(DisplayKey.GatewayURL) + "/v0/subscriptions", callback, headers, true);
            }
            else
            {
                callback.Invoke(0, "Invalid Developer Key", "");
            }
        }

        internal static void GetUserData(string developerKey, EditorNetwork.Response callback)
        {
            if (!string.IsNullOrEmpty(developerKey))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "APIKEY:DEVELOPER " + developerKey);
                EditorNetwork.Get("https://" + EditorCore.DisplayValue(DisplayKey.GatewayURL) + "/v0/user", callback, headers, true);
            }
            else
            {
                callback.Invoke(0, "Invalid Developer Key", "");
            }
        }

        [System.Serializable]
        internal class ApplicationKeyResponseData
        {
            public string apiKey;
            public bool valid;
        }

        [System.Serializable]
        internal class UserData
        {
            public string email;
            public int userId;
            public string firstName;
            public string lastName;
            public int projectId;
            public string projectName;
            public int organizationId;
            public string organizationName;
            public long keyExpiresAt;
        }

        [System.Serializable]
        internal class OrganizationData
        {
            public string organizationName;
            public SubscriptionData[] subscriptions;
        }

        [System.Serializable]
        internal class SubscriptionData
        {
            public long beginning;
            public long expiration;
            public string planType;
            public bool isFreeTrial;

            public EntitlementData entitlements;
        }

        [System.Serializable]
        internal class EntitlementData
        {
            public bool can_access_session_audio;
            public bool can_access_eye_tracking;
            public bool can_create_ab_test;
        }
        #endregion

        #region Dynamic Object Aggregation Manifest


        //TODO move all this to export utility? editor core?
        /// <summary>
        /// generate manifest from scene objects and upload to latest version of scene. should be done only after EditorCore.RefreshSceneVersion
        /// </summary>
        public static void UploadManifest(AggregationManifest manifest, System.Action callback, System.Action nodynamicscallback = null)
        {
            if (manifest.objects.Count == 0)
            {
                Debug.LogWarning("Aggregation Manifest has nothing in list!");
                if (nodynamicscallback != null)
                {
                    nodynamicscallback.Invoke();
                }
                return;
            }

            int manifestCount = 0;
            //write up manifets into parts (if needed)
            int debugBreakManifestLimit = 99;
            while (true)
            {
                debugBreakManifestLimit--;
                if (debugBreakManifestLimit == 0) { Debug.LogError("dynamic aggregation manifest error"); break; }
                if (manifest.objects.Count == 0) { break; }

                AggregationManifest am = new AggregationManifest();
                am.objects.AddRange(manifest.objects.GetRange(0, Mathf.Min(250, manifest.objects.Count)));
                manifest.objects.RemoveRange(0, Mathf.Min(250, manifest.objects.Count));
                string json = "";
                if (ManifestToJson(am, out json))
                {
                    manifestCount++;
                    var currentSettings = Cognitive3D_Preferences.FindCurrentScene();
                    if (currentSettings != null && currentSettings.VersionNumber > 0)
                        SendManifest(json, currentSettings.VersionNumber, callback);
                    else
                        Util.logError("Could not find scene version for current scene");
                }
                else
                {
                    Debug.LogWarning("Aggregation Manifest only contains dynamic objects with generated ids");
                    if (nodynamicscallback != null)
                    {
                        nodynamicscallback.Invoke();
                    }
                }
            }
        }

        static bool ManifestToJson(AggregationManifest manifest, out string json)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"objects\":[");

            List<string> usedIds = new List<string>();
            bool containsValidEntry = false;
            foreach (var entry in manifest.objects)
            {
                if (string.IsNullOrEmpty(entry.mesh)) { Debug.LogWarning(entry.name + " missing meshname"); continue; }
                if (string.IsNullOrEmpty(entry.id)) { Debug.LogWarning(entry.name + " has empty dynamic id. This will not be aggregated"); continue; }
                if (usedIds.Contains(entry.id)) { Debug.LogWarning(entry.name + " using id (" + entry.id + ") that already exists in the scene. This may not be aggregated correctly"); }
                usedIds.Add(entry.id);
                sb.Append("{");
                sb.Append("\"id\":\"");
                sb.Append(entry.id);
                sb.Append("\",");

                sb.Append("\"isController\":\"");
                sb.Append(entry.isController);
                sb.Append("\",");

                sb.Append("\"mesh\":\"");
                sb.Append(entry.mesh);
                sb.Append("\",");

                sb.Append("\"name\":\"");
                sb.Append(entry.name);
                sb.Append("\",");

                sb.Append("\"scaleCustom\":[");
                sb.Append(entry.scaleCustom[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(entry.scaleCustom[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(entry.scaleCustom[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("],");

                sb.Append("\"initialPosition\":[");
                sb.Append(entry.position[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(entry.position[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(entry.position[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("],");

                sb.Append("\"initialRotation\":[");
                sb.Append(entry.rotation[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(entry.rotation[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(entry.rotation[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(entry.rotation[3].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("]");
                sb.Append("},");
                containsValidEntry = true;
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("]}");
            json = sb.ToString();

            return containsValidEntry;
        }

        static System.Action PostManifestResponseAction;
        static void SendManifest(string json, int versionNumber, System.Action callback)
        {
            var settings = Cognitive3D_Preferences.FindCurrentScene();
            if (settings == null)
            {
                Debug.LogWarning("Send Manifest settings are null " + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
                string s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(s))
                {
                    s = "Unknown Scene";
                }
                EditorUtility.DisplayDialog("Dynamic Object Manifest Upload Failed", "Could not find the Scene Settings for \"" + s + "\". Are you sure you've saved, exported and uploaded this scene to SceneExplorer?", "Ok");
                return;
            }

            string url = CognitiveStatics.PostDynamicManifest(settings.SceneId, versionNumber);
            Util.logDebug("Send Manifest Contents: " + json);

            //upload manifest
            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
            {
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                headers.Add("Content-Type", "application/json");
            }
            PostManifestResponseAction = callback;
            EditorNetwork.QueuePost(url, json, PostManifestResponse, headers, false);//AUTH
        }

        static void PostManifestResponse(int responsecode, string error, string text)
        {
            Util.logDebug("Manifest upload complete. responseCode: " + responsecode + " text: " + text + (!string.IsNullOrEmpty(error) ? " error: " + error : ""));
            if (PostManifestResponseAction != null)
            {
                PostManifestResponseAction.Invoke();
                PostManifestResponseAction = null;
            }
        }

        #endregion

        public static Vector3 TransformPointUnscaled(Transform transform, Vector3 position)
        {
            var localToWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            return localToWorldMatrix.MultiplyPoint3x4(position);
        }

        public static Vector3 InverseTransformPointUnscaled(Transform transform, Vector3 position)
        {
            var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(position);
        }
    }

    public static class ColorExtensions
    {
        public static Texture2D ToTexture(this Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}