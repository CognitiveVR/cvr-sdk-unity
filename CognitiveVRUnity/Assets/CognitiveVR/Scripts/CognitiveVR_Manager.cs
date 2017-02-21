using UnityEngine;
using CognitiveVR;
using System.Collections;
using System.Collections.Generic;
#if CVR_STEAMVR
using Valve.VR;
#endif

/// <summary>
/// initializes CognitiveVR analytics. Add components to track additional events
/// </summary>

namespace CognitiveVR
{
    public partial class CognitiveVR_Manager : MonoBehaviour
    {
        #region Events
        public delegate void CoreInitHandler(Error initError);
        /// <summary>
        /// CognitiveVR Core.Init callback
        /// </summary>
        public static event CoreInitHandler InitEvent;
        public void OnInit(Error initError)
        {
            var components = GetComponentsInChildren<CognitiveVR.Components.CognitiveVRAnalyticsComponent>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].CognitiveVR_Init(initError);
            }
            PlayerRecorderInit(initError);
            if (InitEvent != null) { InitEvent(initError); }
        }

        public delegate void UpdateHandler();
        /// <summary>
        /// Update. Called through Manager's update function for easy enabling/disabling
        /// </summary>
        public static event UpdateHandler UpdateEvent;
        public void OnUpdate() { if (UpdateEvent != null) { UpdateEvent(); } }

        public delegate void TickHandler();
        /// <summary>
        /// repeatedly called. interval is CognitiveVR_Preferences.Instance.PlayerSnapshotInterval
        /// </summary>
        public static event TickHandler TickEvent;
        public void OnTick() { if (TickEvent != null) { TickEvent(); } }

        public delegate void QuitHandler(); //quit
        /// <summary>
        /// called from Unity's built in OnApplicationQuit. Cancelling quit gets weird - do all application quit stuff in Manager
        /// </summary>
        public static event QuitHandler QuitEvent;
        public void OnQuit() { if (QuitEvent != null) { QuitEvent(); } }

        public delegate void SendDataHandler(); //send data
        /// <summary>
        /// called when CognitiveVR_Manager.SendData is called. this is called when the data is actually sent to the server
        /// </summary>
        public static event SendDataHandler SendDataEvent;
        public void OnSendData() { if (SendDataEvent != null) { SendDataEvent(); } }

        public delegate void LevelLoadedHandler(); //level
        /// <summary>
        /// called from Unity's SceneManager.SceneLoaded(scene scene)
        /// </summary>
        public static event LevelLoadedHandler LevelLoadedEvent;
        public void OnLevelLoaded() { if (LevelLoadedEvent != null) { LevelLoadedEvent(); } }

#if CVR_STEAMVR
        //1.1
        /*
        public delegate void PoseUpdateHandler(params object[] args);
        /// <summary>
        /// params are SteamVR pose args. does not check index. Currently only used for TrackedDevice valid/disconnected
        /// </summary>
        public static event PoseUpdateHandler PoseUpdateEvent;
        public void OnPoseUpdate(params object[] args) { if (PoseUpdateEvent != null) { PoseUpdateEvent(args); } }
        */

        //1.2
        public delegate void PoseUpdateHandler(params TrackedDevicePose_t[] args);
        /// <summary>
        /// params are SteamVR pose args. does not check index. Currently only used for TrackedDevice valid/disconnected
        /// </summary>
        public static event PoseUpdateHandler PoseUpdateEvent;
        public void OnPoseUpdate(params TrackedDevicePose_t[] args) { if (PoseUpdateEvent != null) { PoseUpdateEvent(args); } }

        //1.1 and 1.2
        public delegate void PoseEventHandler(Valve.VR.EVREventType eventType);
        /// <summary>
        /// polled in Update. sends all events from Valve.VR.OpenVR.System.PollNextEvent(ref vrEvent, size)
        /// </summary>
        public static event PoseEventHandler PoseEvent;
        public void OnPoseEvent(Valve.VR.EVREventType eventType) { if (PoseEvent != null) { PoseEvent(eventType); } }

#endif
        #endregion

        #region HMD and Controllers


#if CVR_OCULUS
        static OVRCameraRig _cameraRig;
        static OVRCameraRig CameraRig
        {
            get
            {
                if (_cameraRig == null)
                {
                    _cameraRig = FindObjectOfType<OVRCameraRig>();
                }
                return _cameraRig;
            }
        }
