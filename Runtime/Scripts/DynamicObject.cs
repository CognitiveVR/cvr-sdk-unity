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
    public class DynamicObject : MonoBehaviour
    {
        //developer facing high level controller type selection
        public enum ControllerType
        {
            Quest2 = 1,
            QuestPro = 2,
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
        }

        //used internally to have a consistent button input image
        internal enum ControllerDisplayType
        {
            vivecontroller = 1, //wand
            vivefocuscontrollerright = 2,
            vivefocuscontrollerleft = 14,
            oculustouchleft = 3,
            oculustouchright = 4,
            oculusquesttouchleft = 5,
            oculusquesttouchright = 6,
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
        //this is only used for a custom editor to help CustomId be set correctly
        [SerializeField]
        internal bool UseCustomId = true;

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
        public string MeshName;

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

            //if a controller, delay registering the controller until the controller name has returned something valid
            if (IsController)
            {
                GameplayReferences.SetController(this, IsRight);
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

            if (SyncWithPlayerGazeTick)
            {
                UpdateRate = 64;
            }

            string registerid = UseCustomId ? CustomId : "";

            if (!UseCustomId && IdPool != null)
            {
                UseCustomId = true;
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
                Cognitive3D_Manager.OnTick += SyncWithGazeTick;
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
                        controllerDisplayType = ControllerDisplayType.oculusquesttouchright;
                        commonDynamicMesh = CommonDynamicMesh.OculusQuestTouchRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.oculusquesttouchleft;
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
                case ControllerType.ViveWand:
                    controllerDisplayType = ControllerDisplayType.vivecontroller;
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
                        controllerDisplayType = ControllerDisplayType.vivefocuscontrollerright;
                        commonDynamicMesh = CommonDynamicMesh.ViveFocusControllerRight;
                    }
                    else
                    {
                        controllerDisplayType = ControllerDisplayType.vivefocuscontrollerleft;
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
            Cognitive3D_Manager.OnTick -= SyncWithGazeTick;

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
                bool isQuestPro = false;
#if C3D_OCULUS
                isQuestPro = OVRPlugin.GetSystemHeadsetType().ToString().Contains("Pro");
#endif
                if (isQuestPro)
                {
                    return CommonDynamicMesh.QuestProTouchLeft;
                }
                else
                {
                    return CommonDynamicMesh.OculusQuestTouchLeft;
                }
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Right")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                bool isQuestPro = false;
#if C3D_OCULUS
                isQuestPro = OVRPlugin.GetSystemHeadsetType().ToString().Contains("Pro");
#endif
                if (isQuestPro)
                {
                    return CommonDynamicMesh.QuestProTouchRight;
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
                return ControllerDisplayType.vivecontroller;
            }

#if !C3D_VIVEWAVE
            if (xrDeviceName.Contains("WVR_CR"))
            {
                return ControllerDisplayType.vivecontroller;
            }
#endif
            if (xrDeviceName.Equals("Oculus Touch Controller - Left")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == false))
            {
                bool isQuestPro = false;
#if C3D_OCULUS
                isQuestPro = OVRPlugin.GetSystemHeadsetType().ToString().Contains("Pro");
#endif
                if (isQuestPro)
                {
                    return ControllerDisplayType.quest_pro_touch_left;
                }
                else
                {
                    return ControllerDisplayType.oculusquesttouchleft;
                }
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Right")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                bool isQuestPro = false;
#if C3D_OCULUS
                isQuestPro = OVRPlugin.GetSystemHeadsetType().ToString().Contains("Pro");
#endif
                if (isQuestPro)
                {
                    return ControllerDisplayType.quest_pro_touch_right;
                }
                else
                {
                    return ControllerDisplayType.oculusquesttouchright;
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
                return ControllerDisplayType.vivefocuscontrollerleft;
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Right")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Right")))
            {
                return ControllerDisplayType.vivefocuscontrollerright;
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
            string s = System.Guid.NewGuid().ToString();
            CustomId = s;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
#endif
    }
}