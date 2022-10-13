using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

//container for data and simple instance implementations (enable,disable) for dynamic object
//also includes fields for initialization
//this would also include some nice functions for beginning/ending engagements

//if this is using a dynamic object id pool, will grab a new id every time 'OnEnable' is called. if this is not needed, changing that function to 'Start' should be fine

namespace Cognitive3D
{
#if C3D_VIVEWAVE
    [DefaultExecutionOrder(+10)] //this must run after PoseTrackerManager on controllers is enabled
#endif
    public class DynamicObject : MonoBehaviour
    {
        public enum CommonDynamicMesh
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
        public bool UseCustomId = true;

        //custom id is set in editor and will be used when set. internal to be accessed by various editor windows
        /// <summary>
        /// should use GetId() to get the currently assigned dynamic object id
        /// </summary>
        [SerializeField] //internal needs to be serialized (redudant?) so editor can access it
        internal string CustomId;
        public float UpdateRate = 0.1f;


        //only used to indicate that the mesh needs to be exported/uploaded. false for controllers
        public bool UseCustomMesh = true;
        public string MeshName;

        public float PositionThreshold = 0.01f;
        public float RotationThreshold = 0.1f;
        public float ScaleThreshold = 0.1f;

        //used to select svg on SE to display button inputs
        public bool IsController;
        public bool IsRight;
        public enum ControllerDisplayType
        {
            vivecontroller = 1, //wand
            vivefocuscontrollerright = 2,
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
            vivefocuscontrollerleft = 14,
        }

        public DynamicObjectIdPool IdPool;

        [System.NonSerialized]
        public Vector3 StartingScale;

        //make this dynamic object record position on the same frame as physics gaze
        public bool SyncWithPlayerGazeTick;

#if C3D_VIVEWAVE
        bool hasCompletedDelay = false;
        IEnumerator Start()
        {
            //vive wave controller loader spawns a prefab (which calls enable) before setting correct values
            if (!IsController) { yield break; }
            if (hasCompletedDelay) { yield break; }
            yield return null;
            hasCompletedDelay = true;
            OnEnable();
        }
#endif

        void DelayEnable(InputDevice device, XRNode node, bool isValid)
        {
            GameplayReferences.OnControllerValidityChange -= DelayEnable;
            OnEnable();
        }

