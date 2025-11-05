using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cognitive3D
{
    [InitializeOnLoad]
    public class ProjectSetupWindow : EditorWindow
    {
        bool keysSet;
        private string developerKey;
        private string apiKey;
        private string devKeyStatusMessage = "";
        private MessageType devKeyStatusType = MessageType.None;

        #region Project Setup Window
        public static void Init()
        {
            SegmentAnalytics.TrackEvent("ProjectSetupWindow_Opened", "ProjectSetupWindow", "new");

            ProjectSetupWindow window = (ProjectSetupWindow)EditorWindow.GetWindow(typeof(ProjectSetupWindow), true, "Project Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(600, 800);
            window.Show();

            window.LoadKeys();
        }

        private void OnEnable()
        {
            if (autoSelectXR)
            {
                AutoSelectXRSDK();
            }

            CacheCurrentScenes();
            UploadTools.OnUploadScenesComplete += CacheCurrentScenes;
            EditorApplication.update += CheckForChanges;
        }

        private void OnDisable()
        {
            CacheCurrentScenes();
            UploadTools.OnUploadScenesComplete -= CacheCurrentScenes;
            EditorApplication.update -= CheckForChanges;
        }
        #endregion

        private Vector2 mainScroll;

        private bool forceUpdateApiKey;
        private string apiKeyFromDashboard = "";

        bool autoSelectXR = true;
        bool previousAutoSelectXR;
        int selectedSDKIndex;
        readonly Dictionary<string, string> availableXrSdks = new Dictionary<string, string>
        {
            { "MetaXR", "C3D_OCULUS" },
            { "PicoXR", "C3D_PICOXR" },
            { "ViveWave", "C3D_VIVEWAVE" },
            { "SteamVR (OpenVR)", "C3D_STEAMVR2" },
            { "SRAnipal", "C3D_SRANIPAL" },
            { "Omnicept", "C3D_OMNICEPT" },
            { "VarjoXR", "C3D_VARJOXR" },
            { "MRTK", "C3D_MRTK" },
            { "Default", "C3D_DEFAULT" }
        };

        GameObject hmd;
        GameObject trackingSpace;
        GameObject rightController;
        GameObject leftController;

        private Vector2 scrollPos;

        private bool selectAll;
        private readonly List<SceneEntry> sceneEntries = new List<SceneEntry>();

        private void OnGUI()
        {
            bool completenessStatus;
            Texture2D statusIcon;

            // Footer height
            float footerHeight = 60f;

            // Scrollable content area (from top of window to above footer)
            Rect contentRect = new Rect(0, 0, position.width, position.height - footerHeight);
            GUILayout.BeginArea(contentRect);
            mainScroll = GUILayout.BeginScrollView(mainScroll);

            // Header background and logo
            if (EditorCore.LogoTexture != null)
            {
                float bgHeight = 100f;

                Rect bgRect = new Rect(0, 0, position.width, bgHeight);
                GUI.DrawTexture(bgRect, EditorCore.BackgroundTexture, ScaleMode.ScaleAndCrop);

                float logoWidth = EditorCore.LogoTexture.width / 3f;
                float logoHeight = EditorCore.LogoTexture.height / 3f;
                float logoX = (position.width - logoWidth) / 2f;
                float logoY = (bgHeight - logoHeight) / 2f;

                GUI.DrawTexture(new Rect(logoX, logoY, logoWidth, logoHeight), EditorCore.LogoTexture, ScaleMode.ScaleToFit);

                GUILayout.Space(bgHeight);
            }

            using (new EditorGUILayout.VerticalScope(EditorCore.styles.ContextPadding))
            {
                GUILayout.Space(5);
                GUILayout.Label("Welcome to the " + EditorCore.DisplayValue(DisplayKey.FullName) + " SDK Project Setup", EditorCore.styles.FeatureTitle);
                GUILayout.Label(
                    "This window will guide you through setting up our SDK in your project and ensuring the features available from packages in your project are automatically recorded.",
                    EditorCore.styles.ItemDescription);

#region Dev and App keys
                completenessStatus = keysSet;
                statusIcon = GetStatusIcon(completenessStatus);

                DrawFoldout("Developer and App Keys", statusIcon, true, () =>
                {
                    GUILayout.Label("Enter your developer key:", EditorCore.styles.DescriptionPadding);
                    developerKey = EditorGUILayout.TextField("Developer Key", developerKey);
                    GUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal();

                    Rect apiKeyRect = EditorGUILayout.GetControlRect();

                    // Draw with EditorGUI to retain full control
                    apiKey = EditorGUI.TextField(apiKeyRect, "Application Key", forceUpdateApiKey ? apiKeyFromDashboard : apiKey);

                    if (forceUpdateApiKey)
                    {
                        forceUpdateApiKey = false;
                        GUI.FocusControl(null); // Optionally clear focus
                    }

                    if (GUILayout.Button("Get from Dashboard", GUILayout.Width(130)))
                    {
                        EditorCore.CheckForExpiredDeveloperKey(developerKey, GetDevKeyResponse);
                        EditorCore.CheckForApplicationKey(developerKey, GetApplicationKeyResponse);
                        EditorCore.GetUserData(developerKey, GetUserResponse);

                        forceUpdateApiKey = true;
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(10);

                    if (!string.IsNullOrEmpty(devKeyStatusMessage))
                    {
                        EditorGUILayout.HelpBox(devKeyStatusMessage, devKeyStatusType);
                    }
                });
#endregion

                EditorGUI.BeginDisabledGroup(!keysSet);
#region XR SDK
                completenessStatus = autoSelectXR;
                statusIcon = GetStatusIcon(completenessStatus);

                DrawFoldout("XR SDK Setup", statusIcon, keysSet, () =>
                {
                    GUILayout.Label(
                    "By default, XR plugins are auto-detected, and features are enabled based on the packages present in the project.",
                    EditorCore.styles.DescriptionPadding);

                    bool newAutoSelectXR = EditorGUILayout.Toggle(new GUIContent("Auto-select XR SDK", "Disable 'Auto-select XR SDK' to configure this manually"), autoSelectXR);

                    if (newAutoSelectXR != previousAutoSelectXR)
                    {
                        if (newAutoSelectXR)
                        {
                            SegmentAnalytics.TrackEvent("EnabledAutoXRSDKSetup_SDKDefinePage", "ProjectSetupSDKDefinePage", "new");
                            AutoSelectXRSDK();
                        }
                        else
                        {
                            SegmentAnalytics.TrackEvent("DisabledAutoXRSDKSetup_SDKDefinePage", "ProjectSetupSDKDefinePage", "new");
                        }
                    }

                    autoSelectXR = newAutoSelectXR;
                    previousAutoSelectXR = newAutoSelectXR;

                    using (new EditorGUI.DisabledScope(autoSelectXR))
                    {
                        selectedSDKIndex = EditorGUILayout.Popup("Select XR SDK", selectedSDKIndex, availableXrSdks.Keys.ToArray());
                    }

                    GUILayout.Space(10);

                    if (EditorCore.HasC3DDefine(out var c3dSymbols))
                    {
                        var readableNames = new List<string>();
                        foreach (var symbol in c3dSymbols)
                        {
                            var sdkName = availableXrSdks.FirstOrDefault(kvp => kvp.Value == symbol).Key;
                            readableNames.Add(string.IsNullOrEmpty(sdkName) ? symbol : sdkName);
                        }
                        string currentDefines = string.Join(", ", readableNames);
                        EditorGUILayout.HelpBox($"XR SDK setup complete. Currently configured for: {currentDefines}", MessageType.Info);
                    }
                    else
                    {
                        if (selectedSDKIndex >= 0 && selectedSDKIndex < availableXrSdks.Count)
                        {
                            EditorGUILayout.HelpBox($"XR SDK requires compilation. Click 'Compile and Finish' below to apply {availableXrSdks.Keys.ElementAt(selectedSDKIndex)} configuration.", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("No XR SDK selected. Please select an SDK from the dropdown above.", MessageType.Warning);
                        }
                    }
                });
#endregion

#region Player Setup
                completenessStatus = EditorCore.GetPreferences().AutoPlayerSetup;
                statusIcon = GetStatusIcon(completenessStatus);

                DrawFoldout("Player Setup", statusIcon, keysSet, () =>
                {
                    GUILayout.Label(
                    "By default, key player objects, including the camera (HMD), tracking space, and controllers are automatically detected and tracked.",
                    EditorCore.styles.DescriptionPadding);

                    bool newAutoPlayerSetupValue = EditorGUILayout.Toggle(
                        new GUIContent("Auto Player Setup", "Disable auto-setup to manually assign these from your existing Player Prefab"),
                        EditorCore.GetPreferences().AutoPlayerSetup
                    );

                    if (newAutoPlayerSetupValue != EditorCore.GetPreferences().AutoPlayerSetup)
                    {
                        EditorCore.GetPreferences().AutoPlayerSetup = newAutoPlayerSetupValue;

                        if (newAutoPlayerSetupValue)
                        {
                            SegmentAnalytics.TrackEvent("EnabledAutoPlayerSetup_PlayerSetupPage", "ProjectSetupPlayerSetupPage", "new");
                        }
                        else
                        {
                            SegmentAnalytics.TrackEvent("DisabledAutoPlayerSetup_PlayerSetupPage", "ProjectSetupPlayerSetupPage", "new");
                        }
                    }

                    GUILayout.Space(10);

                    if (!EditorCore.GetPreferences().AutoPlayerSetup)
                    {
                        EditorGUILayout.HelpBox("For SteamVR, assign GameObjects with SteamVR_Behaviour_Pose components to the controller fields.", MessageType.Info);

                        GUILayout.Space(5);

                        hmd = (GameObject)EditorGUILayout.ObjectField(new GUIContent("HMD", "The display for HMD should be tagged as MainCamera"), hmd, typeof(GameObject), true);
                        trackingSpace = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Tracking Space", "The TrackingSpace is the root transform for the HMD and controllers"), trackingSpace, typeof(GameObject), true);
                        rightController = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Right Controller", "The Right Controller may have Tracked Pose Driver component"), rightController, typeof(GameObject), true);
                        leftController = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Left Controller", "The Left Controller may have Tracked Pose Driver component"), leftController, typeof(GameObject), true);

                        GUILayout.Space(5);

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Set Player", GUILayout.Width(100)))
                        {
                            EditorCore.SetMainCamera(hmd);
                            EditorCore.SetTrackingSpace(trackingSpace);
                            EditorCore.SetController(true, rightController);
                            EditorCore.SetController(false, leftController);
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        GUILayout.Space(5);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Auto Player Setup is enabled, allowing all player-related objects to be automatically detected and tracked.", MessageType.Info);
                    }
                });
#endregion

#region Scene Upload
                completenessStatus = Cognitive3D_Preferences.Instance.sceneSettings.Count > 0;
                statusIcon = GetStatusIcon(completenessStatus);

                DrawFoldout("Scene Upload", statusIcon, keysSet, () =>
                {
                    GUILayout.Label("Configure which scenes from the Build Settings should be prepared and uploaded. Ensure all the scenes you want to track are added to the Build Settings.", EditorCore.styles.DescriptionPadding);
                    GUILayout.BeginHorizontal(EditorCore.styles.HelpBoxPadding);

                    // Warning icon
                    GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(35), GUILayout.Height(35));
                    GUILayout.Label(
                        "For additive scenes, make sure to follow the setup instructions in the documentation.",
                        EditorCore.styles.HelpBoxLabel
                    );

                    if (GUILayout.Button(EditorCore.ExternalLinkIcon, EditorCore.styles.ExternalLink))
                    {
                        Application.OpenURL("https://docs.cognitive3d.com/unity/scenes/#additive-scene-loading");
                    }
                    GUILayout.FlexibleSpace(); // Push content to the left
                    GUILayout.EndHorizontal();

                    if (EditorBuildSettings.scenes.Length == 0)
                    {
                        GUILayout.BeginHorizontal(EditorCore.styles.HelpBoxPadding);
                        // Display error icon
                        GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon"), GUILayout.Width(35), GUILayout.Height(35));
                        GUILayout.Label(
                            "No scenes have been added to the Build Settings.",
                            EditorCore.styles.HelpBoxLabel
                        );
                        GUILayout.EndHorizontal();
                    }

                    EditorGUILayout.BeginVertical(EditorCore.styles.ListBoxPadding);

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    // Select All Toggle
                    bool newSelectAll = EditorGUILayout.Toggle(selectAll, GUILayout.Width(40));
                    if (newSelectAll != selectAll)
                    {
                        selectAll = newSelectAll;
                        foreach (var scene in sceneEntries)
                        {
                            scene.selected = selectAll;
                        }
                    }
                    DrawColumnSeparator();

                    // Column Headers
                    GUILayout.Label("Scene Name", EditorCore.styles.LeftPaddingBoldLabel, GUILayout.Width(185));
                    DrawColumnSeparator();
                    GUILayout.Label("Version Number", EditorCore.styles.LeftPaddingBoldLabel);

                    // Flexible space to push icon to the right
                    GUILayout.FlexibleSpace();

                    // Gear Icon Button
                    if (GUILayout.Button(new GUIContent(EditorCore.SettingsIcon2, "Additional Settings"), EditorStyles.toolbarButton, GUILayout.Width(24)))
                    {
                        GenericMenu gm = new GenericMenu();
                        gm.AddItem(new GUIContent("Full Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 1, OnSelectFullResolution);
                        gm.AddItem(new GUIContent("Half Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 2, OnSelectHalfResolution);
                        gm.AddItem(new GUIContent("Quarter Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 4, OnSelectQuarterResolution);
                        gm.AddSeparator("");
                        gm.AddItem(new GUIContent("Export lowest LOD meshes"), Cognitive3D_Preferences.Instance.ExportSceneLODLowest, OnToggleLODMeshes);

#if UNITY_2020_1_OR_NEWER
                        gm.AddItem(new GUIContent("Include Disabled Dynamic Objects"), Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects, OnToggleIncludeDisabledDynamics);
#endif
                        gm.ShowAsContext();
                    }

                    EditorGUILayout.EndHorizontal();

                    // Scrollable list of scenes
                    scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));

                    for (int i = 0; i < sceneEntries.Count; i++)
                    {
                        string sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneEntries[i].path);

                        EditorGUILayout.BeginHorizontal();
                        sceneEntries[i].selected = EditorGUILayout.Toggle(sceneEntries[i].selected, GUILayout.Width(40));

                        GUILayout.Space(5);
                        GUILayout.Label(sceneName, EditorCore.styles.LeftPaddingLabel, GUILayout.Width(185));

                        GUILayout.Label(sceneEntries[i].versionNumber.ToString(), EditorCore.styles.LeftPaddingLabel);
                        EditorGUILayout.EndHorizontal();
                    }

                    bool allSelected = true;
                    foreach (var scene in sceneEntries)
                    {
                        if (!scene.selected)
                        {
                            allSelected = false;
                            break;
                        }
                    }

                    if (selectAll != allSelected)
                    {
                        selectAll = allSelected;
                    }

                    GUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                });
#endregion

                EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Sticky footer button
            Rect footerRect = new Rect(0, position.height - footerHeight, position.width, footerHeight);
            DrawFooter(footerRect);
        }

        private void DrawFoldout(string title, Texture2D icon, bool foldout, Action drawContent)
        {
            using (var scope = new EditorGUILayout.VerticalScope(EditorCore.styles.List))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.Foldout(foldout, title, true);
                if (icon != null)
                {
                    GUILayout.Label(icon, EditorCore.styles.InlinedIconStyle);
                }
                GUILayout.EndHorizontal();

                if (foldout)
                {
                    using (new EditorGUILayout.VerticalScope(EditorCore.styles.ListLabel))
                    {
                        EditorGUI.indentLevel++;
                        drawContent?.Invoke();
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        Texture2D GetStatusIcon(bool condition)
        {
            return condition ? EditorCore.CompleteCheckmark : EditorCore.CircleWarning;
        }

        private void DrawColumnSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 18, GUILayout.Width(1));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        #region Footer
        private void DrawFooter(Rect footerRect)
        {
            GUILayout.BeginArea(footerRect, EditorStyles.helpBox);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool xrSdkNeedsUpdate = XRSDKNeedsUpdate();
            var selectedScenes = UploadTools.GetSelectedScenes(sceneEntries);
            bool hasScenesToUpload = selectedScenes.Count > 0;

            string footerButtonText = GetFooterButtonText(hasScenesToUpload, xrSdkNeedsUpdate);

            EditorGUI.BeginDisabledGroup(!keysSet); // disable if keySet is false
            if (GUILayout.Button(footerButtonText, GUILayout.Width(140), GUILayout.Height(30)))
            {
                HandleFooterButtonClick(hasScenesToUpload, xrSdkNeedsUpdate, selectedScenes);
                Close();  // Close the window
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.EndArea();
        }

        private string GetFooterButtonText(bool hasScenesToUpload, bool xrSdkNeedsUpdate)
        {
            if (hasScenesToUpload && xrSdkNeedsUpdate)
                return "Upload and Compile";
            if (hasScenesToUpload)
                return "Upload and Finish";
            if (xrSdkNeedsUpdate)
                return "Compile and Finish";
            return "Finish";
        }

        private bool xrSdkPendingAfterUpload;

        private void HandleFooterButtonClick(bool hasScenesToUpload, bool xrSdkNeedsUpdate, List<SceneEntry> selectedScenes)
        {
            if (hasScenesToUpload)
            {
                if (xrSdkNeedsUpdate && !xrSdkPendingAfterUpload)
                {
                    // Wait until upload is complete before setting XRSDK
                    xrSdkPendingAfterUpload = true;
                    UploadTools.OnUploadScenesComplete += ApplyXRSDKAndWaitForCompile;
                }

                UploadTools.UploadScenes(selectedScenes);
            }
            else if (xrSdkNeedsUpdate)
            {
                // No scenes to upload, set XR SDK
                ApplyXRSDKAndWaitForCompile();
            }
        }
        #endregion

        #region Developer and App Key Utilities
        private void LoadKeys()
        {
            developerKey = EditorCore.DeveloperKey;
            apiKey = EditorCore.GetPreferences().ApplicationKey;

            if (!string.IsNullOrEmpty(developerKey) && !string.IsNullOrEmpty(apiKey))
            {
                EditorCore.CheckForExpiredDeveloperKey(developerKey, GetDevKeyResponse);
                EditorCore.CheckForApplicationKey(developerKey, GetApplicationKeyResponse);
                EditorCore.GetUserData(developerKey, GetUserResponse);
            }

            EditorCore.RefreshSceneVersionComplete += CacheCurrentScenes;
        }

        private void SaveDevKey()
        {
            EditorCore.DeveloperKey = developerKey;

            if (!string.IsNullOrEmpty(developerKey) && !string.IsNullOrEmpty(apiKey))
            {
                keysSet = true;
            }
        }

        private void SaveApplicationKey()
        {
            EditorCore.GetPreferences().ApplicationKey = apiKey;
            EditorUtility.SetDirty(EditorCore.GetPreferences());
            AssetDatabase.SaveAssets();
        }

        private int GetDaysUntilExpiry(long unixTimestamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime expiryDate = dateTime.AddSeconds(unixTimestamp / 1000.0).ToLocalTime();
            TimeSpan timeLeft = expiryDate - DateTime.Now;

            return Mathf.Max(0, (int)Math.Floor(timeLeft.TotalDays));
        }
        #endregion

        #region XR SDK Utilities
        [System.NonSerialized]
        double compileStartTime = -1;
        void SetXRSDK()
        {
            // Validate selectedSDKIndex is within bounds
            if (selectedSDKIndex < 0 || selectedSDKIndex >= availableXrSdks.Count)
            {
                Debug.LogError("Invalid SDK index. Please ensure XR SDK is properly selected.");
                return;
            }

            SegmentAnalytics.TrackEvent("SDKDefineIsSet_SDKDefinePage", "ProjectSetupSDKDefinePage", "new");
            EditorCore.SetPlayerDefine(availableXrSdks.Values.ElementAt(selectedSDKIndex));

            if (compileStartTime < 0)
            {
                compileStartTime = EditorApplication.timeSinceStartup;
            }
        }

        private void AutoSelectXRSDK()
        {
            EditorCore.GetPackages(OnGetPackages);
        }

        private bool XRSDKNeedsUpdate()
        {
            // Validate selectedSDKIndex is within bounds
            if (selectedSDKIndex < 0 || selectedSDKIndex >= availableXrSdks.Count)
            {
                return false;
            }
            
            if (EditorCore.HasC3DDefine(out var c3dSymbols))
            {
                string selectedSdk = availableXrSdks.Values.ElementAt(selectedSDKIndex);
                foreach (var symbol in c3dSymbols)
                {
                    if (!symbol.Equals(selectedSdk))
                    {
                        return true;
                    }
                }
            }

            if (!EditorCore.HasC3DDefine())
            {
                return true;
            }
            return false;
        }

        void OnGetPackages(UnityEditor.PackageManager.PackageCollection packages)
        {
            //search from specific sdks (single headset support) to general runtimes (openvr, etc)
            string packageName;
            var XrSdks = availableXrSdks.Keys.ToArray();
            foreach (var package in packages)
            {
                if (package.name == "com.unity.xr.picoxr")
                {
                    packageName = "PicoXR";
                    selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                    return;
                }
                if (package.name == "com.varjo.xr")
                {
                    packageName = "VarjoXR";
                    selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                    return;
                }
                if (package.name == "com.htc.upm.wave.xrsdk")
                {
                    packageName = "ViveWave";
                    selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                    return;
                }
            }

            //specific assets
            var SRAnipalAssets = AssetDatabase.FindAssets("SRanipal");
            if (SRAnipalAssets.Length > 0)
            {
                packageName = "SRAnipal";
                selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                return;
            }

            var GliaAssets = AssetDatabase.FindAssets("lib-client-csharp");
            if (GliaAssets.Length > 0)
            {
                packageName = "Omnicept";
                selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                return;
            }

            var OculusIntegrationAssets = AssetDatabase.FindAssets("t:assemblydefinitionasset oculus.vr");
            if (OculusIntegrationAssets.Length > 0)
            {
                packageName = "MetaXR";
                selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                return;
            }

            var HololensAssets = AssetDatabase.FindAssets("WindowsMRAssembly");
            if (HololensAssets.Length > 0)
            {
                packageName = "MRTK";
                selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                return;
            }

            //general packages
            foreach (var package in packages)
            {
                if (package.name == "com.openvr")
                {
                    packageName = "SteamVR (OpenVR)";
                    selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                    return;
                }
            }

            var SteamVRAssets = AssetDatabase.FindAssets("t:assemblydefinitionasset steamvr");
            if (SteamVRAssets.Length > 0)
            {
                packageName = "SteamVR (OpenVR)";
                selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
                return;
            }

            //default fallback
            packageName = "Default";
            selectedSDKIndex = Array.IndexOf(XrSdks, packageName);
            return;
        }

        private void ApplyXRSDKAndWaitForCompile()
        {
            UploadTools.OnUploadScenesComplete -= ApplyXRSDKAndWaitForCompile;
            xrSdkPendingAfterUpload = false;

            SetXRSDK();
            compileStartTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += MonitorCompileAfterXRSDKChange;
        }

        private void MonitorCompileAfterXRSDKChange()
        {
            if (EditorApplication.isCompiling)
            {
                float elapsed = (float)(EditorApplication.timeSinceStartup - compileStartTime);
                float progress = Mathf.Clamp(Mathf.Log10(elapsed + 1), 0.05f, 0.95f);
                EditorUtility.DisplayProgressBar("Compiling", "Setting player definition...", progress);
                return;
            }

            // Compilation finished
            EditorApplication.update -= MonitorCompileAfterXRSDKChange;
            EditorUtility.ClearProgressBar();
            compileStartTime = -1;
        }
        #endregion
        #region Build Setting Scene Utilities
        private string[] cachedScenePaths;

        void CacheCurrentScenes()
        {
            cachedScenePaths = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            sceneEntries.Clear();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                var c3dScene = Cognitive3D_Preferences.FindSceneByPath(scene.path);
                int versionNumber = c3dScene?.VersionNumber ?? 0;

                bool isSelected = versionNumber == 0;

                var sceneEntry = new SceneEntry(
                    scene.path,
                    versionNumber,
                    isSelected,
                    true
                );

                sceneEntries.Add(sceneEntry);
            }
        }

        void CheckForChanges()
        {
            var currentPaths = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            if (!cachedScenePaths.SequenceEqual(currentPaths))
            {
                CacheCurrentScenes();
            }
        }

        void OnSelectFullResolution()
        {
            Cognitive3D_Preferences.Instance.TextureResize = 1;
        }
        void OnSelectHalfResolution()
        {
            Cognitive3D_Preferences.Instance.TextureResize = 2;
        }
        void OnSelectQuarterResolution()
        {
            Cognitive3D_Preferences.Instance.TextureResize = 4;
        }
        void OnToggleLODMeshes()
        {
            Cognitive3D_Preferences.Instance.ExportSceneLODLowest = !Cognitive3D_Preferences.Instance.ExportSceneLODLowest;
        }

        //Unity 2020.1+
        void OnToggleIncludeDisabledDynamics()
        {
            Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects = !Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects;
        }
        #endregion
        #region Callback Responses
        void GetDevKeyResponse(int responseCode, string error, string text)
        {
            if (responseCode == 200)
            {
                //dev key is fine
                SaveDevKey();
            }
            else
            {
                Debug.LogError("Developer Key invalid or expired: " + error);

                devKeyStatusMessage = "Developer Key invalid or expired.";
                devKeyStatusType = MessageType.Error;
                keysSet = false;
            }
        }

        private void GetApplicationKeyResponse(int responseCode, string error, string text)
        {
            if (responseCode != 200)
            {
                SegmentAnalytics.TrackEvent("InvalidDevKey_ProjectSetup_" + responseCode, "ProjectSetupAPIPage", "new");
                Debug.LogError("GetApplicationKeyResponse response code: " + responseCode + " error: " + error);
                return;
            }

            // Check if response data is valid
            try
            {
                JsonUtility.FromJson<EditorCore.ApplicationKeyResponseData>(text);
                SegmentAnalytics.TrackEvent("ValidDevKey_ProjectSetup", "ProjectSetupAPIPage", "new");
            }
            catch
            {
                Debug.LogError("Invalid JSON response");
                return;
            }

            EditorCore.ApplicationKeyResponseData responseData = JsonUtility.FromJson<EditorCore.ApplicationKeyResponseData>(text);

            //display popup if application key is set but doesn't match the response
            if (!string.IsNullOrEmpty(apiKey) && apiKey != responseData.apiKey)
            {
                SegmentAnalytics.TrackEvent("APIKeyMismatch_ProjectSetup", "ProjectSetupAPIPage", "new");
                var result = EditorUtility.DisplayDialog("Application Key Mismatch", "Do you want to use the latest Application Key available on the Dashboard?", "Ok", "No");
                if (result)
                {
                    apiKey = responseData.apiKey;
                    apiKeyFromDashboard = apiKey;
                    SaveApplicationKey();
                }
            }
            else
            {
                SegmentAnalytics.TrackEvent("APIKeyFound_ProjectSetup", "ProjectSetupAPIPage", "new");
                apiKey = responseData.apiKey;
                apiKeyFromDashboard = apiKey;
                SaveApplicationKey();
            }
        }

        private void GetUserResponse(int responseCode, string error, string text)
        {
            if (responseCode != 200)
            {
                Util.logDevelopment("Failed to retrieve user data" + responseCode + "  " + error);
            }

            try
            {
                var userdata = JsonUtility.FromJson<EditorCore.UserData>(text);
                if (responseCode == 200 && userdata != null)
                {
                    devKeyStatusMessage = $"Organization name: {userdata.organizationName}";
                    long expiration = userdata.keyExpiresAt;
                    int daysRemaining = GetDaysUntilExpiry(expiration);

                    if (string.IsNullOrEmpty(userdata.organizationName))
                    {
                        devKeyStatusMessage += "\nCurrent Subscription Plan: No Subscription";
                    }
                    else
                    {
                        if (expiration == 0)
                        {
                            devKeyStatusType = MessageType.Info;
                            devKeyStatusMessage += "\nDeveloper key is valid and does not expire.";
                        }
                        else if (daysRemaining < 0)
                        {
                            devKeyStatusType = MessageType.Error;
                            devKeyStatusMessage += "\nDeveloper key has expired.";
                        }
                        else
                        {
                            devKeyStatusType = daysRemaining < 7 ? MessageType.Warning : MessageType.Info;
                            devKeyStatusMessage += $"\nDeveloper key valid. Expires in {daysRemaining} day(s).";
                        }
                    }
                }
                SegmentAnalytics.Init();
            }
            catch
            {
                Debug.LogError("Invalid JSON response");
                return;
            }
        }
#endregion
        }
}
