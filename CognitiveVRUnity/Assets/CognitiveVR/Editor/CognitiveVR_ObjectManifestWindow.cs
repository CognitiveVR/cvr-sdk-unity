using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CognitiveVR
{
    public class CognitiveVR_ObjectManifestWindow : EditorWindow
    {
        private static CognitiveVR_ObjectManifestWindow _instance;
        public static CognitiveVR_ObjectManifestWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetWindow<CognitiveVR_ObjectManifestWindow>(true, "cognitiveVR Object Manifest Upload");
                }
                return _instance;
            }
        }
        public static void Init()
        {
            //_instance = GetWindow<CognitiveVR_ObjectManifestWindow>(true, "cognitiveVR Object Manifest Upload");
            Vector2 size = new Vector2(300, 400);
            Instance.minSize = size;
            Instance.maxSize = size;
            Instance.Show();
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

        [System.Serializable]
        public class SceneSettings
        {
            public class ImageTypes
            {
                public bool large;
                public bool small;
            }
            public ImageTypes images;
            public int latestVersion;
        }

        //only need id, mesh and name
        AggregationManifest Manifest;
        int SceneVersion;

        WWW getRequest;
        
        void GetManifest()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("X-HTTP-Method-Override", "GET");
            headers.Add("Authorization", "Bearer " + CognitiveVR.CognitiveVR_Preferences.Instance.authToken);

            var currentSceneSettings = CognitiveVR_Preferences.FindCurrentScene();
            if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
            {
                currentState = "no scene settings!";
                return;
            }
            if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
            {
                currentState = "scene missing id!";
                Debug.LogWarning("current scene doesn't have an id!");
                return;
            }

            string url = "https://sceneexplorer.com/api/objects/" + currentSceneSettings.SceneId;

            getRequest = new WWW(url, new System.Text.UTF8Encoding(true).GetBytes("ignored"), headers);

            Util.logDebug("GetManifest request sent");

            EditorApplication.update += GetManifestResponse;
        }

        void GetAuthResponse(int responseCode)
        {
            CognitiveVR_Settings.AuthResponse -= GetAuthResponse;
            if (responseCode == 401)
            {
                currentState = "need to log in";
                //need to log in and refresh sessionid. this breaks the flow
            }
            else if (responseCode == 200)
            {
                GetManifest();
            }
            else
            {
                Debug.LogWarning("Get Manifest -> Get Auth Response retured code " + responseCode);
            }
        }

        void GetSceneVersion()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("X-HTTP-Method-Override", "GET");
            headers.Add("Authorization", "Bearer " + CognitiveVR.CognitiveVR_Preferences.Instance.authToken);

            var currentSceneSettings = CognitiveVR_Preferences.FindCurrentScene();
            if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
            {
                currentState = "no scene settings!";
                return;
            }
            if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
            {
                currentState = "scene missing id!";
                Debug.LogWarning("Cannot Get Scene Version. Current scene doesn't have an id!");
                return;
            }

            string url = "https://sceneexplorer.com/api/scenes/" + currentSceneSettings.SceneId+"/settings";

            getRequest = new WWW(url, new System.Text.UTF8Encoding(true).GetBytes("ignored"), headers);

            Util.logDebug("GetSceneVersion request sent");
            EditorApplication.update += GetSettingsResponse;
        }

        void GetSettingsResponse()
        {
            if (!getRequest.isDone) { return; }
            EditorApplication.update -= GetSettingsResponse;

            //this should only be called after manifest response gets a 200, so i'm assuming auth is fine

            var responsecode = Util.GetResponseCode(getRequest.responseHeaders);
            Util.logDebug("GetSettingsResponse responseCode: " + responsecode);

            // getRequest.text = {"images":{"large":false,"small":false},"latestVersion":1}
            var sceneSettings = JsonUtility.FromJson<SceneSettings>(getRequest.text);

            SceneVersion = sceneSettings.latestVersion;
            Util.logDebug("Scene Version " + SceneVersion);
        }

        void GetManifestResponse()
        {
            if (!getRequest.isDone) { return; }

            EditorApplication.update -= GetManifestResponse;
            var responsecode = Util.GetResponseCode(getRequest.responseHeaders);
            Util.logDebug("GetManifestResponse responseCode: " + responsecode);
            if (responsecode == 200)
            {
                BuildManifest(getRequest.text);

                //also hit settings to get the current version of the scene
                GetSceneVersion();
                
            }
            else if(responsecode >= 500)
            {
                //some server error
            }
            else if (responsecode >= 400)
            {
                if (responsecode == 401)
                {
                    //not authorized

                    Debug.LogWarning("GetManifestResponse not authorized. Requesting Auth Token");

                    var currentSceneSettings = CognitiveVR_Preferences.FindCurrentScene();
                    if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
                    {
                        return;
                    }
                    if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
                    {
                        Debug.LogWarning("Cannot Get Manifest Response. Current scene doesn't have an id!");
                        return;
                    }

                    string url = "https://sceneexplorer.com/api/tokens/" + currentSceneSettings.SceneId;

                    //request authorization
                    CognitiveVR_Settings.RequestAuthToken(url);
                    CognitiveVR_Settings.AuthResponse += GetAuthResponse;
                }
                else
                {
                    Debug.LogWarning("GetManifestResponse retured code " + responsecode);
                }
            }
        }

        void Refresh()
        {
            NewObjects = null;
            ObjectsInScene = null;
            DeletedObjects = null;
            ObjectsInManifest = null;
        }

        string currentState = "";

        bool gettingManifest = false;
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Get Manifest"))
            {
                Manifest = null;
                gettingManifest = false;
            }
            if (GUILayout.Button("Refresh Scene"))
            {
                Refresh();
            }
            GUILayout.EndHorizontal();

            if (Manifest == null)
            {
                if (!gettingManifest)
                {
                    currentState = "getting manifest";
                    GetManifest();
                    gettingManifest = true;
                }
                GUILayout.Label(currentState);
                //TODO if no scene settings, make a button to open scene export window
                return;
            }

            if (Manifest == null)
            {
                EditorGUILayout.LabelField("loading manifest");
                return;
            }

            if (GUILayout.Button("Print Manifest to Console"))
            {
                string manifest = "";
                foreach (var entry in Manifest.objects)
                {
                    manifest += entry.ToString() + "\n";
                }
                Debug.Log("Dynamic Object Manifest:\n"+manifest);
            }

            EditorStyles.label.wordWrap = true;
            EditorStyles.label.richText = true;

            int titleSize = 175;
            int contentSize = 125;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("New Dynamics Objects:", GUILayout.Width(titleSize));
            EditorGUILayout.LabelField("<color=green>+" + GetNewDynamicObjects().Count + "</color>", GUILayout.Width(contentSize));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Deleted Dynamics Objects:", GUILayout.Width(titleSize));
            EditorGUILayout.LabelField("<color=red>-" + GetDeletedObjects().Count + "</color>",GUILayout.Width(contentSize));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Dynamics Objects in scene:", GUILayout.Width(titleSize));
            EditorGUILayout.LabelField(GetDynamicObjectsInScene().Count.ToString(), GUILayout.Width(contentSize));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Dynamics Objects:", GUILayout.Width(titleSize));
            EditorGUILayout.LabelField((GetDynamicObjectsInScene().Count + GetDeletedObjects().Count).ToString(), GUILayout.Width(contentSize));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Set Custom Ids on Dynamic Objects in the scene. Objects with Custom Ids will be added to the Manifest. Objects in the Manifest are aggregated in SceneExplorer");
            if (GUILayout.Button("Set Custom Ids"))
            {
                SetUniqueObjectIds();
                Refresh();
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Updating a Manifest will add new Dynamic Objects and keep Dynamic Objects that have been deleted");

            if (GUILayout.Button("Update Existing Manifest"))
            {
                UpdateManifest();
                Refresh();
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("A new Manifest will contain only the current Dynamic Objects in the scene. Useful when you have made significant changes and don't want to aggregate deleted Dynamic Objects");

            if (GUILayout.Button("New Manifest"))
            {
                UploadNewManifest();
                Refresh();
            }
        }

        List<DynamicObject> ObjectsInScene;
        public List<DynamicObject> GetDynamicObjectsInScene()
        {
            if (ObjectsInScene == null)
            {
                ObjectsInScene = new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
            }
            return ObjectsInScene;
        }

        //in the scene but not in the manifest
        List<DynamicObject> NewObjects;
        public List<DynamicObject> GetNewDynamicObjects()
        {
            if (NewObjects == null)
            {
                NewObjects = new List<DynamicObject>();

                foreach (var dynamic in GameObject.FindObjectsOfType<DynamicObject>())
                {
                    var matchingEntry = Manifest.objects.Find(delegate (AggregationManifest.AggregationManifestEntry obj) { return obj.id == dynamic.CustomId.ToString(); });
                    if (matchingEntry == null)
                    {
                        NewObjects.Add(dynamic);
                    }
                }
            }
            return NewObjects;
        }

        List<AggregationManifest.AggregationManifestEntry> DeletedObjects;
        public List<AggregationManifest.AggregationManifestEntry> GetDeletedObjects()
        {
            if (DeletedObjects == null)
            {
                DeletedObjects = new List<AggregationManifest.AggregationManifestEntry>();

                List<DynamicObject> sceneObjects = new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());

                foreach (var entry in Manifest.objects)
                {
                    var foundEntry = sceneObjects.Find(delegate (DynamicObject obj) { return obj.CustomId.ToString() == entry.id; });
                    if (foundEntry == null)
                    {
                        DeletedObjects.Add(entry);
                    }
                }
            }
            return DeletedObjects;
        }

        List<AggregationManifest.AggregationManifestEntry> ObjectsInManifest;
        public List<AggregationManifest.AggregationManifestEntry> GetObjectsInManifest()
        {
            if (ObjectsInManifest == null)
            {
                ObjectsInManifest = new List<AggregationManifest.AggregationManifestEntry>(Manifest.objects);
            }
            return ObjectsInManifest;
        }
        
        void BuildManifest(string json)
        {
            Util.logDebug("Build Manifest from json: " + json);

            var allEntries = JsonUtil.GetJsonArray<AggregationManifest.AggregationManifestEntry>(json);

            Debug.Log("Number of Dynamic Objects in current Manifest: " + allEntries.Length);

            Manifest = new AggregationManifest();

            Manifest.objects = new List<AggregationManifest.AggregationManifestEntry>(allEntries);
            Repaint();
        }

        void AddOrReplaceDynamic(DynamicObject dynamic)
        {
            var replaceEntry = Manifest.objects.Find(delegate (AggregationManifest.AggregationManifestEntry obj) { return obj.id == dynamic.CustomId.ToString(); });
            if (replaceEntry == null)
            {
                Manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dynamic.gameObject.name, dynamic.MeshName, dynamic.CustomId.ToString()));
            }
            else
            {
                replaceEntry.mesh = dynamic.MeshName;
                replaceEntry.name = dynamic.gameObject.name;
            }
        }

        void UpdateManifest()
        {
            foreach (var v in GetDynamicObjectsInScene())
            {
                AddOrReplaceDynamic(v);
            }
            var json = ManifestToJson();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("could not write dynamics and manifest to json");
                return;
            }
            SendManifest(json, SceneVersion);
        }

        void UploadNewManifest()
        {
            Manifest.objects.Clear();

            foreach (var v in GetDynamicObjectsInScene())
            {
                AddOrReplaceDynamic(v);
            }
            var json = ManifestToJson();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("could not write dynamics and manifest to json");
                return;
            }
            SendManifest(json, SceneVersion + 1);
        }

        void SetUniqueObjectIds()
        {
            if (Instance == null)
            {
                Debug.LogWarning("instance is null");
                return;
            }
            if (Instance.Manifest == null)
            {
                Debug.LogWarning("instance Manifest is null");
                return;
            }
            if (Instance.Manifest.objects == null)
            {
                Debug.LogWarning("instance Manifest Objects is null");
                return;
            }

            foreach (var dynamic in GameObject.FindObjectsOfType<DynamicObject>())
            {
                if (!dynamic.UseCustomMesh)
                {
                    dynamic.MeshName = dynamic.CommonMesh.ToString().ToLower();
                }

                if (dynamic.CustomId == 0 || dynamic.UseCustomId == false)
                {
                    //set unique object ids should include looking through objectmanifest, not just other dynamics in the scene
                    var customId = GetUniqueIDEditor(dynamic.MeshName);
                    dynamic.CustomId = customId.Id;
                    dynamic.UseCustomId = true;
                    //set custom id
                }
            }
        }

        static int currentUniqueId = 0;
        public static DynamicObjectId GetUniqueIDEditor(string MeshName)
        {

            //in editor. probably writing manifest for aggregation. get all dynamic objects and add them to objectids
            foreach (var v in FindObjectsOfType<DynamicObject>())
            {
                if (v.UseCustomId)
                {
                    DynamicObject.ObjectIds.Add(new DynamicObjectId(v.CustomId, v.MeshName));
                }
            }

            DynamicObjectId usedObjectIdEditor = null;
            AggregationManifest.AggregationManifestEntry usedEntry = null;
            while (true)
            {
                //check each objectid. increment to next if id is found
                currentUniqueId++;

                usedObjectIdEditor = DynamicObject.ObjectIds.Find(delegate (DynamicObjectId obj)
                {
                    return obj.Id == currentUniqueId;
                });

                if (usedObjectIdEditor != null) { continue; }

                usedEntry = Instance.Manifest.objects.Find(delegate (AggregationManifest.AggregationManifestEntry obj)
                {
                    return obj.id == currentUniqueId.ToString();
                });

                if (usedObjectIdEditor == null && usedEntry == null)
                {
                    break;
                }
            }
            return new DynamicObjectId(currentUniqueId, MeshName);
        }

        string ManifestToJson()
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

        void SendManifest(string json,int version)
        {
            var settings = CognitiveVR_Preferences.Instance.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            if (settings == null)
            {
                Debug.LogWarning("settings are null " + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
                string s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(s))
                {
                    s = "Unknown Scene";
                }
                EditorUtility.DisplayDialog("Upload Failed", "Could not find the Scene Settings for \"" + s + "\". Are you sure you've saved, exported and uploaded this scene to SceneExplorer?", "Ok");
                return;
            }

            string url = "https://sceneexplorer.com/api/objects/" + settings.SceneId + "?version=" + version;
            Util.logDebug("Manifest Url: " + url);
            Util.logDebug("Manifest Contents: " + json);

            //upload manifest
            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(json);

            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");

            manifestRequest = new WWW(url, outBytes, headers);

            EditorApplication.update += ManifestResposne;
        }
        
        WWW manifestRequest;

        void ManifestResposne()
        {
            if (!manifestRequest.isDone) { return; }
            EditorApplication.update -= ManifestResposne;
            Debug.Log("Manifest upload complete. response: " + manifestRequest.text + " error: " + manifestRequest.error);
        }
    }
}