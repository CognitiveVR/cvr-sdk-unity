using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using CognitiveVR;


//what is the split between dynamicCORE and dynamicMANAGER?
//MANAGER holds array of data and puts contents into CORE queues
//CORE writes json from queues

//add/remove dynamics to list. passed into core for writing to json
//run through list to check if the dynamic has moved recently
namespace CognitiveVR
{
    //used to update and record all dynamic object changes
    public static class DynamicManager
    {
        //this can track up to 1024 dynamic objects in a single scene AT THE SAME TIME before it needs to expand
        internal static DynamicData[] ActiveDynamicObjectsArray = new DynamicData[1024];
        //this can track up to 16 dynamic objects that appear in a session without a custom id. this helps session json reduce the number of entries in the manifest
        internal static DynamicObjectId[] DynamicObjectIdArray = new DynamicObjectId[16];

        public static void Initialize()
        {
            CognitiveVR.Core.OnSendData += SendData;
            CognitiveVR.Core.UpdateEvent += OnUpdate;
            CognitiveVR.Core.LevelLoadedEvent += OnSceneLoaded;
        }

        public static void RegisterDynamicObject(DynamicData data)
        {
            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (ActiveDynamicObjectsArray[i].active && data.Id == ActiveDynamicObjectsArray[i].Id)
                {
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

            CognitiveVR.DynamicObjectCore.WriteDynamicManifestEntry(data);
        }

        public static void RegisterController(DynamicData data)
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

            CognitiveVR.DynamicObjectCore.WriteControllerManifestEntry(data);
        }

        //public static void RegisterMedia(DynamicData data, string videoUrl)
        //{
        //    bool foundSpot = false;
        //    for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
        //    {
        //        //if (string.IsNullOrEmpty(ActiveDynamicObjectsArray[i].Id))
        //        if (!ActiveDynamicObjectsArray[i].active)
        //        {
        //            ActiveDynamicObjectsArray[i] = data;
        //            foundSpot = true;
        //            break;
        //        }
        //    }
        //    if (!foundSpot)
        //    {
        //        Debug.LogWarning("Dynamic Object Array expanded!");
        //
        //        int nextFreeIndex = ActiveDynamicObjectsArray.Length;
        //        Array.Resize<DynamicData>(ref ActiveDynamicObjectsArray, ActiveDynamicObjectsArray.Length * 2);
        //        //just expanded the array. this spot will be empty
        //        ActiveDynamicObjectsArray[nextFreeIndex] = data;
        //    }
        //
        //    CognitiveVR.Internal.DynamicCore.RegisterMedia(data, videoUrl);
        //}