#endif

        private static Transform _hmd;
        /// <summary>Returns HMD based on included SDK, or Camera.Main if no SDK is used. MAY RETURN NULL!</summary>
        public static Transform HMD
        {
            get
            {
                if (_hmd == null)
                {
#if CVR_STEAMVR
                    SteamVR_Camera cam = FindObjectOfType<SteamVR_Camera>();
                    if (cam != null){ _hmd = cam.transform; }
                    if (_hmd == null)
                    {
                        if (Camera.main == null)
                            return null;
                        _hmd = Camera.main.transform;
                    }
#elif CVR_OCULUS
                    OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
                    if (rig != null)
                    {
                        Camera cam = rig.GetComponentInChildren<Camera>();
                        _hmd = cam.transform;
                    }
                    if (_hmd == null)
                    {
                        if (Camera.main == null)
                            return null;
                        _hmd = Camera.main.transform;
                    }
#elif CVR_FOVE
                    /*FoveEyeCamera eyecam = FindObjectOfType<FoveEyeCamera>();
                    if (eyecam != null)
                    {
                        Camera cam = eyecam.GetComponentInChildren<Camera>();
                        _hmd = cam.transform;
                    }*/
                    if (_hmd == null)
                    {
                        if (Camera.main == null)
                            return null;
                        _hmd = Camera.main.transform;
                    }
#else
                    if (Camera.main == null)
                        return null;
                    _hmd = Camera.main.transform;
#endif
                }
                return _hmd;
            }
        }

#if CVR_STEAMVR

        static void InitializeControllers()
        {
            if (controllers[0].id < 0)
            {
                SteamVR_ControllerManager cm = FindObjectOfType<SteamVR_ControllerManager>();
                if (cm != null)
                {
                    if (cm.left != null)
                    {
                        int controllerIndex = (int)cm.left.GetComponent<SteamVR_TrackedObject>().index;
                        if (controllerIndex > 0)
                        {
                            controllers[0] = new ControllerInfo() { transform = cm.left.transform, isRight = false, id = controllerIndex };
                        }
                    }
                }
            }
            if (controllers[1].id < 0)
            {
                SteamVR_ControllerManager cm = FindObjectOfType<SteamVR_ControllerManager>();
                if (cm != null)
                {
                    if (cm.right != null)
                    {
                        int controllerIndex = (int)cm.right.GetComponent<SteamVR_TrackedObject>().index;
                        if (controllerIndex > 0)
                        {
                            controllers[1] = new ControllerInfo() { transform = cm.right.transform, isRight = true, id = controllerIndex };
                        }
                    }
                }
            }


        }

        public class ControllerInfo
        {
            public Transform transform;
            public bool isRight;
            public int id = -1;
        }

        static ControllerInfo[] controllers = new ControllerInfo[2] { new ControllerInfo(), new ControllerInfo() };

        public static ControllerInfo GetControllerInfo(int deviceID)
        {
            InitializeControllers();
            if (controllers[0].id == deviceID) { return controllers[0]; }
            if (controllers[1].id == deviceID) { return controllers[1]; }
            return null;
        }
