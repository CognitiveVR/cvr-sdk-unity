using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

//container for data and simple instance implementations (enable,disable) for dynamic object
//also includes fields for initialization
//this would also include some nice functions for beginning/ending engagements

//if this is using a dynamic object id pool, will grab a new id every time 'OnEnable' is called. if this is not needed, changing that function to 'Start' should be fine

namespace Cognitive3D
{
#if C3D_VIVEWAVE
    [DefaultExecutionOrder(+10)] //this must run after PoseTrackerManager on controllers is enabled
#endif
    [AddComponentMenu("Cognitive3D/Common/Dynamic Object")]
    [HelpURL("https://docs.cognitive3d.com/unity/dynamic-objects/")]
    public class DynamicObject : MonoBehaviour
    {
        /// <summary>
        /// To know the "ID Source Type" set from inspector
        /// Must follow this order:
        ///     CustomID = 0
        ///     GeneratedID = 1
        ///     PoolID = 2
        /// </summary>
        public enum IdSourceType
        {
            CustomID = 0,
            GeneratedID = 1,
            PoolID = 2
        }

        // Default idSource
        public IdSourceType idSource = IdSourceType.CustomID;
        
        //developer facing high level controller type selection
        public enum ControllerType
        {
            Quest2 = 1,
            QuestPro = 2,
            Quest3 = 9,
            ViveWand = 3,
            WindowsMRController = 4,
            SteamIndex = 5,
            PicoNeo3 = 6,
            PicoNeo4 = 7,
            ViveFocus = 8,
            //Generic = 0, //basically a non-branded oculus touch controller
            //Hand = 9, //might suggest that this includes skeletal hand tracking, which needs some more design
        }
        
        //used internally to have a consistent mesh name string
        internal enum CommonDynamicMesh
        {
            ViveController = 0,
            OculusRiftTouchLeft = 1,
            OculusRiftTouchRight = 2,
            ViveTracker = 3,
            ExitPoll = 4, //used internally
            LeapMotionHandLeft = 5,
            LeapMotionHandRight = 6,
            WindowsMixedRealityLeft = 7,
            WindowsMixedRealityRight = 8,
            VideoSphereLatitude = 9,
            VideoSphereCubemap = 10,
            SnapdragonVRController = 11,
            ViveFocusControllerRight = 12,
            OculusQuestTouchLeft = 13,
            OculusQuestTouchRight = 14,
            PicoNeoControllerLeft = 15,
            PicoNeoControllerRight = 16,
            PicoNeo3ControllerLeft = 17,
            PicoNeo3ControllerRight = 18,
            Unknown = 19,
            ViveFocusControllerLeft = 20,
            SteamIndexLeft = 21,
            SteamIndexRight = 22,
            PicoNeo4ControllerLeft = 23,
            PicoNeo4ControllerRight = 24,
            QuestProTouchLeft = 25,
            QuestProTouchRight = 26,
            QuestPlusTouchLeft = 27,
            QuestPlusTouchRight = 28,
        }

        //used internally to have a consistent button input image
        internal enum ControllerDisplayType
        {
            vive_controller = 1, //wand
            vive_focus_controller_right = 2,
            vive_focus_controller_left = 14,
            oculus_rift_controller_left = 3,
            oculus_rift_controller_right = 4,
            oculus_quest_touch_left = 5,
            oculus_quest_touch_right = 6,
            windows_mixed_reality_controller_left = 7,
            windows_mixed_reality_controller_right = 8,
            pico_neo_2_eye_controller_left = 9,
            pico_neo_2_eye_controller_right = 10,
            pico_neo_3_eye_controller_left = 11,
            pico_neo_3_eye_controller_right = 12,
            unknown = 13,
            pico_neo_4_eye_controller_left = 15,
            pico_neo_4_eye_controller_right = 16,
            steam_index_left = 17,
            steam_index_right = 18,
            quest_pro_touch_left = 19,
            quest_pro_touch_right = 20,
            quest_plus_touch_left = 21,
            quest_plus_touch_right = 22,
        }


#if UNITY_EDITOR
        //stores instanceid. used to check if something in editor has changed
        [System.NonSerialized]
        public int editorInstanceId;
        public bool HasCollider()
        {
            if (Cognitive3D_Preferences.Instance.DynamicObjectSearchInParent)
            {
                var collider = GetComponentInChildren<Collider>();
                if (collider == null)
                {
                    return false;
                }
                return true;
            }
            return true;
        }
#endif

