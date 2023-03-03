using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;

#if C3D_STEAMVR2
using Valve.VR;
using System.IO;
using Valve.Newtonsoft.Json;
#endif

//uploading multiple scenes at once?

namespace Cognitive3D
{
    internal class SceneSetupWindow : EditorWindow
    {
        Rect steptitlerect = new Rect(30, 0, 100, 440);

        internal static void Init()
        {
            SceneSetupWindow window = (SceneSetupWindow)EditorWindow.GetWindow(typeof(SceneSetupWindow), true, "Scene Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();
            initialPlayerSetup = false;

            ExportUtility.ClearUploadSceneSettings();
        }

        enum Page
        {
            ProjectError,
            Welcome,
            PlayerSetup,
            SceneExport,
            SceneUpload,
            SceneUploadProgress,
            SetupComplete
        };
        Page currentPage;

        private void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Equals) { currentPage++; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Minus) { currentPage--; }

            switch (currentPage)
            {
                case Page.ProjectError:
                    ProjectErrorUpdate();
                    break;
                case Page.Welcome:
                    WelcomeUpdate();
                    break;
                case Page.PlayerSetup:
                    ControllerUpdate();
                    break;
                case Page.SceneExport:
                    ExportSceneUpdate();
                    break;
                case Page.SceneUpload:
                    UploadSummaryUpdate();
                    break;
                case Page.SceneUploadProgress:
                    break;
                case Page.SetupComplete:
                    DoneUpdate();
                    break;
                default: break;
            }
            //GUI.Label(steptitlerect, "Scene Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")", "steptitle");

            DrawFooter();
            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        void WelcomeUpdate()
        {
            GUI.Label(steptitlerect, "WELCOME (Version " + Cognitive3D_Manager.SDK_VERSION + ")", "steptitle");

            var settings = Cognitive3D_Preferences.FindCurrentScene();
            GUI.Label(new Rect(30, 45, 440, 440), "Welcome to the " + EditorCore.DisplayValue(DisplayKey.FullName) + " SDK Scene Setup.", "boldlabel");
            GUI.Label(new Rect(30, 100, 440, 440), "There is written documentation and a video guide to help you configure your project for Cognitive3D Analytics.", "normallabel");

            //video link
            if (GUI.Button(new Rect(150, 160, 200, 150), "video"))
            {

            }

            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found == null) //add Cognitive3D_manager
            {
                EditorCore.SpawnManager(EditorCore.DisplayValue(DisplayKey.ManagerName));
            }

            GUI.Label(new Rect(30, 340, 440, 440), "This window will guide you through setting up your player prefab to automatically record controller inputs and export the scene geometry to provide context for your data on SceneExplorer.", "normallabel");
        }

        void ProjectErrorUpdate()
        {
            GUI.Label(new Rect(30, 45, 440, 440), "Project Setup not complete. You must have a developer key set to register a new scene", "normallabel");

            if (GUI.Button(new Rect(130, 175, 240, 30), "Open Project Setup Window"))
            {
                Close();
                ProjectSetupWindow.Init();
            }

            //should check that the dev key is valid. web request or check a cached value
            if (EditorCore.IsDeveloperKeyValid)
            {
                currentPage = Page.Welcome;
            }
        }

#region Controllers

        GameObject leftcontroller;
        GameObject rightcontroller;
        GameObject mainCameraObject;

#if C3D_STEAMVR2
        bool steamvr2bindings = false;
        bool steamvr2actionset = false;
#endif

