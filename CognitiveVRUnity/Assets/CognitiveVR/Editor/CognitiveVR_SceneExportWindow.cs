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
    public class BakeableMesh
    {
        public GameObject tempGo;
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public bool useOriginalscale;
        public Vector3 originalScale;
    }

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
            for (int i = 0; i < versions.Count; i++)
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

        private static bool CreateTargetFolder(string fullName)
        {
            try
            {
                Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName);
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Failed to create folder: CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName, "Ok");
                return false;
            }

            return true;
        }

        //retrun path to CognitiveVR_SceneExplorerExport. create if it doesn't exist
        public static string GetDirectory(string fullName)
        {
            CreateTargetFolder(fullName);

            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName + Path.DirectorySeparatorChar;
        }

        //don't even try exporting the scene. just generate the folder and json file
        public static void ExportSceneAR()
        {
            string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            string objPath = GetDirectory(fullName);

            //if folder exists, delete mtl, obj, png and json contents
            if (Directory.Exists(objPath))
            {
                var files = Directory.GetFiles(objPath);
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }
            }

            //write json settings file
            string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\"" + fullName + "\",\"sdkVersion\":\"" + Core.SDK_VERSION + "\"}";
            File.WriteAllText(objPath + "settings.json", jsonSettingsContents);
        }

        //export and try to decimate the scene

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
                    string objPath = GetDirectory(sceneName);

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


        public static void ExportGLTFScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var gameObjects = scene.GetRootGameObjects();

            List<Transform> t = new List<Transform>();
            foreach (var v in gameObjects)
            {
                if (v.GetComponent<MeshFilter>() != null && v.GetComponent<MeshFilter>().sharedMesh == null) { continue; }
                if (v.activeInHierarchy) { t.Add(v.transform); }
                //check for mesh renderers here, before nodes are constructed for invalid objects?
            }

            List<BakeableMesh> temp = new List<BakeableMesh>();

            EditorUtility.DisplayProgressBar("Export GLTF", "Bake Nonstandard Renderers", 0.10f);
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + scene.name;
            BakeNonstandardRenderers(null, temp, path);
            var exporter = new UnityGLTF.GLTFSceneExporter(t.ToArray(), RetrieveTexturePath, null);
            Directory.CreateDirectory(path);

            EditorUtility.DisplayProgressBar("Export GLTF", "Save GLTF and Bin", 0.50f);
            exporter.SaveGLTFandBin(path, "scene");

            EditorUtility.DisplayProgressBar("Export GLTF", "Resize Textures", 0.75f);
            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i].useOriginalscale)
                {
                    temp[i].meshRenderer.transform.localScale = temp[i].originalScale;
                }
                DestroyImmediate(temp[i].meshFilter);
                DestroyImmediate(temp[i].meshRenderer);
                if (temp[i].tempGo != null)
                    DestroyImmediate(temp[i].tempGo);
            }

            ResizeQueue.Enqueue(path);
            EditorApplication.update -= UpdateResize;
            EditorApplication.update += UpdateResize;
        }

        //wait for update message from the editor - reading files without this delay has issues reading data
        static Queue<string> ResizeQueue = new Queue<string>();
        static void UpdateResize()
        {
            while (ResizeQueue.Count > 0)
            {
                ResizeTexturesInExportFolder(ResizeQueue.Dequeue());
            }

            EditorApplication.update -= UpdateResize;
            EditorUtility.ClearProgressBar();
        }

        static void ResizeTexturesInExportFolder(string folderpath)
        {
            var textureDivisor = CognitiveVR_Preferences.Instance.TextureResize;

            if (textureDivisor == 1) { return; }
            Texture2D texture = new Texture2D(2, 2);
            var files = Directory.GetFiles(folderpath);

            foreach (var file in files)
            {
                if (!file.EndsWith(".png")) { continue; }

                //skip thumbnails
                if (file.EndsWith("cvr_object_thumbnail.png")) { continue; }

                string path = file;

                texture.LoadImage(File.ReadAllBytes(file));

                var newWidth = Mathf.Max(1, Mathf.NextPowerOfTwo(texture.width) / textureDivisor);
                var newHeight = Mathf.Max(1, Mathf.NextPowerOfTwo(texture.height) / textureDivisor);

                TextureScale.Bilinear(texture, newWidth, newHeight);

                //texture = RescaleForExport(texture, Mathf.NextPowerOfTwo(texture.width) / textureDivisor, Mathf.NextPowerOfTwo(texture.height) / textureDivisor);
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
            }
        }

        public static Texture2D RescaleForExport(Texture2D tex, int newWidth, int newHeight)
        {
            Color[] texColors;
            Color[] newColors;
            float ratioX;
            float ratioY;

            newHeight = Mathf.Max(1, newHeight);
            newWidth = Mathf.Max(1, newWidth);

            texColors = tex.GetPixels();
            newColors = new Color[newWidth * newHeight];
            ratioX = ((float)tex.width) / newWidth;
            ratioY = ((float)tex.height) / newHeight;

            int w = tex.width;
            int w2 = newWidth;

            for (var y = 0; y < newHeight; y++)
            {
                var thisY = (int)(ratioY * y) * w;
                var yw = y * w2;
                for (var x = 0; x < w2; x++)
                {
                    newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                }
            }

            Texture2D newText = new Texture2D(newWidth, newHeight);
            newText.SetPixels(newColors);
            return newText;
        }


        static void BakeNonstandardRenderers(DynamicObject rootDynamic, List<BakeableMesh> meshes, string path)
        {
            SkinnedMeshRenderer[] SkinnedMeshes = FindObjectsOfType<SkinnedMeshRenderer>();
            Terrain[] Terrains = FindObjectsOfType<Terrain>();
            Canvas[] Canvases = FindObjectsOfType<Canvas>();
            if (rootDynamic != null)
            {
                SkinnedMeshes = rootDynamic.GetComponentsInChildren<SkinnedMeshRenderer>();
                Terrains = rootDynamic.GetComponentsInChildren<Terrain>();
                Canvases = rootDynamic.GetComponentsInChildren<Canvas>();
            }

            foreach (var skinnedMeshRenderer in SkinnedMeshes)
            {
                if (!skinnedMeshRenderer.gameObject.activeInHierarchy) { continue; }
                if (rootDynamic == null && skinnedMeshRenderer.GetComponentInParent<DynamicObject>() != null)
                {
                    //skinned mesh as child of dynamic when exporting scene
                    continue;
                }
                else if (rootDynamic != null && skinnedMeshRenderer.GetComponentInParent<DynamicObject>() != rootDynamic)
                {
                    //exporting dynamic, found skinned mesh in some other dynamic
                    continue;
                }

                //var pos = skinnedMeshRenderer.transform.localPosition;
                //skinnedMeshRenderer.transform.localPosition = Vector3.zero;
                //var rot = skinnedMeshRenderer.transform.localRotation;
                //skinnedMeshRenderer.transform.localRotation = Quaternion.identity;

                BakeableMesh bm = new BakeableMesh();

                bm.tempGo = new GameObject(skinnedMeshRenderer.gameObject.name);
                bm.tempGo.transform.parent = skinnedMeshRenderer.transform;
                bm.tempGo.transform.localScale = Vector3.one;
                bm.tempGo.transform.localRotation = Quaternion.identity;
                bm.tempGo.transform.localPosition = Vector3.zero;

                bm.meshRenderer = bm.tempGo.AddComponent<MeshRenderer>();
                bm.meshRenderer.sharedMaterial = skinnedMeshRenderer.sharedMaterial;
                bm.meshFilter = bm.tempGo.AddComponent<MeshFilter>();
                var m = new Mesh();
                m.name = skinnedMeshRenderer.sharedMesh.name;
                bm.originalScale = skinnedMeshRenderer.transform.localScale;
                bm.useOriginalscale = true;
                skinnedMeshRenderer.transform.localScale = Vector3.one;
                skinnedMeshRenderer.BakeMesh(m);
                bm.meshFilter.sharedMesh = m;
                meshes.Add(bm);

                //skinnedMeshRenderer.transform.localPosition = pos;
                //skinnedMeshRenderer.transform.localRotation = rot;
            }

            //TODO ignore parent rotation and scale
            foreach (var v in Terrains)
            {
                if (!v.isActiveAndEnabled) { continue; }
                if (rootDynamic == null && v.GetComponentInParent<DynamicObject>() != null)
                {
                    //terrain as child of dynamic when exporting scene
                    continue;
                }
                else if (rootDynamic != null && v.GetComponentInParent<DynamicObject>() != rootDynamic)
                {
                    //exporting dynamic, found terrain in some other dynamic
                    continue;
                }

                BakeableMesh bm = new BakeableMesh();

                bm.tempGo = new GameObject(v.gameObject.name);
                bm.tempGo.transform.parent = v.transform;
                //bm.tempGo.transform.localScale = Vector3.one;
                //bm.tempGo.transform.localRotation = Quaternion.identity;
                bm.tempGo.transform.localPosition = Vector3.zero;


                //generate mesh from heightmap
                bm.meshRenderer = bm.tempGo.AddComponent<MeshRenderer>();
                bm.meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                bm.meshRenderer.sharedMaterial.mainTexture = BakeTerrainTexture(path, v.terrainData);
                bm.meshFilter = bm.tempGo.AddComponent<MeshFilter>();
                bm.meshFilter.sharedMesh = GenerateTerrainMesh(v);
                meshes.Add(bm);
            }

            foreach (var v in Canvases)
            {
                if (!v.isActiveAndEnabled) { continue; }
                if (v.renderMode != RenderMode.WorldSpace) { continue; }
                if (rootDynamic == null && v.GetComponentInParent<DynamicObject>() != null)
                {
                    //canvas as child of dynamic when exporting scene
                    continue;
                }
                else if (rootDynamic != null && v.GetComponentInParent<DynamicObject>() != rootDynamic)
                {
                    //exporting dynamic, found canvas in some other dynamic
                    continue;
                }

                BakeableMesh bm = new BakeableMesh();
                bm.tempGo = new GameObject(v.gameObject.name);
                bm.tempGo.transform.parent = v.transform;
                bm.tempGo.transform.localScale = Vector3.one;
                bm.tempGo.transform.localRotation = Quaternion.identity;
                bm.tempGo.transform.localPosition = Vector3.zero;
                bm.meshRenderer = bm.tempGo.AddComponent<MeshRenderer>();
                bm.meshRenderer.sharedMaterial = new Material(Shader.Find("Transparent/Diffuse"));

                var width = v.GetComponent<RectTransform>().sizeDelta.x * v.transform.localScale.x;
                var height = v.GetComponent<RectTransform>().sizeDelta.y * v.transform.localScale.y;

                //bake texture from render
                var screenshot = Snapshot(v.transform);
                screenshot.name = v.gameObject.name.Replace(' ', '_');
                bm.meshRenderer.sharedMaterial.mainTexture = screenshot;

                bm.meshFilter = bm.tempGo.AddComponent<MeshFilter>();
                //write simple quad
                var mesh = ExportQuad(v.gameObject.name + "_canvas", width, height, v.transform, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, screenshot);
                bm.meshFilter.sharedMesh = mesh;
                meshes.Add(bm);
            }
        }

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
            List<GameObject> gos = new List<GameObject>();
            foreach (var v in dynamics)
                gos.Add(v.gameObject);

            Selection.objects = gos.ToArray();
            return ExportSelectedObjectsPrefab();
        }

        public static string RetrieveTexturePath(UnityEngine.Texture texture)
        {
            return AssetDatabase.GetAssetPath(texture);
        }

        static void RecurseThroughChildren(Transform t, List<DynamicObject> dynamics)
        {
            var d = t.GetComponent<DynamicObject>();
            if (d != null)
            {
                dynamics.Add(d);
            }
            for (int i = 0; i < t.childCount; i++)
            {
                RecurseThroughChildren(t.GetChild(i), dynamics);
            }
        }

        public static Mesh ExportQuad(string meshName, float width, float height, Transform transform, string sceneName, Texture2D tex = null)
        {
            //this writes data into a mesh class then passes it through to dynamic object exporter. to futureproof when we move from obj to gltf

            Vector3 size = new Vector3(width, height, 0);
            Vector3 pivot = size / 2;

            Mesh m = new Mesh();
            m.name = meshName;

            //GameObject go = new GameObject("TEMP_MESH");
            //go.AddComponent<MeshFilter>().mesh = m;

            /*//DEBUGGING
            go.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
            if (tex != null)
            {
                go.GetComponent<MeshRenderer>().material.mainTexture = tex;
                go.GetComponent<MeshRenderer>().material.SetInt("_Mode", 2);
            }
            */

            var verts = new Vector3[4];
            verts[0] = new Vector3(0, 0, 0) - pivot;
            verts[1] = new Vector3(width, 0, 0) - pivot;
            verts[2] = new Vector3(0, height, 0) - pivot;
            verts[3] = new Vector3(width, height, 0) - pivot;
            m.vertices = verts;

            var tris = new int[6];
            tris[0] = 0;
            tris[1] = 2;
            tris[2] = 1;
            tris[3] = 2;
            tris[4] = 3;
            tris[5] = 1;
            m.triangles = tris;

            var norms = new Vector3[4];
            norms[0] = -Vector3.forward;
            norms[1] = -Vector3.forward;
            norms[2] = -Vector3.forward;
            norms[3] = -Vector3.forward;
            m.normals = norms;

            //if width == height, 0-1 uvs are fine
            if (Mathf.Approximately(width, height))
            {
                var uvs = new Vector2[4];
                uvs[0] = new Vector2(0, 0);
                uvs[1] = new Vector2(1, 0);
                uvs[2] = new Vector2(0, 1);
                uvs[3] = new Vector2(1, 1);
                m.uv = uvs;
            }
            else if (height > width)
            {
                var uvs = new Vector2[4];
                uvs[0] = new Vector2(0, 0);
                uvs[1] = new Vector2(width / height, 0);
                uvs[2] = new Vector2(0, 1);
                uvs[3] = new Vector2(width / height, 1);
                m.uv = uvs;
            }
            else
            {
                var uvs = new Vector2[4];
                uvs[0] = new Vector2(0, 0);
                uvs[1] = new Vector2(1, 0);
                uvs[2] = new Vector2(0, height / width);
                uvs[3] = new Vector2(1, height / width);
                m.uv = uvs;
            }

            return m;
        }

        public static Texture2D Snapshot(Transform target, int resolution = 128)
        {
            //var sceneview = (SceneView)SceneView.sceneViews[0];
            //target = Selection.activeTransform;

            GameObject cameraGo = new GameObject("Temp_Camera");
            Camera cam = cameraGo.AddComponent<Camera>();

            //put camera in editor camera position

            cameraGo.transform.rotation = target.rotation;
            cameraGo.transform.position = target.position - target.forward * 0.05f;

            var width = target.GetComponent<RectTransform>().sizeDelta.x * target.localScale.x;
            var height = target.GetComponent<RectTransform>().sizeDelta.y * target.localScale.y;
            if (Mathf.Approximately(width, height))
            {
                //whatever. centered
            }
            else if (height > width) //tall
            {
                //half of the difference between width and height
                cameraGo.transform.position += (cameraGo.transform.right) * (height - width) / 2;
            }
            else //wide
            {
                //half of the difference between width and height
                cameraGo.transform.position += (cameraGo.transform.up) * (width - height) / 2;
            }

            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 0.06f;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(target.GetComponent<RectTransform>().sizeDelta.x * target.localScale.x, target.GetComponent<RectTransform>().sizeDelta.y * target.localScale.y) / 2;
            cam.clearFlags = CameraClearFlags.Color; //WANT TO CLEAR EVERYTHING FROM THIS CAMERA
            cam.backgroundColor = Color.clear;

            //create render texture and assign to camera
            RenderTexture rt = RenderTexture.GetTemporary(resolution, resolution, 16); //new RenderTexture(resolution, resolution, 16);
            RenderTexture.active = rt;
            //GL.Clear(true, true, Color.clear);
            cam.targetTexture = rt;
            //GL.Clear(true, true, Color.clear);


            //RenderTexture.active = rt;

            cam.Render();
            //TODO write non-square textures, do full 0-1 uvs instead of saving blank space


            //write rendertexture to png
            Texture2D tex = new Texture2D(resolution, resolution);
            RenderTexture.active = rt;

            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply();

            //GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;

            //delete stuff
            UnityEngine.Object.DestroyImmediate(cameraGo);

            return tex;
        }

        /// <summary>
        /// export selected gameobjects, temporarily spawn them in the scene if they are prefabs
        /// </summary>
        /// <returns>true if exported at least 1 mesh</returns>
        public static bool ExportSelectedObjectsPrefab()
        {
            List<Transform> entireSelection = new List<Transform>();
            entireSelection.AddRange(Selection.GetTransforms(SelectionMode.Editable));

            if (entireSelection.Count == 0)
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "No Dynamic Objects selected", "Ok");
                return false;
            }

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

            //recursively get all dynamic objects to export
            List<DynamicObject> AllDynamics = new List<DynamicObject>();

            //add spawned things to selection
            //List<Transform> t = new List<Transform>(Selection.transforms);
            //t.AddRange(sceneObjects);
            //Selection.objects = t.ToArray();

            foreach (var selected in Selection.transforms)
            {
                //GLTFExportMenu.RecurseThroughChildren(selected, AllDynamics);
            }
            foreach (var selected in sceneObjects)
            {
                RecurseThroughChildren(selected.transform, AllDynamics);
            }

            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar;
            //create directory

            foreach (var dynamic in AllDynamics)
            {
                if (dynamic == null) { Debug.LogError("ExportSelectedObjectsPrefab trying to export null DynamicObject"); continue; }
                if (string.IsNullOrEmpty(dynamic.MeshName)) { Debug.LogError(dynamic.gameObject.name + " Skipping export because of null/empty mesh name", dynamic.gameObject); continue; }

                //TODO remove successfully exported count. not really useful
                if (exportedMeshNames.Contains(dynamic.MeshName)) { successfullyExportedCount++; continue; } //skip exporting same mesh

                foreach (var common in System.Enum.GetNames(typeof(DynamicObject.CommonDynamicMesh)))
                {
                    if (common.ToLower() == dynamic.MeshName.ToLower())
                    {
                        //don't export common meshes!
                        continue;
                    }
                }

                if (!dynamic.UseCustomMesh)
                {
                    //skip exporting a mesh with no name
                    continue;
                }

                Vector3 originalOffset = dynamic.transform.localPosition;
                dynamic.transform.localPosition = Vector3.zero;
                Quaternion originalRot = dynamic.transform.localRotation;
                dynamic.transform.localRotation = Quaternion.identity;

                //bake skin, terrain, canvas

                //Debug.Log("path " + path + dynamic.MeshName + Path.DirectorySeparatorChar + "   mesh " + dynamic.gameObject.name);

                Directory.CreateDirectory(path + dynamic.MeshName + Path.DirectorySeparatorChar);

                List<BakeableMesh> temp = new List<BakeableMesh>();

                //string path2 = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + scene.name;
                BakeNonstandardRenderers(dynamic, temp, path + dynamic.MeshName + Path.DirectorySeparatorChar);

                var exporter = new UnityGLTF.GLTFSceneExporter(new Transform[1] { dynamic.transform }, RetrieveTexturePath, dynamic);
                exporter.SaveGLTFandBin(path + dynamic.MeshName + Path.DirectorySeparatorChar, dynamic.MeshName);

                successfullyExportedCount++;


                for (int i = 0; i < temp.Count; i++)
                {
                    if (temp[i].useOriginalscale)
                        temp[i].meshRenderer.transform.localScale = temp[i].originalScale;
                    DestroyImmediate(temp[i].meshFilter);
                    DestroyImmediate(temp[i].meshRenderer);
                    if (temp[i].tempGo != null)
                        DestroyImmediate(temp[i].tempGo);
                }

                EditorCore.SaveDynamicThumbnailAutomatic(dynamic.gameObject);

                if (!totalExportedMeshNames.Contains(dynamic.MeshName))
                    totalExportedMeshNames.Add(dynamic.MeshName);
                if (!exportedMeshNames.Contains(dynamic.MeshName))
                {
                    exportedMeshNames.Add(dynamic.MeshName);
                }

                //destroy baked skin, terrain, canvases
                dynamic.transform.localPosition = originalOffset;
                dynamic.transform.localRotation = originalRot;

                ResizeQueue.Enqueue(path + dynamic.MeshName + Path.DirectorySeparatorChar);
                EditorApplication.update -= UpdateResize;
                EditorApplication.update += UpdateResize;
            }

            //destroy the temporary prefabs
            foreach (var v in temporarySpawnedPrefabs)
            {
                GameObject.DestroyImmediate(v);
            }

            if (successfullyExportedCount == 0)
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "No Dynamic Objects successfully exported.\n\nDo you have Mesh Renderers, Skinned Mesh Renderers or Canvas components attached or as children?", "Ok");
                return false;
            }
            if (totalExportedMeshNames.Count == 1)
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "Successfully exported 1 Dynamic Object mesh", "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Dynamic Object Export", "From selected Dynamic Objects , found " + totalExportedMeshNames.Count + " unique mesh names", "Ok");
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

        public static Mesh GenerateTerrainMesh(Terrain terrain)
        {
            float downsample = 4;

            Mesh mesh = new Mesh();
            mesh.name = "temp";

            var w = (int)(terrain.terrainData.heightmapWidth / downsample);
            var h = (int)(terrain.terrainData.heightmapHeight / downsample);

            Vector3[] vertices = new Vector3[w * h];
            Vector2[] uv = new Vector2[w * h];
            Vector4[] tangents = new Vector4[w * h];

            //all points

            Vector2 uvScale = new Vector2(1.0f / (w - 1), 1.0f / (w - 1));
            Vector3 sizeScale = new Vector3(terrain.terrainData.size.x / (w - 1), 1/*terrain.terrainData.size.y*/, terrain.terrainData.size.z / (h - 1));

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float pixelHeight = terrain.terrainData.GetHeight((int)(x * downsample), (int)(y * downsample));
                    Vector3 vertex = new Vector3(x, pixelHeight, y);
                    vertices[y * w + x] = Vector3.Scale(sizeScale, vertex);
                    uv[y * w + x] = Vector2.Scale(new Vector2(x, y), uvScale);

                    // Calculate tangent vector: a vector that goes from previous vertex
                    // to next along X direction. We need tangents if we intend to
                    // use bumpmap shaders on the mesh.
                    //Vector3 vertexL = new Vector3(x - 1, heightMap.GetPixel(x - 1, y).grayscale, y);
                    //Vector3 vertexR = new Vector3(x + 1, heightMap.GetPixel(x + 1, y).grayscale, y);
                    //Vector3 tan = Vector3.Scale(sizeScale, vertexR - vertexL).normalized;
                    //tangents[y * w + x] = new Vector4(tan.x, tan.y, tan.z, -1.0f);

                    tangents[y * w + x] = new Vector4(1, 1, 1, -1.0f);
                }
            }

            //generate mesh strips
            // Assign them to the mesh
            mesh.vertices = vertices;
            mesh.uv = uv;

            // Build triangle indices: 3 indices into vertex array for each triangle
            int[] triangles = new int[(h - 1) * (w - 1) * 6];
            int index = 0;
            for (int y = 0; y < h - 1; y++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    // For each grid cell output two triangles
                    triangles[index++] = (y * w) + x;
                    triangles[index++] = ((y + 1) * w) + x;
                    triangles[index++] = (y * w) + x + 1;

                    triangles[index++] = ((y + 1) * w) + x;
                    triangles[index++] = ((y + 1) * w) + x + 1;
                    triangles[index++] = (y * w) + x + 1;
                }
            }

            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.tangents = tangents;

            return mesh;
        }

        public static Texture2D BakeTerrainTexture(string destinationFolder, TerrainData data)
        {
            float[,,] maps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);

            //LIMIT to 6 layers for now! rbga + black + transparency?
            int layerCount = Mathf.Min(maps.GetLength(2), 6);


            //set terrain textures to readable
            bool[] textureReadable = new bool[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                try
                {
                    if (GetTextureImportFormat(data.splatPrototypes[i].texture, out textureReadable[i]))
                    {
                        Texture2D originalTexture = data.splatPrototypes[i].texture as Texture2D;
                        SetTextureImporterFormat(originalTexture, true);
                    }
                }
                catch
                {

                }
            }

            var sizemax = Mathf.Max(
                Mathf.NextPowerOfTwo((int)(data.heightmapScale.z * data.heightmapResolution * 16)),
                Mathf.NextPowerOfTwo((int)(data.heightmapScale.x * data.heightmapResolution * 16)));

            int sizelimit = Mathf.Min(4096, sizemax);

            Texture2D outTex = new Texture2D(sizelimit, sizelimit);

            //Texture2D outTex = new Texture2D(Mathf.Min(4096, (int)(data.heightmapScale.x * data.heightmapResolution * 64)), Mathf.Min(4096, (int)(data.heightmapScale.z * data.heightmapResolution * 64)));
            outTex.name = data.name.Replace(' ', '_');
            float upscalewidth = (float)outTex.width / (float)data.alphamapWidth; //(data.heightmapScale.x * data.heightmapResolution * 64);
            float upscaleheight = (float)outTex.height / (float)data.alphamapHeight;// (data.heightmapScale.z * data.heightmapResolution * 64);

            float[] colorAtLayer = new float[layerCount];
            SplatPrototype[] prototypes = data.splatPrototypes;

            for (int y = 0; y < outTex.height; y++)
            {
                for (int x = 0; x < outTex.width; x++)
                {
                    for (int i = 0; i < colorAtLayer.Length; i++)
                    {
                        colorAtLayer[i] = maps[(int)(x / upscalewidth), (int)(y / upscaleheight), i];
                    }


                    int highestMap = 0;
                    float highestMapValue = 0;

                    for (int i = 0; i < colorAtLayer.Length; i++)
                    {
                        if (colorAtLayer[i] > highestMapValue)
                        {
                            highestMapValue = colorAtLayer[i];
                            highestMap = i;
                        }
                    }

                    if (prototypes.Length > 0 && prototypes[highestMap].texture != null)
                    {
                        //TODO figure out correct tiling for textures
                        Color color = prototypes[highestMap].texture.GetPixel(y, x);
                        outTex.SetPixel(y, x, color);
                    }
                    else
                    {
                        outTex.SetPixel(y, x, Color.red);
                    }
                }
            }

            outTex.Apply();

            //texture importer to original

            for (int i = 0; i < layerCount; i++)
            {
                try
                {
                    bool ignored;
                    if (GetTextureImportFormat(data.splatPrototypes[i].texture, out ignored))
                    {
                        SetTextureImporterFormat(data.splatPrototypes[i].texture, textureReadable[i]);
                    }
                }
                catch
                {

                }
            }

            return outTex;
        }

        public static bool GetTextureImportFormat(Texture2D texture, out bool isReadable)
        {
            isReadable = false;
            if (null == texture)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;

                isReadable = tImporter.isReadable;
                return true;
            }
            return false;
        }

        public static void SetTextureImporterFormat(Texture2D texture, bool isReadable)
        {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;

                tImporter.isReadable = isReadable;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
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
                EditorUtility.DisplayDialog("Dynamic Object Upload Failed", "Could not find the SceneId for \"" + settings.SceneName + "\". Are you sure you've exported and uploaded this scene to SceneExplorer?", "Ok");
                return false;
            }

            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic";
            var subdirectories = Directory.GetDirectories(path);

            //
            List<string> exportDirectories = new List<string>();
            foreach (var v in subdirectories)
            {
                var split = v.Split(Path.DirectorySeparatorChar);

                if (dynamicMeshNames.Contains(split[split.Length - 1]))
                {
                    exportDirectories.Add(v);
                }
            }

            if (ShowPopupWindow)
            {
                int option = EditorUtility.DisplayDialogComplex("Upload Dynamic Objects", "Do you want to upload " + exportDirectories.Count + " Objects to \"" + settings.SceneName + "\" (" + settings.SceneId + " Version:" + settings.VersionNumber + ")?", "Ok", "Cancel", "Open Directory");
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
            string objectNames = "";
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