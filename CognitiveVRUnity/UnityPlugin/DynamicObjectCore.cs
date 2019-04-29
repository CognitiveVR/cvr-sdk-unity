using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;
using System.Text;
using CognitiveVR.External;

//static place for receiving data from dynamic objects/engagements/etc and sending it

namespace CognitiveVR
{
    public static class DynamicObjectCore
    {

        private static Queue<DynamicObjectSnapshot> NewSnapshotQueue = new Queue<DynamicObjectSnapshot>();
        private static Queue<DynamicObjectManifestEntry> NewObjectManifestQueue = new Queue<DynamicObjectManifestEntry>();

        public static void AddManifestEntry(DynamicObjectManifestEntry newEntry)
        {
            NewObjectManifestQueue.Enqueue(newEntry);
            if ((NewObjectManifestQueue.Count + NewSnapshotQueue.Count) > CognitiveVR_Preferences.S_DynamicSnapshotCount)
            {
                bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.S_DynamicSnapshotMinTimer > Time.realtimeSinceStartup;
                bool withinExtremeBatchSize = NewObjectManifestQueue.Count + NewSnapshotQueue.Count < CognitiveVR_Preferences.S_DynamicExtremeSnapshotCount;

                //within last send interval and less than extreme count
                if (withinMinTimer && withinExtremeBatchSize)
                {
                    return;
                }
                lastSendTime = Time.realtimeSinceStartup;
                NetworkManager.Sender.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
            }
        }

        public static void AddSnapshot(DynamicObjectSnapshot newSnapshot)
        {
            NewSnapshotQueue.Enqueue(newSnapshot);
            if ((NewObjectManifestQueue.Count + NewSnapshotQueue.Count) > CognitiveVR_Preferences.S_DynamicSnapshotCount)
            {
                bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.S_DynamicSnapshotMinTimer > Time.realtimeSinceStartup;
                bool withinExtremeBatchSize = NewObjectManifestQueue.Count + NewSnapshotQueue.Count < CognitiveVR_Preferences.S_DynamicExtremeSnapshotCount;

                //within last send interval and less than extreme count
                if (withinMinTimer && withinExtremeBatchSize)
                {
                    return;
                }
                lastSendTime = Time.realtimeSinceStartup;
                NetworkManager.Sender.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
            }
        }

        private static int jsonpart = 1;

        static DynamicObjectCore()
        {
            Core.CheckSessionId();
            Core.OnSendData += Core_OnSendData;
            NetworkManager.Sender.StartCoroutine(AutomaticSendTimer());

            for (int i = 0; i < CognitiveVR_Preferences.S_DynamicExtremeSnapshotCount; i++)
            {
                DynamicObjectSnapshot.SnapshotPool.Enqueue(new DynamicObjectSnapshot());
            }
        }


        //puts outstanding snapshots (from last update) into json
        internal static void CognitiveVR_Manager_Update()
        {
            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                if (NewSnapshotQueue.Count + NewObjectManifestQueue.Count > 0)
                {
                    CognitiveVR.Util.logError("Dynamic Object Update - sceneid is empty! do not send Dynamic Objects to sceneexplorer");

                    while (NewSnapshotQueue.Count > 0)
                    {
                        NewSnapshotQueue.Dequeue().ReturnToPool();
                    }
                    NewObjectManifestQueue.Clear();
                }
                return;
            }

            //only need this because dynamic objects don't have a clear 'send' function
            //queue
            if ((NewObjectManifestQueue.Count + NewSnapshotQueue.Count) > CognitiveVR_Preferences.S_DynamicSnapshotCount)
            {

                bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.S_DynamicSnapshotMinTimer > Time.realtimeSinceStartup;
                bool withinExtremeBatchSize = NewObjectManifestQueue.Count + NewSnapshotQueue.Count < CognitiveVR_Preferences.S_DynamicExtremeSnapshotCount;

                //within last send interval and less than extreme count
                if (withinMinTimer && withinExtremeBatchSize)
                {
                    return;
                }
                lastSendTime = Time.realtimeSinceStartup;
                //TODO i don't like using network class for owning coroutines. do something with core
                NetworkManager.Sender.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
            }
        }

