using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//container for data and simple instance implementations (enable,disable) for dynamic object
//also includes fields for initialization
//this would also include some nice functions for beginning/ending engagements

namespace CognitiveVR
{
    public class DynamicObject : MonoBehaviour
    {
        public enum CommonDynamicMesh
        {
            ViveController,
            OculusTouchLeft,
            OculusTouchRight,
            ViveTracker,
            ExitPoll,
            LeapMotionHandLeft,
            LeapMotionHandRight,
            MicrosoftMixedRealityLeft,
            MicrosoftMixedRealityRight,
            VideoSphereLatitude,
            VideoSphereCubemap,
            SnapdragonVRController,
        }


#if UNITY_EDITOR
        //stores instanceid. used to check if something in editor has changed
        [System.NonSerialized]
        public int editorInstanceId;
        public bool HasCollider()
        {
            if (CognitiveVR_Preferences.Instance.DynamicObjectSearchInParent)
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

        public string CustomId;
        public float UpdateRate = 0.1f;


        public bool UseCustomMesh = true;
        public string MeshName;
        public CommonDynamicMesh CommonMesh;


        public float PositionThreshold = 0.01f;
        public float RotationThreshold = 0.1f;
        public float ScaleThreshold = 0.1f;


        public bool IsController;
        public bool IsRight;
        public string ControllerType;

        [System.NonSerialized]
        public Vector3 StartingScale;

        //make this dynamic object record position on the same frame as physics gaze
        public bool SyncWithPlayerGazeTick;

        private void OnEnable()
        {
            StartingScale = transform.lossyScale;
            if (CognitiveVR.Core.IsInitialized)
            {                
                string tempMeshName = UseCustomMesh ? MeshName : CommonMesh.ToString().ToLower();

                if (SyncWithPlayerGazeTick)
                {
                    UpdateRate = 64;
                }

                string registerid = UseCustomId ? CustomId : "";
                var Data = new DynamicData(gameObject.name, registerid, tempMeshName, transform, transform.position, transform.rotation, transform.lossyScale, PositionThreshold, RotationThreshold, ScaleThreshold, UpdateRate, IsController, ControllerType,IsRight);

                DataId = Data.Id;

                if (false /*IsMedia*/)
                {
                    //DynamicManager.RegisterMedia(Data, VideoUrl);
                }
                else if (IsController)
                {
                    CognitiveVR.DynamicManager.RegisterController(Data);
                }
                else
                {
                    CognitiveVR.DynamicManager.RegisterDynamicObject(Data);
                }
                if (SyncWithPlayerGazeTick)
                {
                    CognitiveVR.Core.TickEvent += Core_TickEvent;
                }
            }
            else
            {
                CognitiveVR.Core.InitEvent += OnCoreInitialize;
            }
        }

        private void Core_TickEvent()
        {
            CognitiveVR.DynamicManager.RecordDynamic(DataId,false);
        }

        private void OnCoreInitialize(CognitiveVR.Error error)
        {
            CognitiveVR.Core.InitEvent -= OnCoreInitialize;
            OnEnable();
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

        public void BeginEngagement(string engagementName, string uniqueEngagementId = null, List<KeyValuePair<string,object>> properties = null)
        {
            DynamicManager.BeginEngagement(GetId(), engagementName, uniqueEngagementId, properties);
        }

        public void EndEngagement(string engagementName, string uniqueEngagementId = null, List<KeyValuePair<string, object>> properties = null)
        {
            DynamicManager.EndEngagement(GetId(), engagementName, uniqueEngagementId, properties);
        }

        private void OnDisable()
        {
            CognitiveVR.Core.InitEvent -= OnCoreInitialize;
            CognitiveVR.Core.TickEvent -= Core_TickEvent;

            DynamicManager.SetTransform(DataId, transform);

            CognitiveVR.DynamicManager.RemoveDynamicObject(DataId);
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