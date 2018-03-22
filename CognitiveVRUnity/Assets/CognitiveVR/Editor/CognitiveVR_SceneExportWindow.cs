using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;

//an interface for exporting/decimating and uploading scenes and dynamic objects

namespace CognitiveVR
{
    //returned from get scene version. contains info about all versions of a single scene
    public class SceneVersionCollection
    {
        public long createdAt;
        public long updatedAt;
        public string id;
        public List<SceneVersion> versions = new List<SceneVersion>();
        public string customerId;
        public string sceneName;
        public bool isPublic;

        public SceneVersion GetLatestVersion()
        {
            int latest = 0;
            SceneVersion latestscene = null;
            if (versions == null) { Debug.LogError("SceneVersionCollection versions is null!"); return null; }
            for (int i = 0; i<versions.Count;i++)
            {
                if (versions[i].versionNumber > latest)
                {
                    latest = versions[i].versionNumber;
                    latestscene = versions[i];
                }
            }
            return latestscene;
        }

        public SceneVersion GetVersion(int versionnumber)
        {
            var sceneversion = versions.Find(delegate (SceneVersion obj) { return obj.versionNumber == versionnumber; });
            return sceneversion;
        }
    }

    //a specific version of a scene
    [System.Serializable]
    public class SceneVersion
    {
        public long createdAt;
        public long updatedAt;
        public int id;
        public int versionNumber;
        public float scale;
        public string sdkVersion;
        public int sessionCount;
    }

    public class CognitiveVR_SceneExportWindow : EditorWindow
    {
        /*static Vector2 canvasPos;
        static CognitiveVR_Preferences prefs;

        bool exportOptionsFoldout = false;
        static CognitiveVR_Preferences.SceneSettings currentSceneSettings;

        string searchString = "";

        int sceneWidth = 140;
        int keyWidth = 400;


        bool loadedScenes = false;*/

