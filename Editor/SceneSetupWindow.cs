using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D.Components;

#if COGNITIVE3D_INCLUDE_COREUTILITIES
using Unity.XR.CoreUtils;
#endif

#if C3D_VIVEWAVE
using Wave.Essence;
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
using UnityEditor.XR.LegacyInputHelpers;
#endif

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
using Unity.Netcode;
#endif

#if COGNITIVE3D_INCLUDE_NORMCORE
using Normal.Realtime;
#endif

#if C3D_STEAMVR2
using Valve.VR;
using System.IO;
using Valve.Newtonsoft.Json;
#endif

#if COGNITIVE3D_INCLUDE_META_XR_UTILITY
using Meta.XR.MRUtilityKit;
#endif

//uploading multiple scenes at once?

namespace Cognitive3D
{
    internal class SceneSetupWindow : EditorWindow
    {
#if C3D_OCULUS
        static bool wantEyeTrackingEnabled;
        static bool wantPassthroughEnabled;
        static bool wantSocialEnabled;
        static bool wantHandTrackingEnabled;
        static bool wantSceneApiEnabled;

        const string SCENE_MANAGER_NAME = "Cognitive3D_OVRSceneManager";
        const string SCENE_PLANE_PREFAB_NAME = "Cognitive3D_PlanePrefab";
        const string SCENE_VOLUME_PREFAB_NAME = "Cognitive3D_VolumePrefab";
        const string MRUK_NAME = "Cognitive3D_Meta_MRUK";
#endif

        private const string URL_SESSION_TAGS_DOCS = "https://docs.cognitive3d.com/dashboard/organization-settings/#session-tags";
        readonly Rect steptitlerect = new Rect(30, 5, 100, 440);

        private static bool completedUpload = false;
        internal static bool CompletedUpload { get => completedUpload; set => completedUpload = value; }

