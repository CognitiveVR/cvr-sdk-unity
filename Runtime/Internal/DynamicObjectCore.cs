using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Threading;

//deals with formatting dynamic object snapshots and manifest entries to json
//deals with sending through network

namespace Cognitive3D
{
    /*internal static class DynamicObjectCore
    {
        private static int FrameCount;

        //marks the looping coroutine as 'ready' to pull data from the queue on a separate thread
        private static bool ReadyToWriteJson = false;

        private static int jsonPart = 1;

        private static Queue<DynamicObjectSnapshot> queuedSnapshots = new Queue<DynamicObjectSnapshot>();
        private static Queue<DynamicObjectManifestEntry> queuedManifest = new Queue<DynamicObjectManifestEntry>();

        //TODO loop to check if there's some outstanding dynamic data

        static float NextMinSendTime = 0;

        internal static int tempsnapshots = 0;

        internal static void Initialize()
        {
            //TODO start checkwritejson loop
            //Cognitive3D_Manager.NetworkManager.StartCoroutine(CheckWriteJson());
            for (int i = 0; i < Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount; i++)
            {
                DynamicObjectSnapshot.SnapshotPool.Enqueue(new DynamicObjectSnapshot());
            }

            Cognitive3D_Manager.OnUpdate -= Core_UpdateEvent;
            Cognitive3D_Manager.OnUpdate += Core_UpdateEvent;

            nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.DynamicSnapshotMaxTimer;
            Cognitive3D_Manager.NetworkManager.StartCoroutine(AutomaticSendTimer());
        }

        private static float nextSendTime = 0;
        private static IEnumerator AutomaticSendTimer()
        {
            while (true)
            {
                while (nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.DynamicSnapshotMaxTimer;
                if (Cognitive3D_Manager.TrackingScene != null)
                {
                    if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                        Util.logDevelopment("check to automatically send dynamics");
                    if (queuedManifest.Count > 0 || queuedSnapshots.Count > 0)
                    {
                        tempsnapshots = 0;
                        ReadyToWriteJson = true;
                    }
                }
            }
        }

        private static void Core_UpdateEvent(float deltaTime)
        {
            FrameCount = Time.frameCount;
        }

        internal static void WriteControllerManifestEntry(DynamicData data)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            dome.controllerType = data.ControllerType;
            dome.isController = true;
            if (data.IsRightHand)
            {
                dome.Properties = "\"controller\": \"right\"";
            }
            else
            {
                dome.Properties = "\"controller\": \"left\"";
            }
            dome.HasProperties = true;

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > Cognitive3D_Preferences.S_DynamicSnapshotCount)
            {
                //if (Time.time > NextMinSendTime || tempsnapshots > Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount)
                {
                    NextMinSendTime = Time.time + Cognitive3D_Preferences.S_DynamicSnapshotMaxTimer;
                    //check lastSendTimer and extreme batch size
                    tempsnapshots = 0;
                    ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
                }
            }
        }

        internal static void WriteDynamicMediaManifestEntry(DynamicData data, string videourl)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);
            dome.videoURL = videourl;

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > Cognitive3D_Preferences.S_DynamicSnapshotCount)
            {
                //if (Time.time > NextMinSendTime || tempsnapshots > Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount)
                {
                    NextMinSendTime = Time.time + Cognitive3D_Preferences.S_DynamicSnapshotMaxTimer;
                    //check lastSendTimer and extreme batch size
                    tempsnapshots = 0;
                    ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
                }
            }
        }

        internal static void WriteDynamicManifestEntry(DynamicData data, string formattedProperties)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            dome.HasProperties = true;
            dome.Properties = formattedProperties;

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > Cognitive3D_Preferences.S_DynamicSnapshotCount)
            {
                //if (Time.time > NextMinSendTime || tempsnapshots > Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount)
                {
                    NextMinSendTime = Time.time + Cognitive3D_Preferences.S_DynamicSnapshotMaxTimer;
                    //check lastSendTimer and extreme batch size
                    tempsnapshots = 0;
                    ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
                }
            }
        }

        /// <summary>
        /// put data into dynamic manifest
        /// </summary>
        /// <param name="data"></param>
        internal static void WriteDynamicManifestEntry(DynamicData data)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > Cognitive3D_Preferences.S_DynamicSnapshotCount)
            {
                //if (Time.time > NextMinSendTime || tempsnapshots > Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount)
                {
                    NextMinSendTime = Time.time + Cognitive3D_Preferences.S_DynamicSnapshotMaxTimer;
                    //check lastSendTimer and extreme batch size
                    tempsnapshots = 0;
                    ReadyToWriteJson = true;
                }
            }
        }

        internal static void WriteDynamic(DynamicData data, string props, bool writeScale)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            var s = DynamicObjectSnapshot.GetSnapshot();
            s.Id = data.Id;
            s.posX = data.LastPosition.x;
            s.posY = data.LastPosition.y;
            s.posZ = data.LastPosition.z;
            s.rotX = data.LastRotation.x;
            s.rotY = data.LastRotation.y;
            s.rotZ = data.LastRotation.z;
            s.rotW = data.LastRotation.w;

            if (writeScale)
            {
                s.DirtyScale = true;
                s.scaleX = data.LastScale.x;
                s.scaleY = data.LastScale.y;
                s.scaleZ = data.LastScale.z;
            }
            s.Properties = props;
            s.Timestamp = Util.Timestamp(FrameCount);

            queuedSnapshots.Enqueue(s);
            tempsnapshots++;
            if (tempsnapshots > Cognitive3D_Preferences.S_DynamicSnapshotCount)
            {
                //if (Time.time > NextMinSendTime || tempsnapshots > Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount)
                {
                    NextMinSendTime = Time.time + Cognitive3D_Preferences.S_DynamicSnapshotMaxTimer;
                    //check lastSendTimer and extreme batch size
                    tempsnapshots = 0;
                    ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
                }
            }
        }

        //button properties are formated as   ,"buttons":{"input":value,"input":value}
        internal static void WriteDynamicController(DynamicData data, string props, bool writeScale, string jbuttonstates)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Cognitive3D_Manager.TrackingScene == null) { return; }
            var s = DynamicObjectSnapshot.GetSnapshot();
            s.Id = data.Id;
            s.posX = data.LastPosition.x;
            s.posY = data.LastPosition.y;
            s.posZ = data.LastPosition.z;
            s.rotX = data.LastRotation.x;
            s.rotY = data.LastRotation.y;
            s.rotZ = data.LastRotation.z;
            s.rotW = data.LastRotation.w;

            if (writeScale)
            {
                s.DirtyScale = true;
                s.scaleX = data.LastScale.x;
                s.scaleY = data.LastScale.y;
                s.scaleZ = data.LastScale.z;
            }
            s.Properties = props;
            props = null;
            s.Buttons = jbuttonstates;

            s.Timestamp = Util.Timestamp(FrameCount);

            queuedSnapshots.Enqueue(s);
            tempsnapshots++;
            if (tempsnapshots > Cognitive3D_Preferences.S_DynamicSnapshotCount)
            {
                //if (Time.time > NextMinSendTime || tempsnapshots > Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount)
                {
                    NextMinSendTime = Time.time + Cognitive3D_Preferences.S_DynamicSnapshotMaxTimer;
                    //check lastSendTimer and extreme batch size
                    tempsnapshots = 0;
                    ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
                }
            }
        }

        //if WriteImmediate don't start threads 
        //static bool CopyDataToCache = false;

        internal static void FlushData(bool copyDataToCache)
        {
            /*

            if (queuedManifest.Count == 0 && queuedSnapshots.Count == 0) { return; }
            //CopyDataToCache = copyDataToCache;
            //ReadyToWriteJson = true;
            //WriteImmediate = true;

            InterruptThead = true;

            //if (WriteJsonRoutine != null)
            //Core.NetworkManager.StopCoroutine(WriteJsonRoutine);

            while (queuedSnapshots.Count > 0 || queuedManifest.Count > 0)
            {
                //WriteJsonImmediate(copyDataToCache);
            }
            tempsnapshots = 0;
            *//*
        }

        //this limits the amount of data that can be sent in a single batch
        //should drop work done on thread if writing json immediately (like on session end)
        //static bool FlushDataInterruptThread = false;

        //static IEnumerator WriteJsonRoutine;
        static bool InterruptThead = false;

        //loops and calls 'writeJson' until
        /*static IEnumerator CheckWriteJson()
        {
            while(true)
            {
                if (ReadyToWriteJson) //threading and waiting
                {
                    InterruptThead = false;
                    int totalDataToWrite = queuedManifest.Count + queuedSnapshots.Count;
                    totalDataToWrite = Mathf.Min(totalDataToWrite, Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount);

                    //TODO CONSIDER can i do all json building on a new thread and just the network request error handling after its complete?
                    //garbage collection on this string builder is painful

                    var builder = new System.Text.StringBuilder(200 + 128 * totalDataToWrite);
                    int manifestCount = Mathf.Min(queuedManifest.Count, totalDataToWrite);
                    int count = Mathf.Min(queuedSnapshots.Count, totalDataToWrite - manifestCount);

                    bool threadDone = true;
                    bool encounteredError = false;

                    builder.Append("{");

                    //header
                    JsonUtil.SetString("userid", Cognitive3D_Manager.DeviceId, builder);
                    builder.Append(",");

                    if (!string.IsNullOrEmpty(Cognitive3D_Manager.LobbyId))
                    {
                        JsonUtil.SetString("lobbyId", Cognitive3D_Manager.LobbyId, builder);
                        builder.Append(",");
                    }

                    JsonUtil.SetDouble("timestamp", (int)Cognitive3D_Manager.SessionTimeStamp, builder);
                    builder.Append(",");
                    JsonUtil.SetString("sessionid", Cognitive3D_Manager.SessionID, builder);
                    builder.Append(",");
                    JsonUtil.SetInt("part", jsonPart, builder);
                    builder.Append(",");
                    jsonPart++;
                    JsonUtil.SetString("formatversion", "1.0", builder);

                    //manifest entries
                    if (manifestCount > 0)
                    {
                        builder.Append(",\"manifest\":{");
                        threadDone = false;
                        Queue<DynamicObjectManifestEntry> copyQueue = new Queue<DynamicObjectManifestEntry>(queuedManifest);

                        new Thread(() =>
                        {
                            try
                            {
                                for (int i = 0; i < manifestCount; i++)
                                {
                                    if (i != 0)
                                        builder.Append(',');
                                    //var manifestentry = queuedManifest.Dequeue();
                                    var manifestentry = copyQueue.Dequeue();
                                    SetManifestEntry(manifestentry, builder);
                                    //numberOfEntriesCopied++;
                                }
                            }
                            catch
                            {
                                encounteredError = true;
                            }
                            threadDone = true;
                        }).Start();

                        while (!threadDone && !encounteredError)
                        {
                            yield return null;
                        }

                        //compare 
                        builder.Append("}");
                    }

                    //check if this logic can be skipped because it will be invalidated
                    if (!InterruptThead && !encounteredError)
                    {
                        //snapshots
                        if (count > 0)
                        {
                            builder.Append(",\"data\":[");
                            threadDone = false;

                            Queue<DynamicObjectSnapshot> copyQueue = new Queue<DynamicObjectSnapshot>(queuedSnapshots);
                            new Thread(() =>
                            {
                                try
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        if (i != 0)
                                            builder.Append(',');
                                        var snap = copyQueue.Dequeue();
                                        SetSnapshot(snap, builder);
                                    //snap.ReturnToPool();
                                }
                                }
                                catch
                                {
                                    encounteredError = true;
                                }
                                threadDone = true;
                            }).Start();

                            while (!threadDone && !encounteredError)
                            {
                                yield return null;
                            }
                            builder.Append("]");
                        }
                        builder.Append("}");
                    }

                    if (!InterruptThead && !encounteredError)
                    {
                        //if this coroutine reached here and the thread hasn't been interrupted (from flushdata) and encounter no errors
                        //then remove entries and snapshots from real queues
                        try
                        {
                            for (int i = 0; i < manifestCount; i++)
                            {
                                queuedManifest.Dequeue();
                            }
                            for (int i = 0; i < count; i++)
                            {
                                var snap = queuedSnapshots.Dequeue();
                                snap.ReturnToPool();
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }


                        if (queuedSnapshots.Count == 0 && queuedManifest.Count == 0)
                        {
                            ReadyToWriteJson = false;
                        }

                        string s = builder.ToString();
                        string url = CognitiveStatics.POSTDYNAMICDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);

                        /*if (CopyDataToCache)
                        {
                            if (Core.NetworkManager.runtimeCache != null && Core.NetworkManager.runtimeCache.CanWrite(url, s))
                            {
                                Core.NetworkManager.runtimeCache.WriteContent(url, s);
                            }
                        }*//*

                        Cognitive3D_Manager.NetworkManager.Post(url, s);
                        DynamicManager.DynamicObjectSendEvent();
                    }
                }
                else //wait to write data
                {
                    yield return null;
                }
            }
        }

        //writes a batch of data on main thread
        static void WriteJsonImmediate(bool copyDataToCache)
        {
            int totalDataToWrite = queuedManifest.Count + queuedSnapshots.Count;
            totalDataToWrite = Mathf.Min(totalDataToWrite, Cognitive3D_Preferences.S_DynamicExtremeSnapshotCount);

            var builder = new System.Text.StringBuilder(200 + 128 * totalDataToWrite);
            int manifestCount = Mathf.Min(queuedManifest.Count, totalDataToWrite);
            int count = Mathf.Min(queuedSnapshots.Count, totalDataToWrite - manifestCount);

            builder.Append("{");

            //header
            JsonUtil.SetString("userid", Cognitive3D_Manager.DeviceId, builder);
            builder.Append(",");

            if (!string.IsNullOrEmpty(Cognitive3D_Manager.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Cognitive3D_Manager.LobbyId, builder);
                builder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)Cognitive3D_Manager.SessionTimeStamp, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionid", Cognitive3D_Manager.SessionID, builder);
            builder.Append(",");
            JsonUtil.SetInt("part", jsonPart, builder);
            builder.Append(",");
            jsonPart++;
            JsonUtil.SetString("formatversion", "1.0", builder);

            //manifest entries
            if (manifestCount > 0)
            {
                builder.Append(",\"manifest\":{");
                for (int i = 0; i < manifestCount; i++)
                {
                    if (i != 0)
                        builder.Append(',');
                    var manifestentry = queuedManifest.Dequeue();
                    SetManifestEntry(manifestentry, builder);
                }
                builder.Append("}");
            }

            //snapshots
            if (count > 0)
            {
                builder.Append(",\"data\":[");
                for (int i = 0; i < count; i++)
                {
                    if (i != 0)
                        builder.Append(',');
                    var snap = queuedSnapshots.Dequeue();
                    SetSnapshot(snap, builder);
                    snap.ReturnToPool();
                }
                builder.Append("]");
            }
            builder.Append("}");

            string s = builder.ToString();
            string url = CognitiveStatics.POSTDYNAMICDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);

            if (copyDataToCache)
            {
                if (Cognitive3D_Manager.NetworkManager.runtimeCache != null && Cognitive3D_Manager.NetworkManager.runtimeCache.CanWrite(url, s))
                {
                    Cognitive3D_Manager.NetworkManager.runtimeCache.WriteContent(url, s);
                }
            }

            Cognitive3D_Manager.NetworkManager.Post(url, s);
            DynamicManager.DynamicObjectSendEvent();
        }

        static void SetManifestEntry(DynamicObjectManifestEntry entry, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(entry.Id);
            builder.Append("\":{");


            if (!string.IsNullOrEmpty(entry.Name))
            {
                JsonUtil.SetString("name", entry.Name, builder);
                builder.Append(",");
            }
            JsonUtil.SetString("mesh", entry.MeshName, builder);
            builder.Append(",");
            JsonUtil.SetString("fileType", DynamicObjectManifestEntry.FileType, builder);

            if (entry.isVideo)
            {
                JsonUtil.SetString("externalVideoSource", entry.videoURL, builder);
            }

            if (entry.isController)
            {
                builder.Append(",");
                JsonUtil.SetString("controllerType", entry.controllerType, builder);
            }

            //properties should already be formatted, just need to append them here
            if (!string.IsNullOrEmpty(entry.Properties))
            {
                //properties are an array of a single object? weird
                builder.Append(",\"properties\":[{");
                builder.Append(entry.Properties);
                builder.Append("}]");
            }

            builder.Append("}"); //close manifest entry
        }

        static void SetSnapshot(DynamicObjectSnapshot snap, StringBuilder builder)
        {
            builder.Append('{');

            JsonUtil.SetString("id", snap.Id, builder);
            builder.Append(',');
            JsonUtil.SetDouble("time", snap.Timestamp, builder);
            builder.Append(',');
            JsonUtil.SetVectorRaw("p", snap.posX, snap.posY, snap.posZ, builder);
            builder.Append(',');
            JsonUtil.SetQuatRaw("r", snap.rotX, snap.rotY, snap.rotZ, snap.rotW, builder);
            if (snap.DirtyScale)
            {
                builder.Append(',');
                JsonUtil.SetVectorRaw("s", snap.scaleX, snap.scaleY, snap.scaleZ, builder);
            }

            //properties should already be formatted, just need to append them here
            if (!string.IsNullOrEmpty(snap.Properties))
            {
                //properties are an array of a single object? weird
                builder.Append(",\"properties\":[{");
                builder.Append(snap.Properties);
                builder.Append("}]");
            }
            
            if (!string.IsNullOrEmpty(snap.Buttons))
            {
                builder.Append(",\"buttons\":{");
                builder.Append(snap.Buttons);
                builder.Append("}");
            }

            builder.Append("}"); //close object snapshot
        }
        
    }*/
}