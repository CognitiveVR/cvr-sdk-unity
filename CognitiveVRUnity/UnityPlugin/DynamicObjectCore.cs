using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using CognitiveVR.External;
using System.Threading;

//deals with formatting dynamic object snapshots and manifest entries to json
//deals with sending through network

namespace CognitiveVR
{
    public static class DynamicObjectCore
    {
        public static int FrameCount;
        static bool ReadyToWriteJson = false;

        private static int jsonPart = 1;

        static Queue<DynamicObjectSnapshot> queuedSnapshots = new Queue<DynamicObjectSnapshot>();
        static Queue<DynamicObjectManifestEntry> queuedManifest = new Queue<DynamicObjectManifestEntry>();

        private static int tempsnapshots = 0;

        static DynamicObjectCore()
        {
            CognitiveVR.Core.CheckSessionId();
        }

        public static void Initialize()
        {
            NetworkManager.Sender.StartCoroutine(WriteJson());
            //TODO use dynamic extreme snapshot count
            for (int i = 0; i < 100; i++)
            {
                DynamicObjectSnapshot.SnapshotPool.Enqueue(new DynamicObjectSnapshot());
            }
        }

        public static void RegisterController(DynamicData data, string controllerName)
        {
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            dome.HasProperties = true;
            dome.Properties = "\"controller\":\"" + controllerName + "\"";

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > 128)
            {
                tempsnapshots = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        public static void RegisterMedia(DynamicData data, string videourl)
        {
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);
            dome.videoURL = videourl;

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > 128)
            {
                tempsnapshots = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        public static void RegisterDynamic(DynamicData data, string formattedProperties)
        {
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            dome.HasProperties = true;
            dome.Properties = formattedProperties;

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > 128)
            {
                tempsnapshots = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        // put into dynamic manifest. reuses or creates new objectid. returns objectid. prefer using custom id when possible. also adds a snapshot with the property 'enabled'
        public static void RegisterObject(DynamicData data)
        {
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            queuedManifest.Enqueue(dome);
            tempsnapshots++;
            if (tempsnapshots > 128)
            {
                tempsnapshots = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        public static void RecordDynamic(DynamicData data, string props, bool writeScale)
        {
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
                s.scaleX = data.LastScale.x;
                s.scaleY = data.LastScale.y;
                s.scaleZ = data.LastScale.z;
            }
            s.Properties = props;
            s.Timestamp = Util.Timestamp(FrameCount);

            queuedSnapshots.Enqueue(s);
            tempsnapshots++;
            if (tempsnapshots > 128)
            {
                tempsnapshots = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        //button properties are formated as   ,"buttons":{"input":value,"input":value}
        public static void RecordDynamicController(DynamicData data, string props, bool writeScale, string jbuttonstates)
        {
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
                s.scaleX = data.LastScale.x;
                s.scaleY = data.LastScale.y;
                s.scaleZ = data.LastScale.z;
            }
            s.Properties = props;
            s.Buttons = jbuttonstates;

            s.Timestamp = Util.Timestamp(FrameCount);

            queuedSnapshots.Enqueue(s);
            tempsnapshots++;
            if (tempsnapshots > 128)
            {
                tempsnapshots = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        //if WriteImmediate don't start threads 
        static bool WriteImmediate = false;
        internal static void FlushData()
        {
            ReadyToWriteJson = true;
            WriteImmediate = true;
            WriteJson().MoveNext();
            WriteImmediate = false;
        }

        static IEnumerator WriteJson()
        {
            while (true)
            {
                if (!ReadyToWriteJson) { yield return null; }
                else
                {
                    var builder = new System.Text.StringBuilder(10000); //TODO 128 per snapshot * number of snapshots + 200 for header stuff

                    int manifestCount = Mathf.Min(queuedManifest.Count, 128);
                    int count = Mathf.Min(queuedSnapshots.Count,128 - manifestCount);
                    //Debug.Log("write " + manifestCount + " manifest entires and " + count + " snapshots");

                    if (queuedSnapshots.Count - count == 0 && queuedManifest.Count - manifestCount == 0)
                    {
                        ReadyToWriteJson = false;
                    }

                    bool threadDone = true;

                    builder.Append("{");

                    //header
                    JsonUtil.SetString("userid", Core.UserId, builder);
                    builder.Append(",");

                    if (!string.IsNullOrEmpty(Core.LobbyId))
                    {
                        JsonUtil.SetString("lobbyId", Core.LobbyId, builder);
                        builder.Append(",");
                    }

                    JsonUtil.SetDouble("timestamp", (int)Core.SessionTimeStamp, builder);
                    builder.Append(",");
                    JsonUtil.SetString("sessionid", Core.SessionID, builder);
                    builder.Append(",");
                    JsonUtil.SetInt("part", jsonPart, builder);
                    builder.Append(",");
                    jsonPart++;
                    JsonUtil.SetString("formatversion", "1.0", builder);
                    builder.Append(",");

                    //manifest entries
                    if (manifestCount > 0)
                    {
                        builder.Append("\"manifest\":[");
                        threadDone = false;

                        if (WriteImmediate)
                        {
                            for (int i = 0; i < manifestCount; i++)
                            {
                                if (i != 0)
                                    builder.Append(',');
                                var manifestentry = queuedManifest.Dequeue();
                                SetManifestEntry(manifestentry, builder);
                                //manifestentry.ReturnToPool();
                            }
                            threadDone = true;
                        }
                        else
                        {
                            new System.Threading.Thread(() =>
                            {
                                for (int i = 0; i < manifestCount; i++)
                                {
                                    if (i != 0)
                                        builder.Append(',');
                                    var manifestentry = queuedManifest.Dequeue();
                                    SetManifestEntry(manifestentry, builder);
                                    //manifestentry.ReturnToPool();
                                }
                                threadDone = true;
                            }).Start();

                            while (!threadDone)
                            {
                                yield return null;
                            }
                        }

                        if (count>0)
                        {
                            builder.Append("],");
                        }
                        else
                        {
                            builder.Append("]");
                        }
                    }

                    //snapshots
                    if (count > 0)
                    {
                        builder.Append("\"data\":[");
                        threadDone = false;
                        if (WriteImmediate)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                if (i != 0)
                                    builder.Append(',');
                                var snap = queuedSnapshots.Dequeue();
                                SetSnapshot(snap, builder);
                                snap.ReturnToPool();
                            }
                            threadDone = true;
                        }
                        else
                        {
                            new System.Threading.Thread(() =>
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    if (i != 0)
                                        builder.Append(',');
                                    var snap = queuedSnapshots.Dequeue();
                                    SetSnapshot(snap, builder);
                                    snap.ReturnToPool();
                                }
                                threadDone = true;
                            }).Start();

                            while (!threadDone)
                            {
                                yield return null;
                            }
                        }
                        builder.Append("]");
                    }
                    builder.Append("}");

                    string s = builder.ToString();
                    string url = CognitiveStatics.POSTDYNAMICDATA(Core.TrackingSceneId, Core.TrackingSceneVersionNumber);
                    NetworkManager.Post(url, s);
                    //var wr = UnityEngine.Networking.UnityWebRequest.Put(url, s);
                    //wr.method = "POST";
                    //wr.Send();
                    //Debug.Log(s);
                }
            }
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
                builder.Append(",\"buttons\"[{");
                builder.Append(snap.Buttons);
                builder.Append("}]");
            }

            builder.Append("}"); //close object snapshot
        }
    }
}