#endif
        /// <summary>
        /// steamvr ID is tracked device id
        /// oculus ID 0 is right, 1 is left controller
        /// </summary>
        public static Transform GetController(int deviceid)
        {
#if CVR_STEAMVR
            InitializeControllers();
            if (controllers[0].id == deviceid) { return controllers[0].transform; }
            if (controllers[1].id == deviceid) { return controllers[1].transform; }
            return null;
#elif CVR_OCULUS
            // OVR doesn't allow access to controller transforms - Position and Rotation available in OVRInput
            return null;
#else
            return null;
#endif
        }

        public static Transform GetController(bool right)
        {
#if CVR_STEAMVR
            InitializeControllers();

            if (right == controllers[0].isRight) { return controllers[0].transform; }
            if (right == controllers[1].isRight) { return controllers[1].transform; }
            return null;
#elif CVR_OCULUS
            return null;
#else
            return null;
#endif
        }

        /// <summary>Returns Tracked Controller position by index. Based on SDK</summary>
        public static Vector3 GetControllerPosition(bool rightController)
        {
#if CVR_STEAMVR
            InitializeControllers();
            if (rightController)
            {
                if (controllers[0].transform != null)
                { return controllers[0].transform.position; }
            }
            else
            {
                if (controllers[1].transform != null)
                { return controllers[1].transform.position; }
            }
            return Vector3.zero;
#elif CVR_OCULUS
            if (rightController)
            {
                if (CameraRig != null)
                    return CameraRig.rightHandAnchor.position;
            }
            else
            {
                if (CameraRig != null)
                    return CameraRig.leftHandAnchor.position;
            }
            return Vector3.zero;
#else
            return Vector3.zero;
#endif
        }

        #endregion

        private static CognitiveVR_Manager instance;
        public static CognitiveVR_Manager Instance { get { return instance; } }
        YieldInstruction playerSnapshotInverval;

        [Tooltip("Enable cognitiveVR internal debug messages. Can be useful for debugging")]
        public bool EnableLogging = false;
        [Tooltip("Enable automatic initialization. If false, you must manually call Initialize(). Useful for delaying startup in multiplayer games")]
        public bool InitializeOnStart = true;

        /// <summary>
        /// This will return SystemInfo.deviceUniqueIdentifier unless SteamworksUserTracker is present. only register users once! otherwise, there will be lots of uniqueID users with no data!
        /// TODO make this loosly tied to SteamworksUserTracker - if this component is removed, ideally everything will still compile. maybe look for some interface?
        /// </summary>
        EntityInfo GetUniqueEntityID()
        {
            if (GetComponent<CognitiveVR.Components.SteamworksUser>() == null)
                return CognitiveVR.EntityInfo.createUserInfo(SystemInfo.deviceUniqueIdentifier);
            return null;
        }

        void Start()
        {
            GameObject.DontDestroyOnLoad(gameObject);
            if (InitializeOnStart)
                Initialize();
        }

        public void Initialize()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; } //destroy if there's already another manager
            if (instance == this) { return; } //skip if this manage has already been initialized
            instance = this;

            if (string.IsNullOrEmpty(CognitiveVR_Preferences.Instance.CustomerID))
            {
                if (EnableLogging) { Debug.LogWarning("CognitiveVR_Manager CustomerID is missing! Cannot init CognitiveVR"); }
                return;
            }

            CognitiveVR.InitParams initParams = CognitiveVR.InitParams.create
            (
                customerId: CognitiveVR_Preferences.Instance.CustomerID,
                logEnabled: EnableLogging,
                userInfo: GetUniqueEntityID()

            );
            CognitiveVR.Core.init(initParams, OnInit);

            playerSnapshotInverval = new WaitForSeconds(CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval);
            StartCoroutine(Tick());

#if CVR_STEAMVR
            SteamVR_Events.NewPoses.AddListener(OnPoseUpdate); //steamvr 1.2
            //SteamVR_Utils.Event.Listen("new_poses", OnPoseUpdate); //steamvr 1.1
#endif

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_SceneLoaded;
        }

        private void SceneManager_SceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            OnLevelLoaded();
        }

        IEnumerator Tick()
        {
            while (true)
            {
                yield return playerSnapshotInverval;
                OnTick();
            }
        }

        void Update()
        {
            OnUpdate();
            UpdatePlayerRecorder();

#if CVR_STEAMVR
            var system = Valve.VR.OpenVR.System;
            if (system != null)
            {
                var vrEvent = new Valve.VR.VREvent_t();
                var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Valve.VR.VREvent_t));
                for (int i = 0; i < 64; i++)
                {
                    if (!system.PollNextEvent(ref vrEvent, size))
                        break;
                    OnPoseEvent((Valve.VR.EVREventType)vrEvent.eventType);
                }
            }
#endif
        }

        void OnDestroy()
        {
            OnDestroyPlayerRecorder();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
        }

#if UNITY_EDITOR
        //replace with this in unity 5.6
        //http://answers.unity3d.com/questions/495007/editor-script-that-executes-before-build.html
        //class PreBuildProcess : UnityEditor.Build.IPreprocessBuild{}

        [UnityEditor.Callbacks.PostProcessScene()]
        public static void OnPostProcessScene()
        {
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                Debug.Log("cognitiveVR Preferences clearing non-essential info");
                CognitiveVR_Preferences asset = UnityEditor.AssetDatabase.LoadAssetAtPath<CognitiveVR_Preferences>("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
                if (asset == null) { return; }
                //remove any potentially sensitive data from preferences as the asset is being built
                asset.sessionID = string.Empty;
                asset.UserName = string.Empty;
                asset.UserData = new Json.UserData();
                asset.SelectedOrganization = new Json.Organization();
                asset.SelectedProduct = new Json.Product();
            }
        }
#endif

        #region Application Quit
        bool hasCanceled = false;
        void OnApplicationQuit()
        {
            if (QuitEvent == null) { return; }
            if (hasCanceled) { return; }
            Application.CancelQuit();
            hasCanceled = true;
            OnQuit();
            StartCoroutine(SlowQuit());
        }

        IEnumerator SlowQuit()
        {
            yield return new WaitForSeconds(1);
            Application.Quit();
        }
        #endregion
    }
}