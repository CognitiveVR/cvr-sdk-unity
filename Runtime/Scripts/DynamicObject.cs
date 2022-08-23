using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            ViveFocusController = 12, //the 6dof controller
            OculusQuestTouchLeft = 13,
            OculusQuestTouchRight = 14,
            PicoNeoControllerLeft = 15,
            PicoNeoControllerRight = 16,
            PicoNeo3ControllerLeft = 17,
            PicoNeo3ControllerRight = 18
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
        
        [System.NonSerialized]
        public string DataId;
        //this is only used for a custom editor to help CustomId be set correctly
        public bool UseCustomId = true;

        /// <summary>
        /// should use GetId() to get the currently assigned dynamic object id
        /// </summary>
        public string CustomId;
        public float UpdateRate = 0.1f;


        public bool UseCustomMesh = true;
        public string MeshName;
        public CommonDynamicMesh CommonMesh;


        public float PositionThreshold = 0.01f;
        public float RotationThreshold = 0.1f;
        public float ScaleThreshold = 0.1f;

        //used to select svg on SE to display button inputs
        public bool IsController;
        public bool IsRight;
        public enum ControllerDisplayType
        {
            vivecontroller, //wand
            vivefocuscontroller,
            oculustouchleft,
            oculustouchright,
            oculusquesttouchleft,
            oculusquesttouchright,
            windows_mixed_reality_controller_left,
            windows_mixed_reality_controller_right,
            pico_neo_2_eye_controller_left,
            pico_neo_2_eye_controller_right,
            pico_neo_3_eye_controller_left,
            pico_neo_3_eye_controller_right,
        }
        public ControllerDisplayType ControllerType;

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


        private void OnEnable()
        {
#if C3D_VIVEWAVE
            if (IsController && !hasCompletedDelay)
                return;
#endif
            StartingScale = transform.lossyScale;
            if (Cognitive3D.Core.IsInitialized)
            {                
                string tempMeshName = UseCustomMesh ? MeshName : CommonMesh.ToString().ToLower();

                if (!UseCustomMesh && CommonMesh == CommonDynamicMesh.WindowsMixedRealityRight)
                    tempMeshName = "windows_mixed_reality_controller_right";
                if (!UseCustomMesh && CommonMesh == CommonDynamicMesh.WindowsMixedRealityLeft)
                    tempMeshName = "windows_mixed_reality_controller_left";

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

                string controllerName = string.Empty;
                if (IsController)
                    controllerName = ControllerType.ToString();

                var Data = new DynamicData(gameObject.name, registerid, tempMeshName, transform, transform.position, transform.rotation, transform.lossyScale, PositionThreshold, RotationThreshold, ScaleThreshold, UpdateRate, IsController, controllerName ,IsRight);

                DataId = Data.Id;

                if (IsController)
                {
#if C3D_VIVEWAVE
                    var devicetype = GetComponent<WaveVR_PoseTrackerManager>().Type;
                    if (WaveVR_Controller.Input(devicetype).DeviceType == wvr.WVR_DeviceType.WVR_DeviceType_Controller_Left)
                    {
                        Data.IsRightHand = false;
                    }
                    else
                    {
                        Data.IsRightHand = true;
                    }
                    Cognitive3D.GameplayReferences.SetController(gameObject, Data.IsRightHand);
#endif
#if C3D_WINDOWSMR || C3D_XR || C3D_PICOXR
                    Cognitive3D.GameplayReferences.SetController(gameObject, IsRight);
#endif
                    Cognitive3D.DynamicManager.RegisterController(Data);
                }
                else
                {
                    Cognitive3D.DynamicManager.RegisterDynamicObject(Data);
                }
                if (SyncWithPlayerGazeTick)
                {
                    Cognitive3D.Core.TickEvent += Core_TickEvent;
                }
            }
            else
            {
                Core.InitEvent += OnCoreInitialize;
            }
        }

        private void Core_TickEvent()
        {
            Cognitive3D.DynamicManager.RecordDynamic(DataId,false);
        }

        private void OnCoreInitialize(Cognitive3D.Error error)
        {
            if (error == Error.None)
            {
                Core.InitEvent -= OnCoreInitialize;
                OnEnable();
            }
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
            DynamicManager.SetProperties(DataId,properties);
        }

        public void RecordSnapshot(Dictionary<string,object> properties)
        {
            List<KeyValuePair<string, object>> temp = new List<KeyValuePair<string, object>>(properties.Count);
            foreach(var prop in properties)
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
        public void BeginEngagement(string engagementName, string uniqueEngagementId = null, List<KeyValuePair<string,object>> properties = null)
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
            Cognitive3D.Core.TickEvent -= Core_TickEvent;

            DynamicManager.SetTransform(DataId, transform);

            Cognitive3D.DynamicManager.RemoveDynamicObject(DataId);

            Core.InitEvent -= OnCoreInitialize;
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