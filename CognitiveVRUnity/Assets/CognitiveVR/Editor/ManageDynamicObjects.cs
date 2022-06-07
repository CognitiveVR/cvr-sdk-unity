using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

namespace CognitiveVR
{
    public class RenameDynamicWindow : EditorWindow
    {
        static ManageDynamicObjects sourceWindow;
        static string defaultMeshName;
        static System.Action<string> action;
        public static void Init(ManageDynamicObjects dynamicsWindow, string defaultName, System.Action<string> renameAction)
        {
            RenameDynamicWindow window = (RenameDynamicWindow)EditorWindow.GetWindow(typeof(RenameDynamicWindow), true, "Rename");
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

    public class ManageDynamicObjects : EditorWindow
{
    Rect steptitlerect = new Rect(30, 0, 100, 440);
        
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
            public Entry(string meshName, bool exportedMesh, DynamicObject reference, string name, bool initiallySelected)
            {
                objectReference = reference;
                gameobjectName = name;
                this.meshName = meshName;
                hasExportedMesh = exportedMesh;
                selected = initiallySelected;
            }
            public Entry(bool exportedMesh, DynamicObjectIdPool reference, bool initiallySelected)
            {
                isIdPool = true;
                poolReference = reference;
                idPoolCount = poolReference.Ids.Length;
                gameobjectName = poolReference.PrefabName;
                meshName = poolReference.MeshName;
                hasExportedMesh = exportedMesh;
                selected = initiallySelected;
            }
        }

    bool DisableButtons
    {
        get
        {
            var currentscene = CognitiveVR_Preferences.FindCurrentScene();
            bool sceneIsNull = currentscene == null || string.IsNullOrEmpty(currentscene.SceneId);
            return !EditorCore.IsDeveloperKeyValid || sceneIsNull || lastResponseCode != 200;
        }
    }

    public static void Init()
    {
        ManageDynamicObjects window = (ManageDynamicObjects)EditorWindow.GetWindow(typeof(ManageDynamicObjects), true, "");
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
            foreach(var o in objects)
            {
                bool selected = selectedDynamicsOnFocus.Contains(o);


                Entries.Add(new Entry(o.MeshName, (EditorCore.GetExportedDynamicObjectNames().Contains(o.MeshName) || !o.UseCustomMesh), o, o.gameObject.name, selected));
            }
            foreach (var p in pools)
            {
                bool selected = selectedPoolsOnFocus.Contains(p);
                Entries.Add(new Entry(EditorCore.GetExportedDynamicObjectNames().Contains(p.MeshName), p, selected));
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

        var currentscene = CognitiveVR_Preferences.FindCurrentScene();

        if (string.IsNullOrEmpty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name))
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS   Scene Not Saved", "steptitle");
        }
        else if (currentscene == null || string.IsNullOrEmpty(currentscene.SceneId))
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS   Scene Not Uploaded", "steptitle");
        }
        else
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS   " + currentscene.SceneName + " Version: " + currentscene.VersionNumber, "steptitle");
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
        Rect filterRect = new Rect(600 / 2 - searchBarWidth / 2-20, 25, 20, 20);
        Rect searchBarRect = new Rect(600/2 - searchBarWidth/2, 25, searchBarWidth, 20);
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
        if (GUI.Button(searchClearRect,"X", "ghostlabel"))
        {
            searchBarString = string.Empty;
            FilterList(searchBarString);
        }

