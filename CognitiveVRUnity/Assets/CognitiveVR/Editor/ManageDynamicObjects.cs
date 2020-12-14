using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

namespace CognitiveVR
{
public class ManageDynamicObjects : EditorWindow
{
    Rect steptitlerect = new Rect(30, 0, 100, 440);

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
        window.minSize = new Vector2(500, 550);
        window.maxSize = new Vector2(500, 550);
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

    private void OnGUI()
    {
        if (needsRefreshDevKey == true)
        {
            EditorCore.CheckForExpiredDeveloperKey(GetDevKeyResponse);
            needsRefreshDevKey = false;
        }
        GUI.skin = EditorCore.WizardGUISkin;

        GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

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

        GUI.Label(new Rect(30, 45, 440, 440), "These are the active <color=#8A9EB7FF>Dynamic Object components</color> currently found in your scene.", "boldlabel");

        //headers
        Rect mesh = new Rect(30, 95, 120, 30);
        GUI.Label(mesh, "Mesh Name", "dynamicheader");
        Rect gameobject = new Rect(190, 95, 120, 30);
        GUI.Label(gameobject, "GameObject", "dynamicheader");
        //Rect ids = new Rect(320, 95, 120, 30);
        //GUI.Label(ids, "Ids", "dynamicheader");
        Rect uploaded = new Rect(380, 95, 120, 30);
        GUI.Label(uploaded, "Exported Mesh", "dynamicheader");
        //IMPROVEMENT get list of uploaded mesh names from dashboard
        

        //content
        DynamicObject[] tempdynamics = GetDynamicObjects;
        
        if (tempdynamics.Length == 0)
        {
            GUI.Label(new Rect(30, 120, 420, 270), "No objects found.\n\nHave you attached any Dynamic Object components to objects?\n\nAre they active in your hierarchy?", "button_disabledtext");
        }

        DynamicObjectIdPool[] poolAssets = EditorCore.GetDynamicObjectPoolAssets;


        Rect innerScrollSize = new Rect(30, 0, 420, (tempdynamics.Length + poolAssets.Length) * 30);
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 285), dynamicScrollPosition, innerScrollSize, false, true);

        Rect dynamicrect;
        int GuiOffset = 0;
        for (; GuiOffset < tempdynamics.Length; GuiOffset++)
        {
            if (tempdynamics[GuiOffset] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, GuiOffset * 30, 460, 30);
            DrawDynamicObject(tempdynamics[GuiOffset], dynamicrect, GuiOffset % 2 == 0);
        }
        for (int i = 0; i < poolAssets.Length; i++)
        {
            //if (poolAssets[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, (i + GuiOffset) * 30, 460, 30);
            DrawDynamicObjectPool(poolAssets[i], dynamicrect, (i + GuiOffset) % 2 == 0);
        }
        GUI.EndScrollView();
        GUI.Box(new Rect(30, 120, 425, 285), "", "box_sharp_alpha");

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
        foreach(var v in Selection.gameObjects)
        {
            if (v.GetComponentInChildren<DynamicObject>())
                selectionCount++;
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
                foreach(var go in Selection.gameObjects)
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
    
    #region Dynamic Objects

    Vector2 dynamicScrollPosition;

    DynamicObject[] _cachedDynamics;
    DynamicObject[] GetDynamicObjects { get { if (_cachedDynamics == null || _cachedDynamics.Length == 0) { _cachedDynamics = FindObjectsOfType<DynamicObject>(); } return _cachedDynamics; } }

    private void OnFocus()
    {
        RefreshSceneDynamics();
        EditorCore._cachedPoolAssets = null;
    }
    
    void RefreshSceneDynamics()
    {
        _cachedDynamics = FindObjectsOfType<DynamicObject>();
        EditorCore.ExportedDynamicObjects = null;
    }

    //each row is 30 pixels
    void DrawDynamicObject(DynamicObject dynamic, Rect rect, bool darkbackground)
    {
        Event e = Event.current;
        if (e.isMouse && e.type == EventType.MouseDown)
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
        }

        if (darkbackground)
            GUI.Box(rect, "", "dynamicentry_even");
        else
            GUI.Box(rect, "", "dynamicentry_odd");

        //GUI.color = Color.white;

        Rect mesh = new Rect(rect.x + 10, rect.y, 120, rect.height);
        Rect gameobject = new Rect(rect.x + 160, rect.y, 120, rect.height);
        //Rect id = new Rect(rect.x + 290, rect.y, 120, rect.height);

        Rect collider = new Rect(rect.x + 320, rect.y, 24, rect.height);
        Rect uploaded = new Rect(rect.x + 380, rect.y, 24, rect.height);

        if (dynamic.UseCustomMesh)
            GUI.Label(mesh, dynamic.MeshName, "dynamiclabel");
        else
            GUI.Label(mesh, dynamic.CommonMesh.ToString(), "dynamiclabel");
        GUI.Label(gameobject, dynamic.gameObject.name, "dynamiclabel");
        
        if (!dynamic.HasCollider())
        {
            GUI.Label(collider, new GUIContent(EditorCore.Alert,"Tracking Gaze requires a collider"), "image_centered");
        }
        if (EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.MeshName) || !dynamic.UseCustomMesh)
        {
            GUI.Label(uploaded, EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(uploaded, EditorCore.EmptyCheckmark, "image_centered");
        }
    }

    #endregion

    #region DynamicObjectPool

    void DrawDynamicObjectPool(DynamicObjectIdPool dynamicpool, Rect rect, bool darkbackground)
    {
        Event e = Event.current;
        if (e.isMouse && e.type == EventType.MouseDown)
        {
            if (e.mousePosition.x < rect.x || e.mousePosition.x > rect.x+rect.width || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y+rect.height)
            {
                //not clicking on button
            }
            else
            {
                Selection.activeObject = dynamicpool;
            }
        }

        if (darkbackground)
            GUI.Box(rect, "", "dynamicentry_even");
        else
            GUI.Box(rect, "", "dynamicentry_odd");

        //GUI.color = Color.white;

        Rect mesh = new Rect(rect.x + 10, rect.y, 120, rect.height);
        Rect gameobject = new Rect(rect.x + 160, rect.y, 120, rect.height);
        //Rect id = new Rect(rect.x + 290, rect.y, 120, rect.height);

        //Rect collider = new Rect(rect.x + 320, rect.y, 24, rect.height);
        //Rect uploaded = new Rect(rect.x + 380, rect.y, 24, rect.height);

        GUI.Label(mesh, dynamicpool.MeshName, "dynamiclabel");
        GUI.Label(gameobject, "ID POOL (" + dynamicpool.Ids.Length + " ids)", "dynamiclabel");
    }

    #endregion
    
    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 500, 550, 50), EditorGUIUtility.whiteTexture);
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
            public float scale = 1;
            public AggregationManifestEntry(string _name, string _mesh, string _id, float _scale)
            {
                name = _name;
                mesh = _mesh;
                id = _id;
                scale = _scale;
            }
            public override string ToString()
            {
                return "{\"name\":\"" + name + "\",\"mesh\":\"" + mesh + "\",\"id\":\"" + id + "\",\"scale\":\"" + scale + "\"}";
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
            json += "\"scale\":" + entry.scale;
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
                    manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dynamic.gameObject.name, dynamic.MeshName, dynamic.CustomId.ToString(),dynamic.transform.lossyScale.x));
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