        //data id is the general way to get the actual id from the dynamic object (generated or custom id). use GetId
        private string DataId;

        //custom id is set in editor and will be used when set. internal to be accessed by various editor windows
        /// <summary>
        /// should use GetId() to get the currently assigned dynamic object id
        /// </summary>
        [SerializeField]
        internal string CustomId;
        public float UpdateRate = 0.1f;


        //only used to indicate that the mesh needs to be exported/uploaded. false for controllers
        public bool UseCustomMesh
        {
            get { return !IsController; }
        }
        [SerializeField]
        internal string MeshName = string.Empty;

        public float PositionThreshold = 0.01f;
        public float RotationThreshold = 0.1f;
        public float ScaleThreshold = 0.1f;

        //used to select svg on SE to display button inputs
        public bool IsController;
        public bool IsRight;
        public bool IdentifyControllerAtRuntime = true;
        public ControllerType FallbackControllerType;
        private CommonDynamicMesh commonDynamicMesh;
        private ControllerDisplayType controllerDisplayType;

        public DynamicObjectIdPool IdPool;

        [System.NonSerialized]
        public Vector3 StartingScale;

        //make this dynamic object record position on the same frame as physics gaze
        public bool SyncWithPlayerGazeTick;
        private bool hasInitialized;

        void DelayEnable(InputDevice device, XRNode node, bool isValid)
        {
            GameplayReferences.OnControllerValidityChange -= DelayEnable;
            OnEnable();
        }

        private void OnEnable()
        {
            //already initialized, skip
            if (hasInitialized) { return; }
            StartingScale = transform.lossyScale;
            string registerMeshName = MeshName;

            // if a controller, delay registering the controller until the controller name has returned something valid
            // if current device is hands or null, then use fallback
            if (IsController)
            {
                GameplayReferences.SetController(this, IsRight);
                // Special case for hand tracking (particularly when session begins with hand): 
                //  need this because InputDevice.isValid returns false
                //  and InputDevice.name gives us nothing
                if (Cognitive3D_Manager.Instance?.GetComponent<Cognitive3D.Components.HandTracking>())
                {
                    // If starting with hands or none; use fallback controller
                    if (GameplayReferences.GetCurrentTrackedDevice() == GameplayReferences.TrackingType.Hand || GameplayReferences.GetCurrentTrackedDevice() == GameplayReferences.TrackingType.None)
                    {
                        // just quickly look up controller by type, isRight
                        SetControllerFromFallback(FallbackControllerType, IsRight);
                        registerMeshName = commonDynamicMesh.ToString();
                        RegisterDynamicObject(registerMeshName);
                        return;
                    }
                }

                if (IdentifyControllerAtRuntime)
                {
                    InputDevice device;
                    if (!GameplayReferences.GetControllerInfo(IsRight, out device))
                    {
                        GameplayReferences.OnControllerValidityChange += DelayEnable;
                        //register to some 'controller validity changed' event and try later
                        return;
                    }
                    else
                    {
                        controllerDisplayType = GetControllerPopupName(device.name, IsRight);
                        commonDynamicMesh = GetControllerMeshName(device.name, IsRight);
                        registerMeshName = commonDynamicMesh.ToString();

                        if (controllerDisplayType == ControllerDisplayType.unknown ||
                            commonDynamicMesh == CommonDynamicMesh.Unknown)
                        {
                            //failed to identify the controller - use the fallback
                            SetControllerFromFallback(FallbackControllerType, IsRight);
                            registerMeshName = commonDynamicMesh.ToString();
                        }
                    }
                }
                else
                {
                    //just quickly look up controller by type, isRight
                    SetControllerFromFallback(FallbackControllerType, IsRight);
                    registerMeshName = commonDynamicMesh.ToString();
                }
            }

            RegisterDynamicObject(registerMeshName);
        }
        
