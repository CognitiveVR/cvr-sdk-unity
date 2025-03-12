using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
    [DisallowMultipleComponent]
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
        public InputUtil.InputType inputType;
        public bool IsController;
        public bool IsRight;
        public bool IdentifyControllerAtRuntime = true;
        public InputUtil.ControllerType FallbackControllerType;
        private InputUtil.CommonDynamicMesh commonDynamicMesh;
        private InputUtil.ControllerDisplayType controllerDisplayType;

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
                    if (InputUtil.GetCurrentTrackedDevice() == InputUtil.InputType.Hand || InputUtil.GetCurrentTrackedDevice() == InputUtil.InputType.None)
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
                        controllerDisplayType = InputUtil.GetControllerPopupName(device.name, IsRight);
                        commonDynamicMesh = InputUtil.GetControllerMeshName(device.name, IsRight);
                        registerMeshName = commonDynamicMesh.ToString();

                        if (controllerDisplayType == InputUtil.ControllerDisplayType.unknown ||
                            commonDynamicMesh == InputUtil.CommonDynamicMesh.Unknown)
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
                PhysicsGaze.OnGazeTick += SyncWithGazeTick;
            }

            string registerid = (idSource == IdSourceType.CustomID) ? CustomId : "";

            if (idSource == IdSourceType.PoolID && IdPool != null)
            {
                CustomId = IdPool.GetId();
                registerid = CustomId;
            }

            var Data = new DynamicData(gameObject.name, registerid, registerMeshName, transform, transform.position, transform.rotation, transform.lossyScale, PositionThreshold, RotationThreshold, ScaleThreshold, UpdateRate, IsController, inputType.ToString(), controllerDisplayType.ToString(), IsRight);

            DataId = Data.Id;

            if (inputType == InputUtil.InputType.Controller || inputType == InputUtil.InputType.Hand)
            {
                Cognitive3D.DynamicManager.RegisterController(Data);
            }
            else
            {
                Cognitive3D.DynamicManager.RegisterDynamicObject(Data);
            }

            hasInitialized = true;
        }

        //used by controller input tracker component to get the controller display type after initialize tries to identify it or fall back
        internal void GetControllerTypeData(out InputUtil.CommonDynamicMesh mesh, out InputUtil.ControllerDisplayType display)
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
        private void SetControllerFromFallback(InputUtil.ControllerType fallbackControllerType, bool isRight)
        {
            switch (fallbackControllerType)
            {
                case InputUtil.ControllerType.Quest2:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.oculus_quest_touch_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.OculusQuestTouchRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.oculus_quest_touch_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.OculusQuestTouchLeft;
                    }
                    break;
                case InputUtil.ControllerType.QuestPro:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.quest_pro_touch_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.QuestProTouchRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.quest_pro_touch_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.QuestProTouchLeft;
                    }
                    break;
                case InputUtil.ControllerType.Quest3:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.quest_plus_touch_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.QuestPlusTouchRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.quest_plus_touch_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.QuestPlusTouchLeft;
                    }
                    break;
                case InputUtil.ControllerType.ViveWand:
                    controllerDisplayType = InputUtil.ControllerDisplayType.vive_controller;
                    commonDynamicMesh = InputUtil.CommonDynamicMesh.ViveController;
                    break;
                case InputUtil.ControllerType.WindowsMRController:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.windows_mixed_reality_controller_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.WindowsMixedRealityRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.windows_mixed_reality_controller_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.WindowsMixedRealityLeft;
                    }
                    break;
                case InputUtil.ControllerType.SteamIndex:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.steam_index_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.SteamIndexRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.steam_index_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.SteamIndexLeft;
                    }
                    break;
                case InputUtil.ControllerType.PicoNeo3:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.pico_neo_3_eye_controller_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.PicoNeo3ControllerRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.pico_neo_3_eye_controller_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.PicoNeo3ControllerLeft;
                    }
                    break;
                case InputUtil.ControllerType.PicoNeo4:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.pico_neo_4_eye_controller_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.PicoNeo4ControllerRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.pico_neo_4_eye_controller_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.PicoNeo4ControllerLeft;
                    }
                    break;
                case InputUtil.ControllerType.ViveFocus:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.vive_focus_controller_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.ViveFocusControllerRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.vive_focus_controller_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.ViveFocusControllerLeft;
                    }
                    break;
                case InputUtil.ControllerType.Hand:
                    if (isRight)
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.hand_right;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.handRight;
                    }
                    else
                    {
                        controllerDisplayType = InputUtil.ControllerDisplayType.hand_left;
                        commonDynamicMesh = InputUtil.CommonDynamicMesh.handLeft;
                    }
                    break;
                default:
                    controllerDisplayType = InputUtil.ControllerDisplayType.unknown;
                    commonDynamicMesh = InputUtil.CommonDynamicMesh.Unknown;
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

        private void OnDestroy()
        {
            GameplayReferences.OnControllerValidityChange -= DelayEnable;

            PhysicsGaze.OnGazeTick -= SyncWithGazeTick;

            DynamicManager.SetTransform(DataId, transform);

            Cognitive3D.DynamicManager.RemoveDynamicObject(DataId);
            
            hasInitialized = false;
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