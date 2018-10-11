using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;
using UnityEngine.Networking;

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
        //returns true if savedblenderpath ends with blender.exe/app
        static bool IsBlenderPathValid()
        {
            if (string.IsNullOrEmpty(EditorCore.BlenderPath)) { return false; }
#if UNITY_EDITOR_WIN
            return EditorCore.BlenderPath.ToLower().EndsWith("blender.exe");
#elif UNITY_EDITOR_OSX
            return EditorCore.BlenderPath.ToLower().EndsWith("blender.app");
#else
            return false;
#endif
        }

        private void Update()
        {
            Repaint();
        }

        #region Export Scene

        //don't even try exporting the scene. just generate the folder and json file
        public static void ExportSceneAR()
        {
            string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            string objPath = CognitiveVR_SceneExplorerExporter.GetDirectory(fullName);

            //if folder exists, delete mtl, obj, png and json contents
            if (Directory.Exists(objPath))
            {
                var files = Directory.GetFiles(objPath);
                for(int i=0;i<files.Length;i++)
                {
                    File.Delete(files[i]);
                }
            }

            //write json settings file
            string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\"" + fullName + "\",\"sdkVersion\":\"" + Core.SDK_VERSION + "\"}";
            File.WriteAllText(objPath + "settings.json", jsonSettingsContents);
        }

        //export and try to decimate the scene
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

            string objPath = CognitiveVR_SceneExplorerExporter.GetDirectory(fullName);

            //write json settings file
            string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\"" + fullName + "\",\"sdkVersion\":\"" + Core.SDK_VERSION + "\"}";
            File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

            if (!successfulExport)
            {
                Debug.LogError("Scene export failed!");
                return;
            }

            //begin scene decimation
            if (!EditorCore.IsBlenderPathValid)
            {
                Debug.LogWarning("Blender not found during scene export. May result in large files uploaded to Scene Explorer");
                return;
            }

            string filepath = "";
            if (!EditorCore.RecursiveDirectorySearch("", out filepath, "CognitiveVR"+System.IO.Path.DirectorySeparatorChar+"Editor"))
            {
                Debug.LogError("Could not find CognitiveVR/Editor/decimateall.py");
            }

            string decimateScriptPath = filepath + System.IO.Path.DirectorySeparatorChar +"decimateall.py";
            decimateScriptPath = decimateScriptPath.Replace(" ", "\" \"");
            objPath = objPath.Replace(" ", "\" \"");
            fullName = fullName.Replace(" ", "\" \"");

            EditorUtility.ClearProgressBar();

            ProcessStartInfo processInfo;
#if UNITY_EDITOR_WIN
            processInfo = new ProcessStartInfo(EditorCore.BlenderPath);
            processInfo.UseShellExecute = true;
            processInfo.Arguments = "-P " + decimateScriptPath + " " + objPath + " " + EditorCore.ExportSettings.ExplorerMinimumFaceCount + " " + EditorCore.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;