        private void RegisterDynamicObject(string registerMeshName)
        {
            if (SyncWithPlayerGazeTick)
            {
                UpdateRate = 64;
            }

            string registerid = (idSource == IdSourceType.CustomID) ? CustomId : "";

            if (idSource == IdSourceType.PoolID && IdPool != null)
            {
                CustomId = IdPool.GetId();
                registerid = CustomId;
            }

            var Data = new DynamicData(gameObject.name, registerid, registerMeshName, transform, transform.position, transform.rotation, transform.lossyScale, PositionThreshold, RotationThreshold, ScaleThreshold, UpdateRate, IsController, controllerDisplayType.ToString(), IsRight);

            DataId = Data.Id;

            if (IsController)
            {
                Cognitive3D.DynamicManager.RegisterController(Data);
            }
            else
            {
                Cognitive3D.DynamicManager.RegisterDynamicObject(Data);
            }
            if (SyncWithPlayerGazeTick)
            {
                PhysicsGaze.OnGazeTick += SyncWithGazeTick;
            }
            hasInitialized = true;
        }

        //used by controller input tracker component to get the controller display type after initialize tries to identify it or fall back
        internal void GetControllerTypeData(out CommonDynamicMesh mesh, out ControllerDisplayType display)
        {
            //ensure that controller initial values have been set
            if (!hasInitialized)
            {
                OnEnable();
            }
            mesh = commonDynamicMesh;
            display = controllerDisplayType;
        }

