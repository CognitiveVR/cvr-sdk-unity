using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

//dynamic component registers/removes itself from this centrally managed dynamicData array. manager handles sending manifest when
//MANAGER holds array of data and puts contents into CORE queues. this iterates through the array (128/frame) - checks for changes to submit to core
//checking for delta should stay on engine side - engine specific checks that aren't as easily generalized
namespace Cognitive3D
{
    //used to update and record all dynamic object changes
    public static class DynamicManager
    {
        //this can track up to 1024 dynamic objects in a single scene AT THE SAME TIME before it needs to expand
        internal static DynamicData[] ActiveDynamicObjectsArray = new DynamicData[1024];
        //this can track up to 16 dynamic objects that appear in a session without a custom id. this helps session json reduce the number of entries in the manifest
        internal static DynamicObjectId[] DynamicObjectIdArray = new DynamicObjectId[16];

        internal static void Initialize()
        {
            Cognitive3D_Manager.OnUpdate -= OnUpdate;
            Cognitive3D_Manager.OnLevelLoaded -= OnSceneLoaded;

            Cognitive3D_Manager.OnUpdate += OnUpdate;
            Cognitive3D_Manager.OnLevelLoaded += OnSceneLoaded;

            //when the cognitive3d manager begins a new session, should mark all data in ActiveDynamicObjectsArray as needing to send manifest again
            for(int i = 0; i< ActiveDynamicObjectsArray.Length;i++)
            {
                if (ActiveDynamicObjectsArray[i].active)
                    CoreInterface.WriteDynamicManifestEntry(ActiveDynamicObjectsArray[i]);
            }
        }

        /// <summary>
        /// Resets content of Dynamic Manager to avoid carrying internally stored data between game states when no session is active
        /// Intended only for in-app editor tooling
        /// </summary>
        /// <param name="completeReset">Removes all Dynamic Objects internal data. This may cause persistent objects (eg, controllers) to not appear until they are re-enabled</param>
        public static void Reset(bool completeReset)
        {
            if (completeReset)
            {
                ActiveDynamicObjectsArray = new DynamicData[1024];
                DynamicObjectIdArray = new DynamicObjectId[16];
            }
            else
            {
                for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
                {
                    if (ActiveDynamicObjectsArray[i].remove)
                    {
                        ActiveDynamicObjectsArray[i] = new DynamicData();
                    }
                }
            }
        }

        //happens after the network has sent the request, before any response. used by active session view
        public static event Cognitive3D_Manager.onSendData OnDynamicObjectSend;
        internal static void DynamicObjectSendEvent()
        {
            if (OnDynamicObjectSend != null)
                OnDynamicObjectSend.Invoke(false);
        }

        internal static bool GetDynamicObjectName(string id, out string name)
        {
            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (!ActiveDynamicObjectsArray[i].active) { continue; }
                if (id == ActiveDynamicObjectsArray[i].Id)
                {
                    name = ActiveDynamicObjectsArray[i].Name;
                    return true;
                }
            }
            name = string.Empty;

            return false;
        }

