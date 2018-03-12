using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

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

        //only need id, mesh and name
        AggregationManifest Manifest;
        //int SceneVersion;
        SceneVersionCollection SceneVersionCollection;

        WWW getRequest;

        void GetManifest()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("X-HTTP-Method-Override", "GET");
            headers.Add("Authorization", "Bearer " + EditorPrefs.GetString("authToken"));

            var currentSceneSettings = CognitiveVR_Settings.GetPreferences().FindScene(EditorSceneManager.GetActiveScene().name);
            if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
            {
                currentState = "no scene settings!";
                return;
            }
            if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
            {
                currentState = "scene missing id!";
                Util.logWarning("Get Manifest current scene doesn't have an id!");
                return;
            }

            string url = Constants.GETDYNAMICMANIFEST(currentSceneSettings.VersionId);

            getRequest = new WWW(url, null, headers);

            Util.logDebug("GetManifest request sent to " + url);

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
                Util.logWarning("Get Manifest -> Get Auth Response retured code " + responseCode);
            }
        }

        void GetSceneVersion()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("X-HTTP-Method-Override", "GET");
            headers.Add("Authorization", "Bearer " + EditorPrefs.GetString("authToken"));

            var currentSceneSettings = CognitiveVR_Settings.GetPreferences().FindScene(EditorSceneManager.GetActiveScene().name);
            if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
            {
                currentState = "no scene settings!";
                return;
            }
            if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
            {
                currentState = "scene missing id!";
                Util.logWarning("Cannot Get Scene Version. Current scene doesn't have an id!");
                return;
            }

            string url = Constants.GETSCENEVERSIONS(currentSceneSettings.SceneId);

            getRequest = new WWW(url, null, headers);

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

            SceneVersionCollection = JsonUtility.FromJson<SceneVersionCollection>(getRequest.text);

            var sv = SceneVersionCollection.GetLatestVersion();
            Util.logDebug(sv.versionNumber.ToString());
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
            else if (responsecode >= 500)
            {
                //some server error
            }
            else if (responsecode >= 400)
            {
                if (responsecode == 401)
                {
                    //not authorized

                    Util.logWarning("GetManifestResponse not authorized. Requesting Auth Token");

                    var currentSceneSettings = CognitiveVR_Settings.GetPreferences().FindScene(EditorSceneManager.GetActiveScene().name);
                    if (currentSceneSettings == null) //there's a warning in CognitiveVR_Preferences.FindCurrentScene if null
                    {
                        return;
                    }
                    if (string.IsNullOrEmpty(currentSceneSettings.SceneId))
                    {
                        Util.logWarning("Cannot Get Manifest Response. Current scene doesn't have an id!");
                        return;
                    }

                    string url = Constants.POSTAUTHTOKEN(currentSceneSettings.SceneId);

                    //request authorization
                    CognitiveVR_Settings.RequestAuthToken(url);
                    CognitiveVR_Settings.AuthResponse += GetAuthResponse;
                }
                else
                {
                    Util.logWarning("GetManifestResponse retured code " + responsecode);
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

            if (string.IsNullOrEmpty(EditorCore.DeveloperKey))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No Customer ID.\nDid you log in?");
                if (GUILayout.Button("Open\nAccount Settings", GUILayout.Width(120)))
                {
                    CognitiveVR_Settings.Init();
                }
                GUILayout.EndHorizontal();
            }

            if (CognitiveVR_Settings.GetPreferences().sceneSettings.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No scene settings.\nDid you export this scene?");
                if (GUILayout.Button("Open Scene Export\nWindow",GUILayout.Width(120)))
                {
                    CognitiveVR_SceneExportWindow.Init();
                }
                GUILayout.EndHorizontal();
                return;
            }

            var currentSettings = CognitiveVR_Settings.GetPreferences().FindSceneByPath(EditorSceneManager.GetActiveScene().path);
            if (currentSettings == null || string.IsNullOrEmpty(currentSettings.SceneId))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No SceneId.\nDid you export this scene?");
                if (GUILayout.Button("Open Scene Export\nWindow", GUILayout.Width(120)))
                {
                    CognitiveVR_SceneExportWindow.Init();
                }
                GUILayout.EndHorizontal();
                return;
            }

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

            if (SceneVersionCollection == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No saved version for scene.\nDid you upload this scene?");
                if (GUILayout.Button("Scene Export", GUILayout.Width(120)))
                {
                    CognitiveVR_SceneExportWindow.Init();
                }
                //TODO if manifest is also null, display warning that scene probably hasn't been uploaded!
                GUILayout.EndHorizontal();
                return;
            }

            if (GUILayout.Button("Print Manifest to Console"))
            {
                string manifest = "";
                foreach (var entry in Manifest.objects)
                {
                    manifest += entry.ToString() + "\n";
                }
                Util.logDebug("Dynamic Object Manifest:\n" + manifest);
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
            EditorGUILayout.LabelField("<color=red>-" + GetDeletedObjects().Count + "</color>", GUILayout.Width(contentSize));
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
            /*
            GUILayout.Space(10);

            EditorGUILayout.LabelField("A new Manifest will contain only the current Dynamic Objects in the scene. Useful when you have made significant changes and don't want to aggregate deleted Dynamic Objects");

            if (GUILayout.Button("New Manifest"))
            {
                UploadNewManifest();
                Refresh();
            }*/
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
            Util.logDebug("Build Manifest from existing scene explorer data: " + json);

            var allEntries = JsonUtil.GetJsonArray<AggregationManifest.AggregationManifestEntry>(json);

            Debug.Log("Number of Dynamic Objects in current Manifest: " + allEntries.Length);

            Manifest = new AggregationManifest();

            Manifest.objects = new List<AggregationManifest.AggregationManifestEntry>(allEntries);
            Repaint();
        }

        void AddOrReplaceDynamic(AggregationManifest manifest, DynamicObject dynamic)
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

        void UpdateManifest()
        {
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

        void SetUniqueObjectIds()
        {
            if (Instance == null)
            {
                Util.logDebug("Object Manifest Window is null");
                return;
            }
            if (Instance.Manifest == null)
            {
                Util.logDebug("Object Manifest Window Manifest is null");
                return;
            }
            if (Instance.Manifest.objects == null)
            {
                Util.logDebug("Object Manifest Window Manifest Objects is null");
                return;
            }

            var allDynamics = GameObject.FindObjectsOfType<DynamicObject>();
            List<int> usedIds = new List<int>();
            List<DynamicObject> unassignedDynamics = new List<DynamicObject>();
            //add used dynamic ids
            foreach (var dyn in allDynamics)
            {
                if (usedIds.Contains(dyn.CustomId))
                {
                    unassignedDynamics.Add(dyn);
                }
                else
                {
                    usedIds.Add(dyn.CustomId);
                    dyn.UseCustomId = true;
                }
            }

            int currentUniqueId = 1;
            int changedIds = 0;
            //assign all duplicated/unassigned
            foreach (var dyn in unassignedDynamics)
            {
                for (; currentUniqueId < 1000; currentUniqueId++)
                {
                    if (usedIds.Contains(currentUniqueId))
                    {
                        continue;
                    }
                    else
                    {
                        usedIds.Add(currentUniqueId);
                        dyn.CustomId = currentUniqueId;
                        EditorUtility.SetDirty(dyn);
                        changedIds++;
                        dyn.UseCustomId = true;
                        break;
                    }
                }
            }

            if (changedIds > 0)
            {
                //mark stuff + scene dirty
                Debug.Log("Set Unique Ids changed " + changedIds + " new object ids");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
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

        void SendManifest(string json, SceneVersion sceneversion)
        {
            var settings = CognitiveVR_Settings.GetPreferences().FindSceneByPath(EditorSceneManager.GetActiveScene().path);
            if (settings == null)
            {
                Debug.LogWarning("Send Manifest settings are null " + EditorSceneManager.GetActiveScene().path);
                string s = EditorSceneManager.GetActiveScene().name;
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

            manifestRequest = new WWW(url, outBytes, headers);

            EditorApplication.update += ManifestResposne;
        }

        WWW manifestRequest;

        void ManifestResposne()
        {
            if (!manifestRequest.isDone) { return; }
            EditorApplication.update -= ManifestResposne;
            Util.logDebug("Manifest upload complete. response: " + manifestRequest.text + " error: " + manifestRequest.error);
        }
    }
}