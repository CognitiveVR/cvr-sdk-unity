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
            OculusTouchRight,
            ViveTracker
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

        //video settings
        public bool FlipVideo;
        public string ExternalVideoSource;
        float SendFrameTimeRemaining; //counts down to 0 during update. sends video time if it hasn't been sent lately
        float MaxSendFrameTime = 5;
        bool wasPlayingVideo = false;
        bool wasBufferingVideo = false;

        public bool TrackGaze = false;
        float TotalGazeDuration;

        public bool RequiresManualEnable = false;

        //used to append changes in button states to snapshots
        private DynamicObjectButtonStates ButtonStates = null;


        //static variables
        private static int uniqueIdOffset = 1000;
        private static int currentUniqueId;
        //cleared between scenes so new snapshots will re-write to the manifest and get uploaded to the scene
        public static List<DynamicObjectId> ObjectIds = new List<DynamicObjectId>();
        //don't recycle ids between scenes - otherwise ids wont be written into new scene's manifest
        public static void ClearObjectIds()
        {
            Debug.Log("========================clear object ids");
            ObjectIds.Clear();
        }

        //cumulative. all objects
        public static List<DynamicObjectManifestEntry> ObjectManifest = new List<DynamicObjectManifestEntry>();

        //new until they are packaged into json
        public static List<DynamicObjectSnapshot> NewSnapshots = new List<DynamicObjectSnapshot>();
        private static List<DynamicObjectManifestEntry> NewObjectManifest = new List<DynamicObjectManifestEntry>();

        private static List<string> savedDynamicManifest = new List<string>();
        private static List<string> savedDynamicSnapshots = new List<string>();
        //private static int maxSnapshotBatchCount = 64;
        private static int jsonpart = 1;

#if UNITY_5_6_OR_NEWER
        public UnityEngine.Video.VideoPlayer VideoPlayer;
