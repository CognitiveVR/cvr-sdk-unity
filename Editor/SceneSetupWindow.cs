﻿using System.Collections;
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
        Rect steptitlerect = new Rect(30, 5, 100, 440);
        internal static void Init()
        {
            SceneSetupWindow window = (SceneSetupWindow)EditorWindow.GetWindow(typeof(SceneSetupWindow), true, "Scene Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();
            initialPlayerSetup = false;

            ExportUtility.ClearUploadSceneSettings();
        }
        internal static void Init(Rect position)
        {
            SceneSetupWindow window = (SceneSetupWindow)EditorWindow.GetWindow(typeof(SceneSetupWindow), true, "Scene Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.position = new Rect(position.x+5, position.y+5, 500, 550);
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
                    UploadProgressUpdate();
                    break;
                case Page.SetupComplete:
                    DoneUpdate();
                    break;
                default: break;
            }

            DrawFooter();
            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        private void OnFocus()
        {
            hasCheckedForSteamVRActionsSet = false;
        }

        void WelcomeUpdate()
        {
            GUI.Label(steptitlerect, "INTRODUCTION", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "Welcome to the Cognitive3D <b>Scene Setup</b>. This window will guide you through basic configuration to ensure your scene is ready to record data.\n\nThis will include:", "normallabel");
            GUI.Label(new Rect(30, 140, 440, 440), "- Ensure player prefab is configured\n- Set up controller inputs\n- Export Scene Geometry to SceneExplorer","normallabel");
            GUI.Label(new Rect(30, 220, 440, 440), "For a guided walkthrough, you can follow the video below:", "normallabel");

            //video link
            if (GUI.Button(new Rect(150, 300, 200, 150), "video"))
            {
                Application.OpenURL("https://vimeo.com/cognitive3d/videos");
            }

            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found == null) //add Cognitive3D_manager
            {
                EditorCore.SpawnManager(EditorCore.DisplayValue(DisplayKey.ManagerName));
            }
        }

        void ProjectErrorUpdate()
        {
            GUI.Label(steptitlerect, "PROJECT SETUP NOT COMPLETE", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "You must have a Developer Key to set up a new scene.", "normallabel");

            if (GUI.Button(new Rect(150, 100, 200, 30), "Open Project Setup Window"))
            {
                Close();
                ProjectSetupWindow.Init(position);
            }

            //skip this screen if the developer key is valid
            if (EditorCore.IsDeveloperKeyValid)
            {
                currentPage = Page.Welcome;
            }
        }

#region Controllers

        GameObject leftcontroller;
        GameObject rightcontroller;
        GameObject mainCameraObject;

        static bool initialPlayerSetup;
        //called once when entering controller update page. finds/sets expected defaults
        void PlayerSetupStart()
        {
            GUI.Label(steptitlerect, "PLAYER SETUP", "steptitle");
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

        bool AllControllerSetupComplete = false;
        void ControllerUpdate()
        {
            PlayerSetupStart();
            GUI.Label(new Rect(30, 30, 440, 440), "You can use your existing Player Prefab. For most implementations, this is just a quick check to ensure cameras and controllers are configued correctly.", "normallabel");
            GUI.Label(new Rect(30, 100, 440, 440), "The display for the HMD should be tagged as <b>MainCamera</b>", "normallabel");

            //hmd
            int hmdRectHeight = 150;

            GUI.Label(new Rect(30, hmdRectHeight, 50, 30), "HMD", "boldlabel");
            if (GUI.Button(new Rect(130, hmdRectHeight, 310, 30), mainCameraObject != null? mainCameraObject.gameObject.name:"Missing", "button_blueoutline"))
            {
                Selection.activeGameObject = mainCameraObject;
            }

            int pickerID_HMD = 5689466;
            if (GUI.Button(new Rect(440, hmdRectHeight, 30, 30), EditorCore.SearchIconWhite))
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

            Rect hmdAlertRect = new Rect(400, hmdRectHeight, 30, 30);
            if (mainCameraObject == null)
            {
                GUI.Label(hmdAlertRect, new GUIContent(EditorCore.Alert, "Camera GameObject not set"), "image_centered");
            }
            else if (mainCameraObject.CompareTag("MainCamera") == false)
            {
                GUI.Label(hmdAlertRect, new GUIContent(EditorCore.Alert, "Selected Camera is not tagged 'MainCamera'"), "image_centered");
            }
            else
            {
                //warning icon if multiple objects tagged with mainCamera in scene
                int mainCameraCount = 0;
                for (int i = 0; i < Camera.allCamerasCount; i++)
                {
                    if (Camera.allCameras[i].CompareTag("MainCamera"))
                    {
                        mainCameraCount++;
                    }
                }
                if (mainCameraCount > 1)
                {
                    GUI.Label(hmdAlertRect, new GUIContent(EditorCore.Alert, "Multiple cameras are tagged 'MainCamera'. This may cause runtime issues"), "image_centered");
                }
            }

            //controllers
#if C3D_STEAMVR2
            GUI.Label(new Rect(30, 200, 440, 440), "The Controllers should have <b>SteamVR Behaviour Pose</b> components", "normallabel");
#else
            GUI.Label(new Rect(30, 200, 440, 440), "The Controllers should have <b>Tracked Pose Driver</b> components", "normallabel");
#endif

            bool leftControllerIsValid = false;
            bool rightControllerIsValid = false;

            leftControllerIsValid = leftcontroller != null;
            rightControllerIsValid = rightcontroller != null;

            AllControllerSetupComplete = false;
            if (rightControllerIsValid && leftControllerIsValid && Camera.main != null && mainCameraObject == Camera.main.gameObject)
            {
                var rdyn = rightcontroller.GetComponent<DynamicObject>();
                if (rdyn != null && rdyn.IsController && rdyn.IsRight == true)
                {
                    var ldyn = leftcontroller.GetComponent<DynamicObject>();
                    if (ldyn != null && ldyn.IsController && ldyn.IsRight == false)
                    {
                        AllControllerSetupComplete = true;
                    }
                }
            }
            int handOffset = 240;

            //left hand label
            GUI.Label(new Rect(30, handOffset + 15, 50, 30), "Left", "boldlabel");

            string leftname = "Missing";
            if (leftcontroller != null)
                leftname = leftcontroller.gameObject.name;
            if (GUI.Button(new Rect(130, handOffset + 15, 310, 30), leftname, "button_blueoutline"))
            {
                Selection.activeGameObject = leftcontroller;
            }

            int pickerID = 5689465;
            if (GUI.Button(new Rect(440, handOffset + 15, 30, 30), EditorCore.SearchIconWhite))
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
                }
            }

            if (!leftControllerIsValid)
            {
                GUI.Label(new Rect(400, handOffset + 15, 30, 30), EditorCore.Alert, "image_centered");
            }

            //right hand label
            GUI.Label(new Rect(30, handOffset + 50, 50, 30), "Right", "boldlabel");

            string rightname = "Missing";
            if (rightcontroller != null)
                rightname = rightcontroller.gameObject.name;

            if (GUI.Button(new Rect(130, handOffset + 50, 310, 30), rightname, "button_blueoutline"))
            {
                Selection.activeGameObject = rightcontroller;
            }

            pickerID = 5689469;
            if (GUI.Button(new Rect(440, handOffset + 50, 30, 30), EditorCore.SearchIconWhite))
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
                }
            }

            if (!rightControllerIsValid)
            {
                GUI.Label(new Rect(400, handOffset + 50, 30, 30), EditorCore.Alert, "image_centered");
            }

            //drag and drop
            if (new Rect(30, handOffset + 50, 440, 30).Contains(Event.current.mousePosition)) //right hand
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    rightcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(30, handOffset + 15, 440, 30).Contains(Event.current.mousePosition)) //left hand
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

            if (GUI.Button(new Rect(150, 340, 200, 30), "Setup Controller GameObjects"))
            {
                SetupControllers(leftcontroller, rightcontroller);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Event.current.Use();
            }

            if (AllControllerSetupComplete)
            {
                GUI.Label(new Rect(120, 340, 30, 30), EditorCore.CircleCheckmark32, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(120, 340, 30, 30), EditorCore.CircleEmpty32, "image_centered");
            }