        private void OnEnable()
        {
#if C3D_VIVEWAVE
            if (IsController && !hasCompletedDelay)
                return;
#endif
            StartingScale = transform.lossyScale;

            string controllerName = string.Empty;
            string appliedMeshName = MeshName;

            //if a controller, delay registering the controller until the controller name has returned something valid
            if (IsController)
            {
                GameplayReferences.SetController(this, IsRight);
                InputDevice device;
                if (!GameplayReferences.GetControllerInfo(IsRight, out device))
                {
                    GameplayReferences.OnControllerValidityChange += DelayEnable;
                    //register to some 'controller validity changed' event and try later
                    return;
                }
                else
                {
                    controllerName = GetControllerPopupName(device.name, IsRight).ToString();
                    appliedMeshName = GetControllerMeshName(device.name, IsRight).ToString();
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

            var Data = new DynamicData(gameObject.name, registerid, appliedMeshName, transform, transform.position, transform.rotation, transform.lossyScale, PositionThreshold, RotationThreshold, ScaleThreshold, UpdateRate, IsController, controllerName, IsRight);

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

        /// <summary>
        /// Alternate method for beginning a Custom Event and setting this Dynamic Object as the target
        /// </summary>
        /// <param name="engagementName">name of the event</param>
        /// <param name="uniqueEngagementId">if multiple events with the same name are expected on this object, this can be used to end specific events</param>
        /// <param name="properties">optional parameters to add to the custom event</param>
        public void BeginEngagement(string engagementName, string uniqueEngagementId = null, List<KeyValuePair<string, object>> properties = null)
        {
            DynamicManager.BeginEngagement(GetId(), engagementName, uniqueEngagementId, properties);
        }

        /// <summary>
        /// Alternate method to end a Custom Event on a specific Dynamic Object. If the event does not exist, creates and immediately ends the event
        /// </summary>
        /// <param name="engagementName">the name of the Event to end</param>
        /// <param name="uniqueEngagementId">identifies the event to end, regardless of name</param>
        /// <param name="properties">any properties to add to this event before it ends</param>
        public void EndEngagement(string engagementName, string uniqueEngagementId = null, List<KeyValuePair<string, object>> properties = null)
        {
            DynamicManager.EndEngagement(GetId(), engagementName, uniqueEngagementId, properties);
        }

        private void OnDisable()
        {
            Cognitive3D_Manager.OnTick -= SyncWithGazeTick;

            DynamicManager.SetTransform(DataId, transform);

            Cognitive3D.DynamicManager.RemoveDynamicObject(DataId);
        }

        internal static CommonDynamicMesh GetControllerMeshName(string inName, bool isRight)
        {
            if (inName.Contains("Vive Wand")
                || inName.Contains("Vive. Controller MV"))
            {
                return CommonDynamicMesh.ViveController;
            }
            if (inName.Equals("Oculus Touch Controller - Left")
                || (inName.Equals("Oculus Touch Controller OpenXR") && isRight == false))
            {
                return CommonDynamicMesh.OculusQuestTouchLeft;
            }
            if (inName.Equals("Oculus Touch Controller - Right")
                || (inName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                return CommonDynamicMesh.OculusQuestTouchRight;
            }
            if (inName.Equals("OpenVR Controller(vive_cosmos_controller) - Left")
                || (inName.Equals("HTC Vive Controller OpenXR") && isRight == false))
            {
                return CommonDynamicMesh.ViveFocusControllerLeft;
            }
            if (inName.Equals("OpenVR Controller(vive_cosmos_controller) - Right")
                || (inName.Equals("HTC Vive Controller OpenXR") && isRight == true))
            {
                return CommonDynamicMesh.ViveFocusControllerRight;
            }
            /*if (inName.Contains(""))
            {
                return CommonDynamicMesh.ViveTracker;
            }
            if (inName.Contains(""))
            {
                return CommonDynamicMesh.ViveFocusController;
            }
            if (inName.Contains(""))
            {
                return CommonDynamicMesh.OculusRiftTouchLeft;
            }
            if (inName.Contains(""))
            {
                return CommonDynamicMesh.OculusRiftTouchRight;
            }
            if (inName.Contains(""))
            {
                return CommonDynamicMesh.WindowsMixedRealityLeft;
            }
            if (inName.Contains(""))
            {
                return CommonDynamicMesh.WindowsMixedRealityRight;
            }
            if (inName.Contains(""))
            {
                return CommonDynamicMesh.PicoNeoControllerLeft;
            }
            if (inName.Contains(""))
            {
                return CommonDynamicMesh.PicoNeoControllerRight;
            }*/
            if (inName.Equals("PicoXR Controller-Left"))
            {
                return CommonDynamicMesh.PicoNeo3ControllerLeft;
            }
            if (inName.Equals("PicoXR Controller-Right"))
            {
                return CommonDynamicMesh.PicoNeo3ControllerRight;
            }
            return CommonDynamicMesh.Unknown;
        }

        //the svg popup that displays the button presses
        //used by controller input tracker to determine how to record input names
        internal static ControllerDisplayType GetControllerPopupName(string inName, bool isRight)
        {
            if (inName.Contains("Vive Wand")
                || inName.Contains("Vive. Controller MV"))
            {
                return ControllerDisplayType.vivecontroller;
            }
            /*if (inName.Contains(""))
            {
                return ControllerDisplayType.vivefocuscontroller;
            }
            if (inName.Contains(""))
            {
                return ControllerDisplayType.oculustouchleft;
            }
            if (inName.Contains(""))
            {
                return ControllerDisplayType.oculustouchright;
            }*/
            if (inName.Equals("Oculus Touch Controller - Left")
                || (inName.Equals("Oculus Touch Controller OpenXR") && isRight == false))
            {
                return ControllerDisplayType.oculusquesttouchleft;
            }
            if (inName.Equals("Oculus Touch Controller - Right")
                || (inName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                return ControllerDisplayType.oculusquesttouchright;
            }
            /*if (inName.Contains(""))
            {
                return ControllerDisplayType.windows_mixed_reality_controller_left;
            }
            if (inName.Contains(""))
            {
                return ControllerDisplayType.windows_mixed_reality_controller_right;
            }
            if (inName.Contains(""))
            {
                return ControllerDisplayType.pico_neo_2_eye_controller_left;
            }
            if (inName.Contains(""))
            {
                return ControllerDisplayType.pico_neo_2_eye_controller_right;
            }*/
            if (inName.Equals("PicoXR Controller-Left"))
            {
                return ControllerDisplayType.pico_neo_3_eye_controller_left;
            }
            if(inName.Equals("PicoXR Controller-Right"))
            {
                return ControllerDisplayType.pico_neo_3_eye_controller_right;
            }
            if (inName.Equals("OpenVR Controller(vive_cosmos_controller) - Left")
                || (inName.Equals("HTC Vive Controller OpenXR") && isRight == false))
            {
                return ControllerDisplayType.vivefocuscontrollerleft;
            }
            if (inName.Equals("OpenVR Controller(vive_cosmos_controller) - Right")
                || (inName.Equals("HTC Vive Controller OpenXR") && isRight == true))
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
            MeshName = gameObject.name.ToLower().Replace(" ", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_").Replace("?", "_").Replace("*", "_").Replace("\"", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        //set custom id if not set otherwise
        if (string.IsNullOrEmpty(CustomId))
        {
            string s = System.Guid.NewGuid().ToString();
            CustomId = "editor_" + s;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
#endif
    }
}