#elif UNITY_EDITOR_OSX
            processInfo = new ProcessStartInfo("open");
            processInfo.Arguments = EditorCore.BlenderPath + " --args -P " + decimateScriptPath + " " + objPath + " " + EditorCore.ExportSettings.ExplorerMinimumFaceCount + " " + EditorCore.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;
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

            if (hasExistingSceneId) //upload new verison of existing scene
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
                EditorNetwork.Post(Constants.POSTUPDATESCENE(settings.SceneId), wwwForm.data, PostSceneUploadResponse, headers, true, "Upload", "Uploading new version of scene");//AUTH
            }
            else //upload as new scene
            {
                //posting wwwform with headers
                
                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (EditorCore.IsDeveloperKeyValid)
                {
                    headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                    foreach(var v in wwwForm.headers)
                    {
                        headers[v.Key] = v.Value;
                    }
                }
                EditorNetwork.Post(Constants.POSTNEWSCENE(), wwwForm.data, PostSceneUploadResponse, headers, true, "Upload", "Uploading new scene");//AUTH
            }

            UploadComplete = uploadComplete;
        }

        static void PostSceneUploadResponse(int responseCode, string error, string text)
        {
            Debug.Log("UploadScene Response. [RESPONSE CODE] " + responseCode + " [ERROR] " + error + " [TEXT] " + text);

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
            if (!string.IsNullOrEmpty(responseText)) //uploading a new version returns empty. uploading a new scene returns sceneid
            {
                EditorUtility.SetDirty(CognitiveVR_Preferences.Instance);
                UploadSceneSettings.SceneId = responseText;
                AssetDatabase.SaveAssets();
            }

            UploadSceneSettings.LastRevision = System.DateTime.UtcNow.ToBinary();

            //after scene upload response, hit version route to get the version of the scene
            //SendSceneVersionRequest(UploadSceneSettings);

            GUI.FocusControl("NULL");
            EditorUtility.SetDirty(CognitiveVR_Preferences.Instance);
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
        
        #endregion
        

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

            Debug.Log("Starting export of " + dynamics.Length + " Dynamic Objects");
            

            //export all the objects
            int successfullyExportedCount = 0;
            List<string> exportedMeshNames = new List<string>();
            List<string> totalExportedMeshNames = new List<string>();

            foreach (var dynamic in dynamics)
            {
                if (!dynamic.UseCustomMesh)
                {
                    //skip exporting a mesh with no name
                    continue;
                }
                if (string.IsNullOrEmpty(dynamic.MeshName))
                {
                    if (!totalExportedMeshNames.Contains(""))
                        totalExportedMeshNames.Add("");
                    Debug.LogWarning("GameObject " + dynamic.gameObject + " has empty mesh name");
                    continue;
                }

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
                    EditorCore.SaveDynamicThumbnailAutomatic(dynamic.gameObject);
                    successfullyExportedCount++;
                }
                else if (CognitiveVR_SceneExplorerExporter.ExportDynamicObject(dynamic.transform))
                {
                    EditorCore.SaveDynamicThumbnailAutomatic(dynamic.gameObject);
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
                if (!totalExportedMeshNames.Contains(dynamic.MeshName))
                    totalExportedMeshNames.Add(dynamic.MeshName);

                if (!exportedMeshNames.Contains(dynamic.MeshName))
                {
                    exportedMeshNames.Add(dynamic.MeshName);
                }
            }

            if (successfullyExportedCount == 0)
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "No Dynamic Objects successfully exported.\n\nDo you have Mesh Renderers, Skinned Mesh Renderers or Canvas components attached or as children?", "Ok");
                return false;
            }

            EditorUtility.DisplayDialog("Dynamic Object Export", "From all Dynamic Objects in scene, found " + totalExportedMeshNames.Count + " unique mesh names and successfully exported " + successfullyExportedCount, "Ok");
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

            Debug.Log("Starting export of " + entireSelection.Count + " Dynamic Objects");

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
            List<string> totalExportedMeshNames = new List<string>();

            foreach (var v in sceneObjects)
            {
                var dynamic = v.GetComponent<DynamicObject>();
                if (dynamic == null) { continue; }

                if (!dynamic.UseCustomMesh)
                {
                    //skip exporting a mesh with no name
                    continue;
                }
                if (string.IsNullOrEmpty(dynamic.MeshName))
                {
                    if (!totalExportedMeshNames.Contains(""))
                        totalExportedMeshNames.Add("");
                    Debug.LogWarning("GameObject " + dynamic.gameObject + " has empty mesh name");
                    continue;
                }

                if (exportedMeshNames.Contains(dynamic.MeshName)) { successfullyExportedCount++; continue; } //skip exporting same mesh

                if (v.GetComponent<Canvas>() != null)
                {
                    //TODO merge this deeper in the export process. do this recurively ignoring child dynamics
                    //take a snapshot
                    var width = v.GetComponent<RectTransform>().sizeDelta.x * v.localScale.x;
                    var height = v.GetComponent<RectTransform>().sizeDelta.y * v.localScale.y;

                    var screenshot = CognitiveVR_SceneExplorerExporter.Snapshot(v);

                    var mesh = CognitiveVR_SceneExplorerExporter.ExportQuad(dynamic.MeshName, width, height, v, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, screenshot);
                    CognitiveVR_SceneExplorerExporter.ExportDynamicObject(mesh, dynamic.MeshName, screenshot, dynamic.MeshName);
                    EditorCore.SaveDynamicThumbnailAutomatic(dynamic.gameObject);
                    successfullyExportedCount++;
                }
                else if (CognitiveVR_SceneExplorerExporter.ExportDynamicObject(v))
                {
                    EditorCore.SaveDynamicThumbnailAutomatic(dynamic.gameObject);
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
                if (!totalExportedMeshNames.Contains(dynamic.MeshName))
                    totalExportedMeshNames.Add(dynamic.MeshName);
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
                EditorUtility.DisplayDialog("Dynamic Object Export", "No Dynamic Objects selected", "Ok");
                return false;
            }

            if (successfullyExportedCount == 0)
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "No Dynamic Objects successfully exported.\n\nDo you have Mesh Renderers, Skinned Mesh Renderers or Canvas components attached or as children?", "Ok");
                return false;
            }

            if (successfullyExportedCount == 1 && entireSelection.Count == 1)
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "Successfully exported 1 Dynamic Object mesh", "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "From selected Dynamic Objects , found " + totalExportedMeshNames.Count + " unique mesh names and successfully exported " + successfullyExportedCount, "Ok");
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
        /// returns true if successfully started uploading dynamics
        /// </summary>
        /// <param name="ShowPopupWindow"></param>
        /// <returns></returns>
        public static bool UploadAllDynamicObjects(bool ShowPopupWindow = false)
        {
            if (!Directory.Exists(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic"))
            {
                Debug.Log("skip uploading dynamic objects, folder doesn't exist");
                return false;
            }

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
                EditorUtility.DisplayDialog("Dynamic Object Upload Failed", "Could not find the Scene Settings for \"" + s + "\". Are you sure you've saved, exported and uploaded this scene to SceneExplorer?", "Ok");
                return false;
            }

            string sceneid = settings.SceneId;

            if (string.IsNullOrEmpty(sceneid))
            {
                EditorUtility.DisplayDialog("Dynamic Object Upload Failed", "Could not find the SceneId for \"" + settings.SceneName + "\". Are you sure you've exported and uploaded this scene to SceneExplorer?","Ok");
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
                return false;
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

        static UnityWebRequest dynamicUploadWWW;
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
                    dynamicUploadWWW = UnityWebRequest.Post(dynamicObjectForms[0].Url, dynamicObjectForms[0].Form);
                    foreach (var v in dynamicObjectForms[0].Headers)
                        dynamicUploadWWW.SetRequestHeader(v.Key, v.Value);
                    dynamicUploadWWW.Send();
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

            Debug.Log("Finished uploading Dynamic Object mesh: " + currentDynamicUploadName);

            dynamicUploadWWW = null;
        }
        #endregion
    }
}