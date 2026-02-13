using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cognitive3D
{
    public class SceneManagerWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private readonly List<SceneEntry> sceneEntries = new List<SceneEntry>();
        private bool refreshList = true;
        private bool shouldFetchVersions = true;
        private SceneEntry expandedEntry = null;

        // Status icon colors
        private static readonly Color StatusGreen = new Color(0.42f, 0.74f, 0.42f);
        private static readonly Color StatusYellow = new Color(0.77f, 0.66f, 0.30f);
        private static readonly Color StatusGray = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color StatusRed = new Color(0.7f, 0.35f, 0.35f);
        private static readonly Color SeparatorColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color ExpandedBgColor = new Color(0.18f, 0.22f, 0.28f, 0.5f);

        public static void Init()
        {
            SegmentAnalytics.TrackEvent("SceneManagerWindow_Opened", "SceneManagerWindow", "new");

            SceneManagerWindow window = (SceneManagerWindow)EditorWindow.GetWindow(typeof(SceneManagerWindow), true, "Scene Manager (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(600, 800);
            window.Show();
        }

        void OnEnable()
        {
            // Light refresh - only load from preferences, don't fetch versions from API
            RefreshSceneList(fetchVersions: false);
        }

        #region Visual Elements
        public void OnGUI()
        {
            if (refreshList)
            {
                GetSceneEntries();
            }

            GUILayout.BeginVertical(EditorCore.styles.DetailContainer);

            // ─── Header ───
            DrawWindowHeader();

            EditorGUILayout.Space(8);

            // ─── Current Scene Panel ───
            DrawCurrentSceneSection();

            EditorGUILayout.Space(6);

            // ─── Separator ───
            DrawHorizontalLine();

            EditorGUILayout.Space(6);

            // ─── All Scenes Table ───
            DrawAllScenesSection();

            GUILayout.EndVertical();
        }

        private void DrawWindowHeader()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Scene Manager", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Scenes documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/scenes/");
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Manage your scenes, export scene geometry, upload new scenes, update existing scene versions, or view them on the dashboard.",
                EditorStyles.wordWrappedLabel
            );

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
        }

        private void DrawCurrentSceneSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

            // ─── Header row with label + status badge ───
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Current Scene", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(currentScene.name))
            {
                var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScene.path);
                bool hasExportFiles = currentSettings != null && EditorCore.HasSceneExportFiles(currentSettings);
                bool isUploaded = currentSettings != null && currentSettings.VersionId > 0;

                // Status badge
                DrawStatusBadge(hasExportFiles, isUploaded);
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(currentScene.name))
            {
                EditorGUILayout.HelpBox("No scene is currently open. Open a scene to export and upload it.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var settings = Cognitive3D_Preferences.FindSceneByPath(currentScene.path);
            bool configured = settings != null && !string.IsNullOrEmpty(settings.SceneId);
            bool exported = settings != null && EditorCore.HasSceneExportFiles(settings);
            bool uploaded = settings != null && settings.VersionId > 0;

            // ─── Scene info ───
            DrawFieldRow("Name:", currentScene.name);

            if (settings != null)
            {
                DrawFieldRow("Scene ID:", string.IsNullOrEmpty(settings.SceneId) ? "Not Set" : settings.SceneId);
                DrawFieldRow("Version Number:", uploaded ? settings.VersionNumber.ToString() : "Not Uploaded");
            }

            GUILayout.Space(6);

            // ─── Contextual help message ───
            if (!exported)
            {
                EditorGUILayout.HelpBox("Export the scene geometry first, then upload it to the dashboard.", MessageType.Info);
            }
            else if (exported && !uploaded)
            {
                EditorGUILayout.HelpBox("Scene geometry exported. Upload it to the dashboard for 3D session visualization.", MessageType.Info);
            }
            else if (uploaded)
            {
                EditorGUILayout.HelpBox("Scene is uploaded and ready. You can update the scene with new geometry or view it on the dashboard.", MessageType.Info);
            }

            GUILayout.Space(6);

            // ─── Step indicator ───
            DrawStepIndicator(exported, uploaded);

            GUILayout.Space(6);

            // ─── Action buttons ───
            EditorGUILayout.BeginHorizontal();

            // Export — always available
            if (GUILayout.Button(exported ? "Re-Export Scene" : "Export Scene", GUILayout.Height(30)))
            {
                ExportCurrentScene();
            }

            // Upload / Update — enabled after export
            EditorGUI.BeginDisabledGroup(!exported);
            if (GUILayout.Button("Upload Scene", GUILayout.Height(30)))
            {
                UploadCurrentScene();
            }
            EditorGUI.BeginDisabledGroup(!uploaded);
            if (GUILayout.Button("Update Scene", GUILayout.Height(30)))
            {
                // Wrap current scene in a SceneEntry for the version selection flow
                UpdateCurrentScene(settings);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

            // Dashboard button
            EditorGUI.BeginDisabledGroup(!configured || !uploaded);
            if (GUILayout.Button(new GUIContent(EditorCore.ExternalIcon, "Open in Dashboard"), GUILayout.Width(34), GUILayout.Height(30)))
            {
                string url = CognitiveStatics.GetSceneUrl(settings.SceneId, settings.VersionNumber);
                Application.OpenURL(url);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStepIndicator(bool exported, bool uploaded)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Step 1: Export
            DrawStepLabel("1. Export", exported, !exported);

            // Connector line
            DrawStepConnector(exported);

            // Step 2: Upload
            DrawStepLabel("2. Upload", uploaded, exported && !uploaded);

            // Connector line
            DrawStepConnector(uploaded);

            // Step 3: Done
            DrawStepLabel("3. Done", uploaded, false);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStepLabel(string text, bool done, bool active)
        {
            Color prevColor = GUI.color;

            if (done)
                GUI.color = StatusGreen;
            else if (active)
                GUI.color = Color.white;
            else
                GUI.color = StatusGray;

            GUIStyle stepStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = active ? FontStyle.Bold : FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };

            string prefix = done ? "✓ " : "";
            GUILayout.Label(prefix + text, stepStyle, GUILayout.Width(70));

            GUI.color = prevColor;
        }

        private void DrawStepConnector(bool completed)
        {
            Rect lineRect = GUILayoutUtility.GetRect(30, 14, GUILayout.Width(30));
            float y = lineRect.y + lineRect.height / 2;
            EditorGUI.DrawRect(new Rect(lineRect.x, y - 1, lineRect.width, 2),
                completed ? StatusGreen : new Color(0.3f, 0.3f, 0.3f));
        }

        private void DrawStatusBadge(bool exported, bool uploaded)
        {
            string label;
            Color bgColor;

            if (uploaded)
            {
                label = " Uploaded ";
                bgColor = new Color(0.2f, 0.35f, 0.2f);
            }
            else if (exported)
            {
                label = " Exported ";
                bgColor = new Color(0.35f, 0.3f, 0.15f);
            }
            else
            {
                label = " Not Exported ";
                bgColor = new Color(0.35f, 0.2f, 0.2f);
            }

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                margin = new RectOffset(0, 0, 5, 0),
                padding = new RectOffset(4, 4, 2, 2)
            };

            Rect badgeRect = GUILayoutUtility.GetRect(new GUIContent(label), badgeStyle);
            EditorGUI.DrawRect(badgeRect, bgColor);
            GUI.Label(badgeRect, label, badgeStyle);
        }

        private void DrawFieldRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(value);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHorizontalLine()
        {
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, SeparatorColor);
        }

        private void DrawAllScenesSection()
        {
            if (sceneEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No scenes found in preferences. Add scenes to your project and configure them in Cognitive3D settings.", MessageType.Info);
                return;
            }

            // Section header with count + refresh
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("All Scenes (" + sceneEntries.Count + ")", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Legend
            DrawLegendDot(StatusGreen, "Uploaded");
            GUILayout.Space(8);
            DrawLegendDot(StatusYellow, "Exported");
            GUILayout.Space(8);
            DrawLegendDot(StatusGray, "Not Exported");
            GUILayout.Space(12);

            if (GUILayout.Button(new GUIContent(EditorCore.RefreshIcon, "Refresh List and Update Scene Versions"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                // Full refresh - load from preferences AND fetch latest versions from API
                RefreshSceneList(fetchVersions: true);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Table - expands to fill remaining space
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
            DrawHeader();

            // Scrollable area that fills available space
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.ExpandHeight(true));

            foreach (var entry in sceneEntries)
            {
                DrawSceneRow(entry);
            }

            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLegendDot(Color color, string label)
        {
            Rect dotRect = GUILayoutUtility.GetRect(10, 14, GUILayout.Width(10));
            float dotY = dotRect.y + dotRect.height / 2;
            float dotX = dotRect.x + 5;

            // Draw circle
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(dotX, dotY, 0), Vector3.forward, 4f);
            Handles.EndGUI();

            GUIStyle legendStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
            GUILayout.Label(label, legendStyle);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Status column header - matches dot (10px) + arrow (15px) width
            GUILayout.Label("Status", GUILayout.Width(50));
            DrawColumnSeparator();

            // Scene Name header
            GUILayout.Label("Scene Name", GUILayout.Width(130));
            DrawColumnSeparator();

            // Scene ID header
            GUILayout.Label("Scene ID", GUILayout.Width(250));
            DrawColumnSeparator();

            // Version header
            GUILayout.Label("Version", EditorCore.styles.CenteredLabel, GUILayout.Width(100));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 18, GUILayout.Width(1));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        private void DrawSceneRow(SceneEntry entry)
        {
            bool isExpanded = expandedEntry == entry;
            bool isUploaded = !string.IsNullOrEmpty(entry.sceneId) && entry.versionId > 0;
            bool isExported = entry.hasExportFiles;

            EditorGUILayout.BeginVertical();

            // ─── Main row ───
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(30));

            // Alternating row background
            if (Event.current.type == EventType.Repaint)
            {
                int idx = sceneEntries.IndexOf(entry);
                if (idx % 2 == 1)
                {
                    EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.1f));
                }

                if (isExpanded)
                {
                    EditorGUI.DrawRect(rowRect, ExpandedBgColor);
                }
            }

            // Make entire row clickable for expand/collapse (exclude action buttons area)
            Rect clickableArea = new Rect(rowRect.x, rowRect.y, rowRect.width - 80, rowRect.height);
            if (Event.current.type == EventType.MouseDown && clickableArea.Contains(Event.current.mousePosition))
            {
                expandedEntry = isExpanded ? null : entry;
                Event.current.Use();
                GUI.changed = true;
            }

            // Change cursor to pointer when hovering over clickable area
            if (clickableArea.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(clickableArea, MouseCursor.Link);
            }

            // Expand/Collapse Arrow (visual indicator only, row is clickable)
            string arrow = isExpanded ? "▼" : "▶";
            GUIStyle arrowStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft
            };
            GUILayout.Label(arrow, arrowStyle, GUILayout.Width(15), GUILayout.Height(25));

            // Status dot (circle)
            Color statusColor = isUploaded ? StatusGreen : isExported ? StatusYellow : StatusGray;
            Rect dotRect = GUILayoutUtility.GetRect(50, 30, GUILayout.Width(50));
            float dotY = dotRect.y + dotRect.height / 2;
            float dotX = dotRect.x + 10;

            Handles.BeginGUI();
            Handles.color = statusColor;
            Handles.DrawSolidDisc(new Vector3(dotX, dotY, 0), Vector3.forward, 4f);
            Handles.EndGUI(); 

            // Scene Name - vertically centered
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft
            };
            GUILayout.Label(entry.sceneName, labelStyle, GUILayout.Width(130), GUILayout.Height(26));

            // Scene ID (truncated) - vertically centered
            string idDisplay = string.IsNullOrEmpty(entry.sceneId) ? "Not Set" : TruncateId(entry.sceneId, 25);
            GUILayout.Label(idDisplay, labelStyle, GUILayout.Width(250), GUILayout.Height(26));

            // Version Number - centered both vertically and horizontally
            string versionDisplay = entry.versionNumber > 0 ? entry.versionNumber.ToString() : "-";
            GUIStyle centeredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label(versionDisplay, centeredStyle, GUILayout.Width(100), GUILayout.Height(26));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // ─── Expanded details + inline actions ───
            if (isExpanded)
            {
                DrawExpandedDetails(entry, isExported, isUploaded);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExpandedDetails(SceneEntry entry, bool isExported, bool isUploaded)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(4);

            // Info rows - with word wrapping for long text
            GUIStyle wrappedLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Scene Id:", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(string.IsNullOrEmpty(entry.sceneId) ? "Not Set" : entry.sceneId, wrappedLabelStyle, GUILayout.MaxWidth(450));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Scene Path:", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(string.IsNullOrEmpty(entry.scenePath) ? "Not Set" : entry.scenePath, wrappedLabelStyle, GUILayout.MaxWidth(450));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Version Number:", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(entry.versionNumber > 0 ? entry.versionNumber.ToString() : "Not Set");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Version Id:", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(entry.versionId > 0 ? entry.versionId.ToString() : "Not Set");
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(entry.sceneSettings.LastRevision))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("Last Revision:", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label(entry.sceneSettings.LastRevision);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(6);

            // ─── Inline action buttons ───
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            if (GUILayout.Button(new GUIContent("Open", "Opens the scene in project"), GUILayout.Width(80), GUILayout.Height(22)))
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.scenePath);
                }
            }

            if (GUILayout.Button(new GUIContent("Export", "Exports the scene"), GUILayout.Width(80), GUILayout.Height(22)))
            {
                ExportScene(entry);
            }

            EditorGUI.BeginDisabledGroup(!isExported);
            if (GUILayout.Button(new GUIContent("Upload New", "Uploads a new version of the scene"), GUILayout.Width(90), GUILayout.Height(22)))
            {
                UploadScene(entry);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!isExported || !isUploaded);
            if (GUILayout.Button(new GUIContent("Update Version", "Updates the version of the scene"), GUILayout.Width(100), GUILayout.Height(22)))
            {
                UpdateScene(entry);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!isUploaded);
            if (GUILayout.Button(new GUIContent("Dashboard", "Opens the scene on dashboard"), GUILayout.Width(80), GUILayout.Height(22)))
            {
                OpenSceneDashboard(entry);
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(new GUIContent("Remove", "Removes the tracked scene from Cognitive3D preferences file"), GUILayout.Width(80), GUILayout.Height(22)))
            {
                RemoveSceneFromTracking(entry);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private string TruncateId(string id, int maxLength)
        {
            if (string.IsNullOrEmpty(id) || id.Length <= maxLength)
                return id;
            return id.Substring(0, maxLength) + "...";
        }

        #endregion

        #region Scene Management
        /// <summary>
        /// Refreshes the scene list.
        /// </summary>
        /// <param name="fetchVersions">If true, fetches latest versions from API. If false, only loads from preferences.</param>
        internal void RefreshSceneList(bool fetchVersions = true)
        {
            refreshList = true;
            shouldFetchVersions = fetchVersions;
        }

        private void GetSceneEntries()
        {
            sceneEntries.Clear();

            if (Cognitive3D_Preferences.Instance == null || Cognitive3D_Preferences.Instance.sceneSettings == null)
            {
                refreshList = false;
                return;
            }

            foreach (var sceneSettings in Cognitive3D_Preferences.Instance.sceneSettings)
            {
                if (sceneSettings == null) continue;

                bool hasExport = EditorCore.HasSceneExportFiles(sceneSettings);

                sceneEntries.Add(new SceneEntry
                {
                    sceneName = sceneSettings.SceneName,
                    sceneId = sceneSettings.SceneId,
                    scenePath = sceneSettings.ScenePath,
                    versionNumber = sceneSettings.VersionNumber,
                    versionId = sceneSettings.VersionId,
                    hasExportFiles = hasExport,
                    sceneSettings = sceneSettings
                });
            }

            if (shouldFetchVersions)
            {
                shouldFetchVersions = false;
                EditorCore.RefreshAllScenesVersion(() =>
                {
                    refreshList = true;
                });
            }

            refreshList = false;
        }

        private void ExportCurrentScene()
        {
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(currentScene.name))
            {
                EditorUtility.DisplayDialog("No Scene Open", "Please open a scene before exporting.", "Ok");
                return;
            }

            DoExport();
            // Light refresh - just update local list after export
            RefreshSceneList(fetchVersions: false);
        }

        private void UploadCurrentScene()
        {
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(currentScene.name))
            {
                EditorUtility.DisplayDialog("No Scene Open", "Please open a scene before uploading.", "Ok");
                return;
            }

            if (!UploadTools.EnsureSceneReady(currentScene.path)) return;

            UploadTools.UploadSceneAndDynamicsInternal(
                uploadExportedDynamics: true,
                exportAndUploadDynamicsFromScene: true,
                uploadSceneGeometry: true,
                uploadThumbnail: true,
                useOptimizedUpload: true,
                showPopups: true,
                onComplete: OnUploadComplete);
        }

        private void UpdateCurrentScene(Cognitive3D_Preferences.SceneSettings settings)
        {
            if (settings == null) return;

            var entry = new SceneEntry
            {
                sceneName = settings.SceneName,
                sceneId = settings.SceneId,
                scenePath = settings.ScenePath,
                versionNumber = settings.VersionNumber,
                versionId = settings.VersionId,
                hasExportFiles = EditorCore.HasSceneExportFiles(settings),
                sceneSettings = settings
            };

            UpdateScene(entry);
        }

        private void ExportScene(SceneEntry entry)
        {
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (currentScene.path != entry.scenePath)
            {
                if (EditorUtility.DisplayDialog("Load Scene?",
                    "The scene '" + entry.sceneName + "' needs to be loaded to export it. Load now?",
                    "Load and Export", "Cancel"))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.scenePath);
                    DoExport();
                }
            }
            else
            {
                DoExport();
            }
        }

        private void DoExport()
        {
            ExportUtility.ExportGLTFScene(true);

            string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScenePath);

            if (currentSettings == null)
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(currentScenePath));
                string sceneId = new System.Guid(bytes).ToString();

                Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), sceneId, 1);                
            }
            else if (string.IsNullOrEmpty(currentSettings.SceneId))
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(currentScenePath));
                string sceneId = new System.Guid(bytes).ToString();

                currentSettings.SceneId = sceneId;
            }

            string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            string path = EditorCore.GetSubDirectoryPath(fullName);

            ExportUtility.GenerateSettingsFile(path, fullName);
            DebugInformationWindow.WriteDebugToFile(path + "debug.log");

            EditorUtility.SetDirty(EditorCore.GetPreferences());
            UnityEditor.AssetDatabase.SaveAssets();

            // Light refresh - just update local list after export
            RefreshSceneList(fetchVersions: false);
        }

        private void UploadScene(SceneEntry entry)
        {
            var scenePath = entry.scenePath;
            if (string.IsNullOrEmpty(scenePath) || !UploadTools.EnsureSceneReady(scenePath)) return;

            UploadTools.UploadSceneAndDynamicsInternal(
                uploadExportedDynamics: true,
                exportAndUploadDynamicsFromScene: true,
                uploadSceneGeometry: true,
                uploadThumbnail: true,
                useOptimizedUpload: true,
                showPopups: true,
                onComplete: OnUploadComplete);
        }

        private void UpdateScene(SceneEntry entry)
        {
            var scenePath = entry.scenePath;
            if (string.IsNullOrEmpty(scenePath) || !UploadTools.EnsureSceneReady(scenePath)) return;

            VersionSelectionWindow.ShowWindow(entry, (selectedVersion, selectedVersionId) =>
            {
                int originalVersion = entry.sceneSettings.VersionNumber;
                int originalVersionId = entry.sceneSettings.VersionId;

                entry.sceneSettings.VersionNumber = selectedVersion;
                entry.sceneSettings.VersionId = selectedVersionId;

                UploadTools.UpdateDecimatedSceneOptimized(entry.sceneSettings, (responseCode) =>
                {
                    entry.sceneSettings.VersionNumber = originalVersion;
                    entry.sceneSettings.VersionId = originalVersionId;

                    if (responseCode == 200 || responseCode == 201)
                    {
                        OnUploadComplete();
                    }
                }, null);
            });
        }

        private void OpenSceneDashboard(SceneEntry entry)
        {
            string url = CognitiveStatics.GetSceneUrl(entry.sceneId, entry.versionNumber);
            Application.OpenURL(url);
        }

        private void OnUploadComplete()
        {
            // Full refresh - fetch latest versions from API after upload
            RefreshSceneList(fetchVersions: true);
        }

        private void RemoveSceneFromTracking(SceneEntry entry)
        {
            if (EditorUtility.DisplayDialog("Remove Scene from Tracking",
                "Are you sure you want to remove '" + entry.sceneName + "' from tracking?\n\nThis will only remove it from the preferences list. Scene files and exports will not be deleted.",
                "Remove", "Cancel"))
            {
                Cognitive3D_Preferences.Instance.sceneSettings.Remove(entry.sceneSettings);
                EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
                // Light refresh - just update local list after removal
                RefreshSceneList(fetchVersions: false);
            }
        }
        #endregion

        #region Helper Classes
        internal class SceneEntry
        {
            public string sceneName;
            public string sceneId;
            public string scenePath;
            public int versionNumber;
            public int versionId;
            public bool hasExportFiles;
            public Cognitive3D_Preferences.SceneSettings sceneSettings;
        }

        private class VersionSelectionWindow : EditorWindow
        {
            private SceneEntry sceneEntry;
            private System.Action<int, int> onConfirm;
            private int selectedVersion;
            private int selectedVersionId;
            private string versionInput = "";
            private string versionIdInput = "";

            public static void ShowWindow(SceneEntry entry, System.Action<int, int> confirmCallback)
            {
                VersionSelectionWindow window = GetWindow<VersionSelectionWindow>(true, "Update Scene Version", true);
                window.sceneEntry = entry;
                window.onConfirm = confirmCallback;
                window.selectedVersion = entry.versionNumber;
                window.selectedVersionId = entry.versionId;
                window.versionInput = entry.versionNumber.ToString();
                window.versionIdInput = entry.versionId.ToString();
                window.minSize = new Vector2(450, 300);
                window.maxSize = new Vector2(450, 300);
                window.ShowModal();
            }

            private void OnGUI()
            {
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Update Scene Version", EditorStyles.boldLabel);
                GUILayout.Space(5);

                EditorGUILayout.HelpBox(
                    "You are about to update an existing version of '" + sceneEntry.sceneName + "'.\n" +
                    "This will overwrite the selected version without creating a new version.",
                    MessageType.Warning
                );

                GUILayout.Space(10);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Current Latest Version:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Version Number: " + sceneEntry.versionNumber);
                EditorGUILayout.LabelField("Version ID: " + sceneEntry.versionId);
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                EditorGUILayout.LabelField("Select Version to Update:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Version Number:", GUILayout.Width(110));
                versionInput = EditorGUILayout.TextField(versionInput);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (!string.IsNullOrEmpty(versionInput) && int.TryParse(versionInput, out int versionNum))
                {
                    if (versionNum < 1 || versionNum > sceneEntry.versionNumber)
                    {
                        EditorGUILayout.HelpBox("Version number must be between 1 and " + sceneEntry.versionNumber, MessageType.Error);
                    }
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(100)))
                {
                    Close();
                }

                bool canConfirm = int.TryParse(versionInput, out selectedVersion) &&
                                  selectedVersion >= 1 &&
                                  selectedVersion <= sceneEntry.versionNumber &&
                                  selectedVersionId > 0;

                EditorGUI.BeginDisabledGroup(!canConfirm);
                if (GUILayout.Button("Update Version", GUILayout.Width(120)))
                {
                    onConfirm?.Invoke(selectedVersion, selectedVersionId);
                    Close();
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);
            }
        }
        #endregion
    }
}