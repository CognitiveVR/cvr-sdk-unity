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

        public DynamicData Data;
        //this is only used for a custom editor to help CustomId be set correctly
        public bool UseCustomId = true;
        public string CustomId;
        public float UpdateRate = 0.1f;


        public bool UseCustomMesh = true;
        public string MeshName;
        public CommonDynamicMesh CommonMesh;


        public float PositionThreshold = 0.001f;
        public float RotationThreshold = 0.1f;
        public float ScaleThreshold = 0.1f;


        public bool IsController;
        public bool IsRight;
        public string ControllerType;

        [System.NonSerialized]
        public Vector3 StartingScale;

        private void OnEnable()
        {
            StartingScale = transform.lossyScale;
            if (CognitiveVR.Core.IsInitialized)
            {
                if (Data.active == true && Data.remove == false) { return; }
                
                string tempMeshName = UseCustomMesh ? MeshName : CommonMesh.ToString().ToLower();

                Data = new DynamicData(gameObject.name, CustomId, tempMeshName, transform, transform.position, transform.rotation, transform.lossyScale, 0.01f, 1f, 0.1f, UpdateRate, IsController, ControllerType,IsRight);

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
            }
            else
            {
                CognitiveVR.Core.InitEvent += OnCoreInitialize;
            }
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
            if (Data.active)
                return Data.Id;
            if (!string.IsNullOrEmpty(CustomId))
                return CustomId;
            return string.Empty;
        }

        /// <summary>
        /// manually record position and rotation on this dynamic object
        /// </summary>
        public void RecordSnapshot()
        {
            Data.dirty = true;
        }

        /// <summary>
        /// manually record position and rotation on this dynamic object
        /// </summary>
        public void RecordSnapshot(List<KeyValuePair<string, object>> properties)
        {
            Data.dirty = true;
            Data.HasProperties = true;
            Data.Properties = properties;
        }

        public void RecordSnapshot(Dictionary<string,object> properties)
        {
            Data.dirty = true;
            Data.HasProperties = true;

            List<KeyValuePair<string, object>> temp = new List<KeyValuePair<string, object>>(properties.Count);
            foreach(var prop in properties)
            {
                temp.Add(new KeyValuePair<string, object>(prop.Key, prop.Value));
            }
            Data.Properties = temp;
        }

        private void OnDisable()
        {
            CognitiveVR.Core.InitEvent -= OnCoreInitialize;

            //if quitting, return
            Data.LastPosition = transform.position;
            Data.LastRotation = transform.rotation;

            CognitiveVR.DynamicManager.RemoveDynamicObject(Data);
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