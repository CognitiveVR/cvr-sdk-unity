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


        public bool SnapshotOnEnable = true;
        [Tooltip("enables coroutine that continually checks if object has moved")]
        public bool UpdateTicksOnEnable = true;

        [Header("Thresholds")]
        public float PositionThreshold = 0.25f;
        private Vector3 lastPosition;
        public float RotationThreshold = 45f;
        private Quaternion lastRotation;

        [Header("IDs")]
        public bool UseCustomId = false;
        public int CustomId;
        public bool ReleaseIdOnDestroy = true;
        public bool ReleaseIdOnDisable = true;

        public string GroupName;

        public DynamicObjectId ObjectId;

        public bool UseCustomMesh;
        public CommonDynamicMesh CommonMesh;
        public string MeshName;

        [Header("Updates")]
        public bool SyncWithPlayerUpdate = false;
        public float UpdateRate = 0.5f;
        private YieldInstruction updateTick;

        //static variables
        private static int uniqueIdOffset = 1000;
        private static int currentUniqueId;
        public static List<DynamicObjectId> ObjectIds = new List<DynamicObjectId>();

        public static List<DynamicObjectSnapshot> Snapshots = new List<DynamicObjectSnapshot>();
        public static List<DynamicObjectManifestEntry> ObjectManifest = new List<DynamicObjectManifestEntry>();

        void OnEnable()
        {
            if (SnapshotOnEnable)
            {
                var v = NewSnapshot().UpdateTransform();
                if (UpdateTicksOnEnable)
                {
                    v.SetTick(true);
                }
            }
        }

        public IEnumerator UpdateTick()
        {
            updateTick = new WaitForSeconds(UpdateRate);

            while (true)
            {
                yield return updateTick;
                CheckUpdate();
            }
        }

        public void CognitiveVR_Manager_TickEvent()
        {
            CheckUpdate();
        }

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
        }

        public DynamicObjectSnapshot NewSnapshot()
        {
            return NewSnapshot(MeshName);
        }

        public DynamicObjectSnapshot NewSnapshot(string mesh)
        {
            bool needObjectId = false;
            //add object to manifest and set ObjectId
            if (ObjectId == null)
            {
                needObjectId = true;
            }
            else
            {
                var manifestEntry = ObjectManifest.Find(x => x.id == ObjectId.id);
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
                    var recycledId = ObjectIds.Find(x => !x.used && x.meshName == mesh);
                    if (recycledId != null)
                    {
                        ObjectId = recycledId;
                        ObjectId.used = true;
                        //id is already on manifest
                    }
                    else
                    {
                        int newId = GetUniqueID();
                        ObjectId = new DynamicObjectId(newId, MeshName);
                        ObjectManifest.Add(new DynamicObjectManifestEntry(ObjectId.id, gameObject.name, MeshName));
                    }
                }
                else
                {
                    ObjectId = new DynamicObjectId(CustomId, MeshName);
                    ObjectManifest.Add(new DynamicObjectManifestEntry(ObjectId.id, gameObject.name, MeshName));
                }

                if (ObjectManifest.Count == 1)
                {
                    CognitiveVR_Manager.SendDataEvent += CognitiveVR_Manager_SendDataEvent;
                }
            }
            
            //create snapshot for this object
            var snapshot = new DynamicObjectSnapshot(this);
            Snapshots.Add(snapshot);
            return snapshot;
        }

        public void UpdateLastPositions()
        {
            lastPosition = _transform.position;
            lastRotation = _transform.rotation;
        }

        //TODO this should return a dynamicObjectId instance
        public static int GetUniqueID()
        {
            currentUniqueId++;
            return currentUniqueId + uniqueIdOffset;
        }

        private static void CognitiveVR_Manager_SendDataEvent()
        {
            byte[] bytes;

            if (DynamicObject.ObjectManifest.Count > 0 && DynamicObject.Snapshots.Count > 0)
            {
                bytes = FormatDynamicsToString();

                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR.CognitiveVR_Preferences.Instance.FindScene(sceneName);
                if (sceneSettings == null)
                {
                    Debug.Log("scene settings are null " + sceneName);
                    return;
                }

                string url = "https://sceneexplorer.com/api/dynamics/" + sceneSettings.SceneId;

                Debug.Log("send dynamic data to " + url);

                CognitiveVR_Manager.Instance.StartCoroutine(CognitiveVR_Manager.Instance.PostJsonRequest(bytes, url));
            }

            //clear the manifest
            CognitiveVR_Manager.SendDataEvent -= CognitiveVR_Manager_SendDataEvent;
            ObjectManifest.Clear();
        }

        private static byte[] FormatDynamicsToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append("{");

            //header
            builder.Append(JsonUtil.SetString("userid", Core.userId));
            builder.Append(",");
            builder.Append(JsonUtil.SetObject("timestamp", CognitiveVR_Preferences.TimeStamp));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("sessionid", CognitiveVR_Preferences.SessionID));
            builder.Append(",");

            //manifest
            builder.Append("\"manifest\":{");
            for (int i = 0; i < DynamicObject.ObjectManifest.Count; i++)
            {
                builder.Append(SetManifestEntry(DynamicObject.ObjectManifest[i]));
                builder.Append(",");
            }
            if (DynamicObject.ObjectManifest.Count > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }
            builder.Append("},");

            //snapshots
            builder.Append("\"data\":[");
            for (int i = 0; i < DynamicObject.Snapshots.Count; i++)
            {
                builder.Append(SetSnapshot(DynamicObject.Snapshots[i]));
                builder.Append(",");
            }
            if (DynamicObject.Snapshots.Count > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }
            builder.Append("]");


            builder.Append("}");

            Debug.Log(builder.ToString());

            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
            return outBytes;
        }

        public static string SetManifestEntry(DynamicObjectManifestEntry entry)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append("\"");
            builder.Append(entry.id);
            builder.Append("\":{");


            if (!string.IsNullOrEmpty(entry.name))
            {
                builder.Append(JsonUtil.SetString("name", entry.name));
                builder.Append(",");
            }
            builder.Append(JsonUtil.SetString("mesh", entry.meshName));


            /*if (snap.properties != null && snap.properties.Keys.Count > 0)
            {
                builder.Append(",");
                builder.Append("\"properties\":[");
                foreach (var v in snap.properties)
                {
                    if (v.Value.GetType() == typeof(string))
                    {
                        builder.Append(JsonUtil.SetString(v.Key, (string)v.Value));
                    }
                    else
                    {
                        builder.Append(JsonUtil.SetObject(v.Key, v.Value));
                    }
                    builder.Append(",");
                }
                builder.Remove(builder.Length - 1, 1); //remove last comma
                builder.Append("]"); //close properties object
            }*/

            builder.Append("}"); //close transaction object

            return builder.ToString();
        }

        public static string SetSnapshot(DynamicObjectSnapshot snap)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");

            builder.Append(JsonUtil.SetObject("id", snap.id));
            builder.Append(",");
            builder.Append(JsonUtil.SetObject("time", snap.timestamp));
            builder.Append(",");
            builder.Append(JsonUtil.SetVector("p", snap.position));
            builder.Append(",");
            builder.Append(JsonUtil.SetQuat("r", snap.rotation));


            if (snap.properties != null && snap.properties.Keys.Count > 0)
            {
                builder.Append(",");
                builder.Append("\"properties\":[");
                foreach (var v in snap.properties)
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

            builder.Append("}"); //close transaction object

            return builder.ToString();
        }

        void OnDisable()
        {
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            if (!ReleaseIdOnDisable) { return; }
            NewSnapshot().ReleaseUniqueId();
        }

        void OnDestroy()
        {
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            if (!ReleaseIdOnDestroy) { return; }
            NewSnapshot().ReleaseUniqueId();
        }
    }

    public class DynamicObjectSnapshot
    {
        public DynamicObject dynamic;
        public int id;
        public Dictionary<string, object> properties;
        public float[] position = new float[3] { 0, 0, 0 };
        public float[] rotation = new float[4] { 0, 0, 0, 1 };
        public double timestamp;

        public DynamicObjectSnapshot(DynamicObject dynamic)
        {
            this.dynamic = dynamic;
            id = dynamic.ObjectId.id;
            timestamp = Util.Timestamp();
        }

        public DynamicObjectSnapshot(DynamicObject dynamic, Vector3 pos, Quaternion rot, Dictionary<string, object> props = null)
        {
            this.dynamic = dynamic;
            id = dynamic.ObjectId.id;
            properties = props;
            position = new float[3] { 0, 0, 0 };
            rotation = new float[4] { rot.x, rot.y, rot.z, rot.w };
            timestamp = Util.Timestamp();
        }

        public DynamicObjectSnapshot(DynamicObject dynamic, float[] pos, float[] rot, Dictionary<string, object> props = null)
        {
            this.dynamic = dynamic;
            id = dynamic.ObjectId.id;
            properties = props;
            position = pos;
            
            rotation = rot;
            timestamp = Util.Timestamp();
        }


        public DynamicObjectSnapshot UpdateTransform()
        {
            position = new float[3] { dynamic._transform.position.x, dynamic._transform.position.y, dynamic._transform.position.z };
            rotation = new float[4] { dynamic._transform.rotation.x, dynamic._transform.rotation.y, dynamic._transform.rotation.z, dynamic._transform.rotation.w };

            dynamic.UpdateLastPositions();

            return this;
        }

        public DynamicObjectSnapshot SetTick(bool enable)
        {
            dynamic.StopAllCoroutines();
            if (enable)
            {
                if (dynamic.SyncWithPlayerUpdate)
                {
                    CognitiveVR_Manager.TickEvent += dynamic.CognitiveVR_Manager_TickEvent;
                }
                else
                {
                    dynamic.StartCoroutine(dynamic.UpdateTick());
                }
            }
            return this;
        }

        public DynamicObjectSnapshot SetProperties(Dictionary<string, object> dict)
        {
            properties = dict;
            return this;
        }

        public DynamicObjectSnapshot AppendProperties(Dictionary<string, object> dict)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, object>();
            }
            foreach (var v in dict)
            {
                properties[v.Key] = v.Value;
            }
            return this;
        }

        public DynamicObjectSnapshot SetEnabled(bool enable)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, object>();
            }
            properties["enabled"] = enable;
            return this;
        }

        //releasing an id allows a new object with the same mesh to be used instead of bloating the object manifest
        //if objects are pooled on the dev side, this may not be required
        public DynamicObjectSnapshot ReleaseUniqueId()
        {
            var foundId = DynamicObject.ObjectIds.Find(x => x.id == this.id);
            if (foundId != null)
            {
                foundId.used = false;
                this.dynamic.ObjectId = null;
            }
            return this;
        }
    }

    //holds info about which ids are used and what meshes they are held by
    //used to 'release' unique ids so meshes can be pooled in scene explorer
    public class DynamicObjectId
    {
        public int id;
        public bool used = true;
        public string meshName;

        public DynamicObjectId(int id, string meshName)
        {
            this.id = id;
            this.meshName = meshName;
        }
    }

    public class DynamicObjectManifestEntry
    {
        public int id;
        public string name;
        public string meshName;

        public DynamicObjectManifestEntry(int id, string name, string meshName)
        {
            this.id = id;
            this.name = name;
            this.meshName = meshName;
        }
    }
}