        //headers
        Rect toggleRect = new Rect(30, 55, 30,30);
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
            }
            else
            {
                //select all visible
                foreach (var entry in Entries)
                {
                    if (!entry.visible) { entry.selected = false; continue; }
                    entry.selected = true;
                }
            }
        }

        Rect gameobject =  new Rect(60, 55, 120, 30);
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
        if (GUI.Button(mesh,"Mesh Name", meshNameStyle))
        {
            if (SortMethod != SortByMethod.MeshName)
                SortMethod = SortByMethod.MeshName;
            else
                SortMethod = SortByMethod.ReverseMeshName;
            SortByMeshName();
        }

        Rect idrect = new Rect(350, 55, 80, 30);
        //GUI.Label(uploaded, "Exported Mesh", "dynamicheader");
        if (GUI.Button(idrect, "Id", "dynamicheader"))
        {
            /*if (SortMethod != SortByMethod.Duration)
                SortMethod = SortByMethod.Duration;
            else
                SortMethod = SortByMethod.ReverseDuration;
            SortByExported();*/
        }

        Rect uploaded = new Rect(470, 55, 90, 30);
        string exportedStyle = (SortMethod == SortByMethod.Exported || SortMethod == SortByMethod.ReverseExported) ? "dynamicheaderbold" : "dynamicheader";
        if (GUI.Button(uploaded, "Exported Mesh", exportedStyle))
        {
            if (SortMethod != SortByMethod.Exported)
                SortMethod = SortByMethod.Exported;
            else
                SortMethod = SortByMethod.ReverseExported;
            SortByExported();
        }

        //IMPROVEMENT get list of uploaded mesh names from dashboard

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

            gm.ShowAsContext();
            //gm.AddItem("rename selected", false, OnRenameSelected);
        }

        
        Rect innerScrollSize = new Rect(30, 0, 520, visibleCount * 30);
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 80, 540, 325), dynamicScrollPosition, innerScrollSize, false, true);

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
        GUI.Box(new Rect(30, 80, 525, 325), "", "box_sharp_alpha");

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
        /*foreach(var v in Selection.gameObjects)
        {
            if (v.GetComponentInChildren<DynamicObject>())
                selectionCount++;
        }*/
        foreach(var entry in Entries)
            {
                if (entry.selected)
                {
                    selectionCount++;
                }
            }

        //IMPROVEMENT enable mesh upload from selected dynamic object id pool that has exported mesh files
        //if (Selection.activeObject.GetType() == typeof(DynamicObjectIdPool))
        //{
        //    var pool = Selection.activeObject as DynamicObjectIdPool;
        //    if (EditorCore.HasDynamicExportFiles(pool.MeshName))
        //    {
        //        selectionCount++;
        //    }       
        //}

        //texture resolution

        if (CognitiveVR_Preferences.Instance.TextureResize > 4) { CognitiveVR_Preferences.Instance.TextureResize = 4; }

        //resolution settings here
        EditorGUI.BeginDisabledGroup(DisableButtons);
        if (GUI.Button(new Rect(80, 415, 140, 35), new GUIContent("1/4 Resolution", DisableButtons?"":"Quarter resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 4 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 4;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 4)
        {
            GUI.Box(new Rect(80, 415, 140, 35), "", "box_sharp_alpha");
        }

        if (GUI.Button(new Rect(230, 415, 140, 35), new GUIContent("1/2 Resolution", DisableButtons ? "" : "Half resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 2 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 2;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 2)
        {
            GUI.Box(new Rect(230, 415, 140, 35), "", "box_sharp_alpha");
        }

        if (GUI.Button(new Rect(380, 415, 140, 35), new GUIContent("1/1 Resolution", DisableButtons ? "" : "Full resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 1;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 1)
        {
            GUI.Box(new Rect(380, 415, 140, 35), "", "box_sharp_alpha");
        }
        EditorGUI.EndDisabledGroup();

        
        EditorGUI.BeginDisabledGroup(currentscene == null || string.IsNullOrEmpty(currentscene.SceneId) || selectionCount == 0);
        if (GUI.Button(new Rect(80, 460, 215, 30), new GUIContent("Upload " + selectionCount + " Selected Meshes", DisableButtons ? "" : "Export and Upload to " + scenename + " version " + versionnumber)))
        {
            EditorCore.RefreshSceneVersion(() =>
            {
                List<GameObject> uploadList = new List<GameObject>();
                List<DynamicObject> exportList = new List<DynamicObject>();
                foreach(var entry in Entries)
                {
                    var dyn = entry.objectReference;
                    if (dyn == null) { continue; }
                    if (!entry.selected) { continue; }
                    //check if export files exist
                    if (!EditorCore.HasDynamicExportFiles(dyn.MeshName))
                    {
                        exportList.Add(dyn);                        
                    }
                    //check if thumbnail exists
                    if (!EditorCore.HasDynamicObjectThumbnail(dyn.MeshName))
                    {
                        EditorCore.SaveDynamicThumbnailAutomatic(dyn.gameObject);
                    }
                    uploadList.Add(dyn.gameObject);
                }

                ExportUtility.ExportDynamicObjects(exportList);

                EditorCore.RefreshSceneVersion(delegate ()
                {
                    ExportUtility.UploadSelectedDynamicObjectMeshes(uploadList, true);
                    //var manifest = new AggregationManifest();
                    //AddOrReplaceDynamic(manifest, GetDynamicObjectsInScene());
                    ////if an id pool is explicitly selected, you can upload it here. otherwise this should never upload Id Pools automatically! possible these aren't wanted in a scene and will clutter dashboard
                    //ManageDynamicObjects.UploadManifest(manifest, () => ExportUtility.UploadSelectedDynamicObjectMeshes(true));
                    //
                    //foreach (var entry in Entries)
                    //{
                    //    if (!entry.isIdPool) { continue; }
                    //    if (!entry.selected) { continue; }
                    //
                    //    ManageDynamicObjects.AggregationManifest poolManifest = new CognitiveVR.ManageDynamicObjects.AggregationManifest();
                    //    foreach (var id in entry.poolReference.Ids)
                    //    {
                    //        poolManifest.objects.Add(new ManageDynamicObjects.AggregationManifest.AggregationManifestEntry(entry.poolReference.PrefabName, entry.poolReference.MeshName, id, new float[3] { 1, 1, 1 }));
                    //    }
                    //    ManageDynamicObjects.UploadManifest(poolManifest, null);
                    //}
                });
            });
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUI.BeginDisabledGroup(currentscene == null || string.IsNullOrEmpty(currentscene.SceneId));
        if (GUI.Button(new Rect(305, 460, 210, 30), new GUIContent("Upload All Meshes", DisableButtons ? "" : "Export and Upload to " + scenename + " version " + versionnumber)))
        {
            EditorCore.RefreshSceneVersion(() =>
            {
                var dynamics = GameObject.FindObjectsOfType<DynamicObject>();
                List<GameObject> gos = new List<GameObject>();
                foreach (var v in dynamics)
                {
                    gos.Add(v.gameObject);
                }

                Selection.objects = gos.ToArray();

                List<GameObject> uploadList = new List<GameObject>();
                List<DynamicObject> exportList = new List<DynamicObject>();
                foreach (var entry in Entries)
                {
                    var dyn = entry.objectReference;
                    if (dyn == null) { continue; }
                    if (!entry.selected) { continue; }
                    //check if export files exist
                    if (!EditorCore.HasDynamicExportFiles(dyn.MeshName))
                    {
                        exportList.Add(dyn);
                    }
                    //check if thumbnail exists
                    if (!EditorCore.HasDynamicObjectThumbnail(dyn.MeshName))
                    {
                        EditorCore.SaveDynamicThumbnailAutomatic(dyn.gameObject);
                    }
                    uploadList.Add(dyn.gameObject);
                }
                ExportUtility.ExportDynamicObjects(exportList);

                EditorCore.RefreshSceneVersion(delegate ()
                {
                    ExportUtility.UploadSelectedDynamicObjectMeshes(uploadList, true);
                    //var manifest = new AggregationManifest();
                    //AddOrReplaceDynamic(manifest, GetDynamicObjectsInScene());
                    //Important! this should never upload Id Pools automatically! possible these aren't wanted in a scene and will clutter dashboard
                    //ManageDynamicObjects.UploadManifest(manifest, () => );
                });
            });
        }
        EditorGUI.EndDisabledGroup();

        DrawFooter();
        Repaint(); //manually repaint gui each frame to make sure it's responsive
    }

    Vector2 dynamicScrollPosition;

    DynamicObject[] _cachedDynamics;
    DynamicObject[] GetDynamicObjects { get { if (_cachedDynamics == null || _cachedDynamics.Length == 0) { _cachedDynamics = FindObjectsOfType<DynamicObject>(); } return _cachedDynamics; } }


    public static List<DynamicObject> GetDynamicObjectsInScene()
    {
        return new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
    }

    List<DynamicObject> selectedDynamicsOnFocus = new List<DynamicObject>();
        List<DynamicObjectIdPool> selectedPoolsOnFocus = new List<DynamicObjectIdPool>();
    private void OnFocus()
    {
        _cachedDynamics = FindObjectsOfType<DynamicObject>();
        EditorCore.ExportedDynamicObjects = null;
        EditorCore._cachedPoolAssets = null;

        //TODO want to refresh list, but also keep selected items
        //make a list of selected dynamics by reference. when reconstructing the entries list, 
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
            FilterList(searchBarString);
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
            RenameDynamicWindow.Init(this, defaultvalue, RenameGameObject);
        }

        void OnRenameMeshSelected()
        {
            string defaultvalue = string.Empty;
            foreach(var entry in Entries)
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
            RenameDynamicWindow.Init(this, defaultvalue, RenameMesh);
        }

        void OnOpenDynamicExportFolder()
        {
            EditorUtility.RevealInFinder(EditorCore.GetDynamicExportDirectory());
        }

        enum SortByMethod
        {
            GameObjectName,
            ReverseGameObjectName,
            MeshName,
            ReverseMeshName,
            //Id,
            //ReverseId,
            Exported,
            ReverseExported
        }
        SortByMethod SortMethod;

    void SortByName()
    {
        Entries.Sort(delegate (Entry x, Entry y)
        {
            return string.Compare(x.gameobjectName, y.gameobjectName);
        });
        if (SortMethod == SortByMethod.ReverseGameObjectName)
            Entries.Reverse();
    }

    void SortByMeshName()
    {
        Entries.Sort(delegate (Entry x, Entry y)
        {
            return string.Compare(x.meshName, y.meshName);
        });
        if (SortMethod == SortByMethod.ReverseMeshName)
            Entries.Reverse();
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
            Entries.Reverse();
    }

    void OnToggleMeshFilter()
    {
        filterMeshes = !filterMeshes;
        if (searchBarString != string.Empty)
            FilterList(searchBarString);
    }
    void OnToggleGameObjectFilter()
    {
        filterGameObjects = !filterGameObjects;
        if (searchBarString != string.Empty)
            FilterList(searchBarString);
    }
    void OnToggleIdFilter()
    {
        filterIds = !filterIds;
        if (searchBarString != string.Empty)
            FilterList(searchBarString);
    }

    bool CommonMeshesContainsSearch(string search)
        {
            foreach(var commonMesh in System.Enum.GetNames(typeof(DynamicObject.CommonDynamicMesh)))
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

    void DrawDynamicObjectEntry(Entry dynamic, Rect rect, bool darkbackground)
    {
        Event e = Event.current;
        //CONSIDER also allowing selection with e.type == EventType.MouseDrag
        if (e.isMouse && e.type == EventType.MouseDown)
        {
            if (e.mousePosition.x < rect.x + 40 || e.mousePosition.x > rect.x+rect.width - 80 || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y+rect.height)
            {
            }
            else
            {
                if (e.shift) //add to selection
                {
                    if (!dynamic.isIdPool)
                    {
                        GameObject[] gos = new GameObject[Selection.transforms.Length + 1];
                        Selection.gameObjects.CopyTo(gos, 0);
                        gos[gos.Length - 1] = dynamic.objectReference.gameObject;
                        Selection.objects = gos;
                    }
                    else
                    {
                        Object[] gos = new Object[Selection.objects.Length + 1];
                        Selection.objects.CopyTo(gos, 0);
                        gos[gos.Length - 1] = dynamic.poolReference;
                        Selection.objects = gos;
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
        Rect idRect = new Rect(rect.x + 320, rect.y, 150, rect.height);
        Rect uploaded = new Rect(rect.x + 480, rect.y, 24, rect.height);

        var toggleIcon = dynamic.selected ? EditorCore.BlueCheckmark : EditorCore.EmptyBlueCheckmark;
        dynamic.selected = GUI.Toggle(selectedRect, dynamic.selected, toggleIcon, image_centered);

        //gameobject name or id pool count
        GUI.Label(gameobjectRect, dynamic.gameobjectName, dynamiclabel);
        GUI.Label(mesh, dynamic.objectReference.UseCustomMesh?dynamic.meshName:dynamic.objectReference.CommonMesh.ToString(), dynamiclabel);

        if (dynamic.isIdPool)
        {
            GUI.Label(idRect, "ID Pool (" + dynamic.idPoolCount + ")", dynamiclabel);
        }
        else
        {
            GUI.Label(idRect, dynamic.objectReference.CustomId, dynamiclabel);
        }

        //has been exported
        if (!dynamic.objectReference.UseCustomMesh || EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.meshName))
        {
            GUI.Label(uploaded, EditorCore.Checkmark, image_centered);
        }
        else
        {
            GUI.Label(uploaded, EditorCore.EmptyCheckmark, image_centered);
        }
    }
    
    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 500, 600, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        string tooltip = "";
        
        var currentScene = CognitiveVR_Preferences.FindCurrentScene();
        if (currentScene == null || string.IsNullOrEmpty(currentScene.SceneId))
        {
            tooltip = "Upload list of all Dynamic Object IDs. Scene settings not saved";
        }
        else
        {
            tooltip = "Upload list of all Dynamic Object IDs and Mesh Names to " + currentScene.SceneName + " version " + currentScene.VersionNumber;
        }

        bool enabled = !(currentScene == null || string.IsNullOrEmpty(currentScene.SceneId)) && lastResponseCode == 200;
        if (enabled)
        {
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
            if (selectedEntries == 0 || selectedEntries == Entries.Count)
            {
                if (GUI.Button(new Rect(130, 510, 350, 30), new GUIContent("Upload All Ids for Aggregation", tooltip)))
                {
                    //all dynamics in scene - should be selected dynamics?
                    EditorCore.RefreshSceneVersion(delegate ()
                    {
                        AggregationManifest manifest = new AggregationManifest();
                        //var uploadList = new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
                        var uploadList = new List<DynamicObject>();
                        foreach (var entry in Entries)
                        {
                            if (!entry.isIdPool)
                                uploadList.Add(entry.objectReference);
                            else
                            {
                                foreach (var poolid in entry.poolReference.Ids)
                                {
                                    manifest.objects.Add(new ManageDynamicObjects.AggregationManifest.AggregationManifestEntry(entry.poolReference.PrefabName, entry.poolReference.MeshName, poolid, new float[3] { 1, 1, 1 }, new float[3] { 0, 0, 0 }, new float[4] { 0, 0, 0, 1 }));
                                }
                            }
                        }
                        AddOrReplaceDynamic(manifest, uploadList);
                        //TODO manifest add pool ids
                        //Important! this should never upload Id Pools automatically! possible these aren't wanted in a scene and will clutter dashboard
                        ManageDynamicObjects.UploadManifest(manifest, null);
                    });
                }
            }
            else
            {
                if (GUI.Button(new Rect(130, 510, 350, 30), new GUIContent("Upload "+selectionCount+" Selected Ids for Aggregation", tooltip)))
                {
                    //all dynamics in scene - should be selected dynamics?
                    EditorCore.RefreshSceneVersion(delegate ()
                    {
                        AggregationManifest manifest = new AggregationManifest();
                        //var uploadList = new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
                        var uploadList = new List<DynamicObject>();
                        foreach (var entry in Entries)
                        {
                            if (!entry.selected) { continue; }
                            if (!entry.isIdPool)
                                uploadList.Add(entry.objectReference);
                            else
                            {
                                foreach (var poolid in entry.poolReference.Ids)
                                {
                                    manifest.objects.Add(new ManageDynamicObjects.AggregationManifest.AggregationManifestEntry(entry.poolReference.PrefabName, entry.poolReference.MeshName, poolid, new float[3] { 1, 1, 1 }, new float[3] { 0, 0, 0 }, new float[4] { 0, 0, 0, 1 }));
                                }
                            }
                        }
                        AddOrReplaceDynamic(manifest, uploadList);
                        //Important! this should never upload Id Pools automatically! possible these aren't wanted in a scene and will clutter dashboard
                        ManageDynamicObjects.UploadManifest(manifest, null);
                    });
                }
            }
        }
        else
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            string errorMessage;

            if (lastResponseCode != 200)
            {
                errorMessage = "<color=#880000ff>The Developer Key is Invalid or Expired.\n\nPlease visit the project on our dashboard and update the key in the Scene Setup Window</color>";
            }
            else
            {
                errorMessage = "<color=#880000ff>This scene <color=red>" + scene.name.ToUpper() + "</color> does not have a valid SceneId.\n\nPlease upload this scene using the Scene Setup Window</color>";
            }

            if (!EditorCore.IsDeveloperKeyValid)
            {
                errorMessage = "Developer Key not set";
            }
            
            EditorGUI.BeginDisabledGroup(true);
            GUI.Button(new Rect(80, 510, 350, 30), new GUIContent("Upload Ids to SceneExplorer for Aggregation", tooltip));
            EditorGUI.EndDisabledGroup();

            GUI.color = new Color(1, 0.9f, 0.9f);
            GUI.DrawTexture(new Rect(0, 420, 650, 150), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 430, 530, 30), errorMessage,"normallabel");
        
            if (GUI.Button(new Rect(380, 510, 120, 30), "Scene Setup"))
            {
                InitWizard.Init();
            }
        }
    }

    //currently unused
    //get dynamic object aggregation manifest for the current scene
    void GetManifest()
    {
        var currentSceneSettings = CognitiveVR_Preferences.FindCurrentScene();
        if (currentSceneSettings == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
        {
            Util.logWarning("Get Manifest current scene doesn't have an id!");
            return;
        }

        string url = CognitiveStatics.GETDYNAMICMANIFEST(currentSceneSettings.VersionId);

        Dictionary<string, string> headers = new Dictionary<string, string>();
        if (EditorCore.IsDeveloperKeyValid)
            headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
        EditorNetwork.Get(url, GetManifestResponse, headers,false);//AUTH
    }

    
    //currently unused
    void GetManifestResponse(int responsecode, string error, string text)
    {
        if (responsecode == 200)
        {
            //BuildManifest(getRequest.text);
            var allEntries = JsonUtil.GetJsonArray<AggregationManifest.AggregationManifestEntry>(text);

            Debug.Log("Number of Dynamic Objects in current Manifest: " + allEntries.Length);

            var Manifest = new AggregationManifest();

            Manifest.objects = new List<AggregationManifest.AggregationManifestEntry>(allEntries);
            Repaint();

            //also hit settings to get the current version of the scene
            EditorCore.RefreshSceneVersion(null);
        }
        else
        {
            Util.logWarning("GetManifestResponse " + responsecode + " " + error);
        }
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
            public float[] scaleCustom = new float[3]{1,1,1};
            public float[] position = new float[3] { 0, 0, 0 };
            public float[] rotation = new float[4] { 0, 0, 0 , 1};
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
        //public int Version;
        //public string SceneId;
    }

    //only need id, mesh and name
    //static AggregationManifest Manifest;
    //static SceneVersionCollection SceneVersionCollection;

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
        while(true)
        {
            debugBreakManifestLimit--;
            if (debugBreakManifestLimit == 0) { Debug.LogError("dynamic aggregation manifest error"); break; }
            if (manifest.objects.Count == 0) { break; }

            AggregationManifest am = new AggregationManifest();
            am.objects.AddRange(manifest.objects.GetRange(0, Mathf.Min(250,manifest.objects.Count)));
            manifest.objects.RemoveRange(0, Mathf.Min(250, manifest.objects.Count));
            string json = "";
            if (ManifestToJson(am, out json))
            {
                manifestCount++;
                var currentSettings = CognitiveVR_Preferences.FindCurrentScene();
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
            if (usedIds.Contains(entry.id)) { Debug.LogWarning(entry.name + " using id ("+entry.id+") that already exists in the scene. This may not be aggregated correctly"); }
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
        var settings = CognitiveVR_Preferences.FindCurrentScene();
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
            headers.Add("Content-Type","application/json");
        }
        PostManifestResponseAction = callback;
        EditorNetwork.QueuePost(url, json, PostManifestResponse,headers,false);//AUTH
    }

    static void PostManifestResponse(int responsecode, string error, string text)
    {
        Util.logDebug("Manifest upload complete. response: " + text + (!string.IsNullOrEmpty(error)? " error: " + error:""));
        if (PostManifestResponseAction != null)
        {
            PostManifestResponseAction.Invoke();
            PostManifestResponseAction = null;
        }
    }

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
}
}