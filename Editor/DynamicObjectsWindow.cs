using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;

//TODO CONSIDER concept of 'uploading selected' using Unity selection, rather than checkboxes on objects in this window
//YES should have a marker to indicate current hierarchy selection

namespace Cognitive3D
{
    //deserialize list of dynamic objects from dashboard
    [System.Serializable]
    public class DashboardObject
    {
        public string sdkId;
        public string name;
        public string meshName;
        public long updatedAt; //timestamp
        //TODO CONSIDER the list of files, to compare against the uploaded and exported content
    }

    [System.Serializable]
    public class AggregationManifest
    {
        [System.Serializable]
        public class AggregationManifestEntry
        {
            public string name;
            public string mesh;
            public string id;
            public float[] scaleCustom = new float[3] { 1, 1, 1 };
            public float[] position = new float[3] { 0, 0, 0 };
            public float[] rotation = new float[4] { 0, 0, 0, 1 };
            public AggregationManifestEntry(string _name, string _mesh, string _id, float[] _scaleCustom)
            {
                name = _name;
                mesh = _mesh;
                id = _id;
                scaleCustom = _scaleCustom;
            }
            public AggregationManifestEntry(string _name, string _mesh, string _id, float[] _scaleCustom, float[] _position, float[] _rotation)
            {
                name = _name;
                mesh = _mesh;
                id = _id;
                scaleCustom = _scaleCustom;
                position = _position;
                rotation = _rotation;
            }
            public override string ToString()
            {
                return "{\"name\":\"" + name + "\",\"mesh\":\"" + mesh + "\",\"id\":\"" + id +
                    "\",\"scaleCustom\":[" + scaleCustom[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + scaleCustom[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + scaleCustom[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) +
                    "],\"initialPosition\":[" + position[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + position[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + position[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) +
                    "],\"initialRotation\":[" + rotation[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + rotation[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + rotation[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + rotation[3].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "]}";
            }
        }
        public List<AggregationManifestEntry> objects = new List<AggregationManifestEntry>();
    }

    //temporary popup window for mass renaming dynamic object components
    //TODO format to keep consistent look with onboarding screens
    public class RenameDynamicWindow : EditorWindow
    {
        static DynamicObjectsWindow sourceWindow;
        static string defaultMeshName;
        static System.Action<string> action;
        public static void Init(DynamicObjectsWindow dynamicsWindow, string defaultName, System.Action<string> renameAction, string title)
        {
            RenameDynamicWindow window = (RenameDynamicWindow)EditorWindow.GetWindow(typeof(RenameDynamicWindow), true, title);
            window.ShowUtility();
            sourceWindow = dynamicsWindow;
            defaultMeshName = defaultName;
            action = renameAction;
        }

        bool hasDoneInitialFocus = false;
        void OnGUI()
        {
            GUI.SetNextControlName("initialFocus");
            defaultMeshName = GUILayout.TextField(defaultMeshName);

            if (!hasDoneInitialFocus)
            {
                hasDoneInitialFocus = true;
                GUI.FocusControl("initialFocus");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rename"))
            {
                action.Invoke(defaultMeshName);
                sourceWindow.RefreshList();
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                sourceWindow.RefreshList();
                Close();
            }
            GUILayout.EndHorizontal();
            //rename textfield
            //rename + cancel buttons
        }
    }

    public class DynamicObjectsWindow : EditorWindow
    {
        //cached gui styles
        GUIStyle dynamiclabel;
        GUIStyle dynamicentry_odd;
        GUIStyle dynamicentry_even;
        GUIStyle image_centered;

        class Entry
        {
            //IMPROVEMENT for objects in scene, cache warning for missing collider in children
            public bool visible = true; //currently shown in the filtered list
            public bool selected; //not necessarily selected as a gameobject, just checked in this list
            public string meshName;
            public bool hasExportedMesh;
            public bool isIdPool;
            public int idPoolCount;
            public DynamicObject objectReference;
            public DynamicObjectIdPool poolReference;
            public string gameobjectName;
            public bool hasBeenUploaded;
            public Entry(string meshName, bool exportedMesh, DynamicObject reference, string name, bool initiallySelected, bool uploaded)
            {
                objectReference = reference;
                gameobjectName = name;
                this.meshName = meshName;
                hasExportedMesh = exportedMesh;
                selected = initiallySelected;
                hasBeenUploaded = uploaded;
            }
            public Entry(bool exportedMesh, DynamicObjectIdPool reference, bool initiallySelected, bool uploaded)
            {
                isIdPool = true;
                poolReference = reference;
                idPoolCount = poolReference.Ids.Length;
                gameobjectName = poolReference.PrefabName;
                meshName = poolReference.MeshName;
                hasExportedMesh = exportedMesh;
                selected = initiallySelected;
                hasBeenUploaded = uploaded;
            }
        }

        bool DisableButtons
        {
            get
            {
                var currentscene = Cognitive3D_Preferences.FindCurrentScene();
                bool sceneIsNull = currentscene == null || string.IsNullOrEmpty(currentscene.SceneId);
                return !EditorCore.IsDeveloperKeyValid || sceneIsNull || lastResponseCode != 200;
            }
        }

        public static void Init()
        {
            DynamicObjectsWindow window = (DynamicObjectsWindow)EditorWindow.GetWindow(typeof(DynamicObjectsWindow), true, "Dynamic Objects (" +Cognitive3D_Manager.SDK_VERSION +")");
            window.minSize = new Vector2(600, 550);
            window.maxSize = new Vector2(600, 550);
            window.Show();
            EditorCore.CheckForExpiredDeveloperKey(GetDevKeyResponse);
            needsRefreshDevKey = false;
        }

        static bool needsRefreshDevKey = true;
        static int lastResponseCode = 200;
        static void GetDevKeyResponse(int responseCode, string error, string text)
        {
            lastResponseCode = responseCode;
            if (responseCode == 200)
            {
                //dev key is fine
                DynamicObjectsWindow window = (DynamicObjectsWindow)EditorWindow.GetWindow(typeof(DynamicObjectsWindow));
                window.GetDashboardManifest();
            }
            else
            {
                EditorUtility.DisplayDialog("Your developer key has expired", "Please log in to the dashboard, select your project, and generate a new developer key.\n\nNote:\nDeveloper keys allow you to upload and modify Scenes, and the keys expire after 90 days.\nApplication keys authorize your app to send data to our server, and they never expire.", "Ok");
                Debug.LogError("Developer Key invalid or expired");
            }
        }

        List<Entry> Entries;
        List<Entry> GetEntryList(DynamicObject[] objects, DynamicObjectIdPool[] pools)
        {
            if (Entries == null)
            {
                Entries = new List<Entry>(objects.Length + pools.Length);
                foreach (var o in objects)
                {
                    bool selected = selectedDynamicsOnFocus.Contains(o);
                    var found = dashboardObjects.Find(delegate (DashboardObject obj)
                        {
                            if (!o.UseCustomId) { return false; }
                            return obj.sdkId == o.GetId();
                        });
                    bool uploaded = found != null;

                    Entries.Add(new Entry(o.MeshName, (EditorCore.GetExportedDynamicObjectNames().Contains(o.MeshName) || !o.UseCustomMesh), o, o.gameObject.name, selected, uploaded));
                }
                foreach (var p in pools)
                {
                    bool selected = selectedPoolsOnFocus.Contains(p);
                    //var found = dashboardObjects.Find(delegate (DashboardObject obj)
                    //{
                    //    if (!p.UseCustomId) { return false; }
                    //    return obj.sdkId == p.GetId();
                    //});
                    bool uploaded = false;
                    Entries.Add(new Entry(EditorCore.GetExportedDynamicObjectNames().Contains(p.MeshName), p, selected, uploaded));
                }
                selectedDynamicsOnFocus = new List<DynamicObject>();
                selectedPoolsOnFocus = new List<DynamicObjectIdPool>();
            }
            return Entries;
            //Entries
        }

        bool filterMeshes = true;
        bool filterIds = true;
        bool filterGameObjects = true;

        string searchBarString = string.Empty;
        private void OnGUI()
        {
            //cache gui styles
            if (dynamiclabel == null)
            {
                dynamiclabel = EditorCore.WizardGUISkin.GetStyle("dynamiclabel");
                dynamicentry_odd = EditorCore.WizardGUISkin.GetStyle("dynamicentry_odd");
                dynamicentry_even = EditorCore.WizardGUISkin.GetStyle("dynamicentry_even");
                image_centered = EditorCore.WizardGUISkin.GetStyle("image_centered");
            }

            if (needsRefreshDevKey == true)
            {
                EditorCore.CheckForExpiredDeveloperKey(GetDevKeyResponse);
                needsRefreshDevKey = false;
            }
            GUI.skin = EditorCore.WizardGUISkin;

            GUI.DrawTexture(new Rect(0, 0, 600, 550), EditorGUIUtility.whiteTexture);

            var currentscene = Cognitive3D_Preferences.FindCurrentScene();

            Rect steptitlerect = new Rect(0, 0, 600, 25);
            if (string.IsNullOrEmpty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name))
            {
                GUI.Label(steptitlerect, "SCENE:   Not Saved", "image_centered");
            }
            else if (currentscene == null || string.IsNullOrEmpty(currentscene.SceneId))
            {
                GUI.Label(steptitlerect, "SCENE:   Not Uploaded", "image_centered");
            }
            else
            {
                GUI.Label(steptitlerect, "SCENE:   " + currentscene.SceneName + " Version: " + currentscene.VersionNumber, "image_centered");
            }

            //GUI.Label(new Rect(30, 45, 440, 440), "These are the active <color=#8A9EB7FF>Dynamic Object components</color> currently found in your scene.");


            if (GetDynamicObjects.Length == 0)
            {
                GUI.Label(new Rect(30, 80, 520, 270), "No Scene objects found.\n\nHave you attached Dynamic Object components to objects?\nAre the GameObjects active in your hierarchy?", "button_disabledtext");
            }

            //build up list of Entries, then cache?
            GetEntryList(GetDynamicObjects, EditorCore.GetDynamicObjectPoolAssets);

            //filter
            int searchBarWidth = 300;
            Rect filterRect = new Rect(600 / 2 - searchBarWidth / 2 - 20, 25, 20, 20);
            Rect searchBarRect = new Rect(600 / 2 - searchBarWidth / 2, 25, searchBarWidth, 20);
            Rect searchClearRect = new Rect(600 / 2 + searchBarWidth / 2, 25, 20, 20);
            string temp = EditorCore.TextField(searchBarRect, searchBarString, 64);
            if (temp == string.Empty)
            {
                GUI.Label(searchBarRect, "<size=15>Search</size>", "ghostlabel");
                if (searchBarString != string.Empty)
                {
                    ShowAllList();
                }

                searchBarString = temp;
            }
            else if (temp != searchBarString)
            {
                searchBarString = temp;
                //re-filter the list
                FilterList(searchBarString);
            }
            if (GUI.Button(filterRect, EditorCore.FilterIcon, "label"))
            {
                //generic menu
                GenericMenu gm = new GenericMenu();
                gm.AddItem(new GUIContent("Meshes"), filterMeshes, OnToggleMeshFilter);
                gm.AddItem(new GUIContent("GameObject Names"), filterGameObjects, OnToggleGameObjectFilter);
                gm.AddItem(new GUIContent("Ids"), filterIds, OnToggleIdFilter);
                gm.ShowAsContext();
            }
            if (GUI.Button(searchClearRect, "X", "ghostlabel"))
            {
                searchBarString = string.Empty;
                FilterList(searchBarString);
            }

            //headers
            Rect toggleRect = new Rect(30, 55, 30, 30);
            bool allselected = true;
            int visibleCount = 0;
            foreach (var entry in Entries)
            {
                if (!entry.visible) { continue; }
                visibleCount++;
                if (!entry.selected) { allselected = false; }
            }
            if (visibleCount == 0)
                allselected = false;
            var toggleIcon = allselected ? EditorCore.BlueCheckmark : EditorCore.EmptyBlueCheckmark;
            bool pressed = GUI.Button(toggleRect, toggleIcon, "image_centered");
            //select all in hiearchy
            if (pressed)
            {
                if (allselected)
                {
                    //deselect all
                    foreach (var entry in Entries)
                    {
                        entry.selected = false;
                        //entry.selected
                    }
                    SelectDynamicEntries(null);
                }
                else
                {
                    //select all visible
                    foreach (var entry in Entries)
                    {
                        if (!entry.visible) { entry.selected = false; continue; }
                        entry.selected = true;
                    }
                    SelectDynamicEntries(Entries);
                }
            }

            Rect gameobject = new Rect(60, 55, 120, 30);
            string gameObjectNameStyle = (SortMethod == SortByMethod.GameObjectName || SortMethod == SortByMethod.ReverseGameObjectName) ? "dynamicheaderbold" : "dynamicheader";
            if (GUI.Button(gameobject, "GameObject", gameObjectNameStyle))
            {
                if (SortMethod != SortByMethod.GameObjectName)
                    SortMethod = SortByMethod.GameObjectName;
                else
                    SortMethod = SortByMethod.ReverseGameObjectName;
                SortByName();
            }

            Rect mesh = new Rect(210, 55, 120, 30);
            string meshNameStyle = (SortMethod == SortByMethod.MeshName || SortMethod == SortByMethod.ReverseMeshName) ? "dynamicheaderbold" : "dynamicheader";
            if (GUI.Button(mesh, "Mesh Name", meshNameStyle))
            {
                if (SortMethod != SortByMethod.MeshName)
                    SortMethod = SortByMethod.MeshName;
                else
                    SortMethod = SortByMethod.ReverseMeshName;
                SortByMeshName();
            }

            Rect idrect = new Rect(350, 55, 80, 30);
            GUI.Label(idrect, "Id", "dynamicheader");

            Rect exported = new Rect(440, 55, 55, 30);
            string exportedStyle = (SortMethod == SortByMethod.Exported || SortMethod == SortByMethod.ReverseExported) ? "dynamicheaderbold" : "dynamicheader";
            if (GUI.Button(exported, "Exported", exportedStyle))
            {
                if (SortMethod != SortByMethod.Exported)
                    SortMethod = SortByMethod.Exported;
                else
                    SortMethod = SortByMethod.ReverseExported;
                SortByExported();
            }

            Rect uploaded = new Rect(500, 55, 80, 30);
            string uploadedStyle = (SortMethod == SortByMethod.Uploaded || SortMethod == SortByMethod.ReverseUploaded) ? "dynamicheaderbold" : "dynamicheader";
            if (GUI.Button(uploaded,"Uploaded", uploadedStyle))
            {
                if (SortMethod != SortByMethod.Uploaded)
                    SortMethod = SortByMethod.Uploaded;
                else
                    SortMethod = SortByMethod.ReverseUploaded;
                SortByUploaded();
            }

            //gear icon
            Rect tools = new Rect(570, 55, 30, 30);
            if (GUI.Button(tools, EditorCore.SettingsIcon, "image_centered")) //rename dropdown
            {
                //drop down menu?
                GenericMenu gm = new GenericMenu();

                bool enabled = false;
                foreach (var entry in Entries)
                {
                    if (entry.selected) { enabled = true; break; }
                }

                if (!enabled)
                {
                    gm.AddDisabledItem(new GUIContent("Rename Selected Mesh"));
                    gm.AddDisabledItem(new GUIContent("Rename Selected GameObject"));
                }
                else
                {
                    gm.AddItem(new GUIContent("Rename Selected Mesh"), false, OnRenameMeshSelected);
                    gm.AddItem(new GUIContent("Rename Selected GameObject"), false, OnRenameGameObjectSelected);
                }
                gm.AddSeparator("");
                gm.AddItem(new GUIContent("Open Dynamic Export Folder"), false, OnOpenDynamicExportFolder);

                gm.AddItem(new GUIContent("Get Dynamic IDs from Dashboard"), false, GetDashboardManifest);

#if UNITY_2020_1_OR_NEWER
                gm.AddItem(new GUIContent("Include Disabled Dynamic Objects"), Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects, ToggleIncludeDisabledObjects);
#endif
                gm.ShowAsContext();
            }


            Rect innerScrollSize = new Rect(30, 0, 520, visibleCount * 30);
            dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 80, 540, 365), dynamicScrollPosition, innerScrollSize, false, true);

            Rect dynamicrect;
            int GuiOffset = 0;
            int usedRows = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].visible == false) { continue; }
                //if (poolAssets[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
                dynamicrect = new Rect(30, (usedRows + GuiOffset) * 30, 560, 30);
                DrawDynamicObjectEntry(Entries[i], dynamicrect, (usedRows + GuiOffset) % 2 == 0);
                usedRows++;
            }
            GUI.EndScrollView();
            GUI.Box(new Rect(30, 80, 525, 365), "", "box_sharp_alpha");

            //buttons

            string scenename = "Not Saved";
            int versionnumber = 0;
            //string buttontextstyle = "button_bluetext";
            if (currentscene == null || string.IsNullOrEmpty(currentscene.SceneId))
            {
                //buttontextstyle = "button_disabledtext";
            }
            else
            {
                scenename = currentscene.SceneName;
                versionnumber = currentscene.VersionNumber;
            }

            int selectionCount = 0;
            foreach (var entry in Entries)
            {
                if (entry.selected)
                {
                    selectionCount++;
                }
            }
            //texture resolution

            if (Cognitive3D_Preferences.Instance.TextureResize > 4) { Cognitive3D_Preferences.Instance.TextureResize = 4; }

            //resolution settings here
            EditorGUI.BeginDisabledGroup(DisableButtons);
            if (GUI.Button(new Rect(80, 455, 140, 35), new GUIContent("1/4 Resolution", DisableButtons ? "" : "Quarter resolution of dynamic object textures"), Cognitive3D_Preferences.Instance.TextureResize == 4 ? "button_blueoutline" : "button_disabledtext"))
            {
                Cognitive3D_Preferences.Instance.TextureResize = 4;
            }
            if (Cognitive3D_Preferences.Instance.TextureResize != 4)
            {
                GUI.Box(new Rect(80, 455, 140, 35), "", "box_sharp_alpha");
            }

            if (GUI.Button(new Rect(230, 455, 140, 35), new GUIContent("1/2 Resolution", DisableButtons ? "" : "Half resolution of dynamic object textures"), Cognitive3D_Preferences.Instance.TextureResize == 2 ? "button_blueoutline" : "button_disabledtext"))
            {
                Cognitive3D_Preferences.Instance.TextureResize = 2;
            }
            if (Cognitive3D_Preferences.Instance.TextureResize != 2)
            {
                GUI.Box(new Rect(230, 455, 140, 35), "", "box_sharp_alpha");
            }

            if (GUI.Button(new Rect(380, 455, 140, 35), new GUIContent("1/1 Resolution", DisableButtons ? "" : "Full resolution of dynamic object textures"), Cognitive3D_Preferences.Instance.TextureResize == 1 ? "button_blueoutline" : "button_disabledtext"))
            {
                Cognitive3D_Preferences.Instance.TextureResize = 1;
            }
            if (Cognitive3D_Preferences.Instance.TextureResize != 1)
            {
                GUI.Box(new Rect(380, 455, 140, 35), "", "box_sharp_alpha");
            }
            EditorGUI.EndDisabledGroup();

            DrawFooter();
            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        Vector2 dynamicScrollPosition;

        //in 2020+, overload allows for finding disabled objects in the scene as well
        DynamicObject[] _cachedDynamics;
        DynamicObject[] GetDynamicObjects
        {
            get
            {
                if (_cachedDynamics == null || _cachedDynamics.Length == 0)
                {
#if UNITY_2020_1_OR_NEWER
                    _cachedDynamics = FindObjectsOfType<DynamicObject>(Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects);
#else
                    _cachedDynamics = FindObjectsOfType<DynamicObject>();
#endif
                }
                return _cachedDynamics;
            }
        }

        List<DynamicObject> selectedDynamicsOnFocus = new List<DynamicObject>();
        List<DynamicObjectIdPool> selectedPoolsOnFocus = new List<DynamicObjectIdPool>();
        private void OnFocus()
        {
#if UNITY_2020_1_OR_NEWER
            _cachedDynamics = FindObjectsOfType<DynamicObject>(Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects);
#else
            _cachedDynamics = FindObjectsOfType<DynamicObject>();
#endif
            EditorCore.ExportedDynamicObjects = null;
            EditorCore._cachedPoolAssets = null;

            //refresh list, but also keep selected items
            //make a list of selected dynamics by reference. when reconstructing the entries list, save to temporary list
            if (Entries != null)
            {
                foreach (var e in Entries)
                {
                    if (!e.selected) { continue; }
                    if (e.isIdPool && e.poolReference != null)
                    {
                        selectedPoolsOnFocus.Add(e.poolReference);
                    }
                    else if (!e.isIdPool && e.objectReference != null)
                        selectedDynamicsOnFocus.Add(e.objectReference);
                }
            }

            Entries = null;
            GetEntryList(GetDynamicObjects, EditorCore.GetDynamicObjectPoolAssets);
            if (!string.IsNullOrEmpty(searchBarString))
            {
                FilterList(searchBarString);
            }
        }

        public void RefreshList()
        {
            OnFocus();
        }

        void OnRenameGameObjectSelected()
        {
            string defaultvalue = string.Empty;
            foreach (var entry in Entries)
            {
                if (!entry.selected) { continue; }
                if (defaultvalue == string.Empty)
                    defaultvalue = entry.gameobjectName;
                else if (defaultvalue != entry.gameobjectName)
                {
                    defaultvalue = "Multple Values";
                    break;
                }
            }
            RenameDynamicWindow.Init(this, defaultvalue, RenameGameObject, "Rename GameObjects");
        }

        void OnRenameMeshSelected()
        {
            string defaultvalue = string.Empty;
            foreach (var entry in Entries)
            {
                if (!entry.selected) { continue; }
                if (defaultvalue == string.Empty)
                    defaultvalue = entry.meshName;
                else if (defaultvalue != entry.meshName)
                {
                    defaultvalue = "Multple Values";
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

        List<DashboardObject> dashboardObjects = new List<DashboardObject>();
        void GetDashboardManifest()
        {
            var currentSceneSettings = Cognitive3D_Preferences.FindCurrentScene();
            if (currentSceneSettings == null)
            {
                return;
            }
            string url = CognitiveStatics.GETDYNAMICMANIFEST(currentSceneSettings.VersionId);

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
                    Entry found = Entries.Find(delegate (Entry obj)
                    {
                        if (obj.objectReference == null) { return false; }
                        if (!obj.objectReference.UseCustomId) { return false; }
                        return obj.objectReference.GetId() == dashboardObject.sdkId;
                    });
                    if (found == null) { continue; }

                    found.hasBeenUploaded = true;
                }
                Repaint();
                EditorCore.RefreshSceneVersion(null);
            }
            else
            {
                Util.logWarning("GetManifestResponse " + responsecode + " " + error);
            }
        }

#region Sorting
        enum SortByMethod
        {
            GameObjectName,
            ReverseGameObjectName,
            MeshName,
            ReverseMeshName,
            //Id,
            //ReverseId,
            Exported,
            ReverseExported,
            Uploaded,
            ReverseUploaded
        }
        SortByMethod SortMethod;

        void SortByName()
        {
            Entries.Sort(delegate (Entry x, Entry y)
            {
                return string.Compare(x.gameobjectName, y.gameobjectName);
            });
            if (SortMethod == SortByMethod.ReverseGameObjectName)
            {
                Entries.Reverse();
            }
        }

        void SortByMeshName()
        {
            Entries.Sort(delegate (Entry x, Entry y)
            {
                return string.Compare(x.meshName, y.meshName);
            });
            if (SortMethod == SortByMethod.ReverseMeshName)
            {
                Entries.Reverse();
            }
        }

        void SortByExported()
        {
            Entries.Sort(delegate (Entry x, Entry y)
            {
                if (!x.hasExportedMesh && !y.hasExportedMesh) { return 0; }
                if (x.hasExportedMesh && y.hasExportedMesh) { return 0; }
                if (x.hasExportedMesh && !y.hasExportedMesh) { return -1; }
                if (!x.hasExportedMesh && y.hasExportedMesh) { return 1; }
                return -1;
            });
            if (SortMethod == SortByMethod.ReverseExported)
            {
                Entries.Reverse();
            }
        }

        void SortByUploaded()
        {
            Entries.Sort(delegate (Entry x, Entry y)
            {
                if (!x.hasBeenUploaded && !y.hasBeenUploaded) { return 0; }
                if (x.hasBeenUploaded && y.hasBeenUploaded) { return 0; }
                if (x.hasBeenUploaded && !y.hasBeenUploaded) { return -1; }
                if (!x.hasBeenUploaded && y.hasBeenUploaded) { return 1; }
                return -1;
            });
            if (SortMethod == SortByMethod.ReverseUploaded)
            {
                Entries.Reverse();
            }
        }
#endregion

#region Filtering

        void OnToggleMeshFilter()
        {
            filterMeshes = !filterMeshes;
            if (searchBarString != string.Empty)
            {
                FilterList(searchBarString);
            }
        }
        void OnToggleGameObjectFilter()
        {
            filterGameObjects = !filterGameObjects;
            if (searchBarString != string.Empty)
            {
                FilterList(searchBarString);
            }
        }
        void OnToggleIdFilter()
        {
            filterIds = !filterIds;
            if (searchBarString != string.Empty)
            {
                FilterList(searchBarString);
            }
        }

        bool CommonMeshesContainsSearch(string search)
        {
            foreach (var commonMesh in System.Enum.GetNames(typeof(DynamicObject.CommonDynamicMesh)))
            {
                if (commonMesh.ToLower().Contains(search))
                {
                    return true;
                }
            }
            return false;
        }

        public void FilterList(string inputstring)
        {
            string compareString = inputstring.ToLower();
            foreach (var entry in Entries)
            {
                if (entry == null) { continue; }
                if (string.IsNullOrEmpty(entry.meshName)) { continue; }
                if (entry.objectReference == null) { continue; }
                //IMPROVEMENT filter should be applied to id pools

                entry.visible = false;
                if (filterMeshes && entry.objectReference.UseCustomMesh && entry.meshName.ToLower().Contains(compareString))
                {
                    entry.visible = true;
                }
                else if (filterMeshes && !entry.objectReference.UseCustomMesh && CommonMeshesContainsSearch(compareString))
                {
                    entry.visible = true;
                }
                else if (filterGameObjects && entry.gameobjectName.ToLower().Contains(compareString))
                {
                    entry.visible = true;
                }
                else if (filterIds && entry.isIdPool)
                {
                    foreach (var i in entry.poolReference.Ids)
                    {
                        if (i.ToLower().Contains(compareString))
                        {
                            entry.visible = true;
                            break;
                        }
                    }
                }
                else if (filterIds && !entry.isIdPool && entry.objectReference.CustomId.ToLower().Contains(compareString))
                {
                    entry.visible = true;
                }
            }
        }

        void ShowAllList()
        {
            foreach (var entry in Entries)
            {
                entry.visible = true;
            }
        }
#endregion

#region Utilities

        void RenameMesh(string newMeshName)
        {
            foreach (var entry in Entries)
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
            foreach (var entry in Entries)
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

#endregion

        void DrawDynamicObjectEntry(Entry dynamic, Rect rect, bool darkbackground)
        {
            Event e = Event.current;
            //CONSIDER also allowing selection with e.type == EventType.MouseDrag
            if (e.isMouse && e.type == EventType.MouseDown)
            {
                if (e.mousePosition.x < rect.x + 00 || e.mousePosition.x > rect.x + rect.width - 80 || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y + rect.height)
                {
                }
                else
                {
                    if (e.shift) //add/remove selection
                    {
                        if (!dynamic.isIdPool)
                        {
                            if (!Selection.Contains(dynamic.objectReference.gameObject))
                            {
                                Object[] gos = new Object[Selection.objects.Length + 1];
                                Selection.objects.CopyTo(gos, 0);
                                gos[gos.Length - 1] = dynamic.objectReference.gameObject;
                                Selection.objects = gos;
                            }
                            else
                            {
                                var entryList = new List<Object>(Selection.objects);
                                foreach (var v in entryList)
                                {
                                    if (dynamic.objectReference.gameObject == v)
                                    {
                                        entryList.Remove(v);
                                        break;
                                    }
                                }
                                Selection.objects = entryList.ToArray();
                            }
                        }
                        else
                        {
                            if (!Selection.Contains(dynamic.poolReference))
                            {
                                Object[] gos = new Object[Selection.objects.Length + 1];
                                Selection.objects.CopyTo(gos, 0);
                                gos[gos.Length - 1] = dynamic.poolReference;
                                Selection.objects = gos;
                            }
                            else
                            {
                                var entryList = new List<Object>(Selection.objects);
                                foreach (var v in entryList)
                                {
                                    if (dynamic.poolReference == v)
                                    {
                                        entryList.Remove(v);
                                        break;
                                    }
                                }
                                Selection.objects = entryList.ToArray();
                            }
                        }
                    }
                    else
                    {
                        if (!dynamic.isIdPool)
                            Selection.activeTransform = dynamic.objectReference.transform;
                        else
                            Selection.activeObject = dynamic.poolReference;
                    }
                }
            }

            //background
            if (darkbackground)
                GUI.Box(rect, "", dynamicentry_even);
            else
                GUI.Box(rect, "", dynamicentry_odd);

            Rect selectedRect = new Rect(rect.x + 0, rect.y, 30, rect.height);
            Rect gameobjectRect = new Rect(rect.x + 30, rect.y, 140, rect.height);
            Rect mesh = new Rect(rect.x + 180, rect.y, 140, rect.height);
            Rect idRect = new Rect(rect.x + 320, rect.y, 80, rect.height);
            Rect exported = new Rect(rect.x + 420, rect.y, 24, rect.height);
            Rect uploaded = new Rect(rect.x + 480, rect.y, 24, rect.height);

            var toggleIcon = dynamic.selected ? EditorCore.BlueCheckmark : EditorCore.EmptyBlueCheckmark;
            if (dynamic.isIdPool)
            {
                dynamic.selected = Selection.Contains(dynamic.poolReference);
            }
            else
            {
                if (dynamic.objectReference == null) { return; }
                dynamic.selected = Selection.Contains(dynamic.objectReference.gameObject);
            }
            GUI.Toggle(selectedRect, dynamic.selected, toggleIcon, image_centered);

            //gameobject name or id pool count
            GUI.Label(gameobjectRect, dynamic.gameobjectName, dynamiclabel);
            //GUI.Label(mesh, dynamic.objectReference.UseCustomMesh?dynamic.meshName:dynamic.objectReference.CommonMesh.ToString(), dynamiclabel);
            GUI.Label(mesh, dynamic.meshName, dynamiclabel);

            //id type identification
            if (dynamic.isIdPool)
            {
                GUI.Label(idRect, "ID Pool (" + dynamic.idPoolCount + ")", dynamiclabel);
            }
            else if (dynamic.objectReference.UseCustomId)
            {
                GUI.Label(idRect, new GUIContent(dynamic.objectReference.GetId(), dynamic.objectReference.GetId()), dynamiclabel);
            }
            else if (dynamic.objectReference.IdPool != null)
            {
                GUI.Label(idRect, dynamic.objectReference.IdPool.name, dynamiclabel);
            }
            else
            {
                GUI.Label(idRect, "Generated", dynamiclabel);
            }

            //has mesh exported
            if (dynamic.objectReference == null)
            {
                //likely a pool
                //TODO check export directory for files that match the mesh name
            }
            else
            {
                //has been exported
                if (!dynamic.objectReference.UseCustomMesh || EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.meshName))
                {
                    GUI.Label(exported, EditorCore.Checkmark, image_centered);
                }
                else
                {
                    GUI.Label(exported, EditorCore.EmptyCheckmark, image_centered);
                }
            }

            //has been uploaded
            if (dynamic.objectReference == null)
            {
                //id pool
                GUI.Label(uploaded, new GUIContent(EditorCore.EmptyCheckmark, "IDs in this pool have not been uploaded to dashboard"), image_centered);
                //TODO check if any/all ids have been uploaded from id pool asset
            }
            else if (dynamic.objectReference.IdPool != null)
            {
                //dynamic with id pool reference
                GUI.Label(uploaded, new GUIContent("", "Objects using an ID pool. Check that the Pool asset IDs have been uploaded to aggregate these objects"), image_centered);
            }
            else if (dynamic.objectReference.UseCustomId == false)
            {
                //generated at runtime
                GUI.Label(uploaded, new GUIContent("", "Objects with generated IDs are not aggregated between sessions"), image_centered);
            }
            else if (dynamic.hasBeenUploaded)
            {
                GUI.Label(uploaded, new GUIContent(EditorCore.Checkmark,"This object's data will be aggregated across sessions"), image_centered);
            }
            else
            {
                GUI.Label(uploaded, new GUIContent(EditorCore.EmptyCheckmark, "ID does not exist on Dashboard and will not be aggregated across sessions"), image_centered);
            }   
        }

        void DrawFooter()
        {
            GUI.color = EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 600, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            //string tooltip = "";

            //all, unless selected
            int selectionCount = 0;
            int selectedEntries = 0;
            foreach (var entry in Entries)
            {
                if (!entry.selected) { continue; }
                selectedEntries++;
                if (entry.isIdPool)
                {
                    selectionCount += entry.idPoolCount;
                }
                else
                    selectionCount++;
            }

            var currentScene = Cognitive3D_Preferences.FindCurrentScene();
            string scenename = "Not Saved";
            int versionnumber = 0;
            //string buttontextstyle = "button_bluetext";
            if (currentScene == null || string.IsNullOrEmpty(currentScene.SceneId))
            {
                //buttontextstyle = "button_disabledtext";
            }
            else
            {
                scenename = currentScene.SceneName;
                versionnumber = currentScene.VersionNumber;
            }

            bool enabled = !(currentScene == null || string.IsNullOrEmpty(currentScene.SceneId)) && lastResponseCode == 200;
            if (enabled)
            {
                EditorGUI.BeginDisabledGroup(currentScene == null || string.IsNullOrEmpty(currentScene.SceneId) || selectionCount == 0);
                if (GUI.Button(new Rect(80, 510, 215, 30), new GUIContent("Upload " + selectionCount + " Selected Meshes", DisableButtons ? "" : "Export and Upload to " + scenename + " version " + versionnumber)))
                {
                    ExportAndUpload(true);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(currentScene == null || string.IsNullOrEmpty(currentScene.SceneId));
                if (GUI.Button(new Rect(305, 510, 210, 30), new GUIContent("Upload All Meshes", DisableButtons ? "" : "Export and Upload to " + scenename + " version " + versionnumber)))
                {
                    ExportAndUpload(false);
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUI.color = new Color(1, 0.9f, 0.9f);
                GUI.DrawTexture(new Rect(0, 450, 650, 150), EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;

                string errorMessage;
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                if (lastResponseCode != 200 || !EditorCore.IsDeveloperKeyValid)
                {
                    errorMessage = "<color=#880000ff>The Developer Key is Invalid or Expired. Please visit the project on our dashboard and update the key in the Project Setup Window</color>";
                    //TODO add a button to the dashboard? add a button to the project setup window too?
                }
                else
                {
                    //assume the user has done the project setup step first
                    errorMessage = "After completing the Scene Setup Window, you will be able to manage Dynamic Objects directly from here. Once you are done adding Dynamic Object components to GameObjects and Prefabs, press";
                    if (GUI.Button(new Rect(430, 510, 100, 30), "Continue"))
                    {
                        //add popup asking to export all dynamic object meshes
                        bool doExport = EditorUtility.DisplayDialog("Export Dynamic Object Meshes", "Do you want to export meshes for all Dynamic Objects now?", "Ok", "No");
                        if (doExport)
                        {
                            List<DynamicObject> exportList = new List<DynamicObject>();
                            foreach (var entry in Entries)
                            {
                                var dyn = entry.objectReference;
                                if (dyn == null) { continue; }
                                //check if export files exist
                                exportList.Add(dyn);
                            }
                            ExportUtility.ExportDynamicObjects(exportList);
                        }
                        SceneSetupWindow.Init();
                        Close();
                    }
                }

                GUI.Label(new Rect(30, 470, 540, 30), errorMessage, "normallabel");
            }
        }


        /// <summary>
        /// logic to get scene version, then prompt user if they want to export meshes or use existing export in folder
        /// then upload meshes to scene explorer and upload ids for aggregation
        /// </summary>
        /// <param name="selectedOnly"></param>
        void ExportAndUpload(bool selectedOnly)
        {
            EditorCore.RefreshSceneVersion(() =>
            {
                int selection = EditorUtility.DisplayDialogComplex("Export Meshes?", "Do you want to export meshes before uploading to Scene Explorer?", "Yes, export selected meshes", "No, use existing files", "Cancel");

                if (selection == 2) //cancel
                {
                    return;
                }

                List<GameObject> uploadList = new List<GameObject>();
                List<DynamicObject> exportList = new List<DynamicObject>();

                if (selection == 0) //export
                {
                    foreach (var entry in Entries)
                    {
                        var dyn = entry.objectReference;
                        if (dyn == null) { continue; }
                        if (selectedOnly)
                        {
                            if (!entry.selected) { continue; }
                        }
                        //check if export files exist
                        exportList.Add(dyn);
                        uploadList.Add(dyn.gameObject);
                    }
                    ExportUtility.ExportDynamicObjects(exportList);
                }
                else if (selection == 1) //don't export
                {
                    foreach (var entry in Entries)
                    {
                        var dyn = entry.objectReference;
                        if (dyn == null) { continue; }
                        if (selectedOnly)
                        {
                            if (!entry.selected) { continue; }
                        }
                        //check if export files exist
                        uploadList.Add(dyn.gameObject);
                    }
                }

                //upload meshes and ids
                EditorCore.RefreshSceneVersion(delegate ()
                {
                    if (ExportUtility.UploadSelectedDynamicObjectMeshes(uploadList, true))
                    {
                        var manifest = new AggregationManifest();
                        List<DynamicObject> manifestList = new List<DynamicObject>();
                        foreach (var entry in Entries)
                        {
                            if (selectedOnly)
                            {
                                if (!entry.selected) { continue; }
                            }
                            var dyn = entry.objectReference;
                            if (dyn == null) { continue; }

                            if (!entry.isIdPool)
                            {
                                if (dyn.UseCustomId == true)
                                {
                                    manifestList.Add(entry.objectReference);
                                }
                            }
                            else
                            {
                                foreach (var poolid in entry.poolReference.Ids)
                                {
                                    manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(entry.poolReference.PrefabName, entry.poolReference.MeshName, poolid, new float[3] { 1, 1, 1 }, new float[3] { 0, 0, 0 }, new float[4] { 0, 0, 0, 1 }));
                                }
                            }
                        }
                        AddOrReplaceDynamic(manifest, manifestList);
                        UploadManifest(manifest, null);
                    }
                });
            });
        }

        /// <summary>
        /// generate manifest from scene objects and upload to latest version of scene. should be done only after EditorCore.RefreshSceneVersion
        /// </summary>
        public static void UploadManifest(AggregationManifest manifest, System.Action callback, System.Action nodynamicscallback = null)
        {
            //if (Manifest == null) { Manifest = new AggregationManifest(); }
            //if (SceneVersionCollection == null) { Debug.LogError("SceneVersionCollection is null! Make sure RefreshSceneVersion was called before this"); return; }

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
            //Debug.Log("send " + manifestCount + " manifest requests");
        }

        static bool ManifestToJson(AggregationManifest manifest, out string json)
        {
            json = "{\"objects\":[";

            List<string> usedIds = new List<string>();

            bool containsValidEntry = false;
            foreach (var entry in manifest.objects)
            {
                if (string.IsNullOrEmpty(entry.mesh)) { Debug.LogWarning(entry.name + " missing meshname"); continue; }
                if (string.IsNullOrEmpty(entry.id)) { Debug.LogWarning(entry.name + " has empty dynamic id. This will not be aggregated"); continue; }
                if (usedIds.Contains(entry.id)) { Debug.LogWarning(entry.name + " using id (" + entry.id + ") that already exists in the scene. This may not be aggregated correctly"); }
                usedIds.Add(entry.id);
                json += "{";
                json += "\"id\":\"" + entry.id + "\",";
                json += "\"mesh\":\"" + entry.mesh + "\",";
                json += "\"name\":\"" + entry.name + "\",";
                json += "\"scaleCustom\":[" + entry.scaleCustom[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.scaleCustom[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.scaleCustom[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                json += "\"initialPosition\":[" + entry.position[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.position[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.position[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                json += "\"initialRotation\":[" + entry.rotation[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.rotation[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.rotation[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.rotation[3].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "]";
                json += "},";
                containsValidEntry = true;
            }

            json = json.Remove(json.Length - 1, 1);
            json += "]}";

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

            string url = CognitiveStatics.POSTDYNAMICMANIFEST(settings.SceneId, versionNumber);
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
            Util.logDebug("Manifest upload complete. response: " + text + (!string.IsNullOrEmpty(error) ? " error: " + error : ""));
            if (PostManifestResponseAction != null)
            {
                PostManifestResponseAction.Invoke();
                PostManifestResponseAction = null;
            }
        }

        /// <summary>
        /// adds or updates dynamic object ids in a provided manifest for aggregation
        /// </summary>
        /// <param name="manifest"></param>
        /// <param name="scenedynamics"></param>
        public static void AddOrReplaceDynamic(AggregationManifest manifest, List<DynamicObject> scenedynamics)
        {
            bool meshNameMissing = false;
            List<string> missingMeshGameObjects = new List<string>();
            foreach (var dynamic in scenedynamics)
            {

                var replaceEntry = manifest.objects.Find(delegate (AggregationManifest.AggregationManifestEntry obj) { return obj.id == dynamic.CustomId.ToString(); });
                if (replaceEntry == null)
                {
                    //don't include meshes with empty mesh names in manifest
                    if (!string.IsNullOrEmpty(dynamic.MeshName))
                    {
                        manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dynamic.gameObject.name, dynamic.MeshName, dynamic.CustomId.ToString(),
                            new float[3] { dynamic.transform.lossyScale.x, dynamic.transform.lossyScale.y, dynamic.transform.lossyScale.z },
                            new float[3] { dynamic.transform.position.x, dynamic.transform.position.y, dynamic.transform.position.z },
                            new float[4] { dynamic.transform.rotation.x, dynamic.transform.rotation.y, dynamic.transform.rotation.z, dynamic.transform.rotation.w }));
                    }
                    else
                    {
                        missingMeshGameObjects.Add(dynamic.gameObject.name);
                        meshNameMissing = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(dynamic.MeshName))
                    {
                        replaceEntry.mesh = dynamic.MeshName;
                        replaceEntry.name = dynamic.gameObject.name;
                    }
                    else
                    {
                        missingMeshGameObjects.Add(dynamic.gameObject.name);
                        meshNameMissing = true;
                    }
                }
            }

            if (meshNameMissing)
            {
                string debug = "Dynamic Objects missing mesh name:\n";
                foreach (var v in missingMeshGameObjects)
                {
                    debug += v + "\n";
                }
                Debug.LogWarning(debug);
                EditorUtility.DisplayDialog("Error", "One or more Dynamic Objects are missing a mesh name and were not uploaded to scene.\n\nSee Console for details", "Ok");
            }
        }

        void SelectDynamicEntries(List<Entry> entries)
        {
            if (entries == null)
            {
                Selection.objects = null;
                return;
            }
            List<Object> entryGameObjects = new List<Object>();
            foreach(var v in entries)
            {
                if (v.objectReference == null)
                {
                    entryGameObjects.Add(v.poolReference);
                }
                else
                {
                    entryGameObjects.Add(v.objectReference.gameObject);
                }
                
            }
            Selection.objects = entryGameObjects.ToArray();
        }
    }
}