using UnityEngine;
using CognitiveVR;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// initializes CognitiveVR analytics. Add components to track additional events
/// </summary>

namespace CognitiveVR
{
    public class CognitiveVR_Manager : MonoBehaviour
    {
        #region Events
        public delegate void CoreInitHandler(Error initError);
        /// <summary>
        /// CognitiveVR Core.Init callback
        /// </summary>
        public static event CoreInitHandler OnInit;
        public void InitEvent(Error initError)
        {
            foreach (var v in GetComponentsInChildren<CognitiveVRAnalyticsComponent>())
            {
                v.CognitiveVR_Init(initError);
            }
            if (OnInit != null) { OnInit(initError); }
        }

        public delegate void UpdateHandler();
        /// <summary>
        /// Update. Called through Manager's update function for easy enabling/disabling
        /// </summary>
        public static event UpdateHandler OnUpdate;
        public void UpdateEvent() { if (OnUpdate != null) { OnUpdate(); } }

        public delegate void TickHandler();
        /// <summary>
        /// repeatedly called. interval is CognitiveVR_Preferences.Instance.PlayerSnapshotInterval
        /// </summary>
        public static event TickHandler OnTick;
        public void TickEvent() { if (OnTick != null) { OnTick(); } }

        public delegate void QuitHandler(); //quit
        /// <summary>
        /// called from Unity's built in OnApplicationQuit. Cancelling quit gets weird - do all application quit stuff in Manager
        /// </summary>
        public static event QuitHandler OnQuit;
        public void QuitEvent() { if (OnQuit != null) { OnQuit(); } }

        public delegate void LevelLoadedHandler(); //level
        /// <summary>
        /// called from Unity's built in OnLevelWasLoaded(int id) or SceneManager.SceneLoaded(scene scene) in 5.4
        /// </summary>
        public static event LevelLoadedHandler OnLevelLoaded;
        public void LevelLoadEvent() { if (OnLevelLoaded != null) { OnLevelLoaded(); } }

#if CVR_STEAMVR
        public delegate void PoseUpdateHandler(params object[] args);
        /// <summary>
        /// params are SteamVR pose args. does not check index. Currently only used for TrackedDevice valid/disconnected
        /// </summary>
        public static event PoseUpdateHandler OnPoseUpdate;
        public void PoseUpdateEvent(params object[] args) { if (OnPoseUpdate != null) { OnPoseUpdate(args); } }

        public delegate void PoseEventHandler(Valve.VR.EVREventType eventType);
        /// <summary>
        /// polled in Update. sends all events from Valve.VR.OpenVR.System.PollNextEvent(ref vrEvent, size)
        /// </summary>
        public static event PoseEventHandler OnPoseEvent;
        public void PoseEvent(Valve.VR.EVREventType eventType) { if (OnPoseEvent != null) { OnPoseEvent(eventType); } }

#endif
        #endregion

        #region HMD and Controllers

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
                        _hmd = Camera.main.transform;
                    }
#else
                    _hmd = Camera.main.transform;
#endif
                }
                return _hmd;
            }
        }

#if CVR_STEAMVR
        static Transform[] controllers = new Transform[2];
#endif
        /// <summary>Returns Tracked Controller by index. Based on SDK. MAY RETURN NULL!</summary>
        public static Transform GetController(int id)
        {
#if CVR_STEAMVR
            //TODO update controllers when new controller is detected
            if (controllers[id] == null)
            {
                SteamVR_ControllerManager cm = FindObjectOfType<SteamVR_ControllerManager>();
                if (cm != null)
                {
                    if (cm.left != null)
                        controllers[0] = cm.left.transform;
                    if (cm.right != null)
                        controllers[1] = cm.right.transform;
                }
            }
            return controllers[id];
#elif CVR_OCULUS
            // OVR doesn't allow access to controller transforms - Position and Rotation available in OVRInput
            return null;
#else
            return null;
#endif
        }

#endregion

        private static CognitiveVR_Manager instance;
        YieldInstruction playerSnapshotInverval;
        public static double TimeStamp { get; private set; }

        /// <summary>
        /// This will return SystemInfo.deviceUniqueIdentifier unless SteamworksUserTracker is present. only register users once! otherwise, there will be lots of uniqueID users with no data!
        /// TODO make this loosly tied to SteamworksUserTracker - if this component is removed, ideally everything will still compile. maybe look for some interface?
        /// </summary>
        EntityInfo GetUniqueEntityID()
        {
            if (GetComponent<SteamworksUserTracker>() == null)
                return CognitiveVR.EntityInfo.createUserInfo(SystemInfo.deviceUniqueIdentifier);
            return null;
        }

        void Start()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            CognitiveVR.InitParams initParams = CognitiveVR.InitParams.create
            (
                customerId: CognitiveVR_Preferences.Instance.CustomerID,
                logEnabled: false,
                userInfo: GetUniqueEntityID()
            );
            CognitiveVR.Core.init(initParams, InitEvent);

            GameObject.DontDestroyOnLoad(gameObject);

            playerSnapshotInverval = new WaitForSeconds(CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval);
            StartCoroutine(Tick());

            GetController(0);

#if CVR_STEAMVR
            SteamVR_Utils.Event.Listen("new_poses", PoseUpdateEvent);
#endif

#if UNITY_5_4
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_SceneLoaded;
#endif
        }

#if UNITY_5_4
        private void SceneManager_SceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            LevelLoadEvent();
            TimeStamp = Util.Timestamp();
        }
#else
        void OnLevelWasLoaded(int id)
        {
            LevelLoadEvent();
            TimeStamp = Util.Timestamp();
        }
#endif

        IEnumerator Tick()
        {
            while (true)
            {
                yield return playerSnapshotInverval;
                TickEvent();
            }
        }

        void Update()
        {
            UpdateEvent();

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
                    PoseEvent((Valve.VR.EVREventType)vrEvent.eventType);
                }
            }
#endif
        }

#region Application Quit
        void OnApplicationQuit()
        {
            QuitEvent();
        }
#endregion
    }
}