        internal static void Init()
        {
            SceneSetupWindow window = (SceneSetupWindow)EditorWindow.GetWindow(typeof(SceneSetupWindow), true, "Scene Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            currentPage = Page.Welcome;
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();
            initialPlayerSetup = false;

            ExportUtility.ClearUploadSceneSettings();

            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found == null) //add Cognitive3D_manager
            {
                GameObject c3dManagerPrefab = Resources.Load<GameObject>("Cognitive3D_Manager");
                PrefabUtility.InstantiatePrefab(c3dManagerPrefab);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            var settings = Cognitive3D_Preferences.FindCurrentScene();
            Texture2D ignored = null;
            EditorCore.GetSceneThumbnail(settings, ref ignored, true);
#if C3D_OCULUS
            // Get the current state of these components: are they already enabled?
            // This is so the checkbox can accurately display the status of the components instead of defaulting to false
            OVRManager ovrManager = Object.FindObjectOfType<OVRManager>();
            if (ovrManager != null )
            {
                var fi = typeof(OVRManager).GetField("requestEyeTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var requestingEyeTracking = fi.GetValue(ovrManager);
                fi = typeof(OVRManager).GetField("requestFaceTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var requestingFaceTracking = fi.GetValue(ovrManager);
                var faceExpressions = FindObjectOfType<OVRFaceExpressions>();
                if (faceExpressions != null)
                {
                    wantEyeTrackingEnabled = (bool) requestingEyeTracking && (bool) requestingFaceTracking && faceExpressions;
                }
            }
            if (Cognitive3D_Manager.Instance != null)
            {
                wantPassthroughEnabled = Cognitive3D_Manager.Instance.GetComponent<OculusPassthrough>();
                wantSocialEnabled = Cognitive3D_Manager.Instance.GetComponent<OculusSocial>();
                wantHandTrackingEnabled = Cognitive3D_Manager.Instance.GetComponent<HandTracking>();
                wantSceneApiEnabled = Cognitive3D_Manager.Instance.GetComponent<Cognitive3D_MetaSceneMesh>();
            }
#endif
        }

        internal static void Init(Page page)
        {
            SceneSetupWindow window = (SceneSetupWindow)EditorWindow.GetWindow(typeof(SceneSetupWindow), true, "Scene Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            currentPage = page;
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();
            initialPlayerSetup = false;

            ExportUtility.ClearUploadSceneSettings();

            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found == null) //add Cognitive3D_manager
            {
                GameObject c3dManagerPrefab = Resources.Load<GameObject>("Cognitive3D_Manager");
                PrefabUtility.InstantiatePrefab(c3dManagerPrefab);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            var settings = Cognitive3D_Preferences.FindCurrentScene();
            Texture2D ignored = null;
            EditorCore.GetSceneThumbnail(settings, ref ignored, true);
#if C3D_OCULUS
            // Get the current state of these components: are they already enabled?
            // This is so the checkbox can accurately display the status of the components instead of defaulting to false
            OVRManager ovrManager = Object.FindObjectOfType<OVRManager>();
            if (ovrManager != null )
            {
                var fi = typeof(OVRManager).GetField("requestEyeTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var requestingEyeTracking = fi.GetValue(ovrManager);
                fi = typeof(OVRManager).GetField("requestFaceTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var requestingFaceTracking = fi.GetValue(ovrManager);
                var faceExpressions = FindObjectOfType<OVRFaceExpressions>();
                if (faceExpressions != null)
                {
                    wantEyeTrackingEnabled = (bool) requestingEyeTracking && (bool) requestingFaceTracking && faceExpressions;
                }
            }
            if (Cognitive3D_Manager.Instance != null)
            {
                wantPassthroughEnabled = Cognitive3D_Manager.Instance.GetComponent<OculusPassthrough>();
                wantSocialEnabled = Cognitive3D_Manager.Instance.GetComponent<OculusSocial>();
                wantHandTrackingEnabled = Cognitive3D_Manager.Instance.GetComponent<HandTracking>();
                wantSceneApiEnabled = Cognitive3D_Manager.Instance.GetComponent<Cognitive3D_MetaSceneMesh>();
            }
#endif
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

            var settings = Cognitive3D_Preferences.FindCurrentScene();
            Texture2D ignored = null;
            EditorCore.GetSceneThumbnail(settings, ref ignored, true);
        }

        /// <summary>
        /// Instantiates a Cognitive3D_Manager <br/>
        /// Identifies controller and camera objects <br/>
        /// Sets them up (for example with dynamic objects) <br/>
        /// Adds to scene settings
        /// </summary>
        internal static void PerformBasicSetup()
        {
            Init();
            PlayerSetupStart();
        }

        internal enum Page
        {
            ProjectError,
            Welcome,
            PlayerSetup,
            QuestProSetup,
            //TODO pico eyetracker setup
            //TODO wave eyetracker setup
            SceneExport,
            SceneUpload,
            SceneUploadProgress,
            SetupComplete
        };
        private static Page _currentPage;
        public static Page currentPage {
            get {
                return _currentPage;
            }
            internal set {
                _currentPage = value;
            }
        }

        private void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

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
                case Page.QuestProSetup:
                    QuestProSetup();
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
                default:
                    throw new System.NotSupportedException();
            }

            DrawFooter();
            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        private void OnFocus()
        {
            hasCheckedForSteamVRActionsSet = false;
            if (isoSceneImage != null)
            {
                DestroyImmediate(isoSceneImage);
            }

            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManager_activeSceneChangedInEditMode;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += EditorSceneManager_activeSceneChangedInEditMode;
        }

        private void EditorSceneManager_activeSceneChangedInEditMode(UnityEngine.SceneManagement.Scene ignore1, UnityEngine.SceneManagement.Scene ignore2)
        {
            initialPlayerSetup = false;
        }

        void WelcomeUpdate()
        {
            GUI.Label(steptitlerect, "INTRODUCTION", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "Welcome to the Cognitive3D <b>Scene Setup</b>. This window will guide you through basic configuration to ensure your scene is ready to record data.\n\nThis will include:", "normallabel");
            GUI.Label(new Rect(30, 140, 440, 440), "- Ensure player prefab is configured\n- Set up controller inputs\n- Export Scene Geometry to SceneExplorer","normallabel");
            GUI.Label(new Rect(30, 220, 440, 440), "For a guided walkthrough, you can follow the video below:", "normallabel");

            //video link
            if (GUI.Button(new Rect(115, 300, 270, 150), "", "video_centered"))
            {
                Application.OpenURL("https://vimeo.com/749278322");
            }

            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found == null) //add Cognitive3D_manager
            {
                GameObject c3dManagerPrefab = Resources.Load<GameObject>("Cognitive3D_Manager");
                PrefabUtility.InstantiatePrefab(c3dManagerPrefab);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#if PHOTON_UNITY_NETWORKING
    #if C3D_PHOTON
                if (Cognitive3D_Manager.Instance.gameObject.GetComponent<PhotonMultiplayer>() == null)
                {
                    Cognitive3D_Manager.Instance.gameObject.AddComponent<PhotonMultiplayer>();
                }
                if (Cognitive3D_Manager.Instance.gameObject.GetComponent<PhotonView>() == null)
                {
                    Cognitive3D_Manager.Instance.gameObject.AddComponent<PhotonView>();
                }
    #else
                if (Cognitive3D_Manager.Instance.gameObject.GetComponent<PhotonMultiplayer>() != null)
                {
                    DestroyImmediate(Cognitive3D_Manager.Instance.gameObject.GetComponent<PhotonMultiplayer>());
                }
                if (Cognitive3D_Manager.Instance.gameObject.GetComponent<PhotonView>() != null)
                {
                    DestroyImmediate(Cognitive3D_Manager.Instance.gameObject.GetComponent<PhotonView>());
                }
    #endif
#endif

#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
    #if C3D_NETCODE
            if (Cognitive3D_Manager.Instance.gameObject.GetComponent<NetcodeMultiplayer>() == null)
            {
                Cognitive3D_Manager.Instance.gameObject.AddComponent<NetcodeMultiplayer>();
            }
            if (Cognitive3D_Manager.Instance.gameObject.GetComponent<NetworkObject>() == null)
            {
                Cognitive3D_Manager.Instance.gameObject.AddComponent<NetworkObject>();
            }
    #else
            if (Cognitive3D_Manager.Instance.gameObject.GetComponent<NetcodeMultiplayer>() != null)
            {
                DestroyImmediate(Cognitive3D_Manager.Instance.gameObject.GetComponent<NetcodeMultiplayer>());
            }
            if (Cognitive3D_Manager.Instance.gameObject.GetComponent<NetworkObject>() != null)
            {
                DestroyImmediate(Cognitive3D_Manager.Instance.gameObject.GetComponent<NetworkObject>());
            }
    #endif
#endif

#if COGNITIVE3D_INCLUDE_NORMCORE
    #if C3D_NORMCORE
        if (Cognitive3D_Manager.Instance.gameObject.GetComponent<NormcoreMultiplayer>() == null)
        {
            Cognitive3D_Manager.Instance.gameObject.AddComponent<NormcoreMultiplayer>();
        }

        string localPath = "Assets/Resources/Cognitive3D_NormcoreSync.prefab";

        string folderPath = System.IO.Path.GetDirectoryName(localPath);
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        if (!Resources.Load<GameObject>("Cognitive3D_NormcoreSync"))
        {
            GameObject c3dNormcoreSyncObject = new GameObject("Cognitive3D_NormcoreSync");
            PrefabUtility.SaveAsPrefabAsset(c3dNormcoreSyncObject, localPath);
            DestroyImmediate(c3dNormcoreSyncObject);
        }

        GameObject c3dNormcoreSyncPrefab = Resources.Load<GameObject>("Cognitive3D_NormcoreSync");
        if (!c3dNormcoreSyncPrefab.GetComponent<NormcoreSync>())
        {
            c3dNormcoreSyncPrefab.AddComponent<NormcoreSync>();
        }
    #else
        if (Cognitive3D_Manager.Instance.gameObject.GetComponent<NormcoreMultiplayer>() != null)
        {
            DestroyImmediate(Cognitive3D_Manager.Instance.gameObject.GetComponent<NormcoreMultiplayer>());
        }
    #endif
#endif
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

        static GameObject leftcontroller;
        static GameObject rightcontroller;
        static GameObject mainCameraObject;
        static GameObject trackingSpace;

        [System.NonSerialized]
        static bool initialPlayerSetup;

        //called once when entering controller update page. finds/sets expected defaults
        static void PlayerSetupStart()
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

            // Setting left and right controllers for checking controller setup item in Project Validation
            EditorCore.SetControllers(true, rightcontroller);
            EditorCore.SetControllers(false, leftcontroller);

            RoomTrackingSpace trackingSpaceInScene = FindObjectOfType<RoomTrackingSpace>();
            if (trackingSpaceInScene != null)
            {
                trackingSpace = trackingSpaceInScene.gameObject;
            }

            if (leftcontroller != null && rightcontroller != null && trackingSpace != null)
            {
                //found dynamic objects for controllers and tracking space - prefer to use those
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
                trackingSpace = player.trackingOriginTransform.gameObject; 
            }
            else
            {
                var playArea = FindObjectOfType<SteamVR_PlayArea>();
                if (playArea != null)
                {
                    var controllers = playArea.GetComponentsInChildren<SteamVR_Behaviour_Pose>();
                    foreach (var controller in controllers)
                    {
                        if (controller.inputSource == SteamVR_Input_Sources.LeftHand)
                        {
                            leftcontroller = controller.gameObject;
                        }
                        if (controller.inputSource == SteamVR_Input_Sources.RightHand)
                        {
                            rightcontroller = controller.gameObject;
                        }
                    }
                    trackingSpace = playArea.gameObject;
                }
            }
#elif C3D_OCULUS
            //basic setup
            var manager = FindObjectOfType<OVRCameraRig>();
            if (manager != null)
            {
                leftcontroller = manager.leftHandAnchor.gameObject;
                rightcontroller = manager.rightHandAnchor.gameObject;
                trackingSpace = manager.trackingSpace.gameObject;
            }

            OVRManager ovrManager = Object.FindObjectOfType<OVRManager>();
            OVRProjectConfig.CachedProjectConfig.eyeTrackingSupport = OVRProjectConfig.FeatureSupport.Required;
            if (ovrManager != null)
            {
                var fi = typeof(OVRManager).GetField("requestEyeTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                wantEyeTrackingEnabled = (bool)fi.GetValue(ovrManager);
            }
            else
            {
                wantEyeTrackingEnabled = false;
                Debug.LogWarning("OVRManager not found in scene!");
            }
            wantSocialEnabled = Cognitive3D_Manager.Instance.GetComponent<OculusSocial>();
            wantPassthroughEnabled = Cognitive3D_Manager.Instance.GetComponent<OculusPassthrough>();

#elif C3D_VIVEWAVE
            //TODO investigate if automatically detecting vive wave controllers is possible
            if (trackingSpace == null)
            {
                var waveRig = FindObjectOfType<WaveRig>();
                if (waveRig != null)
                {
                    trackingSpace = waveRig.CameraOffset;
                }
            }
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
            if (leftcontroller != null && rightcontroller != null && trackingSpace != null)
            {
                //found controllers and tracking space from VR SDKs
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

            // if tracking space and controllers not found yet, look for it in other ways
#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
            var cameraOffset = FindObjectOfType<CameraOffset>();
            if (cameraOffset != null)
            {
                trackingSpace = cameraOffset.gameObject;
                var cameraOffsetObject = cameraOffset.cameraFloorOffsetObject;
                if (cameraOffsetObject != null)
                {
                    trackingSpace = cameraOffsetObject.gameObject;
                }
            }
#endif
            if (leftcontroller != null && rightcontroller != null && trackingSpace != null)
            {
                //found controllers and tracking space from VR SDKs
                return;
            }
#if COGNITIVE3D_INCLUDE_COREUTILITIES
            var xrRig = FindObjectOfType<XROrigin>();
            if (xrRig != null)
            {
                trackingSpace = xrRig.gameObject;
                var xrRigOffset = xrRig.CameraFloorOffsetObject;
                if (xrRigOffset != null)
                {
                    trackingSpace = xrRigOffset.gameObject;
                }
            }
            if (leftcontroller == null)
            {
                leftcontroller = GameObject.Find("Left Controller")?.gameObject;
            }
            if (rightcontroller == null)
            {
                rightcontroller = GameObject.Find("Right Controller")?.gameObject;
            }
#endif
        }

        bool AllSetupComplete;

        void ControllerUpdate()
        {
            PlayerSetupStart();
            GUI.Label(new Rect(30, 30, 440, 440), "You can use your existing Player Prefab. For most implementations, this is just a quick check to ensure cameras and controllers are configued correctly.", "normallabel");
            GUI.Label(new Rect(30, 100, 440, 440), "The display for the HMD should be tagged as <b>MainCamera</b>", "normallabel");
            GUI.Label(new Rect(30, 150, 440, 440), "The <b>TrackingSpace</b> is the root transform for the HMD and controllers", "normallabel");

            //hmd
            int hmdRectHeight = 200;

            GUI.Label(new Rect(30, hmdRectHeight, 50, 30), "HMD", "boldlabel");
            if (GUI.Button(new Rect(180, hmdRectHeight, 255, 30), mainCameraObject != null? mainCameraObject.gameObject.name:"Missing", "button_blueoutline"))
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


            // tracking space
            int hmdRectHeight2 = 235;

            GUI.Label(new Rect(30, hmdRectHeight2, 150, 30), "Tracking Space", "boldlabel");
            if (GUI.Button(new Rect(180, hmdRectHeight2, 255, 30), trackingSpace != null ? trackingSpace.name : "Missing", "button_blueoutline"))
            {
                Selection.activeGameObject = trackingSpace;
            }

            int pickerID_HMD2 = 5689467;
            if (GUI.Button(new Rect(440, hmdRectHeight2, 30, 30), EditorCore.SearchIconWhite))
            {
                GUI.skin = null;
                EditorGUIUtility.ShowObjectPicker<GameObject>(
                    trackingSpace, true, "", pickerID_HMD2);
                GUI.skin = EditorCore.WizardGUISkin;
            }
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == pickerID_HMD2)
                {
                    trackingSpace = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                }
            }
            if (trackingSpace == null)
            {
                GUI.Label(new Rect(400, hmdRectHeight2, 30, 30), new GUIContent(EditorCore.Alert, "Tracking Space not set"), "image_centered");
            }

            //controllers
#if C3D_STEAMVR2
            GUI.Label(new Rect(30, 280, 440, 440), "The Controllers should have <b>SteamVR Behaviour Pose</b> components", "normallabel");
#else
            GUI.Label(new Rect(30, 280, 440, 440), "The Controllers may have <b>Tracked Pose Driver</b> components", "normallabel");
#endif

            bool leftControllerIsValid = false;
            bool rightControllerIsValid = false;

            leftControllerIsValid = leftcontroller != null;
            rightControllerIsValid = rightcontroller != null;

            AllSetupComplete = false;
            if (rightControllerIsValid && leftControllerIsValid && Camera.main != null && mainCameraObject == Camera.main.gameObject)
            {
                var rdyn = rightcontroller.GetComponent<DynamicObject>();
                if (rdyn != null && rdyn.IsController && rdyn.IsRight == true)
                {
                    var ldyn = leftcontroller.GetComponent<DynamicObject>();
                    if (ldyn != null && ldyn.IsController && ldyn.IsRight == false
                        && (trackingSpace != null && trackingSpace.GetComponent<RoomTrackingSpace>() != null)) // Make sure tracking space isn't null before accessing its component
                    {
                        AllSetupComplete = true;
                    }
                }
            }
            int handOffset = 320;

            //left hand label
            GUI.Label(new Rect(30, handOffset + 15, 150, 30), "Left Controller", "boldlabel");

            string leftname = "Missing";
            if (leftcontroller != null)
                leftname = leftcontroller.gameObject.name;
            if (GUI.Button(new Rect(180, handOffset + 15, 255, 30), leftname, "button_blueoutline"))
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
                GUI.Label(new Rect(400, handOffset + 15, 30, 30), new GUIContent(EditorCore.Alert, "Left Controller not set"), "image_centered");
            }

            //right hand label
            GUI.Label(new Rect(30, handOffset + 50, 150, 30), "Right Controller", "boldlabel");

            string rightname = "Missing";
            if (rightcontroller != null)
                rightname = rightcontroller.gameObject.name;

            if (GUI.Button(new Rect(180, handOffset + 50, 255, 30), rightname, "button_blueoutline"))
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
                GUI.Label(new Rect(400, handOffset + 50, 30, 30), new GUIContent(EditorCore.Alert, "Right Controller not set"), "image_centered");
            }

            //drag and drop
            if (new Rect(180, handOffset + 50, 440, 30).Contains(Event.current.mousePosition)) //right hand
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    rightcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(180, handOffset + 15, 440, 30).Contains(Event.current.mousePosition)) //left hand
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    leftcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(180, hmdRectHeight, 440, 30).Contains(Event.current.mousePosition)) //hmd
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    mainCameraObject = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(180, hmdRectHeight2, 440, 30).Contains(Event.current.mousePosition)) // trackingSpace
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    trackingSpace = (GameObject)DragAndDrop.objectReferences[0];
                }
            }