#if C3D_STEAMVR2

            if (GUI.Button(new Rect(150, 380, 200, 30), "Open SteamVR Input"))
            {
                //may show a popup asking to generate default action.json
                Valve.VR.SteamVR_Input_EditorWindow.ShowWindow();
            }

            //GUI.Label(new Rect(100, 372, 300, 20), "You must have an 'actions.json' file generated from SteamVR");
            if (GUI.Button(new Rect(150, 420, 200, 30), "Append Cognitive Action Set"))
            {
                AppendSteamVRActionSet();
                SetDefaultBindings();
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }

            if (GUI.Button(new Rect(150, 460, 200, 30), "Save and Generate Action Sets"))
            {
                Valve.VR.SteamVR_Input.actionFile.SaveHelperLists();
                Valve.VR.SteamVR_Input.actionFile.Save(SteamVR_Input.GetActionsFilePath());
                Valve.VR.SteamVR_Input_ActionManifest_Manager.CleanBindings(true);
                Debug.Log("<b>[SteamVR Input]</b> Saved actions manifest successfully.");
                Valve.VR.SteamVR_Input_Generator.BeginGeneration();
            }
            if (DoesC3DInputActionSetExist())
            {
                GUI.Label(new Rect(120, 460, 30, 30), EditorCore.CircleCheckmark32, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(120, 460, 30, 30), EditorCore.CircleEmpty32, "image_centered");
            }