        //sets the class variables from the fallback controller type
        private void SetControllerFromFallback(ControllerType fallbackControllerType, bool isRight)
        {
            switch (fallbackControllerType)
            {
                case ControllerType.Quest2:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.oculus_quest_touch_right;
                        commonDynamicMesh = CommonDynamicMesh.OculusQuestTouchRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.oculus_quest_touch_left;
                        commonDynamicMesh = CommonDynamicMesh.OculusQuestTouchLeft;
                    }
                    break;
                case ControllerType.QuestPro:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.quest_pro_touch_right;
                        commonDynamicMesh = CommonDynamicMesh.QuestProTouchRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.quest_pro_touch_left;
                        commonDynamicMesh = CommonDynamicMesh.QuestProTouchLeft;
                    }
                    break;
                case ControllerType.Quest3:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.quest_plus_touch_right;
                        commonDynamicMesh = CommonDynamicMesh.QuestPlusTouchRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.quest_plus_touch_left;
                        commonDynamicMesh = CommonDynamicMesh.QuestPlusTouchLeft;
                    }
                    break;
                case ControllerType.ViveWand:
                    controllerDisplayType = ControllerDisplayType.vive_controller;
                    commonDynamicMesh = CommonDynamicMesh.ViveController;
                    break;
                case ControllerType.WindowsMRController:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.windows_mixed_reality_controller_right;
                        commonDynamicMesh = CommonDynamicMesh.WindowsMixedRealityRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.windows_mixed_reality_controller_left;
                        commonDynamicMesh = CommonDynamicMesh.WindowsMixedRealityLeft;
                    }
                    break;
                case ControllerType.SteamIndex:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.steam_index_right;
                        commonDynamicMesh = CommonDynamicMesh.SteamIndexRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.steam_index_left;
                        commonDynamicMesh = CommonDynamicMesh.SteamIndexLeft;
                    }
                    break;
                case ControllerType.PicoNeo3:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.pico_neo_3_eye_controller_right;
                        commonDynamicMesh = CommonDynamicMesh.PicoNeo3ControllerRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.pico_neo_3_eye_controller_left;
                        commonDynamicMesh = CommonDynamicMesh.PicoNeo3ControllerLeft;
                    }
                    break;
                case ControllerType.PicoNeo4:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.pico_neo_4_eye_controller_right;
                        commonDynamicMesh = CommonDynamicMesh.PicoNeo4ControllerRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.pico_neo_4_eye_controller_left;
                        commonDynamicMesh = CommonDynamicMesh.PicoNeo4ControllerLeft;
                    }
                    break;
                case ControllerType.ViveFocus:
                    if (isRight)
                    {
                        controllerDisplayType = ControllerDisplayType.vive_focus_controller_right;
                        commonDynamicMesh = CommonDynamicMesh.ViveFocusControllerRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.vive_focus_controller_left;
                        commonDynamicMesh = CommonDynamicMesh.ViveFocusControllerLeft;
                    }
                    break;
                default:
                    controllerDisplayType = ControllerDisplayType.unknown;
                    commonDynamicMesh = CommonDynamicMesh.Unknown;
                    break;
            }
        }

        private void SyncWithGazeTick()
        {
            Cognitive3D.DynamicManager.RecordDynamic(DataId, false);
        }

        /// <summary>
        /// returns the Id of the Dynamic Object
        /// </summary>
        /// <returns></returns>
        public string GetId()
        {
            if (DynamicManager.IsDataActive(DataId))
                return DataId;
            if (!string.IsNullOrEmpty(CustomId))
                return CustomId;
            return string.Empty;
        }

        /// <summary>
        /// sets the Id to a specific value. does not check for uniqueness. will unregister the previous id and re-register this id in this session
        /// intended only for in-app editor tooling
        /// </summary>
        /// <param name="customId"></param>
        public void SetCustomId(string customId)
        {
            //remove existing dynamic data from dynamic manager
            OnDisable();

            //update displayed customid
            this.idSource = IdSourceType.CustomID;
            this.CustomId = customId;

            //register new dynamic data with dynamic manager
            OnEnable();
        }

        /// <summary>
        /// sets the meshname to a specific value and re-registers the dynamic object data for this session
        /// intended only for in-app editor tooling
        /// </summary>
        /// <param name="meshName"></param>
        public void SetMeshName(string meshName)
        {
            //remove existing dynamic data from dynamic manager
            OnDisable();

            this.MeshName = meshName;

            //register new dynamic data with dynamic manager
            OnEnable();
        }

        /// <summary>
        /// sets the meshname and customid to a specific value and re-registers the dynamic object data for this session
        /// intended only for in-app editor tooling
        /// </summary>
        /// <param name="customId"></param>
        /// <param name="meshName"></param>
        public void SetCustomIdAndMeshName(string customId, string meshName)
        {
            //remove existing dynamic data from dynamic manager
            OnDisable();

            //update displayed customid
            this.idSource = IdSourceType.CustomID;
            this.CustomId = customId;
            this.MeshName = meshName;

            //register new dynamic data with dynamic manager
            OnEnable();
        }

        /// <summary>
        /// manually record position and rotation on this dynamic object
        /// </summary>
        public void RecordSnapshot()
        {
            DynamicManager.SetDirty(DataId);
        }

        /// <summary>
        /// manually record position and rotation and properties on this dynamic object
        /// </summary>
        public void RecordSnapshot(List<KeyValuePair<string, object>> properties)
        {
            DynamicManager.SetProperties(DataId, properties);
        }

        public void RecordSnapshot(Dictionary<string, object> properties)
        {
            List<KeyValuePair<string, object>> temp = new List<KeyValuePair<string, object>>(properties.Count);
            foreach (var prop in properties)
            {
                temp.Add(new KeyValuePair<string, object>(prop.Key, prop.Value));
            }
            DynamicManager.SetProperties(DataId, temp);
        }

        #region Engagement Events

        /// <summary>
        /// Alternate method for beginning a Custom Event and setting this Dynamic Object as the target
        /// </summary>
        /// <param name="engagementName">name of the event</param>
        public void BeginEngagement(string engagementName)
        {
            DynamicManager.BeginEngagement(GetId(), engagementName, GetId() + engagementName, null);
        }

        /// <summary>
        /// Alternate method for beginning a Custom Event and setting this Dynamic Object as the target
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="uniqueEngagementId"></param>
        public void BeginEngagement(string engagementName, string uniqueEngagementId)
        {
            DynamicManager.BeginEngagement(GetId(), engagementName, uniqueEngagementId, null);
        }

        /// <summary>
        /// Alternate method for beginning a Custom Event and setting this Dynamic Object as the target
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="uniqueEngagementId"></param>
        /// <param name="properties"></param>
        public void BeginEngagement(string engagementName, string uniqueEngagementId, List<KeyValuePair<string, object>> properties)
        {
            DynamicManager.BeginEngagement(GetId(), engagementName, uniqueEngagementId, properties);
        }

        /// <summary>
        /// Alternate method to end a Custom Event on a specific Dynamic Object. If the event does not exist, creates and immediately ends the event
        /// </summary>
        /// <param name="engagementName">the name of the Event to end</param>
        public void EndEngagement(string engagementName)
        {
            DynamicManager.EndEngagement(GetId(), engagementName, GetId() + engagementName, null);
        }

        /// <summary>
        /// Alternate method to end a Custom Event on a specific Dynamic Object. If the event does not exist, creates and immediately ends the event
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="uniqueEngagementId"></param>
        public void EndEngagement(string engagementName, string uniqueEngagementId)
        {
            DynamicManager.EndEngagement(GetId(), engagementName, uniqueEngagementId, null);
        }

        /// <summary>
        /// Alternate method to end a Custom Event on a specific Dynamic Object. If the event does not exist, creates and immediately ends the event
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="uniqueEngagementId"></param>
        /// <param name="properties"></param>
        public void EndEngagement(string engagementName, string uniqueEngagementId, List<KeyValuePair<string, object>> properties)
        {
            DynamicManager.EndEngagement(GetId(), engagementName, uniqueEngagementId, properties);
        }

        #endregion

        private void OnDisable()
        {
            GameplayReferences.OnControllerValidityChange -= DelayEnable;

            PhysicsGaze.OnGazeTick -= SyncWithGazeTick;

            DynamicManager.SetTransform(DataId, transform);

            Cognitive3D.DynamicManager.RemoveDynamicObject(DataId);
            
            hasInitialized = false;
        }

        private static CommonDynamicMesh GetControllerMeshName(string xrDeviceName, bool isRight)
        {
            if (xrDeviceName.Contains("Vive Wand")
                || xrDeviceName.Contains("Vive. Controller MV")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR"))
            {
                return CommonDynamicMesh.ViveController;
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Left")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == false))
            {
                string oculusHeadsetType = "";
#if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
#endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return CommonDynamicMesh.QuestProTouchLeft;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return CommonDynamicMesh.QuestPlusTouchLeft;
                }
                else
                {
                    return CommonDynamicMesh.OculusQuestTouchLeft;
                }
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Right")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                string oculusHeadsetType = "";
#if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
#endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return CommonDynamicMesh.QuestProTouchRight;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return CommonDynamicMesh.QuestPlusTouchRight;
                }
                else
                {
                    return CommonDynamicMesh.OculusQuestTouchRight;
                }
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Left")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Left"))
                && !isRight)
            {
                return CommonDynamicMesh.ViveFocusControllerLeft;
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Right")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Right"))
                && isRight)
            {
                return CommonDynamicMesh.ViveFocusControllerRight;
            }
            if ((xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR"))
                && !isRight)
            {
                return CommonDynamicMesh.WindowsMixedRealityLeft;
            }
            if ((xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR"))
                && isRight)
            {
                return CommonDynamicMesh.WindowsMixedRealityRight;
            }
            if (xrDeviceName.Equals("PicoXR Controller-Left"))
            {
                return CommonDynamicMesh.PicoNeo3ControllerLeft;
            }
            if (xrDeviceName.Equals("PicoXR Controller-Right"))
            {
                return CommonDynamicMesh.PicoNeo3ControllerRight;
            }
            if (xrDeviceName.Equals("PICO Controller-Left"))
            {
                return CommonDynamicMesh.PicoNeo4ControllerLeft;
            }
            if (xrDeviceName.Equals("PICO Controller-Right"))
            {
                return CommonDynamicMesh.PicoNeo4ControllerRight;
            }
            return CommonDynamicMesh.Unknown;
        }

        //the svg popup that displays the button presses
        //used by controller input tracker to determine how to record input names
        private static ControllerDisplayType GetControllerPopupName(string xrDeviceName, bool isRight)
        {
            if (xrDeviceName.Contains("Vive Wand")
                || xrDeviceName.Contains("Vive. Controller MV")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR"))
            {
                return ControllerDisplayType.vive_controller;
            }

#if !C3D_VIVEWAVE
            if (xrDeviceName.Contains("WVR_CR"))
            {
                return ControllerDisplayType.vive_controller;
            }
#endif
            if (xrDeviceName.Equals("Oculus Touch Controller - Left")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == false))
            {
                string oculusHeadsetType = "";
#if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
#endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return ControllerDisplayType.quest_pro_touch_left;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return ControllerDisplayType.quest_plus_touch_left;
                }
                else
                {
                    return ControllerDisplayType.oculus_quest_touch_left;
                }
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Right")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                string oculusHeadsetType = "";
#if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
#endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return ControllerDisplayType.quest_pro_touch_right;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return ControllerDisplayType.quest_plus_touch_right;
                }
                else
                {
                    return ControllerDisplayType.oculus_quest_touch_right;
                }
            }
            if (xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR")
                && isRight == false)
            {
                return ControllerDisplayType.windows_mixed_reality_controller_left;
            }
            if (xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR")
                && isRight == true)
            {
                return ControllerDisplayType.windows_mixed_reality_controller_right;
            }
            if (xrDeviceName.Equals("PicoXR Controller-Left"))
            {
                return ControllerDisplayType.pico_neo_3_eye_controller_left;
            }
            if(xrDeviceName.Equals("PicoXR Controller-Right"))
            {
                return ControllerDisplayType.pico_neo_3_eye_controller_right;
            }
            if (xrDeviceName.Equals("PICO Controller-Left"))
            {
                return ControllerDisplayType.pico_neo_4_eye_controller_left;
            }
            if (xrDeviceName.Equals("PICO Controller-Right"))
            {
                return ControllerDisplayType.pico_neo_4_eye_controller_right;
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Left")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Left")))
            {
                return ControllerDisplayType.vive_focus_controller_left;
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Right")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Right")))
            {
                return ControllerDisplayType.vive_focus_controller_right;
            }
            return ControllerDisplayType.unknown;
        }