        List<string> AddAllScenes()
        {
            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");
            List<string> allSceneNames = new List<string>();

            //sceneNames.Clear();

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);
                allSceneNames.Add(name);
                //sceneNames.Add(name);
            }
            return allSceneNames;
        }

        #region Screenshot

        List<Camera> tempDisabledCameras = new List<Camera>();

        Texture2D cachedScreenshot;

        bool LoadScreenshot(string sceneName, out Texture2D returnTexture)
        {
            if (cachedScreenshot)
            {
                returnTexture = cachedScreenshot;
                return true;
            }
            //if file exists
            if (Directory.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot"))
            {
                if (File.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png"))
                {
                    //load texture from file
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(File.ReadAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png"));
                    returnTexture = tex;
                    cachedScreenshot = returnTexture;
                    return true;
                }
            }
            returnTexture = null;
            return false;
        }

        bool HasSavedScreenshot(string sceneName)
        {
            if (!Directory.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot")) { return false; }
            if (!File.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png")) { return false; }
            return true;
        }

        public void SaveScreenshot(string sceneName, Texture2D tex)
        {
            //create directory
            Directory.CreateDirectory("CognitiveVR_SceneExplorerExport");
            Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName);
            Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot");

            //save file
            File.WriteAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png", tex.EncodeToPNG());
        }

        static void UploadScreenshot(CognitiveVR_Preferences.SceneSettings settings)
        {
            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + settings.SceneName + Path.DirectorySeparatorChar;
            string[] screenshotPath = new string[0];
            if (Directory.Exists(sceneExportDirectory + "screenshot"))
            {
                screenshotPath = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + settings.SceneName + Path.DirectorySeparatorChar + "screenshot");
            }
            else
            {
                Debug.Log("SceneExportWindow Upload can't find directory to screenshot");
                return;
            }

            if (screenshotPath.Length == 0)
            {
                Debug.Log("SceneExportWindow can't load data from screenshot directory");
                return;
            }

            string url = Constants.POSTSCREENSHOT(settings.SceneId, settings.VersionNumber);

            Debug.Log("SceneExportWIndow upload screenshot to " + url);

            WWWForm wwwForm = new WWWForm();
            wwwForm.AddBinaryData("screenshot", File.ReadAllBytes(screenshotPath[0]), "screenshot.png");
            new WWW(url, wwwForm);
        }

        #endregion


        //returns true if savedblenderpath ends with blender.exe/app
        static bool IsBlenderPathValid()
        {
            if (string.IsNullOrEmpty(EditorCore.BlenderPath)) { return false; }
#if UNITY_EDITOR_WIN
            return EditorCore.BlenderPath.ToLower().EndsWith("blender.exe");
#elif UNITY_EDITOR_OSX
            return CognitiveVR_Settings.GetPreferences().SavedBlenderPath.ToLower().EndsWith("blender.app");
#else
            return false;
#endif
        }

        private void Update()
        {
            Repaint();
        }



        /*void DisplaySceneSettings(CognitiveVR_Preferences.SceneSettings settings)
        {
            bool isCurrentScene = false;
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == settings.ScenePath)
            {
                isCurrentScene = true;
            }

            if (isCurrentScene)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.color = new Color(0, 10, 0,1);

                    GUIStyle gs = new GUIStyle(EditorStyles.textField);
                    GUI.Box(selectedRect, "",gs);
                }
                else
                {
                    GUI.color = CognitiveVR_Settings.GreenButton;
                    GUI.Box(selectedRect, "");
                }
            }
            GUI.color = Color.white;
            

            GUILayout.BeginHorizontal();

            GUILayout.Label(settings.SceneName, GUILayout.Width(sceneWidth));

            DateTime dt = DateTime.FromBinary(settings.LastRevision);
            if (dt.Year < 1000)
            {
                GUILayout.Label("Never", GUILayout.Width(sceneWidth));
            }
            else
            {
                string dtString = dt.ToShortDateString();
                GUILayout.Label(dtString, GUILayout.Width(sceneWidth));
            }

            EditorGUI.BeginDisabledGroup(!KeyIsValid(settings.SceneId));
            GUIContent sceneExplorerLink = new GUIContent("View in Web Browser");
            if (KeyIsValid(settings.SceneId))
            {
                sceneExplorerLink.tooltip = Constants.SCENEEXPLORER_SCENE + settings.SceneId;
            }

            if (GUILayout.Button(sceneExplorerLink))
            {
                Application.OpenURL(Constants.SCENEEXPLORER_SCENE + settings.SceneId);
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Load Scene"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(settings.ScenePath);
                Repaint();
            }
            GUILayout.EndHorizontal();

            if (isCurrentScene)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    selectedRect = GUILayoutUtility.GetLastRect();
                    selectedRect.x -= 2;
                    selectedRect.y -= 1;
                    selectedRect.width += 4;
                    selectedRect.height += 4;

                    //if (!takeScreenshot)
                        //Repaint();
                }
            }
        }

        Rect selectedRect;*/

        #region Export Scene

        public static void ExportScene(bool includeTextures, bool staticGeometry, float minSize, int textureDivisor, string developerkey,string texturename)
        {
            if (blenderProcess != null)
            {
                Debug.LogError("Currently decimating a scene. Please wait until this is finished!");
                return;
            }

            if (UploadSceneSettings != null)
            {
                Debug.LogError("Currently uploading a scene. Please wait until this is finished!");
                return;
            }

            string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;

            //export scene from unity
            bool successfulExport = CognitiveVR_SceneExplorerExporter.ExportScene(fullName, includeTextures,staticGeometry,minSize,textureDivisor, texturename);

            if (!successfulExport)
            {
                Debug.LogError("Scene export canceled!");
                return;
            }


            //begin scene decimation
            if (!EditorCore.IsBlenderPathValid)
            {
                Debug.LogError("Blender is not found during scene export! Use Edit>Preferences...CognitivePreferences to locate Blender\nScene: "+ fullName+" exported to folder but not mesh decimated!");
                //return;
            }

            string objPath = CognitiveVR_SceneExplorerExporter.GetDirectory(fullName);
            string decimateScriptPath = Application.dataPath + "/CognitiveVR/Editor/decimateall.py";

            //write json settings file
            string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\""+ fullName + "\",\"sdkVersion\":\"" + Core.SDK_VERSION + "\"}";
            File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

            decimateScriptPath = decimateScriptPath.Replace(" ", "\" \"");
            objPath = objPath.Replace(" ", "\" \"");
            fullName = fullName.Replace(" ", "\" \"");

            EditorUtility.ClearProgressBar();

            //use case for empty 360 video scenes
            if (string.IsNullOrEmpty(EditorCore.BlenderPath))
            {
                Debug.LogError("Blender is not found during scene export! Scene is not being decimated");
            }

            ProcessStartInfo processInfo;
#if UNITY_EDITOR_WIN
            processInfo = new ProcessStartInfo(EditorCore.BlenderPath);
            processInfo.UseShellExecute = true;
            processInfo.Arguments = "-P " + decimateScriptPath + " " + objPath + " " + EditorCore.ExportSettings.ExplorerMinimumFaceCount + " " + EditorCore.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;

#elif UNITY_EDITOR_OSX
            processInfo = new ProcessStartInfo("open");
            processInfo.Arguments = prefs.SavedBlenderPath + " --args -P " + decimateScriptPath + " " + objPath + " " + prefs.ExportSettings.ExplorerMinimumFaceCount + " " + prefs.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;
            processInfo.UseShellExecute = false;
#endif

            //changing scene while blender is decimating the level will break the file that will be automatically uploaded
            blenderProcess = Process.Start(processInfo);
            BlenderRequest = true;
            HasOpenedBlender = false;
            EditorApplication.update += UpdateProcess;
            UploadSceneSettings = CognitiveVR_Preferences.FindCurrentScene();


            blenderStartTime = (float)EditorApplication.timeSinceStartup;
        }

        static float blenderStartTime = 0;
        static float currentBlenderTime = 0;
        static float maxBlenderTime = 240;

        static bool BlenderRequest;
        static bool HasOpenedBlender;
        static Process blenderProcess;

        static void UpdateProcess()
        {
            currentBlenderTime = (float)(EditorApplication.timeSinceStartup - blenderStartTime);
            if (EditorUtility.DisplayCancelableProgressBar("Blender Decimate", "Reducing the polygons and scene complexity using Blender", currentBlenderTime / maxBlenderTime))
            {
                Debug.Log("Cancel Blender process");
                blenderProcess.Kill();
                blenderProcess.Close();
                EditorApplication.update -= UpdateProcess;
                HasOpenedBlender = false;
                blenderProcess = null;
                EditorUtility.ClearProgressBar();
                UploadSceneSettings = null;
                return;
            }

            //could probably clean up some of this
            Process[] blenders;
            if (BlenderRequest == true)
            {
                //Debug.Log("BLENDER - opening");
                blenders = Process.GetProcessesByName("blender");
                if (blenders.Length > 0)
                {
                    BlenderRequest = false;
                    HasOpenedBlender = true;
                }
            }
            if (HasOpenedBlender)
            {
                blenders = Process.GetProcessesByName("blender");
                if (blenderProcess.HasExited)
                {
                    //Debug.Log("BLENDER - finished work");
                    EditorApplication.update -= UpdateProcess;
                    HasOpenedBlender = false;

                    if (blenderProcess.ExitCode != 0)
                    {
                        blenderProcess = null;
                        EditorUtility.ClearProgressBar();
                        UploadSceneSettings = null;
                        return;
                    }

                    blenderProcess = null;
                    
                    EditorUtility.ClearProgressBar();
                    UploadSceneSettings = null;

                    //var blenderSceneSettings = EditorCore.GetPreferences().FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
                    //UploadDecimatedScene(blenderSceneSettings);
                }
            }
        }

        #endregion

        #region Upload Scene
        //displays popup window confirming upload, then uploads the files
        static System.Action UploadComplete;
        public static void UploadDecimatedScene(CognitiveVR_Preferences.SceneSettings settings, System.Action uploadComplete)
        {
            //if uploadNewScene POST
            //else PUT to sceneexplorer/sceneid

            if (settings == null) { UploadSceneSettings = null; return; }

            UploadSceneSettings = settings;

            bool hasExistingSceneId = settings != null && !string.IsNullOrEmpty(settings.SceneId);

            bool uploadConfirmed = false;
            string sceneName = settings.SceneName;
            string[] filePaths = new string[] { };

            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + settings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);

            if (SceneExportDirExists)
            {
                filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar);
            }

            //custom confirm upload popup windows
            if ((!SceneExportDirExists || filePaths.Length <= 1))
            {
                if (EditorUtility.DisplayDialog("Upload Scene", "Scene " + settings.SceneName + " has no exported geometry. Upload anyway?", "Yes", "No"))
                {
                    uploadConfirmed = true;
                    //create a json.settings file in the directory
                    string objPath = CognitiveVR_SceneExplorerExporter.GetDirectory(sceneName);

                    Directory.CreateDirectory(objPath);

                    string jsonSettingsContents = "{ \"scale\":1, \"sceneName\":\"" + settings.SceneName + "\",\"sdkVersion\":\"" + Core.SDK_VERSION + "\"}";
                    File.WriteAllText(objPath + "settings.json", jsonSettingsContents);
                }
            }
            else
            {
                uploadConfirmed = true;
                /*if (EditorUtility.DisplayDialog("Upload Scene", "Do you want to upload \"" + settings.SceneName + "\" to your Dashboard?", "Yes", "No"))
                {
                    
                }*/
            }

            if (!uploadConfirmed)
            {
                UploadSceneSettings = null;
                return;
                //just exit now
            }

            //after confirmation because uploading an empty scene creates a settings.json file
            if (Directory.Exists(sceneExportDirectory))
            {
                filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar);
            }

            string[] screenshotPath = new string[0];
            if (Directory.Exists(sceneExportDirectory + "screenshot"))
            {
                screenshotPath = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot");
            }
            else
            {
                Debug.Log("SceneExportWindow Upload can't find directory to screenshot");
            }

            string fileList = "Upload Files:\n";

            string mtlFilepath = "";
            string objFilepath = "";

            WWWForm wwwForm = new WWWForm();
            foreach (var f in filePaths)
            {
                if (f.ToLower().EndsWith(".ds_store"))
                {
                    Debug.Log("skip file " + f);
                    continue;
                }

                //set obj file. prefer decimated
                if (f.EndsWith(".obj"))
                {
                    if (f.EndsWith("_decimated.obj"))
                    {
                        objFilepath = f;
                    }
                    else if (string.IsNullOrEmpty(objFilepath))
                    {
                        objFilepath = f;
                    }
                    continue;
                }

                //set mtl file. prefer decimated
                if (f.EndsWith(".mtl"))
                {
                    if (f.EndsWith("_decimated.mtl"))
                    {
                        mtlFilepath = f;
                    }
                    else if (string.IsNullOrEmpty(mtlFilepath))
                    {
                        mtlFilepath = f;
                    }
                    continue;
                }

                fileList += f + "\n";

                var data = File.ReadAllBytes(f);
                wwwForm.AddBinaryData("file", data, Path.GetFileName(f));
            }

            if (!string.IsNullOrEmpty(objFilepath))
            {
                //add obj and mtl files
                wwwForm.AddBinaryData("file", File.ReadAllBytes(objFilepath), Path.GetFileName(objFilepath));
                fileList += objFilepath + "\n";
                wwwForm.AddBinaryData("file", File.ReadAllBytes(mtlFilepath), Path.GetFileName(mtlFilepath));
                fileList += mtlFilepath + "\n";
            }

            Debug.Log(fileList);

            if (screenshotPath.Length == 0)
            {
                Debug.Log("SceneExportWindow Upload can't find files in screenshot directory");
            }
            else
            {
                wwwForm.AddBinaryData("screenshot", File.ReadAllBytes(screenshotPath[0]), "screenshot.png");
            }

            //TODO transfer this to CognitiveVR.EditorNetworkManager

            if (hasExistingSceneId)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (EditorCore.IsDeveloperKeyValid)
                {
                    headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                    //headers.Add("Content-Type", "multipart/form-data; boundary=\""+)
                    foreach (var v in wwwForm.headers)
                    {
                        headers[v.Key] = v.Value;
                    }
                }
                EditorNetwork.Post(Constants.POSTUPDATESCENE(settings.SceneId), wwwForm.data, PostUploadResponse, headers, true, "Upload", "Uploading new version of scene");//AUTH
            }
            else
            {
                //posting wwwform with headers
                

                //sceneUploadWWW = new WWW(Constants.POSTNEWSCENE(), wwwForm);
                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (EditorCore.IsDeveloperKeyValid)
                {
                    headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                    //headers.Add("Content-Type", "multipart/form-data; boundary=\""+)
                    foreach(var v in wwwForm.headers)
                    {
                        headers[v.Key] = v.Value;
                    }
                }
                EditorNetwork.Post(Constants.POSTNEWSCENE(), wwwForm.data, PostUploadResponse, headers, true, "Upload", "Uploading new scene");//AUTH
                //Debug.Log("Upload new scene");
            }

            UploadComplete = uploadComplete;
            //EditorApplication.update += UpdateUploadData;
        }

        static void PostUploadResponse(int responseCode, string error, string text)
        {
            if (responseCode != 200 && responseCode != 201)
            {
                Debug.LogError("Scene Upload Error " + error);
                
                UploadSceneSettings = null;
                UploadComplete = null;
                return;
            }

            //response can be <!DOCTYPE html><html lang=en><head><meta charset=utf-8><title>Error</title></head><body><pre>Internal Server Error</pre></body></html>
            if (text.Contains("Internal Server Error") || text.Contains("Bad Request"))
            {
                Debug.LogError("Scene Upload Error:" + text);
                
                UploadSceneSettings = null;
                UploadComplete = null;
                return;
            }

            string responseText = text.Replace("\"", "");
            if (!string.IsNullOrEmpty(responseText))
            {
                UploadSceneSettings.SceneId = responseText;
            }

            UploadSceneSettings.LastRevision = System.DateTime.UtcNow.ToBinary();

            //after scene upload response, hit version route to get the version of the scene
            //SendSceneVersionRequest(UploadSceneSettings);

            GUI.FocusControl("NULL");

            AssetDatabase.SaveAssets();

            if (UploadComplete != null)
                UploadComplete.Invoke();
            UploadComplete = null;

            Debug.Log("<color=green>Scene Upload Complete!</color>");
        }

        public static void ClearUploadSceneSettings() //sometimes not set to null when init window quits
        {
            UploadSceneSettings = null;
        }
        static CognitiveVR_Preferences.SceneSettings UploadSceneSettings;

        /*static void UpdateUploadData()
        {
            if (sceneUploadWWW == null)
            {
                EditorApplication.update -= UpdateUploadData;
                EditorUtility.ClearProgressBar();
                UploadComplete = null;
                return;
            }

            if (EditorUtility.DisplayCancelableProgressBar("Uploading", "Uploading scene data to sceneExplorer.com", sceneUploadWWW.uploadProgress))
            {
                EditorApplication.update -= UpdateUploadData;
                EditorUtility.ClearProgressBar();
                sceneUploadWWW.Dispose();
                sceneUploadWWW = null;
                UploadSceneSettings = null;
                Debug.LogError("Upload canceled!");
                UploadComplete = null;
                return;
            }

            if (!sceneUploadWWW.isDone) { return; }

            EditorUtility.ClearProgressBar();
            EditorApplication.update -= UpdateUploadData;

            //response
            if (!string.IsNullOrEmpty(sceneUploadWWW.error))
            {

                Debug.LogError("Scene Upload Error " + Util.GetResponseCode(sceneUploadWWW.responseHeaders) + ":" + sceneUploadWWW.error);

                sceneUploadWWW.Dispose();
                sceneUploadWWW = null;
                UploadSceneSettings = null;
                UploadComplete = null;
                return;
            }

            //response can be <!DOCTYPE html><html lang=en><head><meta charset=utf-8><title>Error</title></head><body><pre>Internal Server Error</pre></body></html>
            if (sceneUploadWWW.text.Contains("Internal Server Error"))
            {
                Debug.LogError("Scene Upload Error:" + sceneUploadWWW.text);

                sceneUploadWWW.Dispose();
                sceneUploadWWW = null;
                UploadSceneSettings = null;
                UploadComplete = null;
                return;
            }

            string responseText = sceneUploadWWW.text.Replace("\"", "");
            if (!string.IsNullOrEmpty(responseText))
            {
                UploadSceneSettings.SceneId = responseText;
            }

            UploadSceneSettings.LastRevision = System.DateTime.UtcNow.ToBinary();
            sceneUploadWWW.Dispose();
            sceneUploadWWW = null;

            //after scene upload response, hit version route to get the version of the scene
            //SendSceneVersionRequest(UploadSceneSettings);

            GUI.FocusControl("NULL");

            AssetDatabase.SaveAssets();

            if (UploadComplete != null)
                UploadComplete.Invoke();
            UploadComplete = null;

            //if (EditorUtility.DisplayDialog("Upload Complete", UploadSceneSettings.SceneName + " was successfully uploaded! Do you want to open your scene in SceneExplorer?", "Open on SceneExplorer", "Close"))
            //{
            //    Application.OpenURL(Constants.SCENEEXPLORER_SCENE + UploadSceneSettings.SceneId);
            //}

            Debug.Log("<color=green>Scene Upload Complete!</color>");
        }*/
        #endregion

        /*
        static void SendSceneVersionRequest(CognitiveVR_Preferences.SceneSettings settings)
        {
            if (GetSceneVersionWWW != null || authTokenRequest != null)
            {
                Debug.Log("SendSceneVersionRequest waiting for SceneVersionRequest or AuthTokenRequest");
                return;
            }

            var headers = new Dictionary<string, string>();
            headers.Add("X-HTTP-Method-Override", "GET");
            headers.Add("Authorization", "Bearer " + EditorPrefs.GetString("authToken"));

            if (settings == null)
            {
                Debug.Log("SendSceneVersionRequest no scene settings!");
                return;
            }
            if (string.IsNullOrEmpty(settings.SceneId))
            {
                Debug.LogWarning("SendSceneVersionRequest Current scene doesn't have an id!");
                return;
            }

            string url = Constants.GETSCENEVERSIONS(settings.SceneId);

            GetSceneVersionWWW = new WWW(url, null, headers);

            Util.logDebug("SendSceneVersionRequest request sent " + GetSceneVersionWWW.url);
            EditorApplication.update += GetSettingsResponse;
        }

        static void GetSettingsResponse()
        {
            if (GetSceneVersionWWW == null)
            {
                EditorApplication.update -= GetSettingsResponse;
                return;
            }

            if (!GetSceneVersionWWW.isDone) { return; }
            EditorApplication.update -= GetSettingsResponse;

            var responsecode = Util.GetResponseCode(GetSceneVersionWWW.responseHeaders);
            Util.logDebug("GetSettingsResponse responseCode: " + responsecode);

            if (responsecode >= 500)
            {
                //internal server error
                Util.logDebug("GetSettingsResponse - 500 internal server error");
                return;
            }
            else if (responsecode >= 400)
            {
                if (responsecode == 401)
                {
                    //not authorized. get auth token then try to get the scene version again
                    if (UploadSceneSettings == null)
                    {
                        UploadSceneSettings = EditorCore.GetPreferences().FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
                    }
                    string url = Constants.POSTAUTHTOKEN(UploadSceneSettings.SceneId);

                    Util.logDebug("GetSettingsResponse - unauthorized. Get auth token");

                    //request authorization
                    SendAuthTokenRequest(url);
                    return;
                }
                else
                {
                    //some other error
                    Util.logDebug("GetSettingsResponse - some error" + responsecode);
                    return;
                }
            }

            Debug.Log("GetSettingsResponse - got response with scene version");
            var collection = JsonUtility.FromJson<SceneVersionCollection>(GetSceneVersionWWW.text);

            if (UploadSceneSettings == null)
            {
                UploadSceneSettings = EditorCore.GetPreferences().FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            }
            UploadSceneSettings.VersionNumber = collection.GetLatestVersion().versionNumber;
            UploadSceneSettings.VersionId = collection.GetLatestVersion().id;
            AssetDatabase.SaveAssets();
            UploadSceneSettings = null;
            GetSceneVersionWWW = null;
        }

        public static void SendAuthTokenRequest(string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("X-HTTP-Method-Override", "POST");
            headers.Add("Cookie", EditorPrefs.GetString("sessionToken"));

            authTokenRequest = new WWW(url, new System.Text.UTF8Encoding(true).GetBytes("ignored"), headers);
            EditorApplication.update += GetAuthTokenResponse;
        }

        static void GetAuthTokenResponse()
        {
            if (authTokenRequest == null)
            {
                EditorApplication.update -= GetAuthTokenResponse;
                return;
            }
            if (!authTokenRequest.isDone) { return; }
            EditorApplication.update -= GetAuthTokenResponse;

            var responseCode = Util.GetResponseCode(authTokenRequest.responseHeaders);

            if (responseCode >= 500)
            {
                //internal server error
                //OnAuthResponse(responseCode);
                Util.logDebug("GetAuthTokenResponse - 500 internal server error");
                return;
            }
            else if (responseCode >= 400)
            {
                if (responseCode == 401)
                {
                    //session token not authorized
                    Debug.LogWarning("GetAuthTokenResponse Session token not authorized to get auth token. Please log in");

                    CognitiveVR_Settings.Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Account Settings");
                    Vector2 size = new Vector2(300, 550);
                    CognitiveVR_Settings.Instance.minSize = size;
                    CognitiveVR_Settings.Instance.maxSize = size;
                    CognitiveVR_Settings.Instance.Show();
                    CognitiveVR_Settings.Instance.Logout();
                    authTokenRequest = null;
                    UploadSceneSettings = null;
                    GetSceneVersionWWW = null;
                    return;
                }
                else
                {
                    //request is wrong
                    Util.logDebug("GetAuthTokenResponse - some other error " + responseCode);
                    return;
                }
            }

            var tokenResponse = JsonUtility.FromJson<CognitiveVR_Settings.AuthTokenResponse>(authTokenRequest.text);
            EditorPrefs.SetString("authToken", tokenResponse.token);

            authTokenRequest = null;

            SendSceneVersionRequest(UploadSceneSettings);

        }

        public static void RefreshSceneVersion()
        {
            //gets the scene version from api and sets it to the current scene
            var currentSettings = CognitiveVR_Preferences.Instance.FindScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name);
            if (currentSettings != null)
            {
                SendSceneVersionRequest(currentSettings);
            }
        }

        static WWW GetSceneVersionWWW;
        static WWW authTokenRequest;*/

        #region Export Dynamic Objects
        /// <summary>
        /// export all dynamic objects in scene. skip prefabs
        /// </summary>
        /// <returns>true if any dynamics are exported</returns>
        public static bool ExportAllDynamicsInScene()
        {
            //List<Transform> entireSelection = new List<Transform>();
            //entireSelection.AddRange(Selection.GetTransforms(SelectionMode.Editable));

            var dynamics = FindObjectsOfType<DynamicObject>();

            Debug.Log("Starting export of " + dynamics.Length + " dynamic objects");
            

            //export all the objects
            int successfullyExportedCount = 0;
            List<string> exportedMeshNames = new List<string>();

            foreach (var dynamic in dynamics)
            {
                if (exportedMeshNames.Contains(dynamic.MeshName)) { successfullyExportedCount++; continue; } //skip exporting same mesh

                if (dynamic.GetComponent<Canvas>() != null)
                {
                    //TODO merge this deeper in the export process. do this recurively ignoring child dynamics
                    //take a snapshot
                    var width = dynamic.GetComponent<RectTransform>().sizeDelta.x * dynamic.transform.localScale.x;
                    var height = dynamic.GetComponent<RectTransform>().sizeDelta.y * dynamic.transform.localScale.y;

                    var screenshot = CognitiveVR_SceneExplorerExporter.Snapshot(dynamic.transform);

                    var mesh = CognitiveVR_SceneExplorerExporter.ExportQuad(dynamic.MeshName, width, height, dynamic.transform, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, screenshot);
                    CognitiveVR_SceneExplorerExporter.ExportDynamicObject(mesh, dynamic.MeshName, screenshot, dynamic.MeshName);
                    successfullyExportedCount++;
                }
                else if (CognitiveVR_SceneExplorerExporter.ExportDynamicObject(dynamic.transform))
                {
                    successfullyExportedCount++;
                }

                foreach (var common in System.Enum.GetNames(typeof(DynamicObject.CommonDynamicMesh)))
                {
                    if (common.ToLower() == dynamic.MeshName.ToLower())
                    {
                        //don't export common meshes!
                        continue;
                    }
                }

                if (!exportedMeshNames.Contains(dynamic.MeshName))
                {
                    exportedMeshNames.Add(dynamic.MeshName);
                }
            }

            if (successfullyExportedCount == 0)
            {
                EditorUtility.DisplayDialog("Objects exported", "No dynamic objects successfully exported.\n\nDo you have Mesh Renderers, Skinned Mesh Renderers or Canvas components attached or as children?", "Ok");
                return false;
            }

            EditorUtility.DisplayDialog("Objects exported", "Successfully exported " + successfullyExportedCount + "/" + dynamics.Length + " dynamic objects using " + exportedMeshNames.Count + " unique mesh names", "Ok");
            return true;
        }

        /// <summary>
        /// export selected gameobjects, temporarily spawn them in the scene if they are prefabs
        /// </summary>
        /// <returns>true if exported at least 1 mesh</returns>
        public static bool ExportSelectedObjectsPrefab()
        {
            List<Transform> entireSelection = new List<Transform>();
            entireSelection.AddRange(Selection.GetTransforms(SelectionMode.Editable));

            if (entireSelection.Count == 0) { Debug.Log("No Dynamic Objects selected"); return false; }

            Debug.Log("Starting export of " + entireSelection.Count + " dynamic objects");

            List<Transform> sceneObjects = new List<Transform>();
            sceneObjects.AddRange(Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab));

            List<Transform> prefabsToSpawn = new List<Transform>();

            //add prefab objects to a list
            foreach (var v in entireSelection)
            {
                if (!sceneObjects.Contains(v))
                {
                    prefabsToSpawn.Add(v);
                }
            }

            //spawn prefabs
            var temporarySpawnedPrefabs = new List<GameObject>();
            foreach (var v in prefabsToSpawn)
            {
                var newPrefab = GameObject.Instantiate(v.gameObject);
                temporarySpawnedPrefabs.Add(newPrefab);
                sceneObjects.Add(newPrefab.transform);
            }

            //export all the objects
            int successfullyExportedCount = 0;
            List<string> exportedMeshNames = new List<string>();

            foreach (var v in sceneObjects)
            {
                var dynamic = v.GetComponent<DynamicObject>();
                if (dynamic == null) { continue; }
                if (v.GetComponent<Canvas>() != null)
                {
                    //TODO merge this deeper in the export process. do this recurively ignoring child dynamics
                    //take a snapshot
                    var width = v.GetComponent<RectTransform>().sizeDelta.x * v.localScale.x;
                    var height = v.GetComponent<RectTransform>().sizeDelta.y * v.localScale.y;

                    var screenshot = CognitiveVR_SceneExplorerExporter.Snapshot(v);

                    var mesh = CognitiveVR_SceneExplorerExporter.ExportQuad(dynamic.MeshName, width, height, v, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, screenshot);
                    CognitiveVR_SceneExplorerExporter.ExportDynamicObject(mesh, dynamic.MeshName, screenshot, dynamic.MeshName);
                    successfullyExportedCount++;
                }
                else if (CognitiveVR_SceneExplorerExporter.ExportDynamicObject(v))
                {
                    successfullyExportedCount++;
                }

                foreach (var common in System.Enum.GetNames(typeof(DynamicObject.CommonDynamicMesh)))
                {
                    if (common.ToLower() == dynamic.MeshName.ToLower())
                    {
                        //don't export common meshes!
                        continue;
                    }
                }

                if (!exportedMeshNames.Contains(dynamic.MeshName))
                {
                    exportedMeshNames.Add(dynamic.MeshName);
                }
            }

            //destroy the temporary prefabs
            foreach (var v in temporarySpawnedPrefabs)
            {
                GameObject.DestroyImmediate(v);
            }

            if (entireSelection.Count == 0)
            {
                EditorUtility.DisplayDialog("Objects exported", "No dynamic objects selected", "Ok");
                return false;
            }

            if (successfullyExportedCount == 0)
            {
                EditorUtility.DisplayDialog("Objects exported", "No dynamic objects successfully exported.\n\nDo you have Mesh Renderers, Skinned Mesh Renderers or Canvas components attached or as children?", "Ok");
                return false;
            }

            if (successfullyExportedCount == 1 && entireSelection.Count == 1)
            {
                EditorUtility.DisplayDialog("Objects exported", "Successfully exported " + successfullyExportedCount + " dynamic object", "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Objects exported", "Successfully exported " + successfullyExportedCount + "/" + entireSelection.Count + " dynamic objects using " + exportedMeshNames.Count + " unique mesh names", "Ok");
            }
            return true;
        }
        #endregion

        #region Upload Dynamic Objects

        static List<DynamicObjectForm> dynamicObjectForms = new List<DynamicObjectForm>();
        static string currentDynamicUploadName;

        /// <summary>
        /// returns true if successfully uploaded dynamics
        /// </summary>
        /// <param name="ShowPopupWindow"></param>
        /// <returns></returns>
        public static bool UploadSelectedDynamicObjects(bool ShowPopupWindow = false)
        {
            List<string> dynamicMeshNames = new List<string>();
            foreach (var v in Selection.transforms)
            {
                var dyn = v.GetComponent<DynamicObject>();
                if (dyn == null) { continue; }
                dynamicMeshNames.Add(dyn.MeshName);
            }

            if (dynamicMeshNames.Count == 0) { return false; }

            return UploadDynamicObjects(dynamicMeshNames, ShowPopupWindow);
        }

        /// <summary>
        /// returns true if successfully uploaded dynamics
        /// </summary>
        /// <param name="ShowPopupWindow"></param>
        /// <returns></returns>
        public static bool UploadAllDynamicObjects(bool ShowPopupWindow = false)
        {
            List<string> dynamicMeshNames = new List<string>();
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic";
            var subdirectories = Directory.GetDirectories(path);
            foreach (var v in subdirectories)
            {
                var split = v.Split(Path.DirectorySeparatorChar);
                //
                dynamicMeshNames.Add(split[split.Length - 1]);
            }

            //upload all stuff from exported files
            return UploadDynamicObjects(dynamicMeshNames, ShowPopupWindow);
        }

        //search through files. if dynamics.name contains exported folder, upload
        static bool UploadDynamicObjects(List<string> dynamicMeshNames, bool ShowPopupWindow = false)
        {
            string fileList = "Upload Files:\n";

            var settings = CognitiveVR_Preferences.FindCurrentScene();
            if (settings == null)
            {
                string s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(s))
                {
                    s = "Unknown Scene";
                }
                EditorUtility.DisplayDialog("Upload Failed", "Could not find the Scene Settings for \"" + s + "\". Are you sure you've saved, exported and uploaded this scene to SceneExplorer?", "Ok");
                return false;
            }

            string sceneid = settings.SceneId;

            if (string.IsNullOrEmpty(sceneid))
            {
                EditorUtility.DisplayDialog("Upload Failed", "Could not find the SceneId for \"" + settings.SceneName + "\". Are you sure you've exported and uploaded this scene to SceneExplorer?","Ok");
                return false;
            }

            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic";
            var subdirectories = Directory.GetDirectories(path);

            //
            List<string> exportDirectories = new List<string>();
            foreach(var v in subdirectories)
            {
                var split = v.Split(Path.DirectorySeparatorChar);

                if (dynamicMeshNames.Contains(split[split.Length - 1]))
                {
                    exportDirectories.Add(v);
                }
            }

            if (ShowPopupWindow)
            {
                int option = EditorUtility.DisplayDialogComplex("Upload Dynamic Objects", "Do you want to upload " + exportDirectories.Count + " Objects to \"" + settings.SceneName + "\" (" + settings.SceneId + " Version:" + settings.VersionNumber+")?", "Ok", "Cancel", "Open Directory");
                if (option == 0) { } //ok
                else if (option == 1) { return false; } //cancel
                else
                {
#if UNITY_EDITOR_WIN
                    //open directory
                    System.Diagnostics.Process.Start("explorer.exe", path);
                    return false;
#elif UNITY_EDITOR_OSX
                System.Diagnostics.Process.Start("open", path);
                return;
#endif
                }
            }
            string objectNames="";
            foreach (var subdir in exportDirectories)
            {
                var filePaths = Directory.GetFiles(subdir);

                WWWForm wwwForm = new WWWForm();
                foreach (var f in filePaths)
                {
                    if (f.ToLower().EndsWith(".ds_store"))
                    {
                        Debug.Log("skip file " + f);
                        continue;
                    }

                    fileList += f + "\n";

                    var data = File.ReadAllBytes(f);
                    wwwForm.AddBinaryData("file", data, Path.GetFileName(f));
                }

                var dirname = new DirectoryInfo(subdir).Name;

                objectNames += dirname + "\n";

                string uploadUrl = Constants.POSTDYNAMICOBJECTDATA(settings.SceneId, settings.VersionNumber, dirname);

                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (EditorCore.IsDeveloperKeyValid)
                {
                    headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                    //headers.Add("Content-Type", "multipart/form-data; boundary=\""+)
                    foreach (var v in wwwForm.headers)
                    {
                        headers[v.Key] = v.Value;
                    }
                }

                dynamicObjectForms.Add(new DynamicObjectForm(uploadUrl, wwwForm, dirname, headers)); //AUTH
            }

            if (dynamicObjectForms.Count > 0)
            {
                DynamicUploadTotal = dynamicObjectForms.Count;
                DynamicUploadSuccess = 0;
                EditorApplication.update += UpdateUploadDynamics;
            }
            return true;
        }

        class DynamicObjectForm
        {
            public string Url;
            public WWWForm Form;
            public string Name;
            public Dictionary<string, string> Headers;

            public DynamicObjectForm(string url, WWWForm form, string name, Dictionary<string, string> headers)
            {
                Url = url;
                Form = form;
                Name = name;
                Headers = headers;
            }
        }

        static int DynamicUploadTotal;
        static int DynamicUploadSuccess;

        static WWW dynamicUploadWWW;
        static void UpdateUploadDynamics()
        {
            if (dynamicUploadWWW == null)
            {
                //get the next dynamic object to upload from forms
                if (dynamicObjectForms.Count == 0)
                {
                    //DONE!
                    Debug.Log("Dynamic Object uploads complete. " + DynamicUploadSuccess + "/" + DynamicUploadTotal + " succeeded");
                    EditorApplication.update -= UpdateUploadDynamics;
                    EditorUtility.ClearProgressBar();
                    currentDynamicUploadName = string.Empty;
                    return;
                }
                else
                {
                    dynamicUploadWWW = new WWW(dynamicObjectForms[0].Url, dynamicObjectForms[0].Form.data,dynamicObjectForms[0].Headers);
                    currentDynamicUploadName = dynamicObjectForms[0].Name;
                    dynamicObjectForms.RemoveAt(0);
                }
            }

            EditorUtility.DisplayProgressBar("Upload Dynamic Object", currentDynamicUploadName, dynamicUploadWWW.uploadProgress);

            if (!dynamicUploadWWW.isDone) { return; }

            if (!string.IsNullOrEmpty(dynamicUploadWWW.error))
            {
                Debug.LogError(dynamicUploadWWW.error);
            }
            else
            {
                DynamicUploadSuccess++;
            }

            Debug.Log("Finished uploading dynamic object " + currentDynamicUploadName);

            dynamicUploadWWW = null;
        }
        #endregion

        #region Utility

        /*bool KeyIsValid(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }

            //a12345b6-78c9-01d2-3456-78e9f0ghi123

            string pattern = @"[A-Za-z0-9\-+]{" + key.Length + "}";
            bool regexPass = System.Text.RegularExpressions.Regex.IsMatch(key, pattern);
            return regexPass;
        }*/

        /*static void FindBlender()
        {
#if UNITY_EDITOR_WIN
            if (Directory.Exists(@"C:/Program Files/"))
            {
                if (Directory.Exists(@"C:/Program Files/Blender Foundation/"))
                {
                    if (Directory.Exists(@"C:/Program Files/Blender Foundation/Blender"))
                    {
                        if (File.Exists(@"C:/Program Files/Blender Foundation/Blender/blender.exe"))
                        {
                            EditorCore.BlenderPath = @"C:/Program Files/Blender Foundation/Blender/blender.exe";
                        }
                    }
                }
            }
            else if (Directory.Exists(@"C:/Program Files (x86)"))
            {
                if (Directory.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64"))
                {
                    if (Directory.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64"))
                    {
                        if (File.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64/blender.exe"))
                        {
                            EditorCore.BlenderPath = @"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64/blender.exe";
                        }
                    }
                }
            }
#elif UNITY_EDITOR_OSX
            //check /Applications/Blender/blender.app
            if (Directory.Exists(@"/Applications/"))
            {
                if (Directory.Exists(@"/Applications/Blender/"))
                {
                    if (File.Exists(@"/Applications/Blender/blender.app"))
                    {
                        EditorCore.BlenderPath = @"/Applications/Blender/blender.app";
                    }
                }
            }
#endif
        }*/

        //add all the scenes in the project to the scene editor window. keep all the scene settings from preferences
        /*private static void UpdateSceneNames()
        {
            //save these to a temp list
            if (prefs == null)
            {
                prefs = EditorCore.GetPreferences();
            }

            List<CognitiveVR_Preferences.SceneSettings> oldSettings = new List<CognitiveVR_Preferences.SceneSettings>();
            foreach (var v in prefs.sceneSettings)
            {
                oldSettings.Add(v);
            }


            //clear then rebuild the list in preferences
            prefs.sceneSettings.Clear();

            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                prefs.sceneSettings.Add(new CognitiveVR_Preferences.SceneSettings(name, path));
            }

            //match up dictionary keys from temp list
            foreach (var oldSetting in oldSettings)
            {
                foreach (var newSetting in prefs.sceneSettings)
                {
                    if (newSetting.SceneName == oldSetting.SceneName)
                    {
                        newSetting.SceneId = oldSetting.SceneId;
                        newSetting.LastRevision = oldSetting.LastRevision;
                        newSetting.SceneName = oldSetting.SceneName;
                        newSetting.ScenePath = oldSetting.ScenePath;
                        newSetting.VersionId = oldSetting.VersionId;
                        newSetting.VersionNumber = oldSetting.VersionNumber;
                    }
                }
            }
        }*/

        #endregion

        private void OnDestroy()
        {
            if (tempDisabledCameras.Count > 0)
            {
                foreach (var c in tempDisabledCameras)
                {
                    c.enabled = true;
                }
                tempDisabledCameras.Clear();
            }
        }
    }
}