        //this doesn't directly remove a dynamic object - it sets 'remove' so it can be removed on the next tick
        public static void RemoveDynamicObject(DynamicData data)
        {
            if (!Core.IsInitialized) { return; }
            //if application is quitting, return?

            for (int i = 0; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (String.CompareOrdinal(ActiveDynamicObjectsArray[i].Id, data.Id) == 0)
                {
                    ActiveDynamicObjectsArray[i].dirty = true;
                    ActiveDynamicObjectsArray[i].remove = true;

                    if (!ActiveDynamicObjectsArray[i].UseCustomId)
                    {
                        for (int j = 0; j < DynamicObjectIdArray.Length; j++)
                        {
                            if (DynamicObjectIdArray[j].Id == data.Id)
                            {
                                DynamicObjectIdArray[j].Used = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        static Dictionary<string, CustomEvent> Engagements = new Dictionary<string, CustomEvent>();

        /// <summary>
        /// creates a new custom event related to a dynamic object
        /// </summary>
        /// <param name="objectid"></param>
        /// <param name="engagementname"></param>
        /// <param name="uniqueEngagementId"></param>
        /// <param name="properties"></param>
        public static void BeginEngagement(string objectid, string engagementname = "default", string uniqueEngagementId = null, List<KeyValuePair<string, object>> properties = null)
        {
            if (uniqueEngagementId == null)
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

        public static void EndEngagement(string objectid, string engagementname = "default", string uniqueEngagementId = null, List<KeyValuePair<string, object>> properties = null)
        {
            if (uniqueEngagementId == null)
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

            CustomEvent ce = null;
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

        /// <summary>
        /// takes a list of inputs changed this frame. writes a snapshot outside of normal tick
        /// </summary>
        /// <param name="data"></param>
        /// <param name="changedInputs"></param>
        public static void RecordControllerEvent(ref DynamicData data, List<ButtonState> changedInputs)
        {
            if (!Core.IsInitialized) { return; }
            Vector3 pos = data.Transform.position;
            Vector3 scale = data.Transform.lossyScale;
            Quaternion rot = data.Transform.rotation;

            bool writeScale = false;
            if (Vector3.SqrMagnitude(data.LastScale - scale) > data.ScaleThreshold * data.ScaleThreshold)
            {
                //IMPROVEMENT INLINE SQRMAGNITUDE
                //TEST scale threshold
                writeScale = true;
                data.dirty = true;
            }
            
            //write changedinputs into string
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256 * changedInputs.Count);
            if (changedInputs.Count > 0)
            {
                data.dirty = true;
                //builder.Append(",\"buttons\":{");
                for(int i = 0; i<changedInputs.Count;i++)
                {
                    if (i != 0) { builder.Append(","); }
                    builder.Append("\"");
                    builder.Append(changedInputs[i].ButtonName);
                    builder.Append("\":{");
                    builder.Append("\"buttonPercent\":");
                    builder.Append(changedInputs[i].ButtonPercent);
                    if (changedInputs[i].IncludeXY)
                    {
                        builder.Append(",\"x\":");
                        builder.Append(changedInputs[i].X.ToString("0.000"));
                        builder.Append(",\"y\":");
                        builder.Append(changedInputs[i].Y.ToString("0.000"));
                    }
                    builder.Append("}");
                }
                //builder.Append("}");
            }

            if (data.dirty || data.HasProperties || !data.hasEnabled || data.remove) //HasProperties, HasEnabled, Remove should all have Dirty set at the same time
            {
                data.UpdateInterval = 0;

                data.dirty = false;
                data.LastPosition = pos;
                data.LastRotation = rot;
                if (writeScale)
                {
                    data.LastScale = scale;
                }
                string props = null;
                if (data.HasProperties)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
                    for (int j = 0; j < data.Properties.Count; j++)
                    {
                        if (j != 0)
                            sb.Append(",");
                        sb.Append(data.Properties[j].Key);
                        sb.Append(":");
                        sb.Append(data.Properties[j].Value);
                    }
                    props = sb.ToString();
                }

                if (!data.hasEnabled)
                {
                    data.hasEnabled = true;
                    if (data.HasProperties)
                    {
                        props += ",\"enabled\":true";
                    }
                    else
                    {
                        props += "\"enabled\":true";
                    }
                }

                if (data.remove)
                {
                    if (data.HasProperties)
                    {
                        props += ",\"enabled\":false";
                    }
                    else
                    {
                        props += "\"enabled\":false";
                    }
                }

                data.HasProperties = false;
                CognitiveVR.DynamicObjectCore.WriteDynamicController(data, props, writeScale, builder.ToString());
            }
        }

        //alllow ticking a limited number of dynamic objects per frame
        static int i = 0;
        static int maxTicks = 128;
        //IMPROVEMENT some function based on the number of dynamic objects to track and the intervals needed. should be as high as possible without causing 'overlap' between this tick and the next

        //iterate through all dynamic objects
        //alternatively, could go iterate through chunks of dynamics each frame, instead of all at once
        private static void OnUpdate(float deltaTime)
        {
            if (!Core.IsInitialized) { return; }

            //limits the number of dynamic object data that can be processed each update loop
            int numTicks = 0;

            for (; i < ActiveDynamicObjectsArray.Length; i++)
            {
                if (!ActiveDynamicObjectsArray[i].active) { continue; }

                //can set dynamic object to dirty to immediately send snapshot. otherwise wait for update interval
                if (!ActiveDynamicObjectsArray[i].dirty && ActiveDynamicObjectsArray[i].UpdateInterval < ActiveDynamicObjectsArray[i].DesiredUpdateRate) { ActiveDynamicObjectsArray[i].UpdateInterval += deltaTime; continue; }
                ActiveDynamicObjectsArray[i].UpdateInterval = 0;

                //used to skip through position and rotation check if one of them has already been set, or if the data was already marked as 'dirty'
                bool writeData = ActiveDynamicObjectsArray[i].dirty;

                //if removing, don't compare to current transform (possibly destroyed)
                Vector3 pos;
                Vector3 scale;
                Quaternion rot;

                if (ActiveDynamicObjectsArray[i].remove)
                {
                    pos = ActiveDynamicObjectsArray[i].LastPosition;
                    scale = ActiveDynamicObjectsArray[i].LastScale;
                    rot = ActiveDynamicObjectsArray[i].LastRotation;
                }
                else
                {
                    pos = ActiveDynamicObjectsArray[i].Transform.position;
                    scale = ActiveDynamicObjectsArray[i].Transform.lossyScale;
                    rot = ActiveDynamicObjectsArray[i].Transform.rotation;
                }


                //check distance
                if (!writeData)
                {
                    //IMPROVEMENT INLINE SQRMAGNITUDE
                    if (Vector3.SqrMagnitude(pos - ActiveDynamicObjectsArray[i].LastPosition) > ActiveDynamicObjectsArray[i].PositionThreshold * ActiveDynamicObjectsArray[i].PositionThreshold)
                    {
                        ActiveDynamicObjectsArray[i].dirty = true;
                        writeData = true;
                    }
                }

                //check rotation
                if (!writeData)
                {
                    //IMPROVEMENT INLINE DOT
                    float f = Quaternion.Dot(ActiveDynamicObjectsArray[i].LastRotation, rot);

                    float fabs = f < 0 ? f * -1 : f;
                    float min = fabs < 1 ? fabs : 1;

                    if (System.Math.Acos(min) * 114.59156f > ActiveDynamicObjectsArray[i].RotationThreshold)
                    {
                        ActiveDynamicObjectsArray[i].dirty = true;
                        writeData = true;
                    }
                }

                //check scale
                bool writeScale = false;
                if (Vector3.SqrMagnitude(ActiveDynamicObjectsArray[i].LastScale - scale) > ActiveDynamicObjectsArray[i].ScaleThreshold * ActiveDynamicObjectsArray[i].ScaleThreshold)
                {
                    //IMPROVEMENT INLINE SQRMAGNITUDE
                    //TEST scale threshold
                    writeScale = true;
                    writeData = true;
                    ActiveDynamicObjectsArray[i].dirty = true;
                }

                if (writeData || ActiveDynamicObjectsArray[i].dirty || ActiveDynamicObjectsArray[i].HasProperties || !ActiveDynamicObjectsArray[i].hasEnabled || ActiveDynamicObjectsArray[i].remove)
                {
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
                            sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Key);
                            sb.Append(":");
                            sb.Append(ActiveDynamicObjectsArray[i].Properties[j].Value);
                        }
                        props = sb.ToString();
                    }

                    if (!ActiveDynamicObjectsArray[i].hasEnabled)
                    {
                        ActiveDynamicObjectsArray[i].hasEnabled = true;
                        if (ActiveDynamicObjectsArray[i].HasProperties)
                        {
                            props += ",\"enabled\":true";
                        }
                        else
                        {
                            props += "\"enabled\":true";
                        }
                    }

                    if (ActiveDynamicObjectsArray[i].remove)
                    {
                        if (ActiveDynamicObjectsArray[i].HasProperties)
                        {
                            props += ",\"enabled\":false";
                        }
                        else
                        {
                            props += "\"enabled\":false";
                        }
                    }

                    CognitiveVR.DynamicObjectCore.WriteDynamic(ActiveDynamicObjectsArray[i], props, writeScale);
                }


                if (ActiveDynamicObjectsArray[i].remove)
                {
                    ActiveDynamicObjectsArray[i].active = false;
                    ActiveDynamicObjectsArray[i].remove = false;
                    ActiveDynamicObjectsArray[i].hasEnabled = false;
                }

                numTicks++;
                if (numTicks > maxTicks)
                {
                    //limit the number of data points processed each frame
                    return;
                }
            }
            i = 0;
        }

        /// <summary>
        /// used to manually send all outstanding dynamic data immediately
        /// </summary>
        public static void SendData()
        {
            i = 0;
            do
            {
                //call update on everything
                OnUpdate(60);
            }
            while (i != 0);

            //force dynamicCore to send all queued data as web requests
            CognitiveVR.DynamicObjectCore.FlushData();
        }

        //this happens AFTER tracking scene is set
        //all registered dynamic objects will send data in the new scene
        //each dynamic left behind in the old scene should call 'removedynamic' to remove itself from ActiveDynamicObjects list
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode, bool didChangeSceneId)
        {
            //CognitiveVR_Manager will call Core.SendData if sceneid has changed

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
                                DynamicObjectCore.WriteControllerManifestEntry(ActiveDynamicObjectsArray[i]);
                            else
                                DynamicObjectCore.WriteDynamicManifestEntry(ActiveDynamicObjectsArray[i]);
                        }
                    }
                }

                SendData(); //not 100% necessary. immediately sends dynamic manifest and snapshot to new scene
            }
        }

        static int id;
        public static string GetUniqueObjectId(string meshname)
        {
            for (int i = 0; i < DynamicObjectIdArray.Length; i++)
            {
                if (!DynamicObjectIdArray[i].MeshSet) //there's an id that does not have a mesh set (ie, never been used)
                {
                    DynamicObjectIdArray[i].MeshSet = true;
                    DynamicObjectIdArray[i].Used = true;
                    DynamicObjectIdArray[i].MeshName = meshname;
                    id++;
                    DynamicObjectIdArray[i].Id = id.ToString();
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
            id++;
            DynamicObjectIdArray[nextFreeIndex].Id = id.ToString();
            return DynamicObjectIdArray[nextFreeIndex].Id;
        }
    }
}