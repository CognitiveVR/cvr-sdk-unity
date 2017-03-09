using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//manually send snapshots with builder pattern
//presets for controllers/level geometry/pooled enemies/grabable item
//example scripts for snow fortress blocks

/*level geo. no ticks, update transform on start*/
/*controllers. tick. update on start. never disabled. custom id*/
/*enemies. tick. update on start non-custom id. reused on enable*/
/*grabable item. custom id. update ticks. never disabled*/


namespace CognitiveVR
{
    public class DynamicObject : MonoBehaviour
    {
        public enum CommonDynamicMesh
        {
            ViveController,
            OculusTouchLeft,
            OculusTouchRight
        }

        //instance variables    
        private Transform _t;
        public Transform _transform
        {
            get
            {
                if (_t == null)
                    _t = transform;
                return _t;
            }
        }

        private Collider _c;
        public Collider _collider
        {
            get
            {
                if (_c == null)
                {
                    _c = GetComponent<Collider>();
                }
                return _c;
            }
        }

        public bool SnapshotOnEnable = true;
        public bool UpdateTicksOnEnable = true;

        //[Header("Thresholds")]
        public float PositionThreshold = 0.25f;
        private Vector3 lastPosition;
        public float RotationThreshold = 45f;
        private Quaternion lastRotation;

        //[Header("IDs")]
        public bool UseCustomId = false;
        public int CustomId;
        public bool ReleaseIdOnDestroy = true;
        public bool ReleaseIdOnDisable = true;

        public string GroupName;

        public DynamicObjectId ObjectId;

        public bool UseCustomMesh;
        public CommonDynamicMesh CommonMesh;
        public string MeshName;

        //[Header("Updates")]
        public bool SyncWithPlayerUpdate = false;
        public float UpdateRate = 0.5f;
        private YieldInstruction updateTick;

        public bool TrackGaze = false;



        //static variables
        private static int uniqueIdOffset = 1000;
        private static int currentUniqueId;
        public static List<DynamicObjectId> ObjectIds = new List<DynamicObjectId>();

        //cumulative. all objects
        public static List<DynamicObjectManifestEntry> ObjectManifest = new List<DynamicObjectManifestEntry>();

        //new until they are packaged into json
        public static List<DynamicObjectSnapshot> NewSnapshots = new List<DynamicObjectSnapshot>();
        private static List<DynamicObjectManifestEntry> NewObjectManifest = new List<DynamicObjectManifestEntry>();

        private static List<string> savedDynamicManifest = new List<string>();
        private static List<string> savedDynamicSnapshots = new List<string>();
        //private static int maxSnapshotBatchCount = 64;
        private static int jsonpart = 1;

        void OnEnable()
        {
            //set the 'custom mesh name' to be the lowercase of the common name
            if (!UseCustomMesh)
            {
                UseCustomMesh = true;
                MeshName = CommonMesh.ToString().ToLower();
            }

            if (SnapshotOnEnable)
            {
                var v = NewSnapshot().UpdateTransform().SetEnabled(true);
                if (UpdateTicksOnEnable)
                {
                    v.SetTick(true);
                }
            }
        }

        //public so snapshot can begin this
        public IEnumerator UpdateTick()
        {
            updateTick = new WaitForSeconds(UpdateRate);

            while (true)
            {
                yield return updateTick;
                CheckUpdate();
            }
        }

        //public so snapshot can tie cognitivevr_manager tick event to this. this is for syncing player tick and this tick
        public void CognitiveVR_Manager_TickEvent()
        {
            CheckUpdate();
        }

        //puts outstanding snapshots (from last update) into json. this can't happen
        private static void CognitiveVR_Manager_Update()
        {
            WriteSnapshotsToString();
        }

        //write up to 4 dynamic object snapshots each frame
        private static void WriteSnapshotsToString()
        {
            for (int i = 0; i < Mathf.Max((NewObjectManifest.Count+NewSnapshots.Count)/10,4); i++)
            {
                if (NewObjectManifest.Count > 0)
                {
                    savedDynamicManifest.Add(SetManifestEntry(NewObjectManifest[0]));
                    NewObjectManifest.RemoveAt(0);
                    continue;
                }
                if (NewSnapshots.Count > 0)
                {
                    savedDynamicSnapshots.Add(SetSnapshot(NewSnapshots[0]));
                    NewSnapshots.RemoveAt(0);
                    continue;
                }
            }

            if (savedDynamicSnapshots.Count + savedDynamicManifest.Count >= CognitiveVR_Preferences.Instance.DynamicSnapshotCount)
            {
                SendSavedSnapshots();
            }
        }

