using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

public class ManageDynamicObjects : EditorWindow
{
    Rect steptitlerect = new Rect(30, 0, 100, 440);
    Rect boldlabelrect = new Rect(30, 100, 440, 440);

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

        if (currentscene != null)
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS "+ currentscene.SceneName + " Version: " + currentscene.VersionNumber, "steptitle");
            
        }
        else
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS Scene Not Saved", "steptitle");
        }

        GUI.Label(new Rect(30, 45, 440, 440), "These are the current <color=#8A9EB7FF>Dynamic Objects</color> tracked in your scene:", "boldlabel");

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
        Rect innerScrollSize = new Rect(30, 0, 420, tempdynamics.Length * 30); //TODO generate from the number of dynamic object lines there are
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 270), dynamicScrollPosition, innerScrollSize, false, true);

        Rect dynamicrect;
        for (int i = 0; i < tempdynamics.Length; i++)
        {
            if (tempdynamics[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, i * 30, 460, 30);
            DrawDynamicObject(tempdynamics[i], dynamicrect, i % 2 == 0,i%3==0, i % 5 == 0);
        }
        GUI.EndScrollView();
        GUI.Box(new Rect(30, 120, 425, 270), "", "box_sharp_alpha");

        //buttons
        if (GUI.Button(new Rect(180, 400, 140, 40), "Upload All", "button_bluetext"))
        {
            CognitiveVR_SceneExportWindow.ExportAllDynamicsInScene();
            CognitiveVR_SceneExportWindow.UploadAllDynamicObjects(true);
            //TODO pop up upload ids to scene modal
        }

        if (GUI.Button(new Rect(180, 400, 140, 40), "Upload Selected", "button_bluetext"))
        {
            CognitiveVR_SceneExportWindow.ExportAllDynamicsInScene();
            CognitiveVR_SceneExportWindow.UploadAllDynamicObjects(true);
            //TODO pop up upload ids to scene modal
        }

        //export and upload all

        /*if (GUI.Button(new Rect(180, 400, 140, 40), "Upload Selected", "button_bluetext"))
        {
            Debug.Log("upload all dynamics");
            CognitiveVR_SceneExportWindow.ExportAllDynamicsInScene();
            CognitiveVR_SceneExportWindow.UploadAllDynamicObjects(true);
            //TODO pop up upload ids to scene modal
        }*/

        if (GUI.Button(new Rect(30,400,140,40),"Upload Ids to Scene"))
        {
            EditorCore.RefreshSceneVersion(delegate() { UploadManifest(); }); //get latest scene version then upload manifest to there
        }

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
    }

    //each row is 30 pixels
    void DrawDynamicObject(DynamicObject dynamic, Rect rect, bool darkbackground, bool deleted, bool notuploaded)
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

        GUI.Label(mesh, dynamic.MeshName, "dynamiclabel");
        GUI.Label(gameobject, dynamic.gameObject.name, "dynamiclabel");
        //GUI.Label(id, dynamic.CustomId.ToString(), "dynamiclabel");
        if (!dynamic.HasCollider())
        {
            GUI.Label(collider, new GUIContent(EditorCore.Alert,"Tracking Gaze requires a collider"), "image_centered");
        }
        if (EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.MeshName))
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

        //DrawBackButton();

        //DrawNextButton();
    }

    //from manifest window. might have some duplicated logic



    void GetManifest()
    {
        var headers = new Dictionary<string, string>();
        headers.Add("X-HTTP-Method-Override", "GET");
        headers.Add("Authorization", EditorCore.DeveloperKey);

        var currentSceneSettings = EditorCore.GetPreferences().FindScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name);
        if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
        {
            return;
        }
        if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
        {
            Util.logWarning("Get Manifest current scene doesn't have an id!");
            return;
        }

        string url = Constants.GETDYNAMICMANIFEST(currentSceneSettings.VersionId);

        EditorNetwork.Get(url, GetManifestResponse, false);
    }


    void GetManifestResponse(int responsecode, string error, string text)
    {
        Util.logDebug("GetManifestResponse responseCode: " + responsecode);
        if (responsecode == 200)
        {
            //BuildManifest(getRequest.text);

            //also hit settings to get the current version of the scene
            GetSceneVersion();

        }
        else if (responsecode >= 500)
        {
            //some server error
            Util.logWarning("GetManifestResponse 500");
        }
        else if (responsecode >= 400)
        {
            if (responsecode == 401)
            {
                Util.logWarning("GetManifestResponse not authorized");
                //not authorized
            }
            else
            {
                Util.logWarning("GetManifestResponse retured code " + responsecode);
            }
        }
    }

    //send an http request to get the different versions of a scene
    void GetSceneVersion()
    {
        var headers = new Dictionary<string, string>();
        headers.Add("X-HTTP-Method-Override", "GET");
        headers.Add("Authorization", EditorCore.DeveloperKey);

        var currentSceneSettings = CognitiveVR_Preferences.FindCurrentScene();
        if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
        {
            return;
        }
        if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
        {
            Util.logWarning("Cannot Get Scene Version. Current scene doesn't have an id!");
            return;
        }

        string url = Constants.GETSCENEVERSIONS(currentSceneSettings.SceneId);

        EditorNetwork.Get(url, GetSceneSettingsResponse, false);
        Util.logDebug("GetSceneVersion request sent");
    }

    void GetSceneSettingsResponse(int responsecode, string error, string text)
    {
        Util.logDebug("GetSettingsResponse responseCode: " + responsecode);

        SceneVersionCollection = JsonUtility.FromJson<SceneVersionCollection>(text);

        var sv = SceneVersionCollection.GetLatestVersion();
        Util.logDebug(sv.versionNumber.ToString());
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
        public List<AggregationManifestEntry> objects;
        //public int Version;
        //public string SceneId;
    }

    //only need id, mesh and name
    static AggregationManifest Manifest;
    static SceneVersionCollection SceneVersionCollection;

    /// <summary>
    /// generate manifest from scene objects and upload to latest version of scene
    /// </summary>
    public static void UploadManifest()
    {
        ObjectsInScene.Clear();
        foreach (var v in GetDynamicObjectsInScene())
        {
            AddOrReplaceDynamic(Manifest, v);
        }
        var json = ManifestToJson();
        Util.logDebug(json);
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("UpdateManifest - could not write dynamics and manifest from empty json");
            return;
        }
        SendManifest(json, SceneVersionCollection.GetLatestVersion());
    }

    static string ManifestToJson()
    {
        string objectIdManifest = "{\"objects\":[";

        foreach (var entry in Manifest.objects)
        {
            objectIdManifest += "{";
            objectIdManifest += "\"id\":\"" + entry.id + "\",";
            objectIdManifest += "\"mesh\":\"" + entry.mesh + "\",";
            objectIdManifest += "\"name\":\"" + entry.name + "\"";
            objectIdManifest += "},";
        }

        objectIdManifest = objectIdManifest.Remove(objectIdManifest.Length - 1, 1);
        objectIdManifest += "]}";
        return objectIdManifest;
    }

    static void SendManifest(string json, SceneVersion sceneversion)
    {
        var settings = EditorCore.GetPreferences().FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
        if (settings == null)
        {
            Debug.LogWarning("Send Manifest settings are null " + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            string s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(s))
            {
                s = "Unknown Scene";
            }
            EditorUtility.DisplayDialog("Upload Failed", "Could not find the Scene Settings for \"" + s + "\". Are you sure you've saved, exported and uploaded this scene to SceneExplorer?", "Ok");
            return;
        }

        string url = Constants.POSTDYNAMICMANIFEST(settings.SceneId, sceneversion.versionNumber);
        Util.logDebug("Manifest Url: " + url);
        Util.logDebug("Manifest Contents: " + json);

        //upload manifest
        byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(json);

        var headers = new Dictionary<string, string>();
        headers.Add("Content-Type", "application/json");
        headers.Add("Authorization", EditorCore.DeveloperKey);

        EditorNetwork.Post(url, outBytes, PostManifestResponse,false);
    }

    static void PostManifestResponse(int responsecode, string error, string text)
    {
        Util.logDebug("Manifest upload complete. response: " + text + " error: " + error);
    }

    static void AddOrReplaceDynamic(AggregationManifest manifest, DynamicObject dynamic)
    {
        var replaceEntry = manifest.objects.Find(delegate (AggregationManifest.AggregationManifestEntry obj) { return obj.id == dynamic.CustomId.ToString(); });
        if (replaceEntry == null)
        {
            //don't include meshes with empty mesh names in manifest
            if (!string.IsNullOrEmpty(dynamic.MeshName))
            {
                manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dynamic.gameObject.name, dynamic.MeshName, dynamic.CustomId.ToString()));
            }
        }
        else
        {
            replaceEntry.mesh = dynamic.MeshName;
            replaceEntry.name = dynamic.gameObject.name;
        }
    }
}
