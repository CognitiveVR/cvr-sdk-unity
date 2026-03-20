using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class DynamicObjectDetailGUI : IFeatureDetailGUI
    {
        private Vector2 scrollPos;

        private bool selectAll;

        private readonly List<DynamicObjectEntry> dynamicObjects = new List<DynamicObjectEntry>();
        bool refreshList;
        private int lastDynamicCount = -1;

        DynamicObject[] _cachedDynamics;
        DynamicObject[] DynamicObjects
        {
            get
            {
                if (_cachedDynamics == null || _cachedDynamics.Length == 0)
                {
#if UNITY_2020_1_OR_NEWER
                    _cachedDynamics = Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects ? GameObject.FindObjectsByType<DynamicObject>(FindObjectsInactive.Include, FindObjectsSortMode.None) : GameObject.FindObjectsByType<DynamicObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
                    _cachedDynamics = FindObjectsOfType<DynamicObject>();
#endif
                }
                return _cachedDynamics;
            }
        }

        private void CheckDynamicObjectChanges()
        {
            int currentCount = Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects
            ? GameObject.FindObjectsByType<DynamicObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length
            : GameObject.FindObjectsByType<DynamicObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            if (currentCount != lastDynamicCount)
            {
                lastDynamicCount = currentCount;
                RefreshList();
            }
        }

        private double lastCheckTime;

        #region Visual Elements
        public void OnGUI()
        {
            // Check for updates only once per second
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastCheckTime > 1.0)
            {
                lastCheckTime = currentTime;
                CheckDynamicObjectChanges();
            }

            if (refreshList)
            {
                GetEntryList();
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Dynamic Objects", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Dynamic Objects documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/dynamic-objects/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "A Dynamic Object is a specific object in your experience which you wish to track.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            #region Add DynamicObject Components
            GUILayout.Label("1. Add Dynamic Object Component", EditorCore.styles.FeatureTitle);

            GUILayout.Label(
                "Select GameObjects in the scene that you want to track, then click the button below to add the Dynamic Object component or add the component manually if you prefer.",
                EditorStyles.wordWrappedLabel
            );

            if (GUILayout.Button("Add Dynamic Object to Selected", GUILayout.Height(30)))
            {
                AttachDynamicObjectToSelected();
                RefreshList();
            }
            #endregion

            EditorGUILayout.Space(20);

            #region View and Upload Dynamic Objects
            GUILayout.Label("2. Manage and Upload", EditorCore.styles.FeatureTitle);

            GUILayout.Label(
                "These are all Dynamic Objects currently in the scene. Select all or the ones you want to upload.",
                EditorStyles.wordWrappedLabel
            );
            if (DynamicObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("No Dynamic Objects found in the scene.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Dynamics in " + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, EditorCore.styles.IssuesTitleBoldLabel);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawHeader();
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            foreach (var obj in dynamicObjects)
            {
                DrawDynamicObjectRow(obj);
            }
            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Upload button
            bool anySelected = false;
            for (int i = 0; i < dynamicObjects.Count; i++)
            {
                if (dynamicObjects[i].selected)
                {
                    anySelected = true;
                    break;
                }
            }

            EditorGUI.BeginDisabledGroup(!anySelected && EditorCore.IsCurrentSceneValid());
            if (GUILayout.Button("Upload Selected", GUILayout.Height(30)))
            {
                UploadTools.ExportAndUploadDynamics(true, dynamicObjects, this);
            }
            EditorGUI.EndDisabledGroup();

            // Keep selectAll toggle synced
            bool allSelected = true;
            foreach (var obj in dynamicObjects)
            {
                if (!obj.selected)
                {
                    allSelected = false;
                    break;
                }
            }

            if (selectAll != allSelected)
            {
                selectAll = allSelected;
            }
            #endregion
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            bool newSelectAll = EditorGUILayout.Toggle(selectAll, GUILayout.Width(20));
            DrawColumnSeparator();

            // If the checkbox changed, update all rows
            if (newSelectAll != selectAll)
            {
                selectAll = newSelectAll;
                foreach (var obj in dynamicObjects)
                {
                    obj.selected = selectAll;
                }
            }

            GUILayout.Label("GameObject", GUILayout.Width(100));
            DrawColumnSeparator();

            GUILayout.Label("Mesh Name", GUILayout.Width(100));
            DrawColumnSeparator();

            GUILayout.Label("Dynamic ID", GUILayout.Width(100));
            DrawColumnSeparator();

            GUILayout.Label("Exported", EditorCore.styles.CenteredLabel, GUILayout.Width(70));
            DrawColumnSeparator();

            GUILayout.Label("Uploaded", EditorCore.styles.CenteredLabel, GUILayout.Width(70));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(EditorCore.RefreshIcon, "Refresh List"), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                RefreshList();
            }

            if (GUILayout.Button(new GUIContent(EditorCore.SettingsIcon2, "Additional Settings"), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                GenericMenu gm = new GenericMenu();

                bool hasSelectedAnyEntry = false;
                foreach (var entry in dynamicObjects)
                {
                    if (entry.selected) { hasSelectedAnyEntry = true; break; }
                }

                //export resolution options
                gm.AddItem(new GUIContent("Full Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 1, OnSelectFullResolution);
                gm.AddItem(new GUIContent("Half Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 2, OnSelectHalfResolution);
                gm.AddItem(new GUIContent("Quarter Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 4, OnSelectQuarterResolution);
                gm.AddItem(new GUIContent("Export lowest LOD meshes"), Cognitive3D_Preferences.Instance.ExportSceneLODLowest, OnToggleLODMeshes);

                //dynamic object tools
                gm.AddSeparator("");
                if (!hasSelectedAnyEntry)
                {
                    gm.AddDisabledItem(new GUIContent("Rename Selected Mesh"));
                    gm.AddDisabledItem(new GUIContent("Rename Selected GameObject"));
                }
                else
                {
                    gm.AddItem(new GUIContent("Rename Selected Mesh"), false, OnRenameMeshSelected);
                    gm.AddItem(new GUIContent("Rename Selected GameObject"), false, OnRenameGameObjectSelected);
                }

                //asset management tools
                gm.AddSeparator("");
                gm.AddItem(new GUIContent("Open Dynamic Export Folder"), false, OnOpenDynamicExportFolder);
                gm.AddItem(new GUIContent("Get Dynamic IDs from Dashboard"), false, GetDashboardManifest);
                gm.AddItem(new GUIContent("Include Disabled Dynamic Objects"), Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects, ToggleIncludeDisabledObjects);
                gm.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 18, GUILayout.Width(1));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        private void DrawDynamicObjectRow(DynamicObjectEntry obj)
        {
            Rect rowRect = EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(); // Inner padding container
            GUILayout.Space(3); // Top padding

            EditorGUILayout.BeginHorizontal();

            obj.selected = EditorGUILayout.Toggle(obj.selected, GUILayout.Width(20));

            GUILayout.Space(5);
            GUILayout.Label(obj.gameobjectName, GUILayout.Width(100));

            GUILayout.Space(5);
            GUILayout.Label(obj.meshName, GUILayout.Width(100));

            GUILayout.Space(5);
            GUILayout.Label(GetDynamicIDContent(obj.objectReference), GUILayout.Width(100));

            GUILayout.Space(5);
            DrawStatusIcon(obj.hasExportedMesh, obj.objectReference.idSource != DynamicObject.IdSourceType.PoolID, GUILayout.Width(70));

            GUILayout.Space(5);
            DrawStatusIcon(obj.hasBeenUploaded, obj.objectReference.idSource != DynamicObject.IdSourceType.GeneratedID, GUILayout.Width(70));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Handle click event
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                Selection.activeGameObject = obj.objectReference.gameObject;
                Event.current.Use();
            }
        }

        private GUIContent GetDynamicIDContent(DynamicObject dynamicObject)
        {
            switch (dynamicObject.idSource)
            {
                case DynamicObject.IdSourceType.CustomID:
                    return new GUIContent(dynamicObject.GetId(), dynamicObject.GetId());
                case DynamicObject.IdSourceType.GeneratedID:
                    return new GUIContent("Generated ID", "ID will be generated at runtime");
                case DynamicObject.IdSourceType.PoolID:
                    return new GUIContent("Pool ID", "ID will be generated from pool");
                default:
                    return new GUIContent("", "");
            }
        }

        private void DrawStatusIcon(bool state, bool drawIcon, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            GUILayout.FlexibleSpace();
            if (drawIcon)
            {
                if (state)
                {
                    GUILayout.Label(EditorCore.CompleteCheckmark, EditorCore.styles.CompleteIcon);
                }
                else
                {
                    GUILayout.Label(EditorCore.CircleEmpty, EditorCore.styles.IncompleteIcon);
                }
            }
            else
            {
                GUILayout.Label("", EditorCore.styles.IncompleteIcon);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Dynamic Utilities
        internal static void AttachDynamicObjectToSelected()
        {
            foreach (var obj in Selection.gameObjects)
            {
                if (obj.GetComponent<DynamicObject>() == null)
                {
                    Undo.AddComponent<DynamicObject>(obj);
                }
            }
        }

        void RenameMesh(string newMeshName)
        {
            foreach (var entry in dynamicObjects)
            {
                if (!entry.selected) { continue; }
                if (entry.objectReference == null)
                {
                    //id pool
                    entry.poolReference.PrefabName = newMeshName;
                }
                entry.objectReference.MeshName = newMeshName;
            }
        }

        void RenameGameObject(string newGameObjectName)
        {
            foreach (var entry in dynamicObjects)
            {
                if (!entry.selected) { continue; }
                if (entry.objectReference == null)
                {
                    //id pool
                    entry.poolReference.PrefabName = newGameObjectName;
                }
                entry.objectReference.gameObject.name = newGameObjectName;
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

        void OnRenameGameObjectSelected()
        {
            string defaultvalue = string.Empty;
            foreach (var entry in dynamicObjects)
            {
                if (!entry.selected) { continue; }
                if (defaultvalue == string.Empty && entry.objectReference != null)
                    defaultvalue = entry.objectReference.gameObject.name;
                else if (defaultvalue != entry.objectReference.gameObject.name)
                {
                    defaultvalue = "Multiple Values";
                    break;
                }
            }
            RenameDynamicWindow.Init(this, defaultvalue, RenameGameObject, "Rename GameObjects");
        }

        void OnRenameMeshSelected()
        {
            string defaultvalue = string.Empty;
            foreach (var entry in dynamicObjects)
            {
                if (!entry.selected) { continue; }
                if (defaultvalue == string.Empty)
                    defaultvalue = entry.meshName;
                else if (defaultvalue != entry.meshName)
                {
                    defaultvalue = "Multiple Values";
                    break;
                }
            }
            RenameDynamicWindow.Init(this, defaultvalue, RenameMesh, "Rename Meshes");
        }

        void OnOpenDynamicExportFolder()
        {
            EditorUtility.RevealInFinder(EditorCore.GetDynamicExportDirectory());
        }

        //selecting disabled scene objects is supported in 2020+
        void ToggleIncludeDisabledObjects()
        {
            Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects = !Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects;
            RefreshList();
        }
        #endregion

        #region Retrieve Dynamics
        internal void RefreshList()
        {
            refreshList = true;
        }

        private void GetEntryList()
        {
            Clear();
            foreach (var dynamic in DynamicObjects)
            {
                dynamicObjects.Add(
                    new DynamicObjectEntry
                    (
                        dynamic.MeshName,
                        EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.MeshName) || !dynamic.UseCustomMesh,
                        dynamic,
                        dynamic.gameObject.name,
                        !(EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.MeshName) || !dynamic.UseCustomMesh),
                        false
                    )
                );
            }

            GetDashboardManifest();

            refreshList = false;
        }

        readonly List<DashboardObject> dashboardObjects = new List<DashboardObject>();
        void GetDashboardManifest()
        {
            var currentSceneSettings = Cognitive3D_Preferences.FindCurrentScene();
            if (currentSceneSettings == null)
            {
                return;
            }
            string url = CognitiveStatics.GetDynamicManifest(currentSceneSettings.VersionId);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
            {
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            }
            EditorNetwork.Get(url, GetManifestResponse, headers, false);
        }

        void GetManifestResponse(int responsecode, string error, string text)
        {
            if (responsecode == 200)
            {
                try
                {
                    dashboardObjects.Clear();
                    dashboardObjects.AddRange(Util.GetJsonArray<DashboardObject>(text));
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    Debug.Log(text);
                }

                //foreach entry, loop through dynamic object entries on this list and display as 'uploaded' if found
                foreach (var dashboardObject in dashboardObjects)
                {
                    DynamicObjectEntry found = dynamicObjects.Find(delegate (DynamicObjectEntry obj)
                    {
                        if (obj.objectReference == null) { return false; }
                        if (obj.objectReference.idSource != DynamicObject.IdSourceType.CustomID) { return false; }
                        return obj.objectReference.GetId() == dashboardObject.sdkId;
                    });
                    if (found == null) { continue; }

                    found.hasBeenUploaded = true;
                }
                EditorCore.RefreshSceneVersion(null);
            }
            else
            {
                Util.logWarning("GetManifestResponse " + responsecode + " " + error);
            }
        }

        void Clear()
        {
            _cachedDynamics = null;
            dynamicObjects.Clear();
        }
        #endregion
    }
}