        private static void WriteAllSnapshots()
        {
            //write new dynamic object snapshots to strings
            for (int i = 0; i < NewSnapshots.Count; i++)
            {
                if (NewSnapshots[i] == null) { continue; }
                savedDynamicSnapshots.Add(SetSnapshot(NewSnapshots[i]));
                NewSnapshots[i] = null;
                if (savedDynamicSnapshots.Count + savedDynamicManifest.Count >= CognitiveVR_Preferences.Instance.DynamicSnapshotCount)
                {
                    SendSavedSnapshots();
                }
            }
            if (NewSnapshots.Count > 0)
                NewSnapshots.Clear();

            for (int i = 0; i < NewObjectManifest.Count; i++)
            {
                if (NewObjectManifest[i] == null) { continue; }
                savedDynamicManifest.Add(SetManifestEntry(NewObjectManifest[i]));
                NewObjectManifest[i] = null;
                if (savedDynamicSnapshots.Count + savedDynamicManifest.Count >= CognitiveVR_Preferences.Instance.DynamicSnapshotCount)
                {
                    SendSavedSnapshots();
                }
            }
            if (NewObjectManifest.Count > 0)
                NewObjectManifest.Clear();
        }

        /// <summary>
        /// send a snapshot of the position and rotation if the object has moved beyond its threshold
        /// </summary>
        public void CheckUpdate()
        {
            bool doWrite = false;
            if (Vector3.SqrMagnitude(_transform.position - lastPosition) > Mathf.Pow(PositionThreshold, 2))
            {
                doWrite = true;
            }
            else if (Quaternion.Angle(lastRotation, _transform.rotation) > RotationThreshold)
            {
                doWrite = true;
            }
            if (doWrite)
            {
                NewSnapshot().UpdateTransform();
                UpdateLastPositions();
            }

            if (TrackGaze)
            {
                if (CognitiveVR_Manager.HasRequestedDynamicGazeRaycast) { return; }

                CognitiveVR_Manager.RequestDynamicObjectGaze();
            }
        }

        public DynamicObjectSnapshot NewSnapshot()
        {
            return NewSnapshot(MeshName);
        }

        private DynamicObjectSnapshot NewSnapshot(string mesh)
        {
            bool needObjectId = false;
            //add object to manifest and set ObjectId
            if (ObjectId == null)
            {
                needObjectId = true;
            }
            else
            {
                var manifestEntry = ObjectManifest.Find(x => x.Id == ObjectId.Id);
                if (manifestEntry == null)
                {
                    needObjectId = true;
                }
            }

            //new objectId and manifest entry (if required)
            if (needObjectId)
            {
                if (!UseCustomId)
                {
                    var recycledId = ObjectIds.Find(x => !x.Used && x.MeshName == mesh);
                    if (recycledId != null)
                    {
                        ObjectId = recycledId;
                        ObjectId.Used = true;
                        //id is already on manifest
                    }
                    else
                    {
                        ObjectId = GetUniqueID(MeshName);
                        //ObjectId = new DynamicObjectId(newId, MeshName);
                        var manifestEntry = new DynamicObjectManifestEntry(ObjectId.Id, gameObject.name, MeshName);
                        if (!string.IsNullOrEmpty(GroupName))
                        {
                            manifestEntry.Properties = new Dictionary<string, object>() { { "groupname", GroupName } };
                        }
                        ObjectManifest.Add(manifestEntry);
                        NewObjectManifest.Add(manifestEntry);
                    }
                }
                else
                {
                    ObjectId = new DynamicObjectId(CustomId, MeshName);
                    var manifestEntry = new DynamicObjectManifestEntry(ObjectId.Id, gameObject.name, MeshName);
                    if (!string.IsNullOrEmpty(GroupName))
                    {
                        manifestEntry.Properties = new Dictionary<string, object>() { { "groupname", GroupName } };
                    }
                    ObjectManifest.Add(manifestEntry);
                    NewObjectManifest.Add(manifestEntry);
                }

                if (ObjectManifest.Count == 1)
                {
                    CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_Update;
                    CognitiveVR_Manager.SendDataEvent += SendAllSnapshots;
                }
            }
            
            //create snapshot for this object
            var snapshot = new DynamicObjectSnapshot(this);
            NewSnapshots.Add(snapshot);
            return snapshot;
        }

        public void UpdateLastPositions()
        {
            lastPosition = _transform.position;
            lastRotation = _transform.rotation;
        }

        private static DynamicObjectId GetUniqueID(string MeshName)
        {
            currentUniqueId++;
            return new DynamicObjectId(currentUniqueId + uniqueIdOffset, MeshName);
        }

        public static void SendAllSnapshots()
        {
            WriteAllSnapshots();
            SendSavedSnapshots();
        }