            if (GUI.Button(new Rect(160, 420, 200, 30), new GUIContent("Set up GameObjects","Set up the player rig tracking space, attach Dynamic Object components to the controllers, and configures controllers to record button inputs")))
            {
                if (mainCameraObject != null)
                {
                    mainCameraObject.tag = "MainCamera";
                }

                SetupControllers();

                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Event.current.Use();
            }

            if (AllSetupComplete)
            {
                GUI.Label(new Rect(130, 420, 30, 30), EditorCore.CircleCheckmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(128, 420, 32, 32), EditorCore.Alert, "image_centered");
            }
#if C3D_STEAMVR2

            //generate default input file if it doesn't already exist
            bool hasInputActionFile = SteamVR_Input.DoesActionsFileExist();
            if (GUI.Button(new Rect(160, 455, 200, 30), "Append Input Bindings"))
            {
                if (SteamVR_Input.actionFile == null)
                {
                    bool initializeSuccess = SteamVR_Input.InitializeFile(false, false);

                    if (initializeSuccess == false)
                    {
                        //copy
                        SteamVR_CopyExampleInputFiles.CopyFiles(true);
                        System.Threading.Thread.Sleep(1000);
                        SteamVR_Input.InitializeFile();
                    }
                }
                if (SteamVR_Input_EditorWindow.IsOpen())
                {
                    SteamVR_Input_EditorWindow.GetOpenWindow().Close();
                }
                AppendSteamVRActionSet();
                SetDefaultBindings();
                Valve.VR.SteamVR_Input_Generator.BeginGeneration();
            }
            if (DoesC3DInputActionSetExist())
            {
                GUI.Label(new Rect(130, 455, 30, 30), EditorCore.CircleCheckmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(128, 455, 32, 32), EditorCore.Alert, "image_centered");
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

        public static void SetupControllers()
        {
            if (trackingSpace != null && trackingSpace.GetComponent<RoomTrackingSpace>() == null)
            {
                trackingSpace.AddComponent<RoomTrackingSpace>();
            }

            if (leftcontroller != null && leftcontroller.GetComponent<DynamicObject>() == null)
            {
                leftcontroller.AddComponent<DynamicObject>();
            }
            if (rightcontroller != null && rightcontroller.GetComponent<DynamicObject>() == null)
            {
                rightcontroller.AddComponent<DynamicObject>();
            }

            if (Cognitive3D_Manager.Instance == null)
            {
                GameObject c3dManagerPrefab = Resources.Load<GameObject>("Cognitive3D_Manager");
                PrefabUtility.InstantiatePrefab(c3dManagerPrefab);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
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
            
            if (leftcontroller != null)
            {
                var dyn = leftcontroller.GetComponent<DynamicObject>();
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.SyncWithPlayerGazeTick = true;
                dyn.FallbackControllerType = controllerType;
                dyn.idSource = DynamicObject.IdSourceType.GeneratedID;
            }
            if (rightcontroller != null)
            {
                var dyn = rightcontroller.GetComponent<DynamicObject>();
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.SyncWithPlayerGazeTick = true;
                dyn.FallbackControllerType = controllerType;
                dyn.idSource = DynamicObject.IdSourceType.GeneratedID;
            }
        }

        #endregion


        void QuestProSetup()
        {
#if C3D_OCULUS

            GUI.Label(steptitlerect, "ADDITIONAL OCULUS SETUP", "steptitle");
            GUI.Label(new Rect(30, 40, 440, 440), "Please select the additional Oculus features you would like to include:", "normallabel");

            // Social Data
            GUI.Label(new Rect(140, 120, 440, 440), "Oculus Social Data*", "normallabel");
            Rect infoRect = new Rect(320, 115, 30, 30);
            GUI.Label(infoRect, new GUIContent(EditorCore.Info, "Records user Oculus ID, username, and party size."), "image_centered");

            Rect checkboxRect = new Rect(105, 115, 30, 30);

            if (wantSocialEnabled)
            {
                if (GUI.Button(checkboxRect, EditorCore.BoxCheckmark, "image_centered"))
                {
                    wantSocialEnabled = false;
                }
            }
            else
            {
                if (GUI.Button(checkboxRect, EditorCore.BoxEmpty, "image_centered"))
                {
                    wantSocialEnabled = true;
                }
            }


            // Mixed Reality
            GUI.Label(new Rect(140, 175, 440, 440), "Mixed Reality", "normallabel");
            Rect infoRect2 = new Rect(320, 170, 30, 30);
            GUI.Label(infoRect2, new GUIContent(EditorCore.Info, "Records if/when passthrough cameras are enabled."), "image_centered");
            Rect checkboxRect2 = new Rect(105, 170, 30, 30);
            if (wantPassthroughEnabled)
            {
                if (GUI.Button(checkboxRect2, EditorCore.BoxCheckmark, "image_centered"))
                {
                    wantPassthroughEnabled = false;
                }
            }
            else
            {
                if (GUI.Button(checkboxRect2, EditorCore.BoxEmpty, "image_centered"))
                {
                    wantPassthroughEnabled = true;
                }
            }

            // Eye Tracking
            GUI.Label(new Rect(140, 230, 440, 440), "Quest Pro Eyetracking", "normallabel");
            Rect infoRect3 = new Rect(320, 225, 30, 30);
            GUI.Label(infoRect3, new GUIContent(EditorCore.Info, "Enables eyetracking for more accurate understanding of user attention. Currently this requires the OVRFaceExpressions component, which will be added automatically."), "image_centered");

            Rect checkboxRect3 = new Rect(105, 225, 30, 30);
            if (wantEyeTrackingEnabled)
            {
                if (GUI.Button(checkboxRect3, EditorCore.BoxCheckmark, "image_centered"))
                {
                    wantEyeTrackingEnabled = false;
                }
            }
            else
            {
                if (GUI.Button(checkboxRect3, EditorCore.BoxEmpty, "image_centered"))
                {
                    wantEyeTrackingEnabled = true;
                }
            }

            // Hand Tracking
            GUI.Label(new Rect(140, 285, 440, 440), "Quest Hand Tracking", "normallabel");
            Rect infoRect4 = new Rect(320, 280, 30, 30);
            GUI.Label(infoRect4, new GUIContent(EditorCore.Info, "Collects and sends data pertaining to Hand Tracking."), "image_centered");

            Rect checkboxRect4 = new Rect(105, 280, 30, 30);
            if (wantHandTrackingEnabled)
            {
                if (GUI.Button(checkboxRect4, EditorCore.BoxCheckmark, "image_centered"))
                {
                    wantHandTrackingEnabled = false;
                }
            }
            else
            {
                if (GUI.Button(checkboxRect4, EditorCore.BoxEmpty, "image_centered"))
                {
                    wantHandTrackingEnabled = true;
                }
            }

            // Scene API
            GUI.Label(new Rect(140, 335, 440, 440), "Quest 3 Scene API", "normallabel");
            Rect infoRect5 = new Rect(320, 330, 30, 30);
            GUI.Label(infoRect5, new GUIContent(EditorCore.Info, "Collects dimensions of the room the participant is in."), "image_centered");

            Rect checkboxRect5 = new Rect(105, 330, 30, 30);
            if (wantSceneApiEnabled)
            {
                if (GUI.Button(checkboxRect5, EditorCore.BoxCheckmark, "image_centered"))
                {
                    wantSceneApiEnabled = false;
                }
            }
            else
            {
                if (GUI.Button(checkboxRect5, EditorCore.BoxEmpty, "image_centered"))
                {
                    wantSceneApiEnabled = true;
                }
            }

            // Footer
            GUI.Label(new Rect(20, 460, 440, 440), "*Recommended for publishing to the Meta App Store", "caption");
#else
            currentPage++;
#endif
        }

#if C3D_OCULUS
        void ApplyOculusSettings()
        {
            if (wantEyeTrackingEnabled)
            {
                SetEyeTrackingState(true);
            }
            else
            {
                //face expressions component isn't destroyed and face tracking permissions aren't reset
                //we don't know if developers are using these features themselves, so it is safer to leave them how they are rather than destroy everything
            }
            if (wantPassthroughEnabled)
            {
                var passthrough = FindObjectOfType<OculusPassthrough>();
                if (passthrough == null)
                {
                    Cognitive3D_Manager.Instance.gameObject.AddComponent<OculusPassthrough>();
                }
            }
            else
            {
                var passthrough = FindObjectOfType<OculusPassthrough>();
                if (passthrough != null)
                {
                    DestroyImmediate(passthrough);
                }
            }
            if (wantSocialEnabled)
            {
                var social = FindObjectOfType<OculusSocial>();
                if (social == null)
                {
                    Cognitive3D_Manager.Instance.gameObject.AddComponent<OculusSocial>();
                }
            }
            else
            {
                var social = FindObjectOfType<OculusSocial>();
                if (social != null)
                {
                    DestroyImmediate(social);
                }
            }
            if (wantHandTrackingEnabled)
            {
                var hand = FindObjectOfType<HandTracking>();
                if (hand == null)
                {
                    Cognitive3D_Manager.Instance.gameObject.AddComponent<HandTracking>();
                }
            }
            else
            {
                var hand = FindObjectOfType<HandTracking>();
                if (hand != null)
                {
                    DestroyImmediate(hand);
                }
            }

            if (wantSceneApiEnabled)
            {
#if !COGNITIVE3D_INCLUDE_META_CORE_65_OR_NEWER
                // Do not modify OVRManager permissions
                var sceneApi = FindObjectOfType<Cognitive3D_MetaSceneMesh>();
                if (sceneApi == null)
                {
                    Cognitive3D_Manager.Instance.gameObject.AddComponent<Cognitive3D_MetaSceneMesh>();
                }
                GameObject sceneManager = FindObjectOfType<OVRSceneManager>()?.gameObject;
                if (sceneManager == null)
                {
                    sceneManager = new GameObject(SCENE_MANAGER_NAME);
                    sceneManager.AddComponent<OVRSceneModelLoader>();
                }
                
                // OVRSceneModelLoader requires OVRSceneManager so that will automatically add it
                // We have this to future proof this in case that changes
                var sceneManagerComponent = sceneManager.GetComponent<OVRSceneManager>();
                if (sceneManagerComponent == null) 
                {
                    sceneManager.AddComponent<OVRSceneManager>();
                }
                if (sceneManagerComponent.PlanePrefab == null)
                {
                    GameObject planePrefab = GameObject.Find(SCENE_PLANE_PREFAB_NAME);
                    if (planePrefab == null)
                    {
                        planePrefab = new GameObject(SCENE_PLANE_PREFAB_NAME);
                    }
                    // OVRSceneAnchor already has [DisallowMultipleComponent]
                    planePrefab.AddComponent<OVRSceneAnchor>();
                    sceneManagerComponent.PlanePrefab = planePrefab.GetComponent<OVRSceneAnchor>();
                }
                if (sceneManagerComponent.VolumePrefab == null)
                {
                    GameObject volumePrefab = GameObject.Find(SCENE_VOLUME_PREFAB_NAME);
                    if (volumePrefab == null)
                    {
                        volumePrefab = new GameObject(SCENE_VOLUME_PREFAB_NAME);
                    }
                    // OVRSceneAnchor already has [DisallowMultipleComponent]
                    volumePrefab.AddComponent<OVRSceneAnchor>();
                    sceneManagerComponent.VolumePrefab = volumePrefab.GetComponent<OVRSceneAnchor>();
                }
#elif COGNITIVE3D_INCLUDE_META_XR_UTILITY
                var mruk = FindObjectOfType<MRUK>()?.gameObject;
                if (mruk == null)
                {
                    mruk = new GameObject(MRUK_NAME);
                    mruk.AddComponent<MRUK>();
                }
#endif
            }
            else
            {
                // Only destory objects and components we created
                // Do not modify OVRManager permissions
                
                // Component in C3D_Manager prefab
                var sceneApi = FindObjectOfType<Cognitive3D_MetaSceneMesh>();
                if (sceneApi != null)
                {
                    DestroyImmediate(sceneApi);
                }

                // Scene manager that we created
                GameObject sceneManager = GameObject.Find(SCENE_MANAGER_NAME);
                if (sceneManager != null)
                {
                    DestroyImmediate(sceneManager);
                }

                // Plane prefab
                GameObject planePrefab = GameObject.Find(SCENE_PLANE_PREFAB_NAME);
                if (planePrefab != null)
                {
                    DestroyImmediate(planePrefab);
                }

                // Volume prefab
                GameObject volumePrefab = GameObject.Find(SCENE_VOLUME_PREFAB_NAME);
                if (volumePrefab != null)
                {
                    DestroyImmediate(volumePrefab);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }   
#endif

#if C3D_OCULUS

        void SetEyeTrackingState(bool state)
        {
            OVRManager ovrManager = Object.FindObjectOfType<OVRManager>();

            if (state)
            {
                // Enable Eye Tracking
                OVRProjectConfig.CachedProjectConfig.eyeTrackingSupport = OVRProjectConfig.FeatureSupport.Required;
                if (ovrManager != null)
                {
                    var fi = typeof(OVRManager).GetField("requestEyeTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    fi.SetValue(ovrManager, true);
                    EditorUtility.SetDirty(ovrManager);
                }

                var faceExpressions = FindObjectOfType<OVRFaceExpressions>();
                if (faceExpressions == null)
                {
                    // Face Expressions
                    var m_EyeManager = new GameObject("OVRFaceExpressions");
                    m_EyeManager.AddComponent<OVRFaceExpressions>();
                }

                // Face Tracking
                OVRProjectConfig.CachedProjectConfig.faceTrackingSupport = OVRProjectConfig.FeatureSupport.None;
                if (ovrManager != null)
                {
                    var fi = typeof(OVRManager).GetField("requestFaceTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    fi.SetValue(ovrManager, true);
                    EditorUtility.SetDirty(ovrManager);
                }
            }
            else
            {
                // Disable Eye Tracking
                OVRProjectConfig.CachedProjectConfig.eyeTrackingSupport = OVRProjectConfig.FeatureSupport.None;
                if (ovrManager != null)
                {
                    var fi = typeof(OVRManager).GetField("requestEyeTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    fi.SetValue(ovrManager, false);
                    EditorUtility.SetDirty(ovrManager);
                }

                // Destroy Face Expressions component
                if (GameObject.FindObjectOfType<OVRFaceExpressions>())
                {
                    DestroyImmediate(GameObject.FindObjectOfType<OVRFaceExpressions>());
                }

                // Face Tracking
                OVRProjectConfig.CachedProjectConfig.faceTrackingSupport = OVRProjectConfig.FeatureSupport.None;
                if (ovrManager != null)
                {
                    var fi = typeof(OVRManager).GetField("requestFaceTrackingPermissionOnStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    fi.SetValue(ovrManager, false);
                    EditorUtility.SetDirty(ovrManager);
                }
            }
        }
#endif

        Texture2D isoSceneImage;

        void ExportSceneUpdate()
        {
            Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            GUI.Label(steptitlerect, "SCENE EXPORT", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "All geometry will be exported and uploaded to our dashboard. This will provide context for the spatial data points we automatically collect.", "normallabel");
            GUI.Label(new Rect(30, 440, 440, 440), "Refer to Online Documentation for more details about exporting scene geometry.", "normallabel");
            if (Cognitive3D_Preferences.Instance.TextureResize > 4) { Cognitive3D_Preferences.Instance.TextureResize = 4; }
            if (isoSceneImage == null)
            {
                isoSceneImage = EditorCore.GetSceneIsometricThumbnail();
            }

            //draw example scene image
            GUI.Box(new Rect(150, 130, 200, 150), isoSceneImage, "image_centered");

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
                GUI.Label(new Rect(0, 340, 500, 15), displayString, "miniheadercenter");
                GUI.Label(new Rect(120, 290, 30, 30), EditorCore.CircleCheckmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(0, 340, 500, 15), "Scene Not Exported", "miniheadercenter");
                GUI.Label(new Rect(120, 290, 30, 30), EditorCore.Alert, "image_centered");
            }

            if (numberOfLightsInScene > 50)
            {
                GUI.Label(new Rect(0, 370, 500, 15), "<color=red>For visualization in SceneExplorer, fewer than 50 lights are recommended</color>", "miniheadercenter");
            }

            if (GUI.Button(new Rect(150, 290, 200, 30), "Export Scene Geometry"))
            {
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                {
                    if (EditorUtility.DisplayDialog("Export Paused", "Cannot export scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
                    {
                        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
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
                string path = EditorCore.GetSubDirectoryPath(fullName);

                ExportUtility.GenerateSettingsFile(path, fullName);

                DebugInformationWindow.WriteDebugToFile(path + "debug.log");
                EditorUtility.SetDirty(EditorCore.GetPreferences());

                UnityEditor.AssetDatabase.SaveAssets();
                EditorCore.RefreshSceneVersion(null);
            }

            //texture resolution settings
            Rect toolsRect = new Rect(360, 290, 30, 30);
            if (GUI.Button(toolsRect, EditorCore.SettingsIcon,"image_centered")) //rename dropdown
            {
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

        int numberOfLightsInScene;

        bool UploadSceneGeometry = true;
        bool UploadThumbnail = true;
        bool UploadPreviouslyExportedDynamicMeshes = true;
        bool ExportAndUploadDynamicMeshesInScene = true;
        static DynamicObject[] dynamicObjectsInScene;
        bool SceneExistsOnDashboard;
        bool SceneHasExportFiles;

        void UploadSummaryUpdate()
        {
            GUI.Label(steptitlerect, "SCENE UPLOAD SUMMARY", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "These will be uploaded to the Cognitive3D Dashboard:", "normallabel");

            int heightOffset = 80;

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

            /// ////////////////////////////////////
            /// UPLOAD SCENE GEOMETRY
            /// ///////////////////////////////////
            var uploadSceneRect = new Rect(30, heightOffset, 30, 30);
            if (!SceneHasExportFiles)
            {
                //disable 'upload scene geometry' toggle
                GUI.Button(uploadSceneRect, EditorCore.BoxEmpty, "image_centered");
                GUI.Label(new Rect(60, heightOffset + 2, 400, 30), "Scene geometry (no files exported)", "normallabel");
                UploadSceneGeometry = false;
            }
            else
            {
                //upload scene geometry
                if (UploadSceneGeometry)
                {
                    if (GUI.Button(uploadSceneRect, EditorCore.BoxCheckmark, "image_centered"))
                    {
                        UploadSceneGeometry = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadSceneRect, EditorCore.BoxEmpty, "image_centered"))
                    {
                        UploadSceneGeometry = true;
                    }
                }
                string uploadGeometryText = "Scene geometry";
                if (SceneExistsOnDashboard)
                {
                    uploadGeometryText = "Scene geometry (version " + (sceneVersion+1) + ")";
                }
                GUI.Label(new Rect(60, heightOffset + 2, 400, 30), uploadGeometryText, "normallabel");
            }

            var uploadThumbnailRect = new Rect(30, heightOffset+40, 30, 30);
            if (!SceneExistsOnDashboard && !UploadSceneGeometry)
            {
                //disable 'upload scene geometry' toggle
                GUI.Button(uploadThumbnailRect, EditorCore.BoxEmpty, "image_centered");
                GUI.Label(new Rect(60, heightOffset + 42, 340, 30), "Scene thumbnail (no scene exists)", "normallabel");
            }
            else
            {
                //upload thumbnail
                if (UploadThumbnail)
                {
                    if (GUI.Button(uploadThumbnailRect, EditorCore.BoxCheckmark, "image_centered"))
                    {
                        UploadThumbnail = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadThumbnailRect, EditorCore.BoxEmpty, "image_centered"))
                    {
                        UploadThumbnail = true;
                    }
                }
                GUI.Label(new Rect(60, heightOffset + 42, 300, 30), "Scene thumbnail*", "normallabel");
            }

            /// ////////////////////////////////////
            /// EXPORTED DYNAMIC OBJECTS
            /// ///////////////////////////////////
            var uploadDynamicRect = new Rect(30, heightOffset + 80, 30, 30);
            int numExportedDynamicObjects = EditorCore.GetExportedDynamicObjectNames().Count;

            // No scene exists
            if (!SceneExistsOnDashboard && !UploadSceneGeometry)
            {
                GUI.Button(uploadDynamicRect, EditorCore.BoxEmpty, "image_centered");
                GUI.Label(new Rect(60, heightOffset + 82, 420, 30),  numExportedDynamicObjects + " previously exported dynamic meshes (no scene exists)", "normallabel");
                GUI.Label(new Rect(200, heightOffset + 360, 300, 40), "*You can adjust the scene camera to customise your thumbnail");
            }
            else
            {
                //upload dynamics toggle
                if (UploadPreviouslyExportedDynamicMeshes)
                {
                    if (GUI.Button(uploadDynamicRect, EditorCore.BoxCheckmark, "image_centered"))
                    {
                        UploadPreviouslyExportedDynamicMeshes = false;
                    }
                }
                else
                {
                    if (GUI.Button(uploadDynamicRect, EditorCore.BoxEmpty, "image_centered"))
                    {
                        UploadPreviouslyExportedDynamicMeshes = true;
                    }
                }

                GUI.Label(new Rect(60, heightOffset + 82, 420, 30), numExportedDynamicObjects + " previously exported dynamic meshes", "normallabel");
                GUI.Label(new Rect(200, heightOffset + 360, 300, 40), "*You can adjust the scene camera to customise your thumbnail");
            }

            /// ////////////////////////////////////
            /// DYNAMICS IN SCENE BUT NOT EXPORTED
            /// ///////////////////////////////////
            var exportDynamicRect = new Rect(30, heightOffset + 120, 30, 30);
            int dynamicObjectsNotExported = 0;
            List<string> exportedDynamicObjectNames = EditorCore.GetExportedDynamicObjectNames();

            // Go through all dynamic objects in scene
            // See if it has been exported already (use mesh name as an identifier)
            // IGNORE CONTROLLERS
            dynamicObjectsInScene = FindObjectsOfType<DynamicObject>();

            foreach (var dynamicObject in dynamicObjectsInScene)
            {
                bool found = false;
                if (!dynamicObject.IsController)
                {
                    // In case there are NO exported object
                    if (exportedDynamicObjectNames.Count == 0)
                    {
                        dynamicObjectsNotExported++;
                    }
                    else
                    {
                        foreach (var exportedDynamicObjectName in exportedDynamicObjectNames)
                        {
                            if (dynamicObject.MeshName == exportedDynamicObjectName)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            dynamicObjectsNotExported++;
                        }
                    }
                }
            }

            // No scene exists
            if (!SceneExistsOnDashboard && !UploadSceneGeometry)
            {
                GUI.Button(exportDynamicRect, EditorCore.BoxEmpty, "image_centered");
                GUI.Label(new Rect(60, heightOffset + 122, 400, 30), dynamicObjectsNotExported + " new dynamic mesh(es) (no scene exists)", "normallabel");
            }
            else
            {
                // upload dynamics toggle
                if (ExportAndUploadDynamicMeshesInScene)
                {
                    if (GUI.Button(exportDynamicRect, EditorCore.BoxCheckmark, "image_centered"))
                    {
                        ExportAndUploadDynamicMeshesInScene = false;
                    }
                }
                else
                {
                    if (GUI.Button(exportDynamicRect, EditorCore.BoxEmpty, "image_centered"))
                    {
                        ExportAndUploadDynamicMeshesInScene = true;
                    }
                }

                GUI.Label(new Rect(60, heightOffset + 122, 300, 30), dynamicObjectsNotExported + " new dynamic mesh(es)", "normallabel");
            }


            /// ////////////////////////////////////
            /// SCENE THUMBNAIL PREVIEW
            /// ///////////////////////////////////
            var thumbnailRect = new Rect(40, heightOffset+170, 420, 180);
            Texture2D savedThumbnail = null;
            if (UploadThumbnail)
            {
                GUI.Label(new Rect(150, heightOffset + 312, 200, 20), "New Thumbnail from Scene View");
                var sceneRT = EditorCore.GetSceneRenderTexture();
                if (sceneRT != null)
                    GUI.Box(thumbnailRect, sceneRT, "image_centeredboxed");
                else
                    GUI.Box(thumbnailRect, "Scene view not found", "image_centeredboxed");
            }
            // look for thumbnail image file
            else if (EditorCore.GetSceneThumbnail(settings, ref savedThumbnail, false))
            {
                GUI.Label(new Rect(150, heightOffset + 280, 200, 20), "Thumbnail from previous scene version");
                GUI.Box(thumbnailRect, savedThumbnail, "image_centeredboxed");
            }
            // scene exists and has been uploaded, but no image to fall back to use
            else if (SceneExistsOnDashboard)
            {
                GUI.Box(thumbnailRect, "Fallback thumbnail\nnot available", "image_centeredboxed");
            }
            // if a new scene version is uploaded, can it use the previous thumbnail?
            else
            {
                GUI.Box(thumbnailRect, "Thumbnail not uploaded", "image_centeredboxed");
            }
        }

        void UploadProgressUpdate()
        {
            GUI.Label(steptitlerect, "SCENE UPLOADING", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "Uploading Scene Geometry. This will take a moment.", "normallabel");
        }

        void DoneUpdate()
        {
            GUI.Label(steptitlerect, "NEXT STEPS", "steptitle");

            //1 play a session, see it on the dashboard
            GUI.Label(new Rect(30, 30, 440, 440), "The " + EditorCore.DisplayValue(DisplayKey.ManagerName) + " in your scene will record user position, gaze and basic device information.\n\nTo record a Session, just <b>Press Play</b>, put on your headset and look around. <b>Press Stop</b> when you're finished and you'll be able to replay the session on our Dashboard.", "normallabel");
            Rect buttonRect = new Rect(150, 160, 200, 30);
            if (GUI.Button(buttonRect, "Open Dashboard       "))
            {
                var sceneSettings = Cognitive3D_Preferences.FindCurrentScene();
                if (sceneSettings == null)
                {
                    Debug.LogError("Cannot find scene settings for " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                    return;
                }
                Application.OpenURL(CognitiveStatics.GetSceneUrl(sceneSettings.SceneId, sceneSettings.VersionNumber));
            }
            Rect onlineRect = buttonRect;
            onlineRect.x += 82;
            GUI.Label(onlineRect, EditorCore.ExternalIcon);

            //2 link to documentation on session tags
            GUI.Label(new Rect(30, 210, 440, 440), "Please note that sessions run in the editor won't count towards aggregate metrics. For more information, please see our documentation.", "normallabel");
            Rect tagsRect = new Rect(100, 285, 300, 30);
            if (GUI.Button(tagsRect, "Open Session Tags Documentation   "))
            {
                Application.OpenURL(URL_SESSION_TAGS_DOCS);
            }
            Rect tagsRectIcon = tagsRect;
            tagsRectIcon.x += 130;
            GUI.Label(tagsRectIcon, EditorCore.ExternalIcon);

            //3 overview of features in help window
            GUI.Label(new Rect(30, 325, 440, 440), "You can continue your integration to get more insights including:", "normallabel");
            GUI.Label(new Rect(30, 370, 440, 440), " - Custom Events\n - ExitPoll Surveys\n - Ready Room User Onboarding\n - Dynamic Objects", "normallabel");
            if (GUI.Button(new Rect(150, 460, 200, 30), "Open Help Window"))
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
                if (GUI.Button(buttonrect, new GUIContent("Open Online Documentation       ", url)))
                {
                    Application.OpenURL(url);
                }
                Rect onlineRect = buttonrect;
                onlineRect.x += 82;
                GUI.Label(onlineRect, EditorCore.ExternalIcon);
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
                    appearDisabled = !AllSetupComplete;
                    if (!AllSetupComplete)
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
                    appearDisabled = !AllSetupComplete;
                    if (!AllSetupComplete)
                    {
                        if (appearDisabled)
                        {
                            onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without configuring the player prefab?", "Yes", "No")) { currentPage++; } };
                        }
                    }
#endif
                    onclick += () => { numberOfLightsInScene = FindObjectsOfType<Light>().Length; };
                    break;
                case Page.QuestProSetup:
#if C3D_OCULUS
                    text = "Apply";
                    onclick += () => ApplyOculusSettings();
#endif
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
                    // Look below at onclick()
                    // These all happen after that

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
                                    UploadSceneAndDynamics(UploadPreviouslyExportedDynamicMeshes, ExportAndUploadDynamicMeshesInScene, UploadSceneGeometry, UploadThumbnail, true);
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
                            UploadSceneAndDynamics(UploadPreviouslyExportedDynamicMeshes, ExportAndUploadDynamicMeshesInScene, UploadSceneGeometry, UploadThumbnail, true);
                        }
                    };
                    buttonDisabled = !(SceneExistsOnDashboard || (SceneHasExportFiles && UploadSceneGeometry));
                    text = "Upload       ";
                    break;
                case Page.SetupComplete:
                    onclick = () => Close();
                    text = "Close";
                    break;
                case Page.ProjectError:
                    buttonrect = new Rect(600, 0, 0, 0);
                    break;
                default:
                    throw new System.NotSupportedException();
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

            if (currentPage == Page.SceneUpload)
            {
                Rect onlineRect = buttonrect;
                onlineRect.x += 25;
                GUI.Label(onlineRect, EditorCore.CloudUploadIcon);
            }
        }

        internal static void ExportAllDynamicsInScene()
        {
            List<DynamicObject> dynsInSceneList = new List<DynamicObject>();        
            
            // This array HAS TO BE reinitialized here because
            // this function can be from other places and
            // we cannot guarantee that it has been initialized
            dynamicObjectsInScene = FindObjectsOfType<DynamicObject>();
            foreach (var dyn in dynamicObjectsInScene)
            {
                dynsInSceneList.Add(dyn);
            }
            ExportUtility.ExportDynamicObjects(dynsInSceneList);
        }

        static float sceneUploadProgress;
        static double sceneUploadStartTime;
        //TODO styled UI element to display web request progress instead of built-in unity popup
        static void ReceiveSceneUploadProgress(float progress)
        {
            sceneUploadProgress = progress;
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
                    break;
                case Page.QuestProSetup:
                    break;
                case Page.SceneExport:
                    onclick = () => currentPage = Page.PlayerSetup;
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
                default:
                    throw new System.NotSupportedException();
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

        static List<DynamicObject> GetDynamicObjectsInScene()
        {
            return new List<DynamicObject>(GameObject.FindObjectsOfType<DynamicObject>());
        }

        /// <summary>
        /// Upload exported scene and optionally, dynamics
        /// </summary>
        /// <param name="uploadExportedDynamics">If true, upload dynamics from export directory</param>
        /// <param name="exportAndUploadDynamicsFromScene">If true, exports dynamics from scene, and uploads them</param>
        /// <param name="uploadSceneGeometry">If true, upload scene geometry</param>
        /// <param name="uploadThumbnail">If true, upload scene thumbnail</param>
        /// <param name="showPopups">If true, show popups (use false for automation)</param>
        internal static void UploadSceneAndDynamics(bool uploadExportedDynamics, bool exportAndUploadDynamicsFromScene, bool uploadSceneGeometry, bool uploadThumbnail, bool showPopups = false)
        {
            System.Action completedmanifestupload = delegate
            {
                if (uploadExportedDynamics)
                {
                    ExportUtility.UploadAllDynamicObjectMeshes(showPopups);
                }
                else if (exportAndUploadDynamicsFromScene)
                {
                    List<string> dynamicMeshNames = new List<string>();
                    foreach (var dyn in dynamicObjectsInScene)
                    {
                        dynamicMeshNames.Add(dyn.MeshName);
                    }
                    ExportUtility.UploadDynamicObjects(dynamicMeshNames, showPopups);
                }
                currentPage = Page.SetupComplete;
                CompletedUpload = true;
            };

            // Fifth: upload manifest
            System.Action completedRefreshSceneVersion = delegate
            {
                if (uploadExportedDynamics || exportAndUploadDynamicsFromScene)
                {
                    //TODO ask if dev wants to upload disabled dynamic objects as well (if there are any)
                    AggregationManifest manifest = new AggregationManifest();
                    manifest.AddOrReplaceDynamic(GetDynamicObjectsInScene());
                    EditorCore.UploadManifest(manifest, completedmanifestupload, completedmanifestupload);
                }
                else
                {
                    completedmanifestupload.Invoke();
                }
            };

            // Fourth upload dynamics
            System.Action<int> completeSceneUpload = delegate (int responseCode)
            {
                if (responseCode == 200 || responseCode == 201)
                {
                    if (exportAndUploadDynamicsFromScene)
                    {
                        ExportAllDynamicsInScene();
                    }
                    EditorCore.RefreshSceneVersion(completedRefreshSceneVersion); // likely completed in previous step, but just in case
                }
                else
                {
                    // ExportUtility displays an error popup, so don't need to do other UI here
                    currentPage = Page.SceneUpload;
                }
            };

            //third upload scene
            System.Action completeScreenshot = delegate
            {
                Cognitive3D_Preferences.SceneSettings current = Cognitive3D_Preferences.FindCurrentScene();
                if (current == null)
                {
                    Debug.LogError("Trying to upload to a scene with no settings");
                    return;
                }

                if (uploadSceneGeometry)
                {
                    if (showPopups)
                    {
                        if (string.IsNullOrEmpty(current.SceneId))
                        {
                            // NEW SCENE
                            if (EditorUtility.DisplayDialog("Upload New Scene", "Upload " + current.SceneName + " to " + EditorCore.DisplayValue(DisplayKey.ViewerName) + "?", "Ok", "Cancel"))
                            {
                                sceneUploadProgress = 0;
                                sceneUploadStartTime = EditorApplication.timeSinceStartup;
                                currentPage = Page.SceneUploadProgress;
                                ExportUtility.UploadDecimatedScene(current, completeSceneUpload, ReceiveSceneUploadProgress);
                            }
                        }
                        else
                        {
                            // NEW SCENE VERSION
                            if (EditorUtility.DisplayDialog("Upload New Version", "Upload a new version of this existing scene? Will archive previous version", "Ok", "Cancel"))
                            {
                                currentPage = Page.SceneUploadProgress;
                                sceneUploadProgress = 0;
                                sceneUploadStartTime = EditorApplication.timeSinceStartup;
                                ExportUtility.UploadDecimatedScene(current, completeSceneUpload, ReceiveSceneUploadProgress);
                            }
                        }
                    }
                    else // UPLOAD WITHOUT POPUPS
                    {
                        ExportUtility.UploadDecimatedScene(current, completeSceneUpload, ReceiveSceneUploadProgress);
                    }
                }
                else
                {
                    //check to upload the thumbnail (without the scene geo)
                    if (uploadThumbnail)
                    {
                        EditorCore.UploadSceneThumbnail(current);
                    }
                    completeSceneUpload.Invoke(200);
                }
            };

            //second save screenshot
            System.Action completedRefreshSceneVersion1 = delegate
            {
                if (uploadThumbnail)
                {
                    EditorCore.SaveScreenshot(EditorCore.GetSceneRenderTexture(), UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
                }
                else
                {
                    //use the existing screenshot (assuming it exists)
                    completeScreenshot.Invoke();
                    completeScreenshot = null;
                }
            };

            CompletedUpload = false;
            EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
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
                    Debug.Log(SteamVR_Input.GetActionsFileName());
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
                Util.logDevelopment("SceneSetup.AppendSteamVRActionSet Added Cognitive3D Action Set");
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
                Util.logDevelopment("SceneSetup.SetDefaultBindings save Cognitive3D input bindings");
                SaveBindingFile(bindingfile);
            }
            else
            {
                Debug.LogError("SceneSetup.SetDefaultBindings failed to load steamvr binding file");
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

        private void OnDestroy()
        {
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManager_activeSceneChangedInEditMode;
        }
    }
}