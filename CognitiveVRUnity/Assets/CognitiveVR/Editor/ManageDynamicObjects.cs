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

    public static void Init()
    {
        ManageDynamicObjects window = (ManageDynamicObjects)EditorWindow.GetWindow(typeof(ManageDynamicObjects), true, "");
        window.minSize = new Vector2(500, 500);
        window.maxSize = new Vector2(500, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUI.skin = EditorCore.WizardGUISkin;

        GUI.DrawTexture(new Rect(0, 0, 500, 500), EditorGUIUtility.whiteTexture);

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
        GUI.Label(mesh, "Dynamic Mesh Name", "dynamicheader");
        Rect gameobject = new Rect(190, 95, 120, 30);
        GUI.Label(gameobject, "GameObject", "dynamicheader");
        //Rect ids = new Rect(320, 95, 120, 30);
        //GUI.Label(ids, "Ids", "dynamicheader");
        Rect uploaded = new Rect(380, 95, 120, 30);
        GUI.Label(uploaded, "Uploaded", "dynamicheader");
        

        //content
        DynamicObject[] tempdynamics = GetDynamicObjects;

        if (tempdynamics.Length == 0)
        {
            GUI.Label(new Rect(30, 120, 420, 270), "No objects found.\n\nHave you attached any Dynamic Object components to objects?\n\nAre they active in your hierarchy?", "button_disabledtext");
        }

        Rect innerScrollSize = new Rect(30, 0, 420, tempdynamics.Length * 30);
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 285), dynamicScrollPosition, innerScrollSize, false, true);

        Rect dynamicrect;
        for (int i = 0; i < tempdynamics.Length; i++)
        {
            if (tempdynamics[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, i * 30, 460, 30);
            DrawDynamicObject(tempdynamics[i], dynamicrect, i % 2 == 0);
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


        EditorGUI.BeginDisabledGroup(currentscene == null || string.IsNullOrEmpty(currentscene.SceneId));
        if (GUI.Button(new Rect(20, 415, 260, 30), new GUIContent("Upload " + selectionCount + " Selected Meshes", "Export and Upload to " + scenename + " version " + versionnumber), "button_bluetext"))
        {
            //dowhattever thing get scene version
            EditorCore.RefreshSceneVersion(() =>
            {
                if (CognitiveVR_SceneExportWindow.ExportSelectedObjectsPrefab())
                {
                    EditorCore.RefreshSceneVersion(delegate () { ManageDynamicObjects.UploadManifest(() => CognitiveVR_SceneExportWindow.UploadSelectedDynamicObjects(true)); });
                }
            });
        }

        if (GUI.Button(new Rect(260, 415, 260, 30), new GUIContent("Upload All Meshes","Export and Upload to "+ scenename + " version " + versionnumber),"button_bluetext"))
        {
            EditorCore.RefreshSceneVersion(() =>
            {
                if (CognitiveVR_SceneExportWindow.ExportAllDynamicsInScene())
                {
                    EditorCore.RefreshSceneVersion(delegate () { ManageDynamicObjects.UploadManifest(() => CognitiveVR_SceneExportWindow.UploadAllDynamicObjects(true)); });
                }
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
        if (e.isMouse && e.type == EventType.mouseDown)
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
    
    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 450, 500, 50), EditorGUIUtility.whiteTexture);
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

        bool enabled = !(currentScene == null || string.IsNullOrEmpty(currentScene.SceneId));

        //EditorGUI.BeginDisabledGroup(enabled);
        //EditorGUI.EndDisabledGroup();
        if (enabled)
        {
            if (GUI.Button(new Rect(80, 460, 350, 30), new GUIContent("Upload Ids to SceneExplorer for Aggregation", tooltip)))
            {
                EditorCore.RefreshSceneVersion(delegate () { ManageDynamicObjects.UploadManifest(null); });
            }
        }
        else
            GUI.Button(new Rect(80, 460, 350, 30), new GUIContent("Upload Ids to SceneExplorer for Aggregation", tooltip), "button_disabled");
    }

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

        string url = Constants.GETDYNAMICMANIFEST(currentSceneSettings.VersionId);

        Dictionary<string, string> headers = new Dictionary<string, string>();
        if (EditorCore.IsDeveloperKeyValid)
            headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
        EditorNetwork.Get(url, GetManifestResponse, headers,false);//AUTH
    }


    void GetManifestResponse(int responsecode, string error, string text)
    {
        if (responsecode == 200)
        {
            //BuildManifest(getRequest.text);
            var allEntries = JsonUtil.GetJsonArray<AggregationManifest.AggregationManifestEntry>(text);

            Debug.Log("Number of Dynamic Objects in current Manifest: " + allEntries.Length);

            Manifest = new AggregationManifest();

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

    static List<DynamicObject> ObjectsInScene;
    public static List<DynamicObject> GetDynamicObjectsInScene()
    {
        if (ObjectsInScene == null)
        {
            ObjectsInScene = new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
        }
        return ObjectsInScene;
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
            public AggregationManifestEntry(string _name, string _mesh, string _id)
            {
                name = _name;
                mesh = _mesh;
                id = _id;
            }
            public override string ToString()
            {
                return "{\"name\":\"" + name + "\",\"mesh\":\"" + mesh + "\",\"id\":\"" + id + "\"}";
            }
        }
        public List<AggregationManifestEntry> objects = new List<AggregationManifestEntry>();
        //public int Version;
        //public string SceneId;
    }

    //only need id, mesh and name
    static AggregationManifest Manifest;
    //static SceneVersionCollection SceneVersionCollection;

    /// <summary>
    /// generate manifest from scene objects and upload to latest version of scene. should be done only after EditorCore.RefreshSceneVersion
    /// </summary>
    public static void UploadManifest(System.Action callback, System.Action nodynamicscallback = null)
    {
        if (Manifest == null) { Manifest = new AggregationManifest(); }
        //if (SceneVersionCollection == null) { Debug.LogError("SceneVersionCollection is null! Make sure RefreshSceneVersion was called before this"); return; }

        ObjectsInScene = null;

        AddOrReplaceDynamic(Manifest, GetDynamicObjectsInScene());

        if (Manifest.objects.Count == 0)
        {
            Debug.LogWarning("Aggregation Manifest has nothing in list!");
            if (nodynamicscallback != null)
            {
                nodynamicscallback.Invoke();
            }
            return;
        }

        string json = "";
        if (ManifestToJson(out json))
        {
            var currentSettings = CognitiveVR_Preferences.FindCurrentScene();
            if (currentSettings != null && currentSettings.VersionNumber > 0)
                SendManifest(json, currentSettings.VersionNumber, callback);
            else
                Util.logError("Could not find scene version for current scene");
        }
    }

    static bool ManifestToJson(out string json)
    {
        json = "{\"objects\":[";

        List<string> usedIds = new List<string>();

        bool containsValidEntry = false;
        foreach (var entry in Manifest.objects)
        {
            if (string.IsNullOrEmpty(entry.mesh)) { Debug.LogWarning(entry.name + " missing meshname"); continue; }
            if (string.IsNullOrEmpty(entry.id)) { Debug.LogWarning(entry.name + " has empty dynamic id. This will not be aggregated"); continue; }
            if (usedIds.Contains(entry.id)) { Debug.LogWarning(entry.name + " using id that already exists in the scene. This may not be aggregated correctly"); }
            usedIds.Add(entry.id);
            json += "{";
            json += "\"id\":\"" + entry.id + "\",";
            json += "\"mesh\":\"" + entry.mesh + "\",";
            json += "\"name\":\"" + entry.name + "\"";
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

        string url = Constants.POSTDYNAMICMANIFEST(settings.SceneId, versionNumber);
        Util.logDebug("Send Manifest Contents: " + json);

        //upload manifest
        Dictionary<string, string> headers = new Dictionary<string, string>();
        if (EditorCore.IsDeveloperKeyValid)
        {
            headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            headers.Add("Content-Type","application/json");
        }
        PostManifestResponseAction = callback;
        EditorNetwork.Post(url, json, PostManifestResponse,headers,false);//AUTH
    }

    static void PostManifestResponse(int responsecode, string error, string text)
    {
        Util.logDebug("Manifest upload complete. response: " + text + " error: " + error);
        if (PostManifestResponseAction != null)
        {
            PostManifestResponseAction.Invoke();
            PostManifestResponseAction = null;
        }
    }

    static void AddOrReplaceDynamic(AggregationManifest manifest, List<DynamicObject> scenedynamics)
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
                    manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dynamic.gameObject.name, dynamic.MeshName, dynamic.CustomId.ToString()));
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