        /// <summary>
        /// it is recommended that you use PlayerRecorder.SendData instead. that will send all outstanding data
        /// </summary>
        public static void SendSavedSnapshots()
        {
            if (savedDynamicManifest.Count == 0 && savedDynamicSnapshots.Count == 0) { return; }
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR.CognitiveVR_Preferences.Instance.FindScene(sceneName);
            if (sceneSettings == null)
            {
                CognitiveVR.Util.logDebug("scene settings are null " + sceneName);
                savedDynamicManifest.Clear();
                savedDynamicSnapshots.Clear();
                return;
            }

            if (string.IsNullOrEmpty(sceneSettings.SceneId))
            {
                CognitiveVR.Util.logDebug("sceneid is empty. do not send dynamic objects to sceneexplorer");
                savedDynamicManifest.Clear();
                savedDynamicSnapshots.Clear();
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append("{");

            //header
            builder.Append(JsonUtil.SetString("userid", Core.userId));
            builder.Append(",");
            builder.Append(JsonUtil.SetObject("timestamp", CognitiveVR_Preferences.TimeStamp));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("sessionid", CognitiveVR_Preferences.SessionID));
            builder.Append(",");
            builder.Append(JsonUtil.SetObject("part", jsonpart));
            builder.Append(",");
            jsonpart++;

            //format all the savedmanifest entries

            if (savedDynamicManifest.Count > 0)
            {
                //manifest
                builder.Append("\"manifest\":{");
                for (int i = 0; i < savedDynamicManifest.Count; i++)
                {
                    builder.Append(savedDynamicManifest[i]);
                    builder.Append(",");
                }
                if (savedDynamicManifest.Count > 0)
                {
                    builder.Remove(builder.Length - 1, 1);
                }
                builder.Append("},");
            }

            if (savedDynamicSnapshots.Count > 0)
            {
                //snapshots
                builder.Append("\"data\":[");
                for (int i = 0; i < savedDynamicSnapshots.Count; i++)
                {
                    builder.Append(savedDynamicSnapshots[i]);
                    builder.Append(",");
                }
                if (savedDynamicSnapshots.Count > 0)
                {
                    builder.Remove(builder.Length - 1, 1);
                }
                builder.Append("]");
            }
            else
            {
                builder.Remove(builder.Length - 1, 1); //remove last comma from manifest array
            }

            builder.Append("}");

            savedDynamicManifest.Clear();
            savedDynamicSnapshots.Clear();

            string url = "https://sceneexplorer.com/api/dynamics/" + sceneSettings.SceneId;

            CognitiveVR.Util.logDebug("send dynamic data to " + url);

            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
            CognitiveVR_Manager.Instance.StartCoroutine(CognitiveVR_Manager.Instance.PostJsonRequest(outBytes, url));
        }

        private static string SetManifestEntry(DynamicObjectManifestEntry entry)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append("\"");
            builder.Append(entry.Id);
            builder.Append("\":{");


            if (!string.IsNullOrEmpty(entry.Name))
            {
                builder.Append(JsonUtil.SetString("name", entry.Name));
                builder.Append(",");
            }
            builder.Append(JsonUtil.SetString("mesh", entry.MeshName));


            if (entry.Properties != null && entry.Properties.Keys.Count > 0)
            {
                builder.Append(",");
                builder.Append("\"properties\":[");
                foreach (var v in entry.Properties)
                {
                    builder.Append("{");
                    if (v.Value.GetType() == typeof(string))
                    {
                        builder.Append(JsonUtil.SetString(v.Key, (string)v.Value));
                    }
                    else
                    {
                        builder.Append(JsonUtil.SetObject(v.Key, v.Value));
                    }
                    builder.Append("},");
                }
                builder.Remove(builder.Length - 1, 1); //remove last comma
                builder.Append("]"); //close properties object
            }

            builder.Append("}"); //close manifest entry

            return builder.ToString();
        }

        private static string SetSnapshot(DynamicObjectSnapshot snap)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");

            builder.Append(JsonUtil.SetObject("id", snap.Id));
            builder.Append(",");
            builder.Append(JsonUtil.SetObject("time", snap.Timestamp));
            builder.Append(",");
            builder.Append(JsonUtil.SetVector("p", snap.Position));
            builder.Append(",");
            builder.Append(JsonUtil.SetQuat("r", snap.Rotation));


            if (snap.Properties != null && snap.Properties.Keys.Count > 0)
            {
                builder.Append(",");
                builder.Append("\"properties\":[");
                foreach (var v in snap.Properties)
                {
                    builder.Append("{");
                    if (v.Value.GetType() == typeof(string))
                    {
                        builder.Append(JsonUtil.SetString(v.Key, (string)v.Value));
                    }
                    else
                    {
                        builder.Append(JsonUtil.SetObject(v.Key, v.Value));
                    }
                    builder.Append("},");
                }
                builder.Remove(builder.Length - 1, 1); //remove last comma
                builder.Append("]"); //close properties object
            }