#endif
        bool IsVideoPlayer
        {
            get
            {
#if UNITY_5_6_OR_NEWER
                return VideoPlayer != null && !string.IsNullOrEmpty(ExternalVideoSource);
#else
                return false;
#endif
            }
        }

        void OnEnable()
        {
            if (!Application.isPlaying) { return; }
            if (RequiresManualEnable)
            {
                return;
            }

            if (IsVideoPlayer)
            {
#if UNITY_5_6_OR_NEWER
                //VideoPlayer.started += VideoPlayer_started;
                //VideoPlayer.errorReceived += VideoPlayer_errorReceived;
                VideoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
                VideoPlayer.loopPointReached += VideoPlayer_loopPointReached;
#endif
                //TODO wait for first frame should set buffering to true for first snapshot
            }

            //set the 'custom mesh name' to be the lowercase of the common name
            if (!UseCustomMesh)
            {
                UseCustomMesh = true;
                if (CommonMesh == CommonDynamicMesh.ViveController)
                {
                    if (CognitiveVR_Manager.InitResponse == Error.Success)
                    {
                        Debug.Log("dynamic object as init success");
                        CognitiveVR_Manager_InitEvent(Error.Success);
                    }
                    else if(CognitiveVR_Manager.InitResponse == Error.NotInitialized)
                    {
                        Debug.Log("dynamic object listen for init");
                        CognitiveVR_Manager.InitEvent += CognitiveVR_Manager_InitEvent;
                    }
                    else
                    {
                        Debug.Log("dynamic object not registering!");
                    }
                }
                MeshName = CommonMesh.ToString().ToLower();
            }

            if (SnapshotOnEnable)
            {
                var v = NewSnapshot().UpdateTransform().SetEnabled(true);
                if (UpdateTicksOnEnable || IsVideoPlayer)
                {
                    v.SetTick(true);
                }
            }

            if (TrackGaze)
            {
                CognitiveVR_Manager.QuitEvent += SendGazeDurationOnQuit;
            }
        }

        private void VideoPlayer_loopPointReached(UnityEngine.Video.VideoPlayer source)
        {
            Debug.Log("loop point reached");
            SendVideoTime();

            if (VideoPlayer.isLooping)
            { 
                //snapshot at end, then snapshot at beginning
                NewSnapshot().SetProperty("videotime", 0);
                Debug.Log("video player loop point reached to playing=" + VideoPlayer.isPlaying + " at frame " + 0);
            }
            else
            {
                NewSnapshot().SetProperty("videoplay", false).SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
                Debug.Log("stop at loop point. frame " + (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
                wasPlayingVideo = false;
            }
        }

        private void VideoPlayer_prepareCompleted(UnityEngine.Video.VideoPlayer source)
        {
            //buffering complete?
            if (wasBufferingVideo)
            {
                SendVideoTime().SetProperty("videoisbuffer", false);
                wasBufferingVideo = false;
            }
        }

        private void CognitiveVR_Manager_InitEvent(Error initError)
        {
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;

            if (initError != Error.Success) { return; }
#if CVR_STEAMVR
            ButtonStates = new DynamicObjectButtonStates();
            ButtonStates.ButtonStateInit(transform);
#endif
        }

        //used to manually call 
        public void Init()
        {
            RequiresManualEnable = false;
            OnEnable();
        }

        //public so snapshot can begin this
        public IEnumerator UpdateTick()
        {
            updateTick = new WaitForSeconds(UpdateRate);

            while (true)
            {
                yield return updateTick;
                CheckUpdate();
                UpdateFrame(UpdateRate);
            }
        }

        //public so snapshot can tie cognitivevr_manager tick event to this. this is for syncing player tick and this tick
        public void CognitiveVR_Manager_TickEvent()
        {
            CheckUpdate();
            UpdateFrame(CognitiveVR_Preferences.Instance.SnapshotInterval);
        }

        void UpdateFrame(float timeSinceLastTick)
        {
#if UNITY_5_6_OR_NEWER
            if (IsVideoPlayer)
            {
                if (VideoPlayer.isPlaying)
                {
                    SendFrameTimeRemaining -= timeSinceLastTick;
                }
            }
            
            if (SendFrameTimeRemaining < 0)
            {
                SendVideoTime();
            }
#endif
        }

        /// <summary>
        /// makes a new snapshot and adds the video's current frame as a property
        /// </summary>
        /// <returns>returns the new snapshot</returns>
        public DynamicObjectSnapshot SendVideoTime()
        {
            SendFrameTimeRemaining = MaxSendFrameTime;
            return NewSnapshot().SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
        }

        //puts outstanding snapshots (from last update) into json
        private static void CognitiveVR_Manager_Update()
        {
            //TODO check performance on this - how performant is clearing an empty dictionary?
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR.CognitiveVR_Preferences.Instance.FindScene(sceneName);
            if (sceneSettings == null)
            {
                CognitiveVR.Util.logDebug("Dynamic Object Update - scene settings are null " + sceneName);
                NewSnapshots.Clear();
                NewObjectManifest.Clear();
                savedDynamicManifest.Clear();
                savedDynamicSnapshots.Clear();
                return;
            }
            if (string.IsNullOrEmpty(sceneSettings.SceneId))
            {
                CognitiveVR.Util.logDebug("Dynamic Object Update - sceneid is empty. do not send dynamic objects to sceneexplorer");
                NewSnapshots.Clear();
                NewObjectManifest.Clear();
                savedDynamicManifest.Clear();
                savedDynamicSnapshots.Clear();
                return;
            }
            WriteSnapshotsToString();
        }

        public void OnGaze(float time)
        {
            if (!TrackGaze){ return; }
            TotalGazeDuration += time;
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

            /*if (TrackGaze)
            {
                if (CognitiveVR_Manager.HasRequestedDynamicGazeRaycast) { return; }

                CognitiveVR_Manager.RequestDynamicObjectGaze();
            }*/
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

                    //do not allow video players to recycle ids - could point to different urls, making the manifest invalid
                    //could allow sharing objectids if the url target is the same, but that's not stored in the objectid - need to read objectid from manifest

                    if (recycledId != null && !IsVideoPlayer)
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

                        if (MeshName == "vivecontroller")
                        {
                            string controllerName = "left";
                            if (transform == CognitiveVR_Manager.GetController(true) || name.Contains("right"))
                            {
                                controllerName = "right";
                            }
                            else if (transform == CognitiveVR_Manager.GetController(false) || name.Contains("left"))
                            {
                                controllerName = "left";
                            }

                            if (manifestEntry.Properties == null)
                            {
                                manifestEntry.Properties = new Dictionary<string, object>() { { "controller", controllerName } };
                            }
                            else
                            {
                                manifestEntry.Properties.Add("controller", controllerName);
                            }
                        }

                        if (!string.IsNullOrEmpty(ExternalVideoSource))
                        {
                            manifestEntry.videoURL = ExternalVideoSource;
                            manifestEntry.videoFlipped = FlipVideo;
                        }

                        ObjectIds.Add(ObjectId);
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
                    if (!string.IsNullOrEmpty(ExternalVideoSource))
                    {
                        manifestEntry.videoURL = ExternalVideoSource;
                        manifestEntry.videoFlipped = FlipVideo;
                    }
                    ObjectIds.Add(ObjectId);
                    ObjectManifest.Add(manifestEntry);
                    NewObjectManifest.Add(manifestEntry);
                    Debug.Log("added " + MeshName + " id " + CustomId + " to objectid list");
                }

                if (IsVideoPlayer)
                {
                    CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_UpdateEvent;
                }

                if (ObjectManifest.Count == 1)
                {
                    CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_Update;
                    CognitiveVR_Manager.SendDataEvent += SendAllSnapshots;
                }
            }
            
            //create snapshot for this object
            var snapshot = new DynamicObjectSnapshot(this);
            if (ButtonStates != null)
            {
                snapshot.Buttons = ButtonStates.GetDirtyStates();
            }
            if (IsVideoPlayer)
            {
#if UNITY_5_6_OR_NEWER
                if (!VideoPlayer.isPrepared)
                {
                    snapshot.SetProperty("videoisbuffer", true);
                    wasBufferingVideo = true;
                }
#endif
            }
            NewSnapshots.Add(snapshot);
            return snapshot;
        }

        //update on instance of dynamic game obejct
        //only used when dynamic object is written to manifest as a video player
        private void CognitiveVR_Manager_UpdateEvent()
        {
            if (!IsVideoPlayer)
            {
                //likely video player was destroyed
                CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_UpdateEvent;
                return;
            }
#if UNITY_5_6_OR_NEWER
            if (VideoPlayer.isPlaying != wasPlayingVideo)
            {
                if (VideoPlayer.frameRate == 0)
                {
                    //hasn't actually loaded anything yet
                    return;
                }

                SendVideoTime().SetProperty("videoplay", VideoPlayer.isPlaying);

                //NewSnapshot().SetProperty("videoplay", VideoPlayer.isPlaying).SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
                Debug.Log("video player changed to playing=" + VideoPlayer.isPlaying + " at frame " + (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
                wasPlayingVideo = VideoPlayer.isPlaying;
            }
#endif
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

            //redundant? checked when writing snapshots to string
            CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR.CognitiveVR_Preferences.Instance.FindScene(sceneName);
            if (sceneSettings == null)
            {
                CognitiveVR.Util.logDebug("scene settings are null " + sceneName);
                NewSnapshots.Clear();
                NewObjectManifest.Clear();
                savedDynamicManifest.Clear();
                savedDynamicSnapshots.Clear();
                return;
            }
            if (string.IsNullOrEmpty(sceneSettings.SceneId))
            {
                CognitiveVR.Util.logDebug("sceneid is empty. do not send dynamic objects to sceneexplorer");
                NewSnapshots.Clear();
                NewObjectManifest.Clear();
                savedDynamicManifest.Clear();
                savedDynamicSnapshots.Clear();
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(512);

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
            CognitiveVR.Util.logDebug(builder.ToString());


            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
            CognitiveVR_Manager.Instance.StartCoroutine(CognitiveVR_Manager.Instance.PostJsonRequest(outBytes, url));
        }

        private static string SetManifestEntry(DynamicObjectManifestEntry entry)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);

            builder.Append("\"");
            builder.Append(entry.Id);
            builder.Append("\":{");


            if (!string.IsNullOrEmpty(entry.Name))
            {
                builder.Append(JsonUtil.SetString("name", entry.Name));
                builder.Append(",");
            }
            builder.Append(JsonUtil.SetString("mesh", entry.MeshName));

            if (!string.IsNullOrEmpty(entry.videoURL))
            {
                builder.Append(",");
                builder.Append(JsonUtil.SetString("externalVideoSource", entry.videoURL));
                builder.Append(",");
                builder.Append(JsonUtil.SetObject("flipVideo", entry.videoFlipped));
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
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
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
                builder.Append("{");
                foreach (var v in snap.Properties)
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
                builder.Append("}");
                builder.Append("]"); //close properties object
            }

            if (snap.Buttons != null)
            {
                //var dirtyButtons = snap.Dynamic.ButtonStates.GetDirtyStates();

                if (snap.Buttons.Count > 0)
                {
                    builder.Append(",");
                    builder.Append("\"buttons\":{");
                    foreach (var button in snap.Buttons)
                    {
                        builder.Append("\"" + button.Key + "\":{");
                        builder.Append("\"buttonPercent\":" + button.Value.ButtonPercent);
                        if (button.Value.IncludeXY)
                        {
                            builder.Append(",\"x\":" + button.Value.X.ToString("0.000"));
                            builder.Append(",\"y\":" + button.Value.Y.ToString("0.000"));
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

        void OnDisable()
        {
            if (!Application.isPlaying) { return; }
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            if (IsVideoPlayer)
            {
#if UNITY_5_6_OR_NEWER
                //VideoPlayer.started -= VideoPlayer_started;
                //VideoPlayer.errorReceived -= VideoPlayer_errorReceived;
                VideoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
#endif
            }
            if (TrackGaze || !ReleaseIdOnDisable)
            {
                NewSnapshot().SetEnabled(false);
                return;
            }
            NewSnapshot().ReleaseUniqueId();
        }

        void OnDestroy()
        {
            if (!Application.isPlaying) { return; }
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            if (IsVideoPlayer)
            {
#if UNITY_5_6_OR_NEWER
                //VideoPlayer.started -= VideoPlayer_started;
                //VideoPlayer.errorReceived -= VideoPlayer_errorReceived;
                VideoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
#endif
            }
            if (TrackGaze || !ReleaseIdOnDestroy)
            {
                NewSnapshot().SetEnabled(false);
                if (TotalGazeDuration > 0)
                {
                    Debug.Log("destroy dynamic object");
                    Instrumentation.Transaction("cvr.objectgaze").setProperty("object name", gameObject.name).setProperty("duration", TotalGazeDuration).beginAndEnd();
                    TotalGazeDuration = 0;
                }
                return;
            }
            NewSnapshot().ReleaseUniqueId();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward);
        }

        private void SendGazeDurationOnQuit()
        {
            if (TotalGazeDuration > 0)
            {
                Debug.Log("onquit dynamic object");
                Instrumentation.Transaction("cvr.objectgaze").setProperty("object name", gameObject.name).setProperty("duration", TotalGazeDuration).beginAndEnd();
                TotalGazeDuration = 0; //reset to not send OnDestroy event
            }
        }
    }

    public class DynamicObjectSnapshot
    {
        public DynamicObject Dynamic;
        public int Id;
        public Dictionary<string, object> Properties;
        public Dictionary<string, DynamicObjectButtonStates.ButtonState> Buttons;
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

        public DynamicObjectSnapshot SetProperty(string key, object value)
        {
            if (Properties == null)
            {
                Properties = new Dictionary<string, object>();
                Properties.Add(key, value);
                return this;
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

        //releasing an id allows a new object with the same mesh to be used instead of bloating the object manifest
        public DynamicObjectSnapshot ReleaseUniqueId()
        {
            //var foundId = DynamicObject.ObjectIds.Find(x => x.Id == this.Id);
            var foundId = DynamicObject.ObjectIds.Find(delegate (DynamicObjectId obj) { return obj.Id == this.Id; });
            if (foundId != null)
            {
                foundId.Used = false;
            }
            this.Dynamic.ObjectId = null;
            this.SetEnabled(false);
            return this;
        }
    }

    /// <summary>
    /// <para>holds info about which ids are used and what meshes they are held by</para> 
    /// <para>used to 'release' unique ids so meshes can be pooled in scene explorer</para> 
    /// </summary>
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
        public string videoURL;
        public bool videoFlipped;

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

    public class DynamicObjectButtonStates
    {
        public class ButtonState
        {
            public int ButtonPercent = 0;
            public float X = 0;
            public float Y = 0;
            public bool IncludeXY = false;

            public ButtonState(int buttonPercent,float x=0, float y=0, bool includexy = false)
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
                    return s.ButtonPercent == ButtonPercent && Mathf.Approximately(s.X,X) && Mathf.Approximately(s.Y, Y);
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

        public Dictionary<string, ButtonState> CurrentStates = new Dictionary<string, ButtonState>();
        public Dictionary<string, ButtonState> LastStates = new Dictionary<string, ButtonState>();

        public Dictionary<string, ButtonState> DirtyStates = new Dictionary<string, ButtonState>();
        public Dictionary<string, ButtonState> GetDirtyStates()
        {
            DirtyStates.Clear();

            if (!CurrentStates["vive_homebtn"].Equals(LastStates["vive_homebtn"])) { DirtyStates.Add("vive_homebtn", new ButtonState(CurrentStates["vive_homebtn"])); }
            if (!CurrentStates["vive_menubtn"].Equals(LastStates["vive_menubtn"])) { DirtyStates.Add("vive_menubtn", new ButtonState(CurrentStates["vive_menubtn"])); }
            if (!CurrentStates["vive_gripbtn"].Equals(LastStates["vive_gripbtn"])) { DirtyStates.Add("vive_gripbtn", new ButtonState(CurrentStates["vive_gripbtn"])); }
            if (!CurrentStates["vive_padbtn"].Equals(LastStates["vive_padbtn"])) { DirtyStates.Add("vive_padbtn", new ButtonState(CurrentStates["vive_padbtn"])); }
            if (!CurrentStates["vive_trigger"].Equals(LastStates["vive_trigger"])) { DirtyStates.Add("vive_trigger", new ButtonState(CurrentStates["vive_trigger"])); }

            LastStates["vive_homebtn"].Copy(CurrentStates["vive_homebtn"]);
            LastStates["vive_menubtn"].Copy(CurrentStates["vive_menubtn"]);
            LastStates["vive_gripbtn"].Copy(CurrentStates["vive_gripbtn"]);
            LastStates["vive_padbtn"].Copy(CurrentStates["vive_padbtn"]);
            LastStates["vive_trigger"].Copy(CurrentStates["vive_trigger"]);

            return DirtyStates;
        }

#if CVR_STEAMVR
        //TODO if controller is not present at cognitivevrmanager init, it doesn't initialize input tracking
        public void ButtonStateInit(Transform transform)
        {
            SteamVR_TrackedController controller;
            for (int i = 0; i<2; i++)
            {
                bool right = i == 0 ? true : false;
                if (CognitiveVR_Manager.GetController(right) == null)
                {
                    //Debug.LogError("controller is null!");
                    continue;
                }
                if (CognitiveVR_Manager.GetController(right) != transform)
                {
                    //Debug.LogError("controller is not this!");
                    continue;
                }

                controller = CognitiveVR_Manager.GetController(right).GetComponent<SteamVR_TrackedController>();

                if (controller == null)
                {
                    Util.logDebug("Must have a SteamVR_TrackedController component to capture inputs!");
                    continue;
                }
                //controller = CognitiveVR_Manager.GetController(right).gameObject.AddComponent<SteamVR_TrackedController>(); //need to have start called and set controllerindex

                Id = (int)controller.controllerIndex;

                controller.SteamClicked += Controller_SteamClicked;

                controller.MenuButtonClicked += Controller_MenuButtonClicked;
                controller.MenuButtonUnclicked += Controller_MenuButtonUnclicked;

                controller.Gripped += Controller_Gripped;
                controller.Ungripped += Controller_Ungripped;

                controller.PadTouched += Controller_PadTouched;
                controller.PadUntouched += Controller_PadUntouched;

                controller.PadClicked += Controller_PadClicked;
                controller.PadUnclicked += Controller_PadUnclicked;

                CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;
            }

            LastStates.Add("vive_menubtn", new ButtonState(0));
            LastStates.Add("vive_homebtn", new ButtonState(0));
            LastStates.Add("vive_gripbtn", new ButtonState(0));
            LastStates.Add("vive_padbtn", new ButtonState(0,0,0,true));
            LastStates.Add("vive_trigger", new ButtonState(0));

            CurrentStates.Add("vive_menubtn", new ButtonState(0));
            CurrentStates.Add("vive_homebtn", new ButtonState(0));
            CurrentStates.Add("vive_gripbtn", new ButtonState(0));
            CurrentStates.Add("vive_padbtn", new ButtonState(0, 0, 0,true));
            CurrentStates.Add("vive_trigger", new ButtonState(0));
        }

        private void CognitiveVR_Manager_TickEvent()
        {
            CurrentStates["vive_trigger"].ButtonPercent = (int)(SteamVR_Controller.Input(Id).GetState().rAxis1.x * 100);//trigger
        }

        private void Controller_PadUnclicked(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_padbtn"].ButtonPercent = 0;
            CurrentStates["vive_padbtn"].X = e.padX;
            CurrentStates["vive_padbtn"].Y = e.padY;
        }

        private void Controller_PadClicked(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_padbtn"].ButtonPercent = 100;
            CurrentStates["vive_padbtn"].X = e.padX;
            CurrentStates["vive_padbtn"].Y = e.padY;
        }

        private void Controller_PadUntouched(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_padbtn"].ButtonPercent = 0;
            CurrentStates["vive_padbtn"].X = e.padX;
            CurrentStates["vive_padbtn"].Y = e.padY;
        }

        private void Controller_PadTouched(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_padbtn"].ButtonPercent = 0;
            CurrentStates["vive_padbtn"].X = e.padX;
            CurrentStates["vive_padbtn"].Y = e.padY;
        }

        private void Controller_Ungripped(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_gripbtn"].ButtonPercent = 0;
        }

        private void Controller_Gripped(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_gripbtn"].ButtonPercent = 100;
        }

        private void Controller_MenuButtonUnclicked(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_menubtn"].ButtonPercent = 0;
        }

        private void Controller_MenuButtonClicked(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_menubtn"].ButtonPercent = 100;
        }

        private void Controller_SteamClicked(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_homebtn"].ButtonPercent = 100;
        }
#endif
    }
}