        static float nextSendTime = 0;
        internal static IEnumerator AutomaticSendTimer()
        {
            nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.S_DynamicSnapshotMaxTimer;
            while (true)
            {
                while (nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.S_DynamicSnapshotMaxTimer;

                if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                    Util.logDevelopment("check to automatically send dynamics");
                if (NewObjectManifestQueue.Count + NewSnapshotQueue.Count > 0)
                {
                    NetworkManager.Sender.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
                }
            }
        }

        //writes manifest entry and object snapshot to string in threads, then passes value to send saved snapshots
        static IEnumerator Thread_StringThenSend(Queue<DynamicObjectManifestEntry> SendObjectManifest, Queue<DynamicObjectSnapshot> SendObjectSnapshots, CognitiveVR_Preferences.SceneSettings trackingSettings, string uniqueid, double sessiontimestamp, string sessionid)
        {
            //save and clear snapshots and manifest entries
            DynamicObjectManifestEntry[] tempObjectManifest = new DynamicObjectManifestEntry[SendObjectManifest.Count];
            SendObjectManifest.CopyTo(tempObjectManifest, 0);
            SendObjectManifest.Clear();

            //copy snapshots into temporary collection
            DynamicObjectSnapshot[] tempSnapshots = new DynamicObjectSnapshot[SendObjectSnapshots.Count];
            //SendObjectSnapshots.CopyTo(tempSnapshots, 0);
            //SendObjectSnapshots.Clear();

            int index = 0;
            while (SendObjectSnapshots.Count > 0)
            {
                var oldsnapshot = SendObjectSnapshots.Dequeue();
                tempSnapshots[index] = oldsnapshot.Copy();
                index++;
                oldsnapshot.ReturnToPool();
            }

            //write manifest entries to list in thread
            List<string> manifestEntries = new List<string>(tempObjectManifest.Length);
            bool done = true;
            if (tempObjectManifest.Length > 0)
            {
                done = false;
                new System.Threading.Thread(() =>
                {
                    for (int i = 0; i < tempObjectManifest.Length; i++)
                    {
                        manifestEntries.Add(SetManifestEntry(tempObjectManifest[i]));
                    }
                    done = true;
                }).Start();

                while (!done)
                {
                    yield return null;
                }
            }

            //write snapshots to list in thread
            List<string> snapshots = new List<string>(tempSnapshots.Length);
            if (tempSnapshots.Length > 0)
            {
                done = false;
                new System.Threading.Thread(() =>
                {
                    for (int i = 0; i < tempSnapshots.Length; i++)
                    {
                        snapshots.Add(SetSnapshot(tempSnapshots[i]));
                    }
                    done = true;
                }).Start();

                while (!done)
                {
                    yield return null;
                }
            }

            for (int i = 0; i < tempSnapshots.Length; i++)
            {
                tempSnapshots[i].ReturnToPool();
            }

            SendSavedSnapshots(manifestEntries, snapshots, trackingSettings, uniqueid, sessiontimestamp, sessionid);
        }

        //the last realtime dynamic data was successfully sent
        static float lastSendTime = -60;

        static void Core_OnSendData()
        {
            List<string> savedDynamicManifest = new List<string>();
            List<string> savedDynamicSnapshots = new List<string>();

            //write dynamic object snapshots to strings
            DynamicObjectSnapshot snap = null;
            while (NewSnapshotQueue.Count > 0)
            {
                snap = NewSnapshotQueue.Dequeue();
                if (snap == null)
                {
                    Util.logWarning("snapshot immediate is null");
                    continue;
                }
                savedDynamicSnapshots.Add(SetSnapshot(snap));
                snap.ReturnToPool();
                if (savedDynamicSnapshots.Count + savedDynamicManifest.Count >= CognitiveVR_Preferences.S_DynamicSnapshotCount)
                {
                    SendSavedSnapshots(savedDynamicManifest, savedDynamicSnapshots, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID);
                    savedDynamicManifest.Clear();
                    savedDynamicSnapshots.Clear();
                }
            }

            //write dynamic manifest entries to strings
            DynamicObjectManifestEntry entry = null;
            while (NewObjectManifestQueue.Count > 0)
            {
                entry = NewObjectManifestQueue.Dequeue();
                if (entry == null) { continue; }
                savedDynamicManifest.Add(SetManifestEntry(entry));
                if (savedDynamicSnapshots.Count + savedDynamicManifest.Count >= CognitiveVR_Preferences.S_DynamicSnapshotCount)
                {
                    SendSavedSnapshots(savedDynamicManifest, savedDynamicSnapshots, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID);
                    savedDynamicManifest.Clear();
                    savedDynamicSnapshots.Clear();
                }
            }

            //send any outstanding manifest entries or snapshots
            SendSavedSnapshots(savedDynamicManifest, savedDynamicSnapshots, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID);
        }

        //string entries and snapshots are either written in thread or synchronously
        public static void SendSavedSnapshots(List<string> stringEntries, List<string> stringSnapshots, CognitiveVR_Preferences.SceneSettings trackingsettings, string uniqueid, double sessiontimestamp, string sessionid)
        {
            if (stringEntries.Count == 0 && stringSnapshots.Count == 0) { return; }

            //TODO should hold until extreme batch size reached
            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                CognitiveVR.Util.logError("SceneId is empty. Do not send Dynamic Objects to SceneExplorer");

                while (NewSnapshotQueue.Count > 0)
                {
                    NewSnapshotQueue.Dequeue().ReturnToPool();
                }
                NewObjectManifestQueue.Clear();
                return;
            }

            System.Text.StringBuilder sendSnapshotBuilder = new System.Text.StringBuilder(256 * CognitiveVR_Preferences.S_DynamicExtremeSnapshotCount + 8000);

            sendSnapshotBuilder.Append("{");

            //header
            JsonUtil.SetString("userid", uniqueid, sendSnapshotBuilder);
            sendSnapshotBuilder.Append(",");

            if (!string.IsNullOrEmpty(Core.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Core.LobbyId, sendSnapshotBuilder);
                sendSnapshotBuilder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)sessiontimestamp, sendSnapshotBuilder);
            sendSnapshotBuilder.Append(",");
            JsonUtil.SetString("sessionid", sessionid, sendSnapshotBuilder);
            sendSnapshotBuilder.Append(",");
            JsonUtil.SetInt("part", jsonpart, sendSnapshotBuilder);
            sendSnapshotBuilder.Append(",");
            jsonpart++;
            JsonUtil.SetString("formatversion", "1.0", sendSnapshotBuilder);
            sendSnapshotBuilder.Append(",");

