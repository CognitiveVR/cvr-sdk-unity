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
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
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
        

    class Entry
    {
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
            public Entry(bool exportedMesh, DynamicObjectIdPool reference)
            {
                isIdPool = true;
                poolReference = reference;
                idPoolCount = poolReference.Ids.Length;
                gameobjectName = poolReference.PrefabName;
                meshName = poolReference.MeshName;
                hasExportedMesh = exportedMesh;
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
                Entries.Add(new Entry(EditorCore.GetExportedDynamicObjectNames().Contains(p.MeshName), p));
            }
                selectedDynamicsOnFocus = new List<DynamicObject>();
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
            GUI.Label(new Rect(30, 80, 420, 270), "No objects found.\n\nHave you attached any Dynamic Object components to objects?\n\nAre they active in your hierarchy?", "button_disabledtext");
        }

        //DynamicObjectIdPool[] poolAssets = EditorCore.GetDynamicObjectPoolAssets;

        //build up list of Entries, then cache?
        GetEntryList(GetDynamicObjects, EditorCore.GetDynamicObjectPoolAssets);

        //filter
        Rect filterRect = new Rect(40, 25, 20, 20);
        Rect searchBarRect = new Rect(60, 25, 200, 20);
        Rect eyeGlassRect = new Rect(260, 25, 50, 20);
        Rect eyeGlassRect2 = new Rect(290, 25, 50, 20);
        //dropdown with id/mesh/gameobject toggles


        //EditorGUIUtility.FindTexture( "toolbarsearch focused@2x" )
        var filterIcon = EditorGUIUtility.FindTexture("d_FilterByType");
        var settingsIcon = EditorGUIUtility.FindTexture("_Popup");
        //_Popup   _Popup@2x
        //	d_Search Icon
        //	console.infoicon
        //d_FilterByType

        //GUI.Label(eyeGlassRect, searchIcon);
        //GUI.Label(eyeGlassRect2, endIcon);
        string temp = GUI.TextField(searchBarRect, searchBarString);
        if (temp == string.Empty)
        {
            GUI.Label(searchBarRect, "<size=15>Search</size>", "ghostlabel");
            if (searchBarString != string.Empty)
                {
                    Debug.Log("show all");
                    ShowAllList();
                }
                
            searchBarString = temp;
        }
        else if (temp != searchBarString)
        {
                Debug.Log("DIRTY SEARCH STRING");
            searchBarString = temp;
                //re-filter the list
                FilterList(searchBarString);
        }

        if (GUI.Button(filterRect, filterIcon, "label"))
        {
            //generic menu
            GenericMenu gm = new GenericMenu();
            gm.AddItem(new GUIContent("Meshes"), filterMeshes, OnToggleMeshFilter);
            gm.AddItem(new GUIContent("GameObjects"), filterGameObjects, OnToggleGameObjectFilter);
            gm.AddItem(new GUIContent("Ids"), filterIds, OnToggleIdFilter);
            gm.ShowAsContext();
        }

        //GUI.TextField

        //headers
        Rect toggleRect = new Rect(40, 55, 20, 30);
        bool pressed = GUI.Button(toggleRect, "","toggle");
        if (pressed)
        {
            bool allselected = true;
            foreach (var entry in Entries)
            {
                if (!entry.visible) { continue; }
                if (!entry.selected) { allselected = false; break; }
            }
            if (allselected)
            {
                Debug.Log("deselect all");
                //deselect all
                foreach (var entry in Entries)
                {
                    entry.selected = false;
                    //entry.selected
                }
            }
            else
            {
                Debug.Log("select all visible");
                //select all visible
                foreach (var entry in Entries)
                {
                    if (!entry.visible) { entry.selected = false; continue; }
                    entry.selected = true;
                }
            }
        }

        Rect gameobject =  new Rect(60, 55, 120, 30);
        //GUI.Label(gameobject, "GameObject", "dynamicheader");
        if (GUI.Button(gameobject, "GameObject", "dynamicheader"))
        {
            if (SortMethod != SortByMethod.Duration)
                SortMethod = SortByMethod.Duration;
            else
                SortMethod = SortByMethod.ReverseDuration;
            SortByName();
        }

        Rect mesh = new Rect(210, 55, 120, 30);
        //GUI.Label(mesh, "Mesh Name", "dynamicheader");
        if (GUI.Button(mesh,"Mesh Name","dynamicheader"))
        {
            if (SortMethod != SortByMethod.Duration)
                SortMethod = SortByMethod.Duration;
            else
                SortMethod = SortByMethod.ReverseDuration;
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

        Rect uploaded = new Rect(480, 55, 80, 30);
        //GUI.Label(uploaded, "Exported Mesh", "dynamicheader");
        if (GUI.Button(uploaded, "Exported Mesh", "dynamicheader"))
        {
            if (SortMethod != SortByMethod.Duration)
                SortMethod = SortByMethod.Duration;
            else
                SortMethod = SortByMethod.ReverseDuration;
            SortByExported();
        }

        //IMPROVEMENT get list of uploaded mesh names from dashboard

        

        //gear icon
        Rect tools = new Rect(580, 55, 20, 30);
        if (GUI.Button(tools, settingsIcon,"label")) //rename dropdown
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
            gm.ShowAsContext();
            //gm.AddItem("rename selected", false, OnRenameSelected);
        }


        Rect innerScrollSize = new Rect(30, 0, 520, (Entries.Count) * 30);
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
        if (GUI.Button(new Rect(30, 415, 140, 35), new GUIContent("1/4 Resolution", DisableButtons?"":"Quarter resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 4 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 4;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 4)
        {
            GUI.Box(new Rect(30, 415, 140, 35), "", "box_sharp_alpha");
        }

        if (GUI.Button(new Rect(180, 415, 140, 35), new GUIContent("1/2 Resolution", DisableButtons ? "" : "Half resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 2 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 2;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 2)
        {
            GUI.Box(new Rect(180, 415, 140, 35), "", "box_sharp_alpha");
        }

        if (GUI.Button(new Rect(330, 415, 140, 35), new GUIContent("1/1 Resolution", DisableButtons ? "" : "Full resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 1;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 1)
        {
            GUI.Box(new Rect(330, 415, 140, 35), "", "box_sharp_alpha");
        }
        EditorGUI.EndDisabledGroup();

        
        EditorGUI.BeginDisabledGroup(currentscene == null || string.IsNullOrEmpty(currentscene.SceneId) || selectionCount == 0);
        if (GUI.Button(new Rect(30, 460, 200, 30), new GUIContent("Upload " + selectionCount + " Selected Meshes", DisableButtons ? "" : "Export and Upload to " + scenename + " version " + versionnumber)))
        {
            EditorCore.RefreshSceneVersion(() =>
            {
                foreach(var entry in Entries)
                //foreach(var go in Selection.gameObjects)
                {
                    var dyn = entry.objectReference;
                    if (dyn == null) { continue; }
                    //check if export files exist
                    if (!EditorCore.HasDynamicExportFiles(dyn.MeshName))
                    {
                        ExportUtility.ExportDynamicObject(dyn);
                    }
                    //check if thumbnail exists
                    if (!EditorCore.HasDynamicObjectThumbnail(dyn.MeshName))
                    {
                        EditorCore.SaveDynamicThumbnailAutomatic(dyn.gameObject);
                    }
                }

                EditorCore.RefreshSceneVersion(delegate ()
                {
                    var manifest = new AggregationManifest();
                    AddOrReplaceDynamic(manifest, GetDynamicObjectsInScene());
                    ManageDynamicObjects.UploadManifest(manifest, () => ExportUtility.UploadSelectedDynamicObjectMeshes(true));
                });
            });
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUI.BeginDisabledGroup(currentscene == null || string.IsNullOrEmpty(currentscene.SceneId));
        if (GUI.Button(new Rect(270, 460, 200, 30), new GUIContent("Upload All Meshes", DisableButtons ? "" : "Export and Upload to " + scenename + " version " + versionnumber)))
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

                foreach (var go in Selection.gameObjects)
                {
                    var dyn = go.GetComponent<DynamicObject>();
                    if (dyn == null) { continue; }
                    //check if export files exist
                    if (!EditorCore.HasDynamicExportFiles(dyn.MeshName))
                    {
                        ExportUtility.ExportDynamicObject(dyn);
                    }
                    //check if thumbnail exists
                    if (!EditorCore.HasDynamicObjectThumbnail(dyn.MeshName))
                    {
                        EditorCore.SaveDynamicThumbnailAutomatic(dyn.gameObject);
                    }
                }

                EditorCore.RefreshSceneVersion(delegate ()
                {
                    var manifest = new AggregationManifest();
                    AddOrReplaceDynamic(manifest, GetDynamicObjectsInScene());
                    ManageDynamicObjects.UploadManifest(manifest, () => ExportUtility.UploadSelectedDynamicObjectMeshes(true));
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


        List<DynamicObject> selectedDynamicsOnFocus = new List<DynamicObject>();
    private void OnFocus()
    {
        _cachedDynamics = FindObjectsOfType<DynamicObject>();
        EditorCore.ExportedDynamicObjects = null;
        EditorCore._cachedPoolAssets = null;
        
        //TODO want to refresh list, but also keep selected items
        //make a list of selected dynamics by reference. when reconstructing the entries list, 
        foreach(var e in Entries)
        {
            if (e.objectReference == null) { continue; }
            if (!e.selected) { continue; }
            selectedDynamicsOnFocus.Add(e.objectReference);
        }
            
        Entries = null;
        GetEntryList(GetDynamicObjects, EditorCore.GetDynamicObjectPoolAssets);
        if (!string.IsNullOrEmpty(searchBarString))
            FilterList(searchBarString);
    }
    
    //void RefreshSceneDynamics()
    //{
    //    _cachedDynamics = FindObjectsOfType<DynamicObject>();
    //    EditorCore.ExportedDynamicObjects = null;
    //}

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
                    defaultvalue = "Multple";
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
                    defaultvalue = "Multple";
                    break;
                }
            }
            RenameDynamicWindow.Init(this, defaultvalue, RenameMesh);
        }

        enum SortByMethod
        {
            Sequence,
            ReverseSequence,
            Duration,
            ReverseDuration,
            Visits,
            ReverseVisits
        }
        SortByMethod SortMethod;

    void SortByName()
    {
        Entries.Sort(delegate (Entry x, Entry y)
        {
            return string.Compare(x.gameobjectName, y.gameobjectName);
        });
        if (SortMethod == SortByMethod.ReverseDuration)
            Entries.Reverse();
    }

    void SortByMeshName()
    {
        Entries.Sort(delegate (Entry x, Entry y)
        {
            return string.Compare(x.meshName, y.meshName);
        });
        if (SortMethod == SortByMethod.ReverseDuration)
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
        if (SortMethod == SortByMethod.ReverseDuration)
            Entries.Reverse();
    }

    void DrawSortIcons()
    {
        //SequenceSortIcon.enabled = false;
        //DurationSortIcon.enabled = false;
        //VisitsSortIcon.enabled = false;
        //
        //if (SortMethod == SortByMethod.Sequence)
        //{
        //    SequenceSortIcon.enabled = true;
        //    SequenceSortIcon.transform.rotation = Quaternion.identity;
        //}
        //else if (SortMethod == SortByMethod.ReverseSequence)
        //{
        //    SequenceSortIcon.enabled = true;
        //    SequenceSortIcon.transform.rotation = Quaternion.Euler(0, 0, 180);
        //}
        //else if (SortMethod == SortByMethod.Duration)
        //{
        //    DurationSortIcon.enabled = true;
        //    DurationSortIcon.transform.rotation = Quaternion.identity;
        //}
        //else if (SortMethod == SortByMethod.ReverseDuration)
        //{
        //    DurationSortIcon.enabled = true;
        //    DurationSortIcon.transform.rotation = Quaternion.Euler(0, 0, 180);
        //}
        //else if (SortMethod == SortByMethod.Visits)
        //{
        //    VisitsSortIcon.enabled = true;
        //    VisitsSortIcon.transform.rotation = Quaternion.identity;
        //}
        //else if (SortMethod == SortByMethod.ReverseVisits)
        //{
        //    VisitsSortIcon.enabled = true;
        //    VisitsSortIcon.transform.rotation = Quaternion.Euler(0, 0, 180);
        //}
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

    public void FilterList(string inputstring)
    {
        string compareString = inputstring.ToLower();
        foreach (var entry in Entries)
        {
            entry.visible = false;
            if (filterMeshes && entry.meshName.ToLower().Contains(compareString))
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

        /*
    public void FilterMeshNames(string meshname)
    {
        foreach(var entry in Entries)
        {
            entry.visible = false;
            if (entry.meshName.Contains(meshname))
            {
                entry.visible = true;
            }
        }
    }

    public void FilterObjectNames(string objectName)
    {
        foreach(var entry in Entries)
        {
            entry.visible = false;
            if (entry.gameobjectName.Contains(objectName))
            {
                entry.visible = true;
            }
        }
    }

    public void FilterIds(string id)
    {
        foreach (var entry in Entries)
        {
            entry.visible = false;
            if (!entry.isIdPool)
            {
                if (entry.objectReference.CustomId == id)
                    entry.visible = true;
            }
            else
            {
                foreach(var i in entry.poolReference.Ids)
                {
                    if (i.Contains(id))
                    {
                        entry.visible = true;
                        break;
                    }
                }
            }
        }
    }
        */
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
        /*if (e.isMouse && e.type == EventType.MouseDown)
        {
            if (e.mousePosition.x < rect.x || e.mousePosition.x > rect.x+rect.width || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y+rect.height)
            {
            }
            else
            {
                if (e.shift) //add to selection
                {
                    GameObject[] gos = new GameObject[Selection.transforms.Length + 1];
                    Selection.gameObjects.CopyTo(gos, 0);
                    gos[gos.Length - 1] = dynamic.gameObject;
                    Selection.objects = gos;
                }
                else
                {
                    Selection.activeTransform = dynamic.transform;
                }
            }
        }*/

        //background
        if (darkbackground)
            GUI.Box(rect, "", "dynamicentry_even");
        else
            GUI.Box(rect, "", "dynamicentry_odd");

        Rect selectedRect = new Rect(rect.x + 0, rect.y, 30, rect.height);
        Rect gameobjectRect = new Rect(rect.x + 30, rect.y, 140, rect.height);
        Rect mesh = new Rect(rect.x + 180, rect.y, 140, rect.height);
        Rect collider = new Rect(rect.x + 320, rect.y, 24, rect.height);
        Rect idRect = new Rect(rect.x + 320, rect.y, 150, rect.height);
        Rect uploaded = new Rect(rect.x + 480, rect.y, 24, rect.height);

        dynamic.selected = GUI.Toggle(selectedRect, dynamic.selected,"");


        //gameobject name or id pool count
        GUI.Label(gameobjectRect, dynamic.gameobjectName, "dynamiclabel");
        GUI.Label(mesh, dynamic.meshName, "dynamiclabel");

        if (dynamic.isIdPool)
        {
                GUI.Label(idRect, "ID Pool (" + dynamic.idPoolCount + ")", "dynamiclabel");
            }
        else
        {
            GUI.Label(idRect, dynamic.objectReference.CustomId, "dynamiclabel");
        }

        //has been exported
        if (EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.meshName)) //|| !dynamic.UseCustomMesh
        {
            GUI.Label(uploaded, EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(uploaded, EditorCore.EmptyCheckmark, "image_centered");
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
            if (GUI.Button(new Rect(80, 510, 350, 30), new GUIContent("Upload Ids to SceneExplorer for Aggregation", tooltip)))
            {
                EditorCore.RefreshSceneVersion(delegate ()
                {
                    AggregationManifest manifest = new AggregationManifest();
                    AddOrReplaceDynamic(manifest, GetDynamicObjectsInScene());
                    //Important! this should never upload Id Pools automatically! possible these aren't wanted in a scene and will clutter dashboard
                    ManageDynamicObjects.UploadManifest(manifest, null);
                });
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
            GUI.DrawTexture(new Rect(0, 420, 550, 150), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 430, 430, 30), errorMessage,"normallabel");
        
            if (GUI.Button(new Rect(380, 510, 80, 30), "Fix"))
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
    
    public static List<DynamicObject> GetDynamicObjectsInScene()
    {
        return new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
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
            public AggregationManifestEntry(string _name, string _mesh, string _id, float[] _scaleCustom)
            {
                name = _name;
                mesh = _mesh;
                id = _id;
                scaleCustom = _scaleCustom;
            }
            public override string ToString()
            {
                //return "{\"name\":\"" + name + "\",\"mesh\":\"" + mesh + "\",\"id\":\"" + id + "\",\"scaleCustom\":\"" + scaleCustom[0] + "," + scaleCustom[1] + "," + scaleCustom[2] + ",\"initialPosition\":\"" + position[0] + "," + position[1] + "," + position[2] + ",\"initialRotation\":\"" + rotation[0] + "," + rotation[1] + "," + rotation[2] + "," + rotation[3] + "\"}";
                return "{\"name\":\"" + name + "\",\"mesh\":\"" + mesh + "\",\"id\":\"" + id + "\",\"scaleCustom\":\"" + scaleCustom[0] + "," + scaleCustom[1] + "," + scaleCustom[2] + "\"}";        
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

        Debug.Log("send " + manifestCount + " manifest requests");

        /*string json = "";
        if (ManifestToJson(manifest, out json))
        {
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
        }*/
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
//<<<<<<< Updated upstream
            json += "\"scaleCustom\":[" + entry.scaleCustom[0] + "," + entry.scaleCustom[1] + "," + entry.scaleCustom[2] + "]";
            json += "},";
//=======
            //json += "\"scaleCustom\":[" + entry.scaleCustom[0] + "," + entry.scaleCustom[1] + "," + entry.scaleCustom[2] + "],";
                //json += "\"initialPosition\":[" + entry.position[0] + "," + entry.position[1] + "," + entry.position[2] + "],";
                //json += "\"initialRotation\":[" + entry.rotation[0] + "," + entry.rotation[1] + "," + entry.rotation[2] + "," + entry.rotation[3] + "]";
//                json += "},";
//>>>>>>> Stashed changes
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
                    manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dynamic.gameObject.name, dynamic.MeshName, dynamic.CustomId.ToString(),new float[3] { dynamic.transform.lossyScale.x, dynamic.transform.lossyScale.y, dynamic.transform.lossyScale.z }));
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