        //static so it resets on recompile, which will allow PlayerSetupStart to run again
        static bool initialPlayerSetup;
        //called once when entering controller update page. finds/sets expected defaults
        void PlayerSetupStart()
        {
            if (initialPlayerSetup) { return; }
            initialPlayerSetup = true;

            var camera = Camera.main;
            if (camera != null)
            {
                mainCameraObject = camera.gameObject;
            }
            foreach(var dyn in FindObjectsOfType<DynamicObject>())
            {
                if (dyn.IsController && dyn.IsRight)
                {
                    rightcontroller = dyn.gameObject;
                }
                else if (dyn.IsController && !dyn.IsRight)
                {
                    leftcontroller = dyn.gameObject;
                }
            }

            if (leftcontroller != null && rightcontroller != null)
            {
                //found dynamic objects for controllers - prefer to use those
                return;
            }

            //otherwise use SDK specific controller references
            
#if C3D_STEAMVR2
            //interaction system setup
            var player = FindObjectOfType<Valve.VR.InteractionSystem.Player>();
            if (player)
            {
                leftcontroller = player.hands[0].gameObject;
                rightcontroller = player.hands[1].gameObject;
            }
#elif C3D_OCULUS
            //basic setup
            var manager = FindObjectOfType<OVRCameraRig>();
            if (manager != null)
            {
                leftcontroller = manager.leftHandAnchor.gameObject;
                rightcontroller = manager.rightHandAnchor.gameObject;
            }
#elif C3D_VIVEWAVE
            //TODO investigate if automatically detecting vive wave controllers is possible
#elif C3D_PICOVR
            //basic setup
            var manager = FindObjectOfType<Pvr_Controller>();
            if (manager != null)
            {
                if (manager.controller0 != null)
                    leftcontroller = manager.controller0;
                if (manager.controller1 != null)
                    rightcontroller = manager.controller1;
            }
#elif C3D_PICOXR
            //TODO investigate if automatically detecting pico controllers is possible using PicoXR package
#endif
            if (leftcontroller != null && rightcontroller != null)
            {
                //found controllers from VR SDKs
                return;
            }

            //find tracked pose drivers in scene
            var trackedPoseDrivers = FindObjectsOfType<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            foreach (var driver in trackedPoseDrivers)
            {
                if (driver.deviceType == UnityEngine.SpatialTracking.TrackedPoseDriver.DeviceType.GenericXRController && driver.poseSource == UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.RightPose)
                {
                    //right hand
                    rightcontroller = driver.gameObject;
                }
                else if (driver.deviceType == UnityEngine.SpatialTracking.TrackedPoseDriver.DeviceType.GenericXRController && driver.poseSource == UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.LeftPose)
                {
                    //left hand
                    leftcontroller = driver.gameObject;
                }
            }
        }