        internal static void RegisterDynamicObject(DynamicData data)
        {
            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].active && string.CompareOrdinal(data.Id, ActiveDynamicObjectsArray[i].Id) == 0)
                {
                    //id is already used. don't add to list again
                    if (data.UseCustomId)
                    {
                        Util.logError("DynamicManager::RegisterDynamicObject found existing ID " + data.Id);
                    }
                    else
                    {
                        Util.logWarning("DynamicManager::RegisterDynamicObject reuse ID " + data.Id);
                    }
                    //should set dynamic data in array to match. updates 'remove' bool and any other variables. DOES NOT write to new dynamics manifest
                    ActiveDynamicObjectsArray[i] = data;
                    return;
                }
            }

            bool foundSpot = false;
            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (!ActiveDynamicObjectsArray[i].active)
                {
                    ActiveDynamicObjectsArray[i] = data;
                    foundSpot = true;
                    break;
                }
            }
            if (!foundSpot)
            {
                Util.logWarning("Dynamic Object Array expanded!");

                int nextFreeIndex = ActiveDynamicObjectsArray.Length;
                Array.Resize<DynamicData>(ref ActiveDynamicObjectsArray, ActiveDynamicObjectsArray.Length * 2);
                //just expanded the array. this spot will be empty
                ActiveDynamicObjectsArray[nextFreeIndex] = data;
            }
            if (Cognitive3D_Manager.IsInitialized)
                CoreInterface.WriteDynamicManifestEntry(data);
        }

        //this doesn't directly remove a dynamic object - it sets 'remove' so it can be removed on the next tick
        //if there isn't an active session (such as in-app creator tools or between sessions) this will immediately remove the associated data
        internal static void RemoveDynamicObject(string id)
        {
            if (!Cognitive3D_Manager.IsInitialized)
            {
                for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
                {
                    if (String.CompareOrdinal(ActiveDynamicObjectsArray[i].Id, id) == 0)
                    {
                        ActiveDynamicObjectsArray[i] = new DynamicData();
                    }
                }
            }
            else
            {
                for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
                {
                    if (String.CompareOrdinal(ActiveDynamicObjectsArray[i].Id, id) == 0)
                    {
                        //set the dynamic data to write 'enabled = false' property next update
                        ActiveDynamicObjectsArray[i].dirty = true;
                        ActiveDynamicObjectsArray[i].remove = true;

                        if (!ActiveDynamicObjectsArray[i].UseCustomId)
                        {
                            for (int j = 0; j < DynamicObjectIdArray.Length; j++)
                            {
                                if (DynamicObjectIdArray[j].Id == id)
                                {
                                    //set the id in the manifest to be reusable
                                    DynamicObjectIdArray[j].Used = false;
                                    break;
                                }
                            }
                        }
                        return; //there should only be one dynamic data with this id
                    }
                }
                Util.logError("remove dynamic object id " + id + " not found");
            }
        }

        #region Engagement Events

        static Dictionary<string, CustomEvent> Engagements = new Dictionary<string, CustomEvent>();

        /// <summary>
        /// creates a new custom event related to a dynamic object
        /// </summary>
        /// <param name="objectid"></param>
        /// <param name="engagementname"></param>
        public static void BeginEngagement(string objectid, string engagementname)
        {
            BeginEngagement(objectid, engagementname, objectid + engagementname, null);
        }

        /// <summary>
        /// creates a new custom event related to a dynamic object
        /// </summary>
        /// <param name="objectid"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        public static void BeginEngagement(string objectid, string engagementname, string uniqueEngagementId)
        {
            BeginEngagement(objectid, engagementname, uniqueEngagementId, null);
        }

        /// <summary>
        /// creates a new custom event related to a dynamic object
        /// </summary>
        /// <param name="dynamicObject"></param>
        /// <param name="engagementname"></param>
        public static void BeginEngagement(DynamicObject dynamicObject, string engagementname)
        {
            BeginEngagement(dynamicObject.GetId(), engagementname, dynamicObject.GetId() + engagementname, null);
        }

        /// <summary>
        /// creates a new custom event related to a dynamic object
        /// </summary>
        /// <param name="dynamicObject"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        public static void BeginEngagement(DynamicObject dynamicObject, string engagementname, string uniqueEngagementId)
        {
            BeginEngagement(dynamicObject.GetId(), engagementname, uniqueEngagementId, null);
        }

        /// <summary>
        /// creates a new custom event related to a dynamic object
        /// </summary>
        /// <param name="dynamicObject"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        /// /// <param name="properties"></param>
        public static void BeginEngagement(DynamicObject dynamicObject, string engagementname, string uniqueEngagementId, List<KeyValuePair<string, object>> properties)
        {
            BeginEngagement(dynamicObject.GetId(), engagementname, uniqueEngagementId, properties);
        }

        /// <summary>
        /// creates a new custom event related to a dynamic object
        /// </summary>
        /// <param name="objectid"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        /// <param name="properties"></param>
        public static void BeginEngagement(string objectid, string engagementname, string uniqueEngagementId, List<KeyValuePair<string, object>> properties)
        {
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            if (string.IsNullOrEmpty(uniqueEngagementId))
            {
                uniqueEngagementId = objectid + " " + engagementname;
            }

            CustomEvent ce = new CustomEvent(engagementname).SetProperties(properties).SetDynamicObject(objectid);
            if (!Engagements.ContainsKey(uniqueEngagementId))
            {
                Engagements.Add(uniqueEngagementId, ce);
            }
            else
            {
                Vector3 pos = Vector3.zero;
                for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
                {
                    if (objectid == ActiveDynamicObjectsArray[i].Id)
                    {
                        pos = ActiveDynamicObjectsArray[i].LastPosition;
                    }
                }
                Engagements[uniqueEngagementId].Send(pos);
                Engagements[uniqueEngagementId] = ce;
            }
        }

        /// <summary>
        /// ends an existing custom event related to a dynamic object
        /// </summary>
        /// <param name="objectid"></param>
        /// <param name="engagementname"></param>
        public static void EndEngagement(string objectid, string engagementname)
        {
            EndEngagement(objectid, engagementname,string.Empty,null);
        }

        /// <summary>
        /// ends an existing custom event related to a dynamic object
        /// </summary>
        /// <param name="objectid"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        public static void EndEngagement(string objectid, string engagementname, string uniqueEngagementId)
        {
            EndEngagement(objectid, engagementname, uniqueEngagementId, null);
        }

        /// <summary>
        /// ends an existing custom event related to a dynamic object
        /// </summary>
        /// <param name="dynamicObject"></param>
        /// <param name="engagementname"></param>
        public static void EndEngagement(DynamicObject dynamicObject, string engagementname)
        {
            EndEngagement(dynamicObject.GetId(), engagementname, dynamicObject.GetId() + engagementname, null);
        }

        /// <summary>
        /// ends an existing custom event related to a dynamic object
        /// </summary>
        /// <param name="dynamicObject"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        public static void EndEngagement(DynamicObject dynamicObject, string engagementname, string uniqueEngagementId)
        {
            EndEngagement(dynamicObject.GetId(), engagementname, uniqueEngagementId, null);
        }

        /// <summary>
        /// ends an existing custom event related to a dynamic object
        /// </summary>
        /// <param name="dynamicObject"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        public static void EndEngagement(DynamicObject dynamicObject, string engagementname, string uniqueEngagementId, List<KeyValuePair<string, object>> properties)
        {
            EndEngagement(dynamicObject.GetId(), engagementname, uniqueEngagementId, properties);
        }

        /// <summary>
        /// ends an existing custom event related to a dynamic object
        /// </summary>
        /// <param name="objectid"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        /// <param name="properties"></param>
        public static void EndEngagement(string objectid, string engagementname, string uniqueEngagementId, List<KeyValuePair<string, object>> properties)
        {
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            if (string.IsNullOrEmpty(uniqueEngagementId))
            {
                uniqueEngagementId = objectid + " " + engagementname;
            }

            Vector3 pos = Vector3.zero;
            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (objectid == ActiveDynamicObjectsArray[i].Id)
                {
                    pos = ActiveDynamicObjectsArray[i].LastPosition;
                }
            }

            CustomEvent ce;
            if (Engagements.TryGetValue(uniqueEngagementId, out ce))
            {
                ce.SetProperties(properties).Send(pos);
                Engagements.Remove(uniqueEngagementId);
            }
            else
            {
                //create and send immediately
                new CustomEvent(engagementname).SetProperties(properties).SetDynamicObject(objectid).Send(pos);
            }
        }

        #endregion