#if UNITY_EDITOR
        private void Reset()
    {
        //set name is not set otherwise
        if (string.IsNullOrEmpty(MeshName))
        {
            //CONSIDER meshfilter name + material name if one is found on this gameobject?
            MeshName = gameObject.name.ToLower().Replace(" ", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_").Replace("?", "_").Replace("*", "_").Replace("\"", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        //set custom id if not set otherwise
        if (string.IsNullOrEmpty(CustomId))
        {
            // This is used to create a consistent id from objects which are not in the scene
            // Instead of GUID we are using hash so the ids are always consistent given the object name
#if C3D_USE_DETERMINISTIC_DYNAMIC_ID
            System.Security.Cryptography.SHA256 mySha256 = System.Security.Cryptography.SHA256.Create();
            byte[] myStringInBytes = mySha256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(MeshName));
            string s = "";
            for (int i = 0; i < myStringInBytes.Length; i++)
            {
                s += myStringInBytes[i].ToString("x2"); // "x2" means format the byte as hexadecimal
            }
            s = s.Substring(0, 31); // take the first 32 characters

            // format as abcdefgh-1234-5678-1234-abcdefghijkl
            s = s.Insert(8, "-");
            s = s.Insert(13, "-");
            s = s.Insert(18, "-");
            s = s.Insert(23, "-");
#else
            string s = System.Guid.NewGuid().ToString();
#endif
            CustomId = s;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
#endif
    }
}