        void ControllerUpdate()
        {
            PlayerSetupStart();

            GUI.Label(steptitlerect, "PLAYER SETUP", "steptitle");

            GUI.Label(new Rect(30, 45, 440, 440), "Ensure the Player Game Objects are configured.", "normallabel");

            //hmd
            int hmdRectHeight = 170;

            GUI.Label(new Rect(30, hmdRectHeight, 50, 30), "HMD", "boldlabel");
            if (GUI.Button(new Rect(80, hmdRectHeight, 290, 30), mainCameraObject != null? mainCameraObject.gameObject.name:"Missing", "button_blueoutline"))
            {
                Selection.activeGameObject = mainCameraObject;
            }

            int pickerID_HMD = 5689466;
            if (GUI.Button(new Rect(370, hmdRectHeight, 100, 30), "Select..."))
            {
                GUI.skin = null;
                EditorGUIUtility.ShowObjectPicker<GameObject>(
                    mainCameraObject, true, "", pickerID_HMD);
                GUI.skin = EditorCore.WizardGUISkin;
            }
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == pickerID_HMD)
                {
                    mainCameraObject = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                }
            }

            if (Camera.main != null && mainCameraObject != null && mainCameraObject == Camera.main.gameObject)
            {
                GUI.Label(new Rect(320, hmdRectHeight, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(320, hmdRectHeight, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }
            //TODO warning icon if multiple objects tagged with mainCamera in scene

            GUI.Label(new Rect(30, 100, 440, 440), "Cognitive3D requires a camera with the MainCamera tag to function properly", "normallabel");


            bool allSetupConfigured = false;
            bool leftControllerIsValid = false;
            bool rightControllerIsValid = false;

            leftControllerIsValid = leftcontroller != null;
            rightControllerIsValid = rightcontroller != null;

            if (rightControllerIsValid && leftControllerIsValid && Camera.main != null && mainCameraObject == Camera.main.gameObject)
            {
                var rdyn = rightcontroller.GetComponent<DynamicObject>();
                if (rdyn != null && rdyn.IsController && rdyn.IsRight == true)
                {
                    var ldyn = leftcontroller.GetComponent<DynamicObject>();
                    if (ldyn != null && ldyn.IsController && ldyn.IsRight == false)
                    {
                        allSetupConfigured = true;
                    }
                }
            }

            int offset = -35; //indicates how much vertical offset to add to setup features so controller selection has space

            //left hand label
            GUI.Label(new Rect(30, 245 + offset, 50, 30), "Left", "boldlabel");

            string leftname = "Missing";
            if (leftcontroller != null)
                leftname = leftcontroller.gameObject.name;
            if (GUI.Button(new Rect(80, 245 + offset, 290, 30), leftname, "button_blueoutline"))
            {
                Selection.activeGameObject = leftcontroller;
            }

            int pickerID = 5689465;
            if (GUI.Button(new Rect(370, 245 + offset, 100, 30), "Select..."))
            {
                GUI.skin = null;
                EditorGUIUtility.ShowObjectPicker<GameObject>(
                    leftcontroller, true, "", pickerID);
                GUI.skin = EditorCore.WizardGUISkin;
            }
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == pickerID)
                {
                    leftcontroller = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                    Debug.Log("selected " + leftcontroller.name);
                }
            }

            if (leftControllerIsValid)
            {
                GUI.Label(new Rect(320, 245 + offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(320, 245 + offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            //right hand label
            GUI.Label(new Rect(30, 285 + offset, 50, 30), "Right", "boldlabel");

            string rightname = "Missing";
            if (rightcontroller != null)
                rightname = rightcontroller.gameObject.name;

            if (GUI.Button(new Rect(80, 285 + offset, 290, 30), rightname, "button_blueoutline"))
            {
                Selection.activeGameObject = rightcontroller;
            }

            pickerID = 5689469;
            if (GUI.Button(new Rect(370, 285 + offset, 100, 30), "Select..."))
            {
                GUI.skin = null;
                EditorGUIUtility.ShowObjectPicker<GameObject>(
                    rightcontroller, true, "", pickerID);
                GUI.skin = EditorCore.WizardGUISkin;
            }
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == pickerID)
                {
                    rightcontroller = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                    Debug.Log("selected " + rightcontroller.name);
                }
            }

            if (rightControllerIsValid)
            {
                GUI.Label(new Rect(320, 285 + offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(320, 285 + offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            //drag and drop
            if (new Rect(30, 285 + offset, 440, 30).Contains(Event.current.mousePosition)) //right hand
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    rightcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(30, 245 + offset, 440, 30).Contains(Event.current.mousePosition)) //left hand
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    leftcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(30, hmdRectHeight, 440, 30).Contains(Event.current.mousePosition)) //hmd
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    mainCameraObject = (GameObject)DragAndDrop.objectReferences[0];
                }
            }

            if (GUI.Button(new Rect(125, 360 + offset, 250, 30), "Setup Player GameObjects"))
            {
                if (mainCameraObject != null)
                {
                    mainCameraObject.tag = "MainCamera";
                }

                SetupControllers(leftcontroller, rightcontroller);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Event.current.Use();
            }

            if (allSetupConfigured)
            {
                GUI.Label(new Rect(360, 360 + offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(360, 360 + offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

#if C3D_STEAMVR2
            int steamvr2offset = -30;
            GUI.Label(new Rect(135, 390 + steamvr2offset, 300, 20), "You must have an 'actions.json' file generated from SteamVR");
            if (GUI.Button(new Rect(125, 410 + steamvr2offset, 250, 30), "Append Cognitive Action Set"))
            {
                steamvr2actionset = true;
                AppendSteamVRActionSet();
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Event.current.Use();
            }
            if (steamvr2actionset)
            {
                GUI.Label(new Rect(360, 410 + steamvr2offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(360, 410 + steamvr2offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            if (GUI.Button(new Rect(125, 450 + steamvr2offset, 250, 30), "Add Default Bindings"))
            {
                steamvr2bindings = true;
                SetDefaultBindings();
                Event.current.Use();
            }
            if (steamvr2bindings)
            {
                GUI.Label(new Rect(360, 450 + steamvr2offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(360, 450 + steamvr2offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            if (steamvr2bindings && steamvr2actionset && allSetupConfigured)
            {
                GUI.Label(new Rect(105, 480 + steamvr2offset, 300, 20), "Need to open SteamVR Input window and press 'Save and generate' button");
            }
#endif
        }

        public static void SetupControllers(GameObject left, GameObject right)
        {
            Debug.Log("Set Controller Dynamic Object Settings. Create Controller Input Tracker component");

            if (left != null && left.GetComponent<DynamicObject>() == null)
            {
                left.AddComponent<DynamicObject>();
            }
            if (right != null && right.GetComponent<DynamicObject>() == null)
            {
                right.AddComponent<DynamicObject>();
            }

            //add a single controller input tracker to the cognitive3d_manager
            var inputTracker = Cognitive3D_Manager.Instance.gameObject.GetComponent<Components.ControllerInputTracker>();
            if (inputTracker == null)
            {
                Cognitive3D_Manager.Instance.gameObject.AddComponent<Components.ControllerInputTracker>();
            }

            DynamicObject.ControllerType controllerType = DynamicObject.ControllerType.Quest2;
#if C3D_STEAMVR2
                controllerType = DynamicObject.ControllerType.ViveWand;
#elif C3D_OCULUS
                controllerType = DynamicObject.ControllerType.Quest2;
#elif C3D_PICOXR
                controllerType = DynamicObject.ControllerType.PicoNeo3;
#elif C3D_VIVEWAVE
                controllerType = DynamicObject.ControllerType.ViveFocus;
#endif
            
            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.SyncWithPlayerGazeTick = true;
                dyn.FallbackControllerType = controllerType;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.SyncWithPlayerGazeTick = true;
                dyn.FallbackControllerType = controllerType;
            }
        }

#endregion


        void ExportSceneUpdate()
        {
            GUI.Label(steptitlerect, "EXPORT SCENE GEOMETRY", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "All geometry without a <color=#1fa4f2>Dynamic Object</color> component will be exported and uploaded to our dashboard. This will provide context for the spatial data points we automatically collect.\n\nThis process usually only needs to be done once. If you make a major change to your scene geometry (for example, moving a wall), you should export the scene geometry again so the context of the player's behaviour is accurate.\n\nRefer to documentation for more details about exporting scene geometry", "normallabel");

            //GUI.Label(new Rect(200, 380, 200, 30), "Texture Resolution", "miniheader");

            if (Cognitive3D_Preferences.Instance.TextureResize > 4) { Cognitive3D_Preferences.Instance.TextureResize = 4; }

            //draw image
            GUI.Box(new Rect(175, 270, 150, 150), EditorCore.SceneBackground, "image_centered");

            if (EditorCore.HasSceneExportFiles(Cognitive3D_Preferences.FindCurrentScene()))
            {
                float sceneSize = EditorCore.GetSceneFileSize(Cognitive3D_Preferences.FindCurrentScene());
                string displayString;
                if (sceneSize < 1)
                {
                    displayString = "Exported File Size: <1 MB";
                }
                else if (sceneSize > 500)
                {
                    displayString = "<color=red>Warning. Exported File Size: " + string.Format("{0:0}", sceneSize) + " MB.This scene will take a while to upload and view" + ((Cognitive3D_Preferences.Instance.TextureResize != 4) ? "\nConsider lowering texture resolution settings</color>" : "</color>");
                }
                else
                {
                    displayString = "Exported File Size: " + string.Format("{0:0}", sceneSize) + " MB";
                }
                GUI.Label(new Rect(0, 400, 500, 15), displayString, "miniheadercenter");
            }

            if (numberOfLights > 50)
            {
                GUI.Label(new Rect(0, 425, 500, 15), "<color=red>For visualization in SceneExplorer, fewer than 50 lights are recommended</color>", "miniheadercenter");
            }

            if (GUI.Button(new Rect(150, 455, 200, 35), "Export Scene Geometry"))
            {
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                {
                    if (EditorUtility.DisplayDialog("Export Failed", "Cannot export scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
                    {
                        if (UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                        {
                        }
                        else
                        {
                            return;//cancel from save scene window
                        }
                    }
                    else
                    {
                        return;//cancel from 'do you want to save' popup
                    }
                }
                ExportUtility.ExportGLTFScene();

                string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                string objPath = EditorCore.GetSubDirectoryPath(fullName);
                string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\"" + fullName + "\",\"sdkVersion\":\"" + Cognitive3D_Manager.SDK_VERSION + "\"}";
                System.IO.File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

                DebugInformationWindow.WriteDebugToFile(objPath + "debug.log");

                Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorUtility.SetDirty(EditorCore.GetPreferences());

                UnityEditor.AssetDatabase.SaveAssets();
                EditorCore.RefreshSceneVersion(null);
            }

            //texture resolution settings
            Rect toolsRect = new Rect(360, 455, 35, 35);
            if (GUI.Button(toolsRect, EditorCore.SettingsIcon,"image_centered")) //rename dropdown
            {
                //drop down menu?
                GenericMenu gm = new GenericMenu();
                gm.AddItem(new GUIContent("Full Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 1, OnSelectFullResolution);
                gm.AddItem(new GUIContent("Half Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 2, OnSelectHalfResolution);
                gm.AddItem(new GUIContent("Quarter Texture Resolution"), Cognitive3D_Preferences.Instance.TextureResize == 4, OnSelectQuarterResolution);
                gm.AddSeparator("");
                gm.AddItem(new GUIContent("Export lowest LOD meshes"), Cognitive3D_Preferences.Instance.ExportSceneLODLowest, OnToggleLODMeshes);

#if UNITY_2020_1_OR_NEWER
                gm.AddItem(new GUIContent("Include Disabled Dynamic Objects"), Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects, ToggleIncludeDisabledObjects);
#endif
                gm.ShowAsContext();
            }

        }

        void OnSelectFullResolution()
        {
            Cognitive3D_Preferences.Instance.TextureResize = 1;
        }
        void OnSelectHalfResolution()
        {
            Cognitive3D_Preferences.Instance.TextureResize = 2;
        }
        void OnSelectQuarterResolution()
        {
            Cognitive3D_Preferences.Instance.TextureResize = 4;
        }
        void OnToggleLODMeshes()
        {
            Cognitive3D_Preferences.Instance.ExportSceneLODLowest = !Cognitive3D_Preferences.Instance.ExportSceneLODLowest;
        }

        int numberOfLights = 0;

        bool UploadSceneGeometry = true;
        bool UploadThumbnail = true;
        bool UploadDynamicMeshes = true;

        bool SceneExistsOnDashboard;
        bool SceneHasExportFiles;

        void UploadSummaryUpdate()
        {
            GUI.Label(steptitlerect, "UPLOAD", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "The following will be uploaded to the Dashboard:", "normallabel");

            int sceneVersion = 0;
            var settings = Cognitive3D_Preferences.FindCurrentScene();
            if (settings != null && !string.IsNullOrEmpty(settings.SceneId)) //has been uploaded. this is a new version
            {
                SceneExistsOnDashboard = true;
                sceneVersion = settings.VersionNumber;
            }

            SceneHasExportFiles = EditorCore.HasSceneExportFiles(Cognitive3D_Preferences.FindCurrentScene());

            var uploadSceneRect = new Rect(30, 200, 30, 30);
            if (!SceneHasExportFiles)
            {
                //disable 'upload scene geometry' toggle
                GUI.Button(uploadSceneRect, EditorCore.EmptyCheckmark, "image_centered");
                GUI.Label(new Rect(60, 202, 400, 30), "Upload Scene Geometry (No files exported)", "normallabel");
                UploadSceneGeometry = false;
            }
            else
            {
                //upload scene geometry
                if (UploadSceneGeometry)
                {
                    if (GUI.Button(uploadSceneRect, EditorCore.BlueCheckmark, "image_centered"))
                    {
                        UploadSceneGeometry = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadSceneRect, EditorCore.EmptyBlueCheckmark, "image_centered"))
                    {
                        UploadSceneGeometry = true;
                    }
                }
                string uploadGeometryText = "Upload Scene Geometry";
                if (SceneExistsOnDashboard)
                {
                    uploadGeometryText = "Upload Scene Geometry (Version " + (sceneVersion+1) + ")";
                }
                GUI.Label(new Rect(60, 202, 400, 30), uploadGeometryText, "normallabel");
            }

            var uploadThumbnailRect = new Rect(30, 240, 30, 30);
            if (!EditorCore.HasSceneThumbnail(settings) && (!SceneExistsOnDashboard && !SceneHasExportFiles))
            {
                //disable 'upload scene geometry' toggle
                GUI.Button(uploadThumbnailRect, EditorCore.EmptyCheckmark, "image_centered");
                GUI.Label(new Rect(60, 242, 300, 30), "Upload Scene Thumbnail (No image exists)", "normallabel");
            }
            else
            {
                //upload thumbnail
                if (UploadThumbnail)
                {
                    if (GUI.Button(uploadThumbnailRect, EditorCore.BlueCheckmark, "image_centered"))
                    {
                        UploadThumbnail = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadThumbnailRect, EditorCore.EmptyBlueCheckmark, "image_centered"))
                    {
                        UploadThumbnail = true;
                    }
                }
                GUI.Label(new Rect(60, 242, 300, 30), "Upload Scene Thumbnail", "normallabel");
            }

            //upload dynamics
            int dynamicObjectCount = EditorCore.GetExportedDynamicObjectNames().Count;
            var uploadDynamicRect = new Rect(30, 280, 30, 30);

            if (!SceneExistsOnDashboard && (!UploadSceneGeometry))
            {
                //can't upload dynamics
                GUI.Button(uploadDynamicRect, EditorCore.EmptyCheckmark, "image_centered");
                GUI.Label(new Rect(60, 282, 300, 30), "Upload " + dynamicObjectCount + " Dynamic Meshes (No Scene Exists)", "normallabel");
            }
            else
            {
                //upload dynamics toggle
                if (UploadDynamicMeshes)
                {
                    if (GUI.Button(uploadDynamicRect, EditorCore.BlueCheckmark, "image_centered"))
                    {
                        UploadDynamicMeshes = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadDynamicRect, EditorCore.EmptyBlueCheckmark, "image_centered"))
                    {
                        UploadDynamicMeshes = true;
                    }
                }
                GUI.Label(new Rect(60, 282, 300, 30), "Upload " + dynamicObjectCount + " Dynamic Meshes", "normallabel");
            }


            //debug
            //GUI.Label(new Rect(30, 100, 440, 20), SceneHasExportFiles + " scene has export files");
            //GUI.Label(new Rect(30, 120, 440, 20), SceneExistsOnDashboard + " exists on dashboard");
            //GUI.Label(new Rect(30,110,440,20), UploadSceneGeometry + " upload scene geo");
            //GUI.Label(new Rect(30, 140, 440, 20), (SceneExistsOnDashboard || UploadSceneGeometry) + " will be uploading scene files");

            //scene thumbnail preview
            var thumbnailRect = new Rect(175, 330, 150, 150);
            if (UploadThumbnail)
            {
                GUI.Label(new Rect(195, 480, 150, 20), "Thumbnail from Scene View");
                var sceneRT = EditorCore.GetSceneRenderTexture();
                if (sceneRT != null)
                    GUI.Box(thumbnailRect, sceneRT, "image_centeredboxed");
                else
                    GUI.Box(thumbnailRect, "Scene view not found", "image_centeredboxed");
            }
            else
            {
                //if a new scene version is uploaded, can it use the previous thumbnail?
                GUI.Box(thumbnailRect, "Thumbnail not uploaded", "image_centeredboxed");
            }
        }
        
        void DoneUpdate()
        {
            GUI.Label(steptitlerect, "DONE!", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "The <color=#8A9EB7FF>" + EditorCore.DisplayValue(DisplayKey.ManagerName) + "</color> in your scene will record user position, gaze and basic device information.\n\nYou can view sessions from the Dashboard.", "boldlabel");
            if (GUI.Button(new Rect(150, 150, 200, 40), "Open Dashboard", "button_bluetext"))
            {
                Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Dashboard);
            }

            GUI.Label(new Rect(30, 385, 440, 440), "Make sure your users understand your experience with a simple training scene. Add the Ready Room sample to your project", "boldlabel");
            if (GUI.Button(new Rect(150, 440, 200, 40), "Ready Room Setup", "button_bluetext"))
            {
                //can i open a specific package from the package manager window
                var readyRoomScenes = AssetDatabase.FindAssets("t:scene readyroom");
                if (readyRoomScenes.Length == 1)
                {
                    //ask if want save
                    if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(readyRoomScenes[0]));
                        Close();
                    }
                }
            }

            GUI.Label(new Rect(30, 205, 440, 440), "-Want to ask users about their experience?\n-Need to add more Dynamic Objects?\n-Have some Sensors?\n-Tracking user's gaze on a video or image?\n-Multiplayer?\n", "boldlabel");
            if (GUI.Button(new Rect(150, 320, 200, 40), "Open Documentation", "button_bluetext"))
            {
                Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Documentation);
            }
        }

        void DrawFooter()
        {
            GUI.color = EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            
            DrawBackButton();
            DrawNextButton();
        }

        void DrawNextButton()
        {
            bool buttonDisabled = false;
            bool appearDisabled = false; //used on dynamic upload page to skip step
            string text = "Next";
            System.Action onclick = () => currentPage++;
            Rect buttonrect = new Rect(410, 510, 80, 30);

            switch (currentPage)
            {
                case Page.Welcome:
                    break;
                case Page.PlayerSetup:
                    onclick += () => { numberOfLights = FindObjectsOfType<Light>().Length; };
                    break;
                case Page.SceneExport:
                    appearDisabled = !EditorCore.HasSceneExportFiles(Cognitive3D_Preferences.FindCurrentScene());

                    if (appearDisabled)
                    {
                        onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without exporting your scene?", "Yes", "No")) { currentPage++; } };
                    }
                    onclick += () => Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    text = "Next";
                    break;
                case Page.SceneUpload:
                    System.Action completedmanifestupload = delegate ()
                    {
                        ExportUtility.UploadAllDynamicObjectMeshes(true);
                        currentPage++;
                    };

                    //fifth upload manifest
                    System.Action completedRefreshSceneVersion = delegate ()
                    {
                        //TODO ask if dev wants to upload disabled dynamic objects as well (if there are any)
                        AggregationManifest manifest = new AggregationManifest();
                        DynamicObjectsWindow.AddOrReplaceDynamic(manifest, GetDynamicObjectsInScene());
                        DynamicObjectsWindow.UploadManifest(manifest, completedmanifestupload, completedmanifestupload);
                    };

                    //fourth upload dynamics
                    System.Action completeSceneUpload = delegate () {
                        EditorCore.RefreshSceneVersion(completedRefreshSceneVersion); //likely completed in previous step, but just in case
                    };

                    //third upload scene
                    System.Action completeScreenshot = delegate () {

                        Cognitive3D_Preferences.SceneSettings current = Cognitive3D_Preferences.FindCurrentScene();

                        if (current == null || string.IsNullOrEmpty(current.SceneId))
                        {
                            if (EditorUtility.DisplayDialog("Upload New Scene", "Upload " + current.SceneName + " to " + EditorCore.DisplayValue(DisplayKey.ViewerName) + "?", "Ok", "Cancel"))
                            {
                                //new scene
                                ExportUtility.UploadDecimatedScene(current, completeSceneUpload);
                            }
                        }
                        else
                        {
                            //new version
                            if (EditorUtility.DisplayDialog("Upload New Version", "Upload a new version of this existing scene? Will archive previous version", "Ok", "Cancel"))
                            {
                                ExportUtility.UploadDecimatedScene(current, completeSceneUpload);
                            }
                        }
                    };

                    //second save screenshot
                    System.Action completedRefreshSceneVersion1 = delegate ()
                    {
                        if (UploadThumbnail)
                        {
                            EditorCore.SaveScreenshot(EditorCore.GetSceneRenderTexture(), UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
                        }
                        else
                        {
                            EditorCore.DeleteSceneThumbnail(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                            completeScreenshot.Invoke();
                            completeScreenshot = null;
                        }
                    };

                    //only do this if uploading new scene files
                    //first refresh scene version
                    onclick = () =>
                    {
                        if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) //scene not saved. "do want save?" popup
                        {
                            if (EditorUtility.DisplayDialog("Upload Paused", "Cannot upload scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
                            {
                                if (UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                                {
                                    EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
                                }
                                else
                                {
                                    return;//cancel from save scene window
                                }
                            }
                            else
                            {
                                return;//cancel from 'do you want to save' popup
                            }
                        }
                        else
                        {
                            EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
                        }
                    };

                    buttonDisabled = !(SceneExistsOnDashboard || (SceneHasExportFiles && UploadSceneGeometry));
                    text = "Upload";
                    break;
                case Page.SetupComplete:
                    onclick = () => Close();
                    text = "Close";
                    break;
                case Page.ProjectError:
                    buttonrect = new Rect(600, 0, 0, 0);
                    break;
            }

            if (appearDisabled)
            {
                if (GUI.Button(buttonrect, text, "button_disabled"))
                {
                    onclick.Invoke();
                }
            }
            else if (buttonDisabled)
            {
                GUI.Button(buttonrect, text, "button_disabled");
            }
            else
            {
                if (GUI.Button(buttonrect, text))
                {
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }

        void DrawBackButton()
        {
            bool buttonDisabled = false;
            string text = "Back";
            System.Action onclick = () => currentPage--;
            Rect buttonrect = new Rect(10, 510, 80, 30);

            switch (currentPage)
            {
                case Page.Welcome: buttonDisabled = true; break;
                case Page.PlayerSetup:
                    text = "Back";
                    break;
                case Page.SceneExport:
                    break;
                case Page.SceneUpload:
                    break;
                case Page.SceneUploadProgress:
                case Page.SetupComplete:
                    buttonDisabled = true;
                    onclick = null;
                    break;
                case Page.ProjectError:
                    buttonrect = new Rect(600, 0, 0, 0);
                    break;
                default: break;
            }

            if (buttonDisabled)
            {
                GUI.Button(buttonrect, text, "button_disabledtext");
            }
            else
            {
                if (GUI.Button(buttonrect, text))
                {
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }

        List<DynamicObject> GetDynamicObjectsInScene()
        {
            return new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
        }

#if C3D_STEAMVR2
        internal static void AppendSteamVRActionSet()
        {
            SteamVR_Input_ActionFile actionfile;
            if (LoadActionFile(out actionfile))
            {
                var cognitiveActionSet = new SteamVR_Input_ActionFile_ActionSet() { name = "/actions/C3D_Input", usage = "single" };

                //if actions.json already contains cognitive action set
                if (actionfile.action_sets.Contains(cognitiveActionSet))
                {
                    Debug.Log("SteamVR action set already contains Cognitive Action Set. Skip adding action set to actions.json");
                    return;
                }

                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/C3D_Input/in/Grip", type = "boolean" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/C3D_Input/in/Trigger", type = "vector1" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/C3D_Input/in/Touchpad", type = "vector2" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/C3D_Input/in/Touchpad_Press", type = "boolean" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/C3D_Input/in/Touchpad_Touch", type = "boolean" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/C3D_Input/in/Menu", type = "boolean" });
                actionfile.action_sets.Add(cognitiveActionSet);

                SaveActionFile(actionfile);
                Debug.Log("InitWizard::AppendSteamVRActionSet Added Cognitive3D Action Set");
            }
            else
            {
                Debug.LogError("InitWizard::AppendSteamVRActionSet SteamVR LoadActionFile failed!");
            }
        }

        static bool LoadActionFile(out SteamVR_Input_ActionFile actionFile)
        {
            string actionsFilePath = SteamVR_Input.GetActionsFileFolder(true) + "/actions.json";
            if (!File.Exists(actionsFilePath))
            {
                Debug.LogErrorFormat("<b>[SteamVR]</b> Actions file doesn't exist: {0}", actionsFilePath);
                actionFile = new SteamVR_Input_ActionFile();
                return false;
            }

            actionFile = JsonConvert.DeserializeObject<SteamVR_Input_ActionFile>(File.ReadAllText(actionsFilePath));

            return true;
        }

        static bool SaveActionFile(SteamVR_Input_ActionFile actionFile)
        {
            string actionsFilePath = SteamVR_Input.GetActionsFileFolder(true) +"/actions.json";
            string newJSON = JsonConvert.SerializeObject(actionFile, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            File.WriteAllText(actionsFilePath, newJSON);

            Debug.Log("saved " + SteamVR_Settings.instance.actionsFilePath + " with Cognitive3D input action set");

            return true;
        }

        internal static void SetDefaultBindings()
        {
            SteamVR_Input_BindingFile bindingfile;
            if (LoadBindingFile(out bindingfile))
            {
                if (bindingfile.bindings.ContainsKey("/actions/c3d_input"))
                {
                    bindingfile.bindings.Remove("/actions/c3d_input");
                }

                SteamVR_Input_BindingFile_ActionList actionlist = new SteamVR_Input_BindingFile_ActionList();

                actionlist.sources.Add(createSource("button", "/user/hand/left/input/grip","click", "/actions/c3d_input/in/grip"));
                actionlist.sources.Add(createSource("button", "/user/hand/right/input/grip", "click", "/actions/c3d_input/in/grip"));

                actionlist.sources.Add(createSource("button", "/user/hand/left/input/menu","click", "/actions/c3d_input/in/menu"));
                actionlist.sources.Add(createSource("button", "/user/hand/right/input/menu", "click", "/actions/c3d_input/in/menu"));

                actionlist.sources.Add(createSource("trigger", "/user/hand/left/input/trigger", "pull", "/actions/c3d_input/in/trigger"));
                actionlist.sources.Add(createSource("trigger", "/user/hand/right/input/trigger", "pull", "/actions/c3d_input/in/trigger"));

                //left touchpad
                SteamVR_Input_BindingFile_Source bindingSource_left_pad = new SteamVR_Input_BindingFile_Source();
                bindingSource_left_pad.mode = "trackpad";
                bindingSource_left_pad.path = "/user/hand/left/input/trackpad";
                {
                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_press = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_press.Add("output", "/actions/c3d_input/in/touchpad_press");
                    bindingSource_left_pad.inputs.Add("click", stringDictionary_press);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_touch = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_touch.Add("output", "/actions/c3d_input/in/touchpad_touch");
                    bindingSource_left_pad.inputs.Add("touch", stringDictionary_touch);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_pos = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_pos.Add("output", "/actions/c3d_input/in/touchpad");
                    bindingSource_left_pad.inputs.Add("position", stringDictionary_pos);
                }
                actionlist.sources.Add(bindingSource_left_pad);

                //right touchpad
                SteamVR_Input_BindingFile_Source bindingSource_right_pad = new SteamVR_Input_BindingFile_Source();
                bindingSource_right_pad.mode = "trackpad";
                bindingSource_right_pad.path = "/user/hand/right/input/trackpad";
                {
                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_press = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_press.Add("output", "/actions/c3d_input/in/touchpad_press");
                    bindingSource_right_pad.inputs.Add("click", stringDictionary_press);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_touch = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_touch.Add("output", "/actions/c3d_input/in/touchpad_touch");
                    bindingSource_right_pad.inputs.Add("touch", stringDictionary_touch);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_pos = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_pos.Add("output", "/actions/c3d_input/in/touchpad");
                    bindingSource_right_pad.inputs.Add("position", stringDictionary_pos);
                }
                actionlist.sources.Add(bindingSource_right_pad);

                bindingfile.bindings.Add("/actions/c3d_input", actionlist);
                Debug.Log("InitWizard::SetDefaultBindings save Cognitive3D input bindings");
                SaveBindingFile(bindingfile);
            }
            else
            {
                Debug.Log("InitWizard::SetDefaultBindings failed to load steamvr actions");
            }
        }

        //mode = button, path = "/user/hand/left/input/grip", actiontype = "click", action = "/actions/c3d_input/in/grip"
        static SteamVR_Input_BindingFile_Source createSource(string mode, string path, string actiontype, string action)
        {
            SteamVR_Input_BindingFile_Source bindingSource = new SteamVR_Input_BindingFile_Source();
            bindingSource.mode = mode;
            bindingSource.path = path;

            SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
            stringDictionary.Add("output", action);
            bindingSource.inputs.Add(actiontype, stringDictionary);

            return bindingSource;
        }

        static bool LoadBindingFile(out SteamVR_Input_BindingFile bindingfile)
        {
            string bindingFilePath = SteamVR_Input.GetActionsFileFolder(true) + "/bindings_vive_controller.json";
            if (!File.Exists(bindingFilePath))
            {
                Debug.LogErrorFormat("<b>[SteamVR]</b> binding file doesn't exist: {0}", bindingFilePath);
                bindingfile = new SteamVR_Input_BindingFile();
                return false;
            }

            bindingfile = JsonConvert.DeserializeObject<SteamVR_Input_BindingFile>(File.ReadAllText(bindingFilePath));

            return true;
        }

        static bool SaveBindingFile(SteamVR_Input_BindingFile bindingfile)
        {
            string bindingFilePath = SteamVR_Input.GetActionsFileFolder(true) + "/bindings_vive_controller.json";

            string newJSON = JsonConvert.SerializeObject(bindingfile, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            File.WriteAllText(bindingFilePath, newJSON);

            Debug.Log("saved default bindings for Cognitive3D inputs");

            return true;
        }
#endif
    }
}