            //format all the savedmanifest entries

            if (stringEntries.Count > 0)
            {
                //manifest
                sendSnapshotBuilder.Append("\"manifest\":{");
                for (int i = 0; i < stringEntries.Count; i++)
                {
                    sendSnapshotBuilder.Append(stringEntries[i]);
                    sendSnapshotBuilder.Append(",");
                }
                sendSnapshotBuilder.Remove(sendSnapshotBuilder.Length - 1, 1);
                sendSnapshotBuilder.Append("},");
            }

            if (stringSnapshots.Count > 0)
            {
                //snapshots
                sendSnapshotBuilder.Append("\"data\":[");
                for (int i = 0; i < stringSnapshots.Count; i++)
                {
                    sendSnapshotBuilder.Append(stringSnapshots[i]);
                    sendSnapshotBuilder.Append(",");
                }
                sendSnapshotBuilder.Remove(sendSnapshotBuilder.Length - 1, 1);
                sendSnapshotBuilder.Append("]");
            }
            else
            {
                sendSnapshotBuilder.Remove(sendSnapshotBuilder.Length - 1, 1); //remove last comma from manifest array
            }

            sendSnapshotBuilder.Append("}");

            string url = CognitiveStatics.POSTDYNAMICDATA(trackingsettings.SceneId, trackingsettings.VersionNumber);

            string content = sendSnapshotBuilder.ToString();