            builder.Append("}"); //close object snapshot

            return builder.ToString();
        }

        void OnDisable()
        {
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            if (!ReleaseIdOnDisable) { return; }
            if (TrackGaze) { return; }
            NewSnapshot().ReleaseUniqueId();
        }

        void OnDestroy()
        {
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            if (!ReleaseIdOnDestroy) { return; }
            if (TrackGaze) { return; }
            NewSnapshot().ReleaseUniqueId();
        }
    }

    public class DynamicObjectSnapshot
    {
        public DynamicObject Dynamic;
        public int Id;
        public Dictionary<string, object> Properties;
        public float[] Position = new float[3] { 0, 0, 0 };
        public float[] Rotation = new float[4] { 0, 0, 0, 1 };
        public double Timestamp;

        public DynamicObjectSnapshot(DynamicObject dynamic)
        {
            this.Dynamic = dynamic;
            Id = dynamic.ObjectId.Id;
            Timestamp = Util.Timestamp();
        }

        public DynamicObjectSnapshot(DynamicObject dynamic, Vector3 pos, Quaternion rot, Dictionary<string, object> props = null)
        {
            this.Dynamic = dynamic;
            Id = dynamic.ObjectId.Id;
            Properties = props;
            Position = new float[3] { 0, 0, 0 };
            Rotation = new float[4] { rot.x, rot.y, rot.z, rot.w };
            Timestamp = Util.Timestamp();
        }

        public DynamicObjectSnapshot(DynamicObject dynamic, float[] pos, float[] rot, Dictionary<string, object> props = null)
        {
            this.Dynamic = dynamic;
            Id = dynamic.ObjectId.Id;
            Properties = props;
            Position = pos;
            
            Rotation = rot;
            Timestamp = Util.Timestamp();
        }

        /// <summary>
        /// Add the position and rotation to the snapshot, even if the dynamic object hasn't moved beyond it's threshold
        /// </summary>
        public DynamicObjectSnapshot UpdateTransform()
        {
            Position = new float[3] { Dynamic._transform.position.x, Dynamic._transform.position.y, Dynamic._transform.position.z };
            Rotation = new float[4] { Dynamic._transform.rotation.x, Dynamic._transform.rotation.y, Dynamic._transform.rotation.z, Dynamic._transform.rotation.w };

            Dynamic.UpdateLastPositions();

            return this;
        }

        /// <summary>
        /// Enable or Disable the Tick coroutine to automatically update the dynamic object's position and rotation
        /// </summary>
        /// <param name="enable"></param>
        public DynamicObjectSnapshot SetTick(bool enable)
        {
            CognitiveVR_Manager.TickEvent -= Dynamic.CognitiveVR_Manager_TickEvent;
            Dynamic.StopAllCoroutines();
            if (enable)
            {
                if (Dynamic.SyncWithPlayerUpdate)
                {
                    CognitiveVR_Manager.TickEvent += Dynamic.CognitiveVR_Manager_TickEvent;
                }
                else
                {
                    Dynamic.StartCoroutine(Dynamic.UpdateTick());
                }
            }
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

        //releasing an id allows a new object with the same mesh to be used instead of bloating the object manifest
        public DynamicObjectSnapshot ReleaseUniqueId()
        {
            var foundId = DynamicObject.ObjectIds.Find(x => x.Id == this.Id);
            if (foundId != null)
            {
                foundId.Used = false;
                this.Dynamic.ObjectId = null;
                this.SetEnabled(false);
            }
            return this;
        }
    }

    //holds info about which ids are used and what meshes they are held by
    //used to 'release' unique ids so meshes can be pooled in scene explorer
    public class DynamicObjectId
    {
        public int Id;
        public bool Used = true;
        public string MeshName;

        public DynamicObjectId(int id, string meshName)
        {
            this.Id = id;
            this.MeshName = meshName;
        }
    }

    public class DynamicObjectManifestEntry
    {
        public int Id;
        public string Name;
        public string MeshName;
        public Dictionary<string, object> Properties;

        public DynamicObjectManifestEntry(int id, string name, string meshName)
        {
            this.Id = id;
            this.Name = name;
            this.MeshName = meshName;
        }

        public DynamicObjectManifestEntry(int id, string name, string meshName,Dictionary<string,object>props)
        {
            this.Id = id;
            this.Name = name;
            this.MeshName = meshName;
            this.Properties = props;
        }
    }
}