#region Controllers and Hands Manager
        private static DynamicData[] ActiveInputsArray = new DynamicData[24];

        internal static void RegisterController(XRNode controller, bool isRight, string registerId = "")
        {
            InputUtil.TryGetInputDevice(controller, out InputDevice controllerDevice);
            InputUtil.TryGetControllerPosition(controller, out Vector3 pos);
            InputUtil.TryGetControllerRotation(controller, out Quaternion rot);

            var controllerDisplayType = InputUtil.GetControllerPopupName(controllerDevice.name, isRight);
            var commonDynamicMesh = InputUtil.GetControllerMeshName(controllerDevice.name, isRight);
            var registerMeshName = commonDynamicMesh.ToString();
            var data = new DynamicData(controllerDevice.name, registerId, registerMeshName, pos, rot, 0.01f, 0.1f, 0.1f, InputUtil.InputType.Controller.ToString(), controllerDisplayType.ToString(), isRight);

            bool foundSpot = false;
            // Check for existing dynamic with same mesh name and controller type
            // Register controller and set manifest entry properties
            for (int i = 0; i < ActiveInputsArray.Length; i++)
            {
                if (ActiveInputsArray[i].active && 
                    ActiveInputsArray[i].MeshName == registerMeshName && 
                    ActiveInputsArray[i].IsRightHand == isRight)
                {
                    // Update the existing dynamic data
                    ActiveInputsArray[i].LastPosition = pos;
                    ActiveInputsArray[i].LastRotation = rot;
                    data = ActiveInputsArray[i];// Avoid creating a new dynamic
                    foundSpot = true;
                }
                else if (!ActiveInputsArray[i].active)
                {
                    ActiveInputsArray[i] = data;
                    foundSpot = true;
                    break;
                }
            }

            if (!foundSpot)
            {
                Util.logWarning("Dynamic Object Array expanded!");

                int nextFreeIndex = ActiveInputsArray.Length;
                Array.Resize<DynamicData>(ref ActiveInputsArray, ActiveInputsArray.Length * 2);
                //just expanded the array. this spot will be empty
                ActiveInputsArray[nextFreeIndex] = data;
            }

            if (Cognitive3D_Manager.IsInitialized)
                CoreInterface.WriteControllerManifestEntry(data);
        }

        internal static void RegisterController(DynamicData data)
        {
            //check for duplicate ids in all data
            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].active && data.Id == ActiveDynamicObjectsArray[i].Id)
                {
                    return;
                }
            }

            //register controller and set manifest entry properties
            bool foundSpot = false;
            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (!ActiveDynamicObjectsArray[i].active)
                {
                    ActiveDynamicObjectsArray[i] = data;
                    foundSpot = true;
                    break;
                }
            }
            if (!foundSpot)
            {
                Util.logWarning("Dynamic Object Array expanded!");

                int nextFreeIndex = ActiveDynamicObjectsArray.Length;
                Array.Resize<DynamicData>(ref ActiveDynamicObjectsArray, ActiveDynamicObjectsArray.Length * 2);
                //just expanded the array. this spot will be empty
                ActiveDynamicObjectsArray[nextFreeIndex] = data;
            }
            if (Cognitive3D_Manager.IsInitialized)
                CoreInterface.WriteControllerManifestEntry(data);
        }

        internal static void RegisterHand(XRNode hand, bool isRight, string registerId = "")
        {
            InputUtil.TryGetControllerPosition(hand, out Vector3 pos);
            InputUtil.TryGetControllerRotation(hand, out Quaternion rot);

            var name = isRight ? "RightHand" : "LeftHand";
            var controllerDisplayType = isRight ? InputUtil.ControllerDisplayType.hand_right : InputUtil.ControllerDisplayType.hand_left;
            var commonDynamicMesh = isRight ? InputUtil.CommonDynamicMesh.handRight : InputUtil.CommonDynamicMesh.handLeft;
            var registerMeshName = commonDynamicMesh.ToString();
            var data = new DynamicData(name, registerId, registerMeshName, pos, rot, 0.01f, 0.1f, 0.1f, InputUtil.InputType.Hand.ToString(), controllerDisplayType.ToString(), isRight);

            bool foundSpot = false;
            // Check for existing dynamic with same mesh name and controller type
            // Register hand and set manifest entry properties
            for (int i = 0; i < ActiveInputsArray.Length; i++)
            {
                if (ActiveInputsArray[i].active && 
                    ActiveInputsArray[i].MeshName == registerMeshName && 
                    ActiveInputsArray[i].IsRightHand == isRight)
                {
                    // Update the existing dynamic data
                    ActiveInputsArray[i].LastPosition = pos;
                    ActiveInputsArray[i].LastRotation = rot;
                    data = ActiveInputsArray[i];// Avoid creating a new dynamic
                    foundSpot = true;
                }
                else if (!ActiveInputsArray[i].active)
                {
                    ActiveInputsArray[i] = data;
                    foundSpot = true;
                    break;
                }
            }

            if (!foundSpot)
            {
                Util.logWarning("Dynamic Object Array expanded!");

                int nextFreeIndex = ActiveInputsArray.Length;
                Array.Resize<DynamicData>(ref ActiveInputsArray, ActiveInputsArray.Length * 2);
                //just expanded the array. this spot will be empty
                ActiveInputsArray[nextFreeIndex] = data;
            }

            if (Cognitive3D_Manager.IsInitialized)
                CoreInterface.WriteControllerManifestEntry(data);
        }

        internal static DynamicData GetInputDynamicData(InputUtil.InputType type, bool isRight)
        {
            for (int i = 0; i < ActiveInputsArray.Length; i++)
            {
                if (ActiveInputsArray[i].IsController &&
                    ActiveInputsArray[i].InputType == type.ToString() &&
                    ActiveInputsArray[i].IsRightHand == isRight)
                {
                    return ActiveInputsArray[i];
                }
            }
            
            Util.logError($"No DynamicData found for type {type} and isRight {isRight}");
            return new DynamicData(); // Return null if no matching dynamic data is found
        }

        internal static void UpdateDynamicInputEnabledState(InputUtil.InputType currentTrackedDevice)
        {
            for (int i = 0; i < ActiveInputsArray.Length; i++)
            {
                if (ActiveInputsArray[i].active)
                {
                    List<KeyValuePair<string, object>> properties = new List<KeyValuePair<string, object>>
                    {
                        new KeyValuePair<string, object>("enabled",
                        ActiveInputsArray[i].InputType == currentTrackedDevice.ToString())
                    };

                    ActiveInputsArray[i].Properties = properties;
                    ActiveInputsArray[i].dirty = true;
                    ActiveInputsArray[i].HasProperties = true;
                }
            }
        }

        /// <summary>
        /// takes a list of inputs changed this frame. writes a snapshot outside of normal tick
        /// </summary>
        /// <param name="data"></param>
        /// <param name="changedInputs"></param>
        internal static void RecordControllerEvent(string id, List<ButtonState> changedInputs)
        {
            bool found = false;
            int i = 0;
            for (; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].Id == id)
                {
                    found = true;
                    break;
                }
            }
            if (!found) { Util.logDebug("Dynamic Object ID " + id + " not found"); return; }


            if (!Cognitive3D_Manager.IsInitialized) { return; }
            Vector3 pos = ActiveDynamicObjectsArray[i].Transform.position;
            Vector3 scale = ActiveDynamicObjectsArray[i].Transform.lossyScale;
            Quaternion rot = ActiveDynamicObjectsArray[i].Transform.rotation;

            bool writeScale = false;
            if (Vector3.SqrMagnitude(ActiveDynamicObjectsArray[i].LastScale - scale) > ActiveDynamicObjectsArray[i].ScaleThreshold * ActiveDynamicObjectsArray[i].ScaleThreshold)
            {
                //IMPROVEMENT INLINE SQRMAGNITUDE
                //TEST scale threshold
                writeScale = true;
                ActiveDynamicObjectsArray[i].dirty = true;
            }
            
            //write changedinputs into string
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256 * changedInputs.Count);
            if (changedInputs.Count > 0)
            {
                ActiveDynamicObjectsArray[i].dirty = true;
                for(int j = 0; j<changedInputs.Count;j++)
                {
                    if (j != 0) { builder.Append(","); }
                    builder.Append("\"");
                    builder.Append(changedInputs[j].ButtonName);
                    builder.Append("\":{");
                    builder.Append("\"buttonPercent\":");
                    builder.Append(changedInputs[j].ButtonPercent);
                    if (changedInputs[j].IncludeXY)
                    {
                        builder.Append(",\"x\":");
                        builder.Append(changedInputs[j].X.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                        builder.Append(",\"y\":");
                        builder.Append(changedInputs[j].Y.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    builder.Append("}");
                }
            }

            if (ActiveDynamicObjectsArray[i].dirty || ActiveDynamicObjectsArray[i].HasProperties || !ActiveDynamicObjectsArray[i].hasEnabled || ActiveDynamicObjectsArray[i].remove) //HasProperties, HasEnabled, Remove should all have Dirty set at the same time
            {
                ActiveDynamicObjectsArray[i].UpdateInterval = 0;

                ActiveDynamicObjectsArray[i].dirty = false;
                ActiveDynamicObjectsArray[i].LastPosition = pos;
                ActiveDynamicObjectsArray[i].LastRotation = rot;
                if (writeScale)
                {
                    ActiveDynamicObjectsArray[i].LastScale = scale;
                }
                string props = null;
                if (ActiveDynamicObjectsArray[i].HasProperties)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
                    for (int j = 0; j < ActiveDynamicObjectsArray[i].Properties.Count; j++)
                    {
                        if (j != 0)
                            sb.Append(",");
                        sb.Append("{\"");
                        sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Key);
                        sb.Append("\":");

                        if (ActiveDynamicObjectsArray[i].Properties[j].Value is string)
                        {
                            sb.Append("\"");
                            sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Value);
                            sb.Append("\"");
                        } else if (ActiveDynamicObjectsArray[i].Properties[j].Value is bool) // Explicitly handle booleans
                        {
                            sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Value.ToString().ToLower()); // Convert to lowercase
                        }
                        else
                        {
                            sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Value);
                        }
                        sb.Append("}");
                    }
                    props = sb.ToString();
                }

                if (!ActiveDynamicObjectsArray[i].hasEnabled)
                {
                    ActiveDynamicObjectsArray[i].hasEnabled = true;
                    if (ActiveDynamicObjectsArray[i].HasProperties || !string.IsNullOrEmpty(props))
                    {
                        props += ",{\"enabled\":true}";
                    }
                    else
                    {
                        props += "{\"enabled\":true}";
                    }
                    writeScale = true;
                    ActiveDynamicObjectsArray[i].LastScale = scale;
                }

                if (ActiveDynamicObjectsArray[i].remove)
                {
                    if (ActiveDynamicObjectsArray[i].HasProperties || !string.IsNullOrEmpty(props))
                    {
                        props += ",{\"enabled\":false}";
                    }
                    else
                    {
                        props += "{\"enabled\":false}";
                    }
                }

                if (ActiveDynamicObjectsArray[i].HasProperties)
                {
                    ActiveDynamicObjectsArray[i].HasProperties = false;
                    ActiveDynamicObjectsArray[i].Properties = null;
                }
                CoreInterface.WriteDynamicController(ActiveDynamicObjectsArray[i], props, writeScale, builder.ToString(),Util.Timestamp(Time.frameCount));
            }
        }

        internal static void RecordControllerEvent(bool isRight, List<ButtonState> changedInputs)
        {
            var controller = GetInputDynamicData(InputUtil.InputType.Controller, isRight);

            if (!String.IsNullOrEmpty(controller.Id))
            {
                bool found = false;
                int i = 0;
                for (; i < ActiveInputsArray.Length; i++)
                {
                    if (ActiveInputsArray[i].Id == controller.Id)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) { Util.logDebug("Dynamic Object ID " + controller.Id + " not found"); return; }


                if (!Cognitive3D_Manager.IsInitialized) { return; }
                Vector3 pos;
                Quaternion rot;

                XRNode handNode = isRight ? XRNode.RightHand : XRNode.LeftHand;
                InputUtil.TryGetControllerPosition(handNode, out pos);
                InputUtil.TryGetControllerRotation(handNode, out rot);
                
                //write changedinputs into string
                System.Text.StringBuilder builder = new System.Text.StringBuilder(256 * changedInputs.Count);
                if (changedInputs.Count > 0)
                {
                    ActiveInputsArray[i].dirty = true;
                    for(int j = 0; j<changedInputs.Count;j++)
                    {
                        if (j != 0) { builder.Append(","); }
                        builder.Append("\"");
                        builder.Append(changedInputs[j].ButtonName);
                        builder.Append("\":{");
                        builder.Append("\"buttonPercent\":");
                        builder.Append(changedInputs[j].ButtonPercent);
                        if (changedInputs[j].IncludeXY)
                        {
                            builder.Append(",\"x\":");
                            builder.Append(changedInputs[j].X.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                            builder.Append(",\"y\":");
                            builder.Append(changedInputs[j].Y.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        builder.Append("}");
                    }
                }

                if (ActiveInputsArray[i].dirty || ActiveInputsArray[i].HasProperties || !ActiveInputsArray[i].hasEnabled || ActiveInputsArray[i].remove) //HasProperties, HasEnabled, Remove should all have Dirty set at the same time
                {
                    ActiveInputsArray[i].UpdateInterval = 0;

                    ActiveInputsArray[i].dirty = false;
                    ActiveInputsArray[i].LastPosition = pos;
                    ActiveInputsArray[i].LastRotation = rot;
                    string props = null;
                    if (ActiveInputsArray[i].HasProperties)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
                        for (int j = 0; j < ActiveInputsArray[i].Properties.Count; j++)
                        {
                            if (j != 0)
                                sb.Append(",");
                            sb.Append("{\"");
                            sb.Append(ActiveInputsArray[i].Properties[j].Key);
                            sb.Append("\":");

                            if (ActiveInputsArray[i].Properties[j].Value is string)
                            {
                                sb.Append("\"");
                                sb.Append(ActiveInputsArray[i].Properties[j].Value);
                                sb.Append("\"");
                            } else if (ActiveInputsArray[i].Properties[j].Value is bool) // Explicitly handle booleans
                            {
                                sb.Append(ActiveInputsArray[i].Properties[j].Value.ToString().ToLower()); // Convert to lowercase
                            }
                            else
                            {
                                sb.Append(ActiveInputsArray[i].Properties[j].Value);
                            }
                            sb.Append("}");
                        }
                        props = sb.ToString();

                        ActiveInputsArray[i].HasProperties = false;
                        ActiveInputsArray[i].Properties = null;
                    }
                    
                    CoreInterface.WriteDynamicController(ActiveInputsArray[i], props, false, builder.ToString(),Util.Timestamp(Time.frameCount));
                }
            }
            
        }

        // Resets input array
        internal static void InputReset()
        {
            if (!Cognitive3D_Manager.IsInitialized) return;
            Array.Clear(ActiveInputsArray, 0, ActiveInputsArray.Length);
        }
#endregion

        internal static void SetProperties(string id, List<KeyValuePair<string,object>> properties)
        {
            bool found = false;
            int i = 0;
            for (; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].Id == id)
                {
                    found = true;
                    break;
                }
            }
            if (!found) { Util.logDebug("Dynamic Object ID " + id + " not found"); return; }
            ActiveDynamicObjectsArray[i].dirty = true;
            ActiveDynamicObjectsArray[i].HasProperties = true;
            ActiveDynamicObjectsArray[i].Properties = properties;
        }

        internal static void SetDirty(string id)
        {
            bool found = false;
            int i = 0;
            for (; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].Id == id)
                {
                    found = true;
                    break;
                }
            }
            if (!found) { Util.logDebug("Dynamic Object ID " + id + " not found"); return; }
            ActiveDynamicObjectsArray[i].dirty = true;
        }

        internal static void SetTransform(string id, Transform transform)
        {
            bool found = false;
            int i = 0;
            for (; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].Id == id)
                {
                    found = true;
                    break;
                }
            }
            if (!found) { Util.logDebug("Dynamic Object ID " + id + " not found"); return; }
            ActiveDynamicObjectsArray[i].LastPosition = transform.position;
            ActiveDynamicObjectsArray[i].LastRotation = transform.rotation;
        }

        /// <summary>
        /// returns true if dynamic object has been registered, is active and is not being removed
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool IsDataActive(string id)
        {
            if (string.IsNullOrEmpty(id)) { return false; }
            bool found = false;
            int i = 0;
            for (; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].Id == id)
                {
                    found = true;
                    break;
                }
            }
            if (!found) { Util.logDebug("Dynamic Object ID " + id + " not found"); return false; }
            return ActiveDynamicObjectsArray[i].active && !ActiveDynamicObjectsArray[i].remove;
        }

        /// <summary>
        /// manually record a dynamic object. shouldn't be called except when you need a _precise_ time
        /// </summary>
        /// <param name="id"></param>
        /// <param name="forceWrite">writes snapshot even if dynamic hasn't moved beyond threshold</param>
        internal static void RecordDynamic(string id, bool forceWrite)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }

            int i = 0;
            bool found = false;
            for(; i< ActiveDynamicObjectsArray.Length;i++)
            {
                if (ActiveDynamicObjectsArray[i].Id == id)
                {
                    found = true;
                    break;
                }
            }
            if (!found) { Util.logDebug("Dynamic Object ID " + id + " not found"); return; }
            if (!ActiveDynamicObjectsArray[i].Transform) return;

            ActiveDynamicObjectsArray[i].dirty = true;

            Vector3 pos = ActiveDynamicObjectsArray[i].Transform.position;
            Vector3 scale = ActiveDynamicObjectsArray[i].Transform.lossyScale;
            Quaternion rot = ActiveDynamicObjectsArray[i].Transform.rotation;

            //check distance
            if (!forceWrite)
            {
                //IMPROVEMENT INLINE SQRMAGNITUDE
                if (Vector3.SqrMagnitude(pos - ActiveDynamicObjectsArray[i].LastPosition) > ActiveDynamicObjectsArray[i].PositionThreshold * ActiveDynamicObjectsArray[i].PositionThreshold)
                {
                    ActiveDynamicObjectsArray[i].dirty = true;
                    forceWrite = true;
                }
            }

            //check rotation
            if (!forceWrite)
            {
                //IMPROVEMENT INLINE DOT
                float f = Quaternion.Dot(ActiveDynamicObjectsArray[i].LastRotation, rot);

                float fabs = f < 0 ? f * -1 : f;
                float min = fabs < 1 ? fabs : 1;

                if (System.Math.Acos(min) * 114.59156f > ActiveDynamicObjectsArray[i].RotationThreshold)
                {
                    ActiveDynamicObjectsArray[i].dirty = true;
                    forceWrite = true;
                }
            }
            //check scale
            bool writeScale = false;
            if (Vector3.SqrMagnitude(ActiveDynamicObjectsArray[i].LastScale - scale) > ActiveDynamicObjectsArray[i].ScaleThreshold * ActiveDynamicObjectsArray[i].ScaleThreshold)
            {
                //IMPROVEMENT INLINE SQRMAGNITUDE
                //TEST scale threshold
                writeScale = true;
                forceWrite = true;
                ActiveDynamicObjectsArray[i].dirty = true;
            }

            if (forceWrite)
            {

                if (ActiveDynamicObjectsArray[i].dirty || ActiveDynamicObjectsArray[i].HasProperties || !ActiveDynamicObjectsArray[i].hasEnabled || ActiveDynamicObjectsArray[i].remove) //HasProperties, HasEnabled, Remove should all have Dirty set at the same time
                {
                    ActiveDynamicObjectsArray[i].UpdateInterval = 0;

                    ActiveDynamicObjectsArray[i].dirty = false;
                    ActiveDynamicObjectsArray[i].LastPosition = pos;
                    ActiveDynamicObjectsArray[i].LastRotation = rot;
                    if (writeScale)
                    {
                        ActiveDynamicObjectsArray[i].LastScale = scale;
                    }
                    string props = null;
                    if (ActiveDynamicObjectsArray[i].HasProperties)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
                        for (int j = 0; j < ActiveDynamicObjectsArray[i].Properties.Count; j++)
                        {
                            if (j != 0)
                                sb.Append(",");
                            sb.Append("{\"");
                            sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Key);
                            sb.Append("\":");

                            if (ActiveDynamicObjectsArray[i].Properties[j].Value is string)
                            {
                                sb.Append("\"");
                                sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Value);
                                sb.Append("\"");
                            }else if (ActiveDynamicObjectsArray[i].Properties[j].Value is bool) // Explicitly handle booleans
                            {
                                sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Value.ToString().ToLower()); // Convert to lowercase
                            }
                            else
                            {
                                sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Value);
                            }
                            sb.Append("}");
                        }
                        props = sb.ToString();
                    }

                    if (!ActiveDynamicObjectsArray[i].hasEnabled)
                    {
                        ActiveDynamicObjectsArray[i].hasEnabled = true;
                        if (ActiveDynamicObjectsArray[i].HasProperties || !string.IsNullOrEmpty(props))
                        {
                            props += ",{\"enabled\":true}";
                        }
                        else
                        {
                            props += "{\"enabled\":true}";
                        }
                        writeScale = true;
                        ActiveDynamicObjectsArray[i].LastScale = scale;
                    }

                    if (ActiveDynamicObjectsArray[i].remove)
                    {
                        if (ActiveDynamicObjectsArray[i].HasProperties || !string.IsNullOrEmpty(props))
                        {
                            props += ",{\"enabled\":false}";
                        }
                        else
                        {
                            props += "{\"enabled\":false}";
                        }
                    }

                    if (ActiveDynamicObjectsArray[i].HasProperties)
                    {
                        ActiveDynamicObjectsArray[i].HasProperties = false;
                        ActiveDynamicObjectsArray[i].Properties = null;
                    }
                    CoreInterface.WriteDynamic(ActiveDynamicObjectsArray[i], props, writeScale, Util.Timestamp(Time.frameCount));
                }
            }
        }

        //allow ticking a limited number of dynamic objects per frame
        static int index = 0;
        static int maxTicks = 128;
        //IMPROVEMENT some function based on the number of dynamic objects to track and the intervals needed. should be as high as possible without causing 'overlap' between this tick and the next

        //iterate through all dynamic objects
        //alternatively, could go iterate through chunks of dynamics each frame, instead of all at once
        private static void OnUpdate(float deltaTime)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (string.IsNullOrEmpty(Cognitive3D_Manager.TrackingSceneId)) { return; }

            //limits the number of dynamic object data that can be processed each update loop
            int numTicks = 0;

            // Process controllers/hands
            ProcessInputDynamicArray(ref ActiveInputsArray, InputUtil.GetCurrentTrackedDevice(), deltaTime, ref numTicks);

            // Process dynamic objects
            ProcessDynamicArray(ref ActiveDynamicObjectsArray, deltaTime, ref numTicks);
        }

        private static void ProcessDynamicArray(ref DynamicData[] array, float deltaTime, ref int numTicks)
        {
            for (; index < array.Length; index++)
            {
                if (!array[index].active) continue;

                if (!array[index].dirty && array[index].UpdateInterval < array[index].DesiredUpdateRate) 
                {
                    array[index].UpdateInterval += deltaTime; 
                    continue;
                }
                array[index].UpdateInterval = 0;

                bool writeData = array[index].dirty;

                // Get position, scale, rotation depending on whether it's a controller or dynamic object
                Vector3 pos;
                Vector3 scale;
                Quaternion rot;

                if (array[index].Transform == null) array[index].remove = true;
                pos = array[index].remove ? array[index].LastPosition : array[index].Transform.position;
                scale = array[index].remove ? array[index].LastScale : array[index].Transform.lossyScale;
                rot = array[index].remove ? array[index].LastRotation : array[index].Transform.rotation;

                //check distance
                if (!writeData)
                {
                    //IMPROVEMENT INLINE SQRMAGNITUDE
                    if (Vector3.SqrMagnitude(pos - array[index].LastPosition) > array[index].PositionThreshold * array[index].PositionThreshold)
                    {
                        array[index].dirty = true;
                        writeData = true;
                    }
                }

                //check rotation
                if (!writeData)
                {
                    //IMPROVEMENT INLINE DOT
                    float f = Quaternion.Dot(array[index].LastRotation, rot);

                    float fabs = f < 0 ? f * -1 : f;
                    float min = fabs < 1 ? fabs : 1;

                    if (System.Math.Acos(min) * 114.59156f > array[index].RotationThreshold)
                    {
                        array[index].dirty = true;
                        writeData = true;
                    }
                }

                //check scale
                bool writeScale = false;
                if (Vector3.SqrMagnitude(array[index].LastScale - scale) > array[index].ScaleThreshold * array[index].ScaleThreshold)
                {
                    //IMPROVEMENT INLINE SQRMAGNITUDE
                    //TEST scale threshold
                    writeScale = true;
                    writeData = true;
                    array[index].dirty = true;
                }

                // Update the dynamic object only if necessary
                if (writeData || array[index].dirty || array[index].HasProperties || !array[index].hasEnabled || array[index].remove)
                {
                    array[index].dirty = false;
                    array[index].LastPosition = pos;
                    array[index].LastRotation = rot;

                    if (writeScale)
                    {
                        array[index].LastScale = scale;
                    }

                    string props = null;
                    if (array[index].HasProperties)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
                        for (int j = 0; j < array[index].Properties.Count; j++)
                        {
                            if (j != 0)
                                sb.Append(",");
                            sb.Append("{\"");
                            sb.Append(array[index].Properties[j].Key);
                            sb.Append("\":");

                            if (array[index].Properties[j].Value is string)
                            {
                                sb.Append("\"");
                                sb.Append(array[index].Properties[j].Value);
                                sb.Append("\"");
                            }else if (array[index].Properties[j].Value is bool) // Explicitly handle booleans
                            {
                                sb.Append(array[index].Properties[j].Value.ToString().ToLower()); // Convert to lowercase
                            }
                            else
                            {
                                sb.Append(array[index].Properties[j].Value);
                            }
                            sb.Append("}");
                        }
                        props = sb.ToString();
                    }
                    
                    if (!array[index].hasEnabled)
                    {
                        array[index].hasEnabled = true;
                        if (array[index].HasProperties || !string.IsNullOrEmpty(props))
                        {
                            props += ",{\"enabled\":true}";
                        }
                        else
                        {
                            props += "{\"enabled\":true}";
                        }
                    }

                    if (array[index].remove)
                    {
                        if (array[index].HasProperties || !string.IsNullOrEmpty(props))
                        {
                            props += ",{\"enabled\":false}";
                        }
                        else
                        {
                            props += "{\"enabled\":false}";
                        }
                    }

                    if (array[index].HasProperties)
                    {
                        array[index].HasProperties = false;
                        array[index].Properties = null;
                    }

                    CoreInterface.WriteDynamic(array[index], props, writeScale, Util.Timestamp(Time.frameCount));
                }

                if (array[index].remove)
                {
                    array[index].active = false;
                    array[index].remove = false;
                    array[index].hasEnabled = false;
                }

                numTicks++;
                if (numTicks > maxTicks)
                {
                    //limit the number of data points processed each frame
                    return;
                }
            }
            index = 0;
        }

        private static void ProcessInputDynamicArray(ref DynamicData[] array, InputUtil.InputType currentInputTracking, float deltaTime, ref int numTicks)
        {
            for (; index < array.Length; index++)
            {
                if (!array[index].active) continue;

                if (!array[index].dirty && currentInputTracking.ToString() != array[index].InputType)
                {
                    continue;
                }

                if (!array[index].dirty && array[index].UpdateInterval < array[index].DesiredUpdateRate) 
                {
                    array[index].UpdateInterval += deltaTime; 
                    continue;
                }
                array[index].UpdateInterval = 0;

                bool writeData = array[index].dirty;

                // Get position, scale, rotation depending on whether it's a controller or dynamic object
                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;

                if (array[index].IsController)
                {
                    XRNode handNode = array[index].IsRightHand ? XRNode.RightHand : XRNode.LeftHand;
                    InputUtil.TryGetControllerPosition(handNode, out pos);
                    InputUtil.TryGetControllerRotation(handNode, out rot);
                }

                //check distance
                if (!writeData)
                {
                    //IMPROVEMENT INLINE SQRMAGNITUDE
                    if (Vector3.SqrMagnitude(pos - array[index].LastPosition) > array[index].PositionThreshold * array[index].PositionThreshold)
                    {
                        array[index].dirty = true;
                        writeData = true;
                    }
                }

                //check rotation
                if (!writeData)
                {
                    //IMPROVEMENT INLINE DOT
                    float f = Quaternion.Dot(array[index].LastRotation, rot);

                    float fabs = f < 0 ? f * -1 : f;
                    float min = fabs < 1 ? fabs : 1;

                    if (System.Math.Acos(min) * 114.59156f > array[index].RotationThreshold)
                    {
                        array[index].dirty = true;
                        writeData = true;
                    }
                }

                // Update the dynamic object only if necessary
                if (writeData || array[index].dirty || array[index].HasProperties || array[index].remove)
                {
                    array[index].dirty = false;
                    array[index].LastPosition = pos;
                    array[index].LastRotation = rot;

                    string props = null;
                    if (array[index].HasProperties)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
                        for (int j = 0; j < array[index].Properties.Count; j++)
                        {
                            if (j != 0)
                                sb.Append(",");
                            sb.Append("{\"");
                            sb.Append(array[index].Properties[j].Key);
                            sb.Append("\":");

                            if (array[index].Properties[j].Value is string)
                            {
                                sb.Append("\"");
                                sb.Append(array[index].Properties[j].Value);
                                sb.Append("\"");
                            }else if (array[index].Properties[j].Value is bool) // Explicitly handle booleans
                            {
                                sb.Append(array[index].Properties[j].Value.ToString().ToLower()); // Convert to lowercase
                            }
                            else
                            {
                                sb.Append(array[index].Properties[j].Value);
                            }
                            sb.Append("}");
                        }
                        props = sb.ToString();

                        array[index].HasProperties = false;
                        array[index].Properties = null;
                    }

                    CoreInterface.WriteDynamic(array[index], props, false, Util.Timestamp(Time.frameCount));
                }

                if (array[index].remove)
                {
                    array[index].active = false;
                    array[index].remove = false;
                }

                numTicks++;
                if (numTicks > maxTicks)
                {
                    //limit the number of data points processed each frame
                    return;
                }
            }
            index = 0;
        }


        /// <summary>
        /// used to manually send all outstanding dynamic data immediately
        /// called from FlushData
        /// </summary>
        internal static void SendData(bool copyDataToCache)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (string.IsNullOrEmpty(Cognitive3D_Manager.TrackingSceneId)) { return; }

            int dirtycount = 0;
            //set all active dynamics as dirty
            for (int i = 0; i<ActiveDynamicObjectsArray.Length;i++)
            {
                if (!ActiveDynamicObjectsArray[i].active) { continue; }
                ActiveDynamicObjectsArray[i].dirty = true;
                dirtycount++;
            }

            //set loop index to start
            index = 0;
            do
            {
                //call update until the loop completes (which sets index = 0 and returns)
                //this adds all snapshots to queuedSnapshot queue
                OnUpdate(0);
            }
            while (index != 0);

            //force dynamicCore to send all queued data as web requests
            CoreInterface.Flush(copyDataToCache);
        }

        //this happens AFTER tracking scene is set
        //all registered dynamic objects will send data in the new scene
        //each dynamic left behind in the old scene should call 'removedynamic' to remove itself from ActiveDynamicObjects list
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode, bool didChangeSceneId)
        {
            //Cognitive3D_Manager will call Core.SendData if sceneid has changed

            if (didChangeSceneId)
            {
                for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
                {
                    if (!ActiveDynamicObjectsArray[i].remove)
                    {
                        ActiveDynamicObjectsArray[i].hasEnabled = false;

                        if (ActiveDynamicObjectsArray[i].active)
                        {
                            if (ActiveDynamicObjectsArray[i].IsController)
                            {
                                //DynamicObjectCore.WriteControllerManifestEntry(ActiveDynamicObjectsArray[i]);
                                CoreInterface.WriteControllerManifestEntry(ActiveDynamicObjectsArray[i]);
                            }
                            else
                            {
                                //DynamicObjectCore.WriteDynamicManifestEntry(ActiveDynamicObjectsArray[i]);
                                CoreInterface.WriteDynamicManifestEntry(ActiveDynamicObjectsArray[i]);
                            }
                        }
                    }
                }

                SendData(false); //not 100% necessary. immediately sends dynamic manifest and snapshot to new scene
            }
        }

        static int lastValidId;
        internal static string GetUniqueObjectId(string meshname)
        {
            for (int i = 0; i < DynamicObjectIdArray.Length; i++)
            {
                if (!DynamicObjectIdArray[i].MeshSet) //there's an id that does not have a mesh set (ie, never been used)
                {
                    DynamicObjectIdArray[i].MeshSet = true;
                    DynamicObjectIdArray[i].Used = true;
                    DynamicObjectIdArray[i].MeshName = meshname;
                    lastValidId++;
                    DynamicObjectIdArray[i].Id = lastValidId.ToString();
                    return DynamicObjectIdArray[i].Id;
                }
                else if (DynamicObjectIdArray[i].Used == false && DynamicObjectIdArray[i].MeshName == meshname) //an unused id with a matching mesh name
                {
                    DynamicObjectIdArray[i].Used = true;
                    return DynamicObjectIdArray[i].Id;
                }
            }


            int nextFreeIndex = DynamicObjectIdArray.Length;
            Array.Resize<DynamicObjectId>(ref DynamicObjectIdArray, DynamicObjectIdArray.Length * 2);

            DynamicObjectIdArray[nextFreeIndex].MeshSet = true;
            DynamicObjectIdArray[nextFreeIndex].Used = true;
            DynamicObjectIdArray[nextFreeIndex].MeshName = meshname;
            lastValidId++;
            DynamicObjectIdArray[nextFreeIndex].Id = lastValidId.ToString();
            return DynamicObjectIdArray[nextFreeIndex].Id;
        }
    }
}