#endif
        }

        bool hasCheckedForSteamVRActionsSet;
        bool hasFoundSteamVRActionSet;
        //used with steamvr
        bool DoesC3DInputActionSetExist()
        {
            if (!hasCheckedForSteamVRActionsSet)
            {
                hasFoundSteamVRActionSet = false;
                hasCheckedForSteamVRActionsSet = true;
                string className = "Valve.VR.SteamVR_Input_ActionSet_C3D_Input";

                System.Reflection.Assembly steamvrActionAssembly = null;
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var a in assemblies)
                {
                    if (a.GetName().Name == "SteamVR_Actions")
                    {
                        steamvrActionAssembly = a;
                        break;
                    }
                }
                if (steamvrActionAssembly == null)
                {
                    return false;
                }
                var t = steamvrActionAssembly.GetType(className);
                if (t == null)
                {
                    return false;
                }
                hasFoundSteamVRActionSet = true;
                return true;
            }
            else
            {
                return hasFoundSteamVRActionSet;
            }
        }

        public static void SetupControllers(GameObject left, GameObject right)
        {
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
                Debug.Log("Set Controller Dynamic Object Settings. Create Controller Input Tracker component");
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
            GUI.Label(steptitlerect, "SCENE EXPORT", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "All geometry without a <b>Dynamic Object</b> component will be exported and uploaded to our dashboard. This will provide context for the spatial data points we automatically collect.\n\nRefer to Online Documentation for more details about exporting scene geometry.", "normallabel");
            if (Cognitive3D_Preferences.Instance.TextureResize > 4) { Cognitive3D_Preferences.Instance.TextureResize = 4; }

            //draw example scene image
            GUI.Box(new Rect(175, 230, 150, 150), EditorCore.SceneBackground, "image_centered");

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
                GUI.Label(new Rect(0, 365, 500, 15), displayString, "miniheadercenter");
            }

            if (numberOfLights > 50)
            {
                GUI.Label(new Rect(0, 405, 500, 15), "<color=red>For visualization in SceneExplorer, fewer than 50 lights are recommended</color>", "miniheadercenter");
            }

            if (GUI.Button(new Rect(150, 440, 200, 30), "Export Scene Geometry"))
            {
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                {
                    if (EditorUtility.DisplayDialog("Export Paused", "Cannot export scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
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
            Rect toolsRect = new Rect(360, 440, 35, 35);
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
                gm.AddItem(new GUIContent("Include Disabled Dynamic Objects"), Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects, OnToggleIncludeDisabledDynamics);
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

        //Unity 2020.1+
        void OnToggleIncludeDisabledDynamics()
        {
            Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects = !Cognitive3D_Preferences.Instance.IncludeDisabledDynamicObjects;
        }

        int numberOfLights = 0;

        bool UploadSceneGeometry = true;
        bool UploadThumbnail = true;
        bool UploadDynamicMeshes = true;

        bool SceneExistsOnDashboard;
        bool SceneHasExportFiles;

        void UploadSummaryUpdate()
        {
            GUI.Label(steptitlerect, "SCENE UPLOAD SUMMARY", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "The following will be uploaded to the Dashboard:", "normallabel");

            int heightOffset = 150;

            int sceneVersion = 0;
            var settings = Cognitive3D_Preferences.FindCurrentScene();
            if (settings != null && !string.IsNullOrEmpty(settings.SceneId)) //has been uploaded. this is a new version
            {
                SceneExistsOnDashboard = true;
                sceneVersion = settings.VersionNumber;
            }
            else
            {
                SceneExistsOnDashboard = false;
            }

            SceneHasExportFiles = EditorCore.HasSceneExportFiles(Cognitive3D_Preferences.FindCurrentScene());

            var uploadSceneRect = new Rect(30, heightOffset, 30, 30);
            if (!SceneHasExportFiles)
            {
                //disable 'upload scene geometry' toggle
                GUI.Button(uploadSceneRect, EditorCore.CircleEmpty32, "image_centered");
                GUI.Label(new Rect(60, heightOffset+2, 400, 30), "Upload Scene Geometry (No files exported)", "normallabel");
                UploadSceneGeometry = false;
            }
            else
            {
                //upload scene geometry
                if (UploadSceneGeometry)
                {
                    if (GUI.Button(uploadSceneRect, EditorCore.BoxCheckmark32, "image_centered"))
                    {
                        UploadSceneGeometry = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadSceneRect, EditorCore.BoxEmpty32, "image_centered"))
                    {
                        UploadSceneGeometry = true;
                    }
                }
                string uploadGeometryText = "Upload Scene Geometry";
                if (SceneExistsOnDashboard)
                {
                    uploadGeometryText = "Upload Scene Geometry (Version " + (sceneVersion+1) + ")";
                }
                GUI.Label(new Rect(60, heightOffset+2, 400, 30), uploadGeometryText, "normallabel");
            }

            var uploadThumbnailRect = new Rect(30, heightOffset+40, 30, 30);
            if (!SceneExistsOnDashboard && !UploadSceneGeometry)
            {
                //disable 'upload scene geometry' toggle
                GUI.Button(uploadThumbnailRect, EditorCore.CircleEmpty32, "image_centered");
                GUI.Label(new Rect(60, heightOffset+42, 300, 30), "Upload Scene Thumbnail (No Scene exists)", "normallabel");
            }
            else
            {
                //upload thumbnail
                if (UploadThumbnail)
                {
                    if (GUI.Button(uploadThumbnailRect, EditorCore.BoxCheckmark32, "image_centered"))
                    {
                        UploadThumbnail = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadThumbnailRect, EditorCore.BoxEmpty32, "image_centered"))
                    {
                        UploadThumbnail = true;
                    }
                }
                GUI.Label(new Rect(60, heightOffset+42, 300, 30), "Upload Scene Thumbnail", "normallabel");
            }

            //upload dynamics
            int dynamicObjectCount = EditorCore.GetExportedDynamicObjectNames().Count;
            var uploadDynamicRect = new Rect(30, heightOffset+80, 30, 30);

            if (!SceneExistsOnDashboard && !UploadSceneGeometry)
            {
                //can't upload dynamics
                GUI.Button(uploadDynamicRect, EditorCore.CircleEmpty32, "image_centered");
                GUI.Label(new Rect(60, heightOffset+82, 400, 30), "Upload " + dynamicObjectCount + " Dynamic Meshes (No Scene exists)", "normallabel");
            }
            else
            {
                //upload dynamics toggle
                if (UploadDynamicMeshes)
                {
                    if (GUI.Button(uploadDynamicRect, EditorCore.BoxCheckmark32, "image_centered"))
                    {
                        UploadDynamicMeshes = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadDynamicRect, EditorCore.BoxEmpty32, "image_centered"))
                    {
                        UploadDynamicMeshes = true;
                    }
                }
                GUI.Label(new Rect(60, heightOffset+82, 300, 30), "Upload " + dynamicObjectCount + " Dynamic Meshes", "normallabel");
            }


            //debug
            //GUI.Label(new Rect(30, 100, 440, 20), SceneHasExportFiles + " scene has export files");
            //GUI.Label(new Rect(30, 120, 440, 20), SceneExistsOnDashboard + " exists on dashboard");
            //GUI.Label(new Rect(30,110,440,20), UploadSceneGeometry + " upload scene geo");
            //GUI.Label(new Rect(30, 140, 440, 20), (SceneExistsOnDashboard || UploadSceneGeometry) + " will be uploading scene files");

            //scene thumbnail preview
            var thumbnailRect = new Rect(175, heightOffset+130, 150, 150);
            if (UploadThumbnail)
            {
                GUI.Label(new Rect(150, heightOffset+280, 200, 20), "New Thumbnail from Scene View");
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

        void UploadProgressUpdate()
        {
            GUI.Label(steptitlerect, "SCENE UPLOADING", "steptitle");

            //GUI.Label(new Rect(30, 30, 440, 25), "Uploading Scene Geometry","normallabel");
        }

        void DoneUpdate()
        {
            GUI.Label(steptitlerect, "NEXT STEPS", "steptitle");

            //1 play a session, see it on the dashboard
            GUI.Label(new Rect(30, 30, 440, 440), "The " + EditorCore.DisplayValue(DisplayKey.ManagerName) + " in your scene will record user position, gaze and basic device information.\n\nTo record a Session, just <b>Press Play</b>, put on your headset and look around. <b>Press Stop</b> when you're finished and you'll be able to replay the session on our Dashboard", "normallabel");
            if (GUI.Button(new Rect(150, 170, 200, 30), "Open Dashboard"))
            {
                var sceneSettings = Cognitive3D_Preferences.FindCurrentScene();
                if (sceneSettings == null)
                {
                    Debug.LogError("Cannot find scene settings for " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                    return;
                }
                Application.OpenURL(CognitiveStatics.SCENELINK(sceneSettings.SceneId, sceneSettings.VersionNumber));
            }

            //2 overview of features in help window
            GUI.Label(new Rect(30, 250, 440, 440), "You can continue your integration to get more insights including:", "normallabel");
            GUI.Label(new Rect(30, 300, 440, 440), " - Custom Events\n - ExitPoll Surveys\n - Ready Room User Onboarding\n - Dynamic Objects", "normallabel");
            
            if (GUI.Button(new Rect(150, 400, 200, 30), "Open Help"))
            {
                HelpWindow.Init();
            }
        }

        void DrawFooter()
        {
            GUI.color = EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            
            DrawBackButton();
            DrawNextButton();

            if (currentPage == Page.SceneExport)
            {
                Rect buttonrect = new Rect(150, 510, 200, 30);
                string url = "https://docs.cognitive3d.com/unity/scenes/";
                if (GUI.Button(buttonrect, new GUIContent("Open Online Documentation", url)))
                {
                    Application.OpenURL(url);
                }
            }
        }

        void DrawNextButton()
        {
            bool buttonDisabled = false;
            bool appearDisabled = false; //used on dynamic upload page to skip step
            bool buttonAppear = true;
            string text = "Next";
            System.Action onclick = () => currentPage++;
            Rect buttonrect = new Rect(410, 510, 80, 30);

            switch (currentPage)
            {
                case Page.Welcome:
                    break;
                case Page.PlayerSetup:
#if C3D_STEAMVR2
                    appearDisabled = !AllControllerSetupComplete;
                    if (!AllControllerSetupComplete)
                    {
                        if (appearDisabled)
                        {
                            onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without configuring the player prefab?", "Yes", "No")) { currentPage++; } };
                        }
                    }
                    else
                    {
                        appearDisabled = !hasFoundSteamVRActionSet;
                        if (!hasFoundSteamVRActionSet)
                        {
                            if (appearDisabled)
                            {
                                onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without creating the necessary SteamVR Input Action Set files?", "Yes", "No")) { currentPage++; } };
                            }
                        }
                    }

#else
                    appearDisabled = !AllControllerSetupComplete;
                    if (!AllControllerSetupComplete)
                    {
                        //onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without creating the necessary files?", "Yes", "No")) { currentPage++; } };
                        if (appearDisabled)
                        {
                            onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without configuring the player prefab?", "Yes", "No")) { currentPage++; } };
                        }
                    }
#endif
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
                case Page.SceneUploadProgress:
                    buttonAppear = false;
                    break;
                case Page.SceneUpload:
                    System.Action completedmanifestupload = delegate ()
                    {
                        ExportUtility.UploadAllDynamicObjectMeshes(true);
                        currentPage = Page.SetupComplete;
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

                        currentPage = Page.SceneUploadProgress;

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

            if (!buttonAppear)
            {
                return;
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
            bool buttonAppear = true;
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
                    buttonAppear = false;
                    break;
                case Page.SetupComplete:
                    buttonAppear = false;
                    buttonDisabled = true;
                    onclick = null;
                    break;
                case Page.ProjectError:
                    buttonrect = new Rect(600, 0, 0, 0);
                    break;
                default: break;
            }

            if (!buttonAppear)
            {
                return;
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
                Debug.Log("SceneSetup.AppendSteamVRActionSet Added Cognitive3D Action Set");
            }
            else
            {
                Debug.LogError("SceneSetup.AppendSteamVRActionSet SteamVR LoadActionFile failed!");
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
                Debug.Log("SceneSetup.SetDefaultBindings save Cognitive3D input bindings");
                SaveBindingFile(bindingfile);
            }
            else
            {
                Debug.Log("SceneSetup.SetDefaultBindings failed to load steamvr actions");
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