            CognitiveVR.NetworkManager.Post(url, content);
        }

        static string SetManifestEntry(DynamicObjectManifestEntry entry)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);

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

            if (!string.IsNullOrEmpty(entry.videoURL))
            {
                builder.Append(",");
                JsonUtil.SetString("externalVideoSource", entry.videoURL, builder);
                builder.Append(",");
                JsonUtil.SetObject("flipVideo", entry.videoFlipped, builder);
            }

            if (entry.isController)
            {
                builder.Append(",");
                JsonUtil.SetString("controllerType", entry.controllerType, builder);
            }

            if (entry.Properties != null && entry.Properties.Keys.Count > 0)
            {
                builder.Append(",");
                builder.Append("\"properties\":[");
                foreach (var v in entry.Properties)
                {
                    builder.Append("{");
                    if (v.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(v.Key, (string)v.Value, builder);
                    }
                    else
                    {
                        JsonUtil.SetObject(v.Key, v.Value, builder);
                    }
                    builder.Append("},");
                }
                builder.Remove(builder.Length - 1, 1); //remove last comma
                builder.Append("]"); //close properties object
            }

            builder.Append("}"); //close manifest entry

            return builder.ToString();
        }

        static string SetSnapshot(DynamicObjectSnapshot snap)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
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


            if (snap.Properties != null && snap.Properties.Keys.Count > 0)
            {
                builder.Append(",");
                builder.Append("\"properties\":[");
                builder.Append("{");
                foreach (var v in snap.Properties)
                {
                    if (v.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(v.Key, (string)v.Value, builder);
                    }
                    else
                    {
                        JsonUtil.SetObject(v.Key, v.Value, builder);
                    }
                    builder.Append(",");
                }
                builder.Remove(builder.Length - 1, 1); //remove last comma
                builder.Append("}");
                builder.Append("]"); //close properties object
            }

            if (snap.Buttons != null)
            {
                if (snap.Buttons.Count > 0)
                {
                    builder.Append(",");
                    builder.Append("\"buttons\":{");
                    foreach (var button in snap.Buttons)
                    {
                        builder.Append("\"");
                        builder.Append(button.Key);
                        builder.Append("\":{");
                        builder.Append("\"buttonPercent\":");
                        builder.Append(button.Value.ButtonPercent);
                        if (button.Value.IncludeXY)
                        {
                            builder.Append(",\"x\":");
                            builder.Append(button.Value.X.ToString("0.000"));
                            builder.Append(",\"y\":");
                            builder.Append(button.Value.Y.ToString("0.000"));
                        }
                        builder.Append("},");
                    }
                    builder.Remove(builder.Length - 1, 1); //remove last comma
                    builder.Append("}");
                }
            }

            builder.Append("}"); //close object snapshot

            return builder.ToString();
        }
    }

    public class ButtonState
    {
        public int ButtonPercent = 0;
        public float X = 0;
        public float Y = 0;
        public bool IncludeXY = false;

        public ButtonState(int buttonPercent, float x = 0, float y = 0, bool includexy = false)
        {
            ButtonPercent = buttonPercent;
            X = x;
            Y = y;
            IncludeXY = includexy;
        }

        public ButtonState(ButtonState source)
        {
            ButtonPercent = source.ButtonPercent;
            IncludeXY = source.IncludeXY;
            X = source.X;
            Y = source.Y;
        }

        //compare as if simply a container for data
        public override bool Equals(object obj)
        {
            var s = (ButtonState)obj;

            if (!IncludeXY)
            {
                return s.ButtonPercent == ButtonPercent;
            }
            else
            {
                return s.ButtonPercent == ButtonPercent && Mathf.Approximately(s.X, X) && Mathf.Approximately(s.Y, Y);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void Copy(ButtonState source)
        {
            ButtonPercent = source.ButtonPercent;
            IncludeXY = source.IncludeXY;
            X = source.X;
            Y = source.Y;
        }
    }

    public class DynamicObjectSnapshot
    {
        public static Queue<DynamicObjectSnapshot> SnapshotPool = new Queue<DynamicObjectSnapshot>();

        public DynamicObjectSnapshot Copy()
        {
            var dyn = GetSnapshot(Id);
            dyn.Timestamp = Timestamp;
            dyn.Id = Id;
            dyn.posX = posX;
            dyn.posY = posY;
            dyn.posZ = posZ;
            dyn.rotX = rotX;
            dyn.rotY = rotY;
            dyn.rotZ = rotZ;
            dyn.rotW = rotW;
            dyn.DirtyScale = DirtyScale;
            dyn.scaleX = scaleX;
            dyn.scaleY = scaleY;
            dyn.scaleZ = scaleZ;

            if (Buttons != null)
            {
                dyn.Buttons = new Dictionary<string, ButtonState>(Buttons.Count);
                foreach (var v in Buttons)
                {
                    dyn.Buttons.Add(v.Key, new ButtonState(v.Value));
                }
            }
            if (Properties != null)
            {
                dyn.Properties = new Dictionary<string, object>(Properties.Count);
                foreach (var v in Properties)
                {
                    dyn.Properties.Add(v.Key, v.Value); //as long as the property value is a value type, everything should be fine
                }
            }
            return dyn;
        }

        public void ReturnToPool()
        {
            Properties = null;
            Buttons = null;
            posX = 0;
            posY = 0;
            posZ = 0;
            rotX = 0;
            rotY = 0;
            rotZ = 0;
            rotW = 1;
            scaleX = 1;
            scaleY = 1;
            scaleZ = 1;
            DirtyScale = false;
            SnapshotPool.Enqueue(this);
        }

        public static DynamicObjectSnapshot GetSnapshot(string id)
        {
            if (SnapshotPool.Count > 0)
            {
                DynamicObjectSnapshot dos = SnapshotPool.Dequeue();
                if (dos == null)
                {
                    dos = new DynamicObjectSnapshot();
                }
                dos.Id = id;
                dos.Timestamp = Util.Timestamp(Time.frameCount);
                return dos;
            }
            else
            {
                var dos = new DynamicObjectSnapshot();
                dos.Id = id;
                dos.Timestamp = Util.Timestamp(Time.frameCount);
                return dos;
            }
        }

        public string Id;
        public Dictionary<string, object> Properties;
        public Dictionary<string, CognitiveVR.ButtonState> Buttons;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public bool DirtyScale = false;
        public float scaleX, scaleY, scaleZ;
        public double Timestamp;

        public DynamicObjectSnapshot(string dynamicObjectId)
        {
            Id = dynamicObjectId;
            Timestamp = Util.Timestamp(Time.frameCount);
        }

        public DynamicObjectSnapshot()
        {
            //empty. only used to fill the pool
        }

        private DynamicObjectSnapshot(string dynamicObjectId, Vector3 pos, Quaternion rot, Dictionary<string, object> props = null)
        {
            Id = dynamicObjectId;
            Properties = props;

            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;

            rotX = rot.x;
            rotY = rot.y;
            rotZ = rot.z;
            rotW = rot.w;

            Timestamp = Util.Timestamp(Time.frameCount);
        }

        private DynamicObjectSnapshot(string dynamicObjectId, float[] pos, float[] rot, Dictionary<string, object> props = null)
        {
            Id = dynamicObjectId;
            Properties = props;
            posX = pos[0];
            posY = pos[1];
            posZ = pos[2];

            rotX = rot[0];
            rotY = rot[1];
            rotZ = rot[2];
            rotW = rot[3];
            Timestamp = Util.Timestamp(Time.frameCount);
        }

        /// <summary>
        /// Add the position and rotation to the snapshot, even if the dynamic object hasn't moved beyond its threshold
        /// </summary>
        public DynamicObjectSnapshot UpdateTransform(Vector3 pos, Quaternion rot)
        {
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;

            rotX = rot.x;
            rotY = rot.y;
            rotZ = rot.z;
            rotW = rot.w;

            return this;
        }

        /// <summary>
        /// Set various properties on the snapshot. Currently unused
        /// </summary>
        /// <param name="dict"></param>
        public DynamicObjectSnapshot SetProperties(Dictionary<string, object> dict)
        {
            Properties = dict;
            return this;
        }

        public DynamicObjectSnapshot SetProperty(string key, object value)
        {
            if (Properties == null)
            {
                Properties = new Dictionary<string, object>();
            }

            if (Properties.ContainsKey(key))
            {
                Properties[key] = value;
            }
            else
            {
                Properties.Add(key, value);
            }
            return this;
        }

        /// <summary>
        /// Append various properties on the snapshot without overwriting previous properties. Currently unused
        /// </summary>
        /// <param name="dict"></param>
        public DynamicObjectSnapshot AppendProperties(Dictionary<string, object> dict)
        {
            if (Properties == null)
            {
                Properties = new Dictionary<string, object>();
            }
            foreach (var v in dict)
            {
                Properties[v.Key] = v.Value;
            }
            return this;
        }

        /// <summary>
        /// Hide or show the dynamic object on SceneExplorer. This is happens automatically when you create, disable or destroy a gameobject
        /// </summary>
        /// <param name="enable"></param>
        public DynamicObjectSnapshot SetEnabled(bool enable)
        {
            if (Properties == null)
            {
                Properties = new Dictionary<string, object>();
            }
            Properties["enabled"] = enable;
            return this;
        }
    }

    public class DynamicObjectManifestEntry
    {
        public static string FileType = "gltf";

        public string Id;
        public string Name;
        public string MeshName;
        public Dictionary<string, object> Properties;
        public string videoURL;
        public bool videoFlipped;
        public bool isController;
        public string controllerType;

        public DynamicObjectManifestEntry(string id, string name, string meshName)
        {
            this.Id = id;
            this.Name = name;
            this.MeshName = meshName;
        }

        public DynamicObjectManifestEntry(string id, string name, string meshName, Dictionary<string, object> props)
        {
            this.Id = id;
            this.Name = name;
            this.MeshName = meshName;
            this.Properties = props;
        }
    }
}