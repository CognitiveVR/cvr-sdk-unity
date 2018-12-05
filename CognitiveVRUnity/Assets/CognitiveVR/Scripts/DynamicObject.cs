using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//this should only contain the component to send dynamic data/engagements to the plugin


//manually send snapshots with builder pattern
//presets for controllers/level geometry/pooled enemies/grabable item
//example scripts for snow fortress blocks

/*level geo. no ticks, update transform on start*/
/*controllers. tick. update on start. never disabled. custom id*/
/*enemies. tick. update on start non-custom id. reused on enable*/
/*grabable item. custom id. update ticks. never disabled*/

//iterate through and write updates
namespace CognitiveVR
{
    public class DynamicObject : MonoBehaviour
    {
#if UNITY_EDITOR
        //stores instanceid. used to check if something in editor has changed
        public int editorInstanceId;
#endif

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

        [HideInInspector]
        public Transform _t;

        public bool SnapshotOnEnable = true;
        public bool ContinuallyUpdateTransform = true;

        public float PositionThreshold = 0.001f;
        public Vector3 lastPosition;
        public float RotationThreshold = 0.1f;
        public Quaternion lastRotation;

        public bool UseCustomId = true;
        public string CustomId = "";
        public bool ReleaseIdOnDestroy = false; //only release the id for reuse if not tracking gaze
        public bool ReleaseIdOnDisable = false; //only release the id for reuse if not tracking gaze

        public string GroupName;

        private DynamicObjectId viewerId;
        //used internally for scene explorer
        public DynamicObjectId ViewerId
        {
            get
            {
                if (viewerId == null)
                    GenerateDynamicObjectId();
                return viewerId;
            }
            set
            {
                viewerId = value;
            }
        }

        //the unique identifying string for this dynamic object
        public string Id
        {
            get
            {
                return ViewerId.Id;
            }
        }

        public bool UseCustomMesh = true;
        public CommonDynamicMesh CommonMesh;
        public string MeshName;

        public bool SyncWithPlayerUpdate = true;
        public float UpdateRate = 0.5f;
        private YieldInstruction updateTick;

        //video settings
        bool FlipVideo = false;
        public string ExternalVideoSource;
        float SendFrameTimeRemaining; //counts down to 0 during update. sends video time if it hasn't been sent lately
        float MaxSendFrameTime = 5;
        bool wasPlayingVideo = false;
        bool wasBufferingVideo = false;

        public bool TrackGaze = true;
        float TotalGazeDuration;

        public bool RequiresManualEnable = false;

        //engagement name, engagement event. cleared when snapshots sent
        List<EngagementEvent> DirtyEngagements = null;

        //engagement name, engagement event
        List<EngagementEvent> Engagements = null;

        //each engagement event
        public class EngagementEvent
        {
            //internal
            public bool Active = true;

            //written to snapshot
            public string EngagementType;
            public string Parent = "-1";
            public float EngagementTime = 0;
            public int EngagementNumber;
            //public int EngagementCount = 1; count is figured out by list.count in engagementsDict

            public EngagementEvent(EngagementEvent source)
            {
                EngagementType = source.EngagementType;
                Parent = source.Parent;
                EngagementTime = source.EngagementTime;
                EngagementNumber = source.EngagementNumber;
            }

            public EngagementEvent(string name, string parent, int engagementNumber)
            {
                EngagementType = name;
                Parent = parent;
                EngagementNumber = engagementNumber;
            }
        }

        //static variables
        private static int uniqueIdOffset = 1000;
        private static int currentUniqueId;
        //cleared between scenes so new snapshots will re-write to the manifest and get uploaded to the scene
        public static List<DynamicObjectId> ObjectIds = new List<DynamicObjectId>();

        ///don't recycle object ids between scenes - otherwise ids wont be written into new scene's manifest
        ///disconnect all the objectids from dynamics. they will make new objectids in the scene when they write a new snapshot
        public static void ClearObjectIds()
        {
            foreach (var v in ObjectIds)
            {
                if (v == null) { continue; }
                if (v.Target == null) { continue; }
                v.Target.ViewerId = null;
            }
            ObjectIds.Clear();
        }

        private static Queue<DynamicObjectSnapshot> NewSnapshotQueue = new Queue<DynamicObjectSnapshot>();
        private static Queue<DynamicObjectManifestEntry> NewObjectManifestQueue = new Queue<DynamicObjectManifestEntry>();

        private static int jsonpart = 1;

        public UnityEngine.Video.VideoPlayer VideoPlayer;
        bool IsVideoPlayer;

        bool registeredToEvents = false;

        /// <summary>
        /// called on enable and after scene load. registers to tick and records 'onenable' snapshot for new scene
        /// </summary>
        void OnEnable()
        {
            if (transform != null)
            {
                _t = transform;
            }
            else
            {
                Util.logWarning("Dynamic Object destroyed");
                return;
            }

            if (!Application.isPlaying) { return; }
            if (RequiresManualEnable)
            {
                return;
            }

            //set the 'custom mesh name' to be the lowercase of the common name
            if (!UseCustomMesh)
            {
                UseCustomMesh = true;
                MeshName = CommonMesh.ToString().ToLower();
            }

            if (!registeredToEvents)
            {
                registeredToEvents = true;
                CognitiveVR_Manager.LevelLoadedEvent += CognitiveVR_Manager_LevelLoadedEvent;

                if (VideoPlayer != null && !string.IsNullOrEmpty(ExternalVideoSource))
                {
                    IsVideoPlayer = true;
                    VideoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
                    VideoPlayer.loopPointReached += VideoPlayer_loopPointReached;

                    //TODO wait for first frame should set buffering to true for first snapshot
                }
            }

            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                return;
            }

            if (CognitiveVR_Manager.InitResponse == Error.Success)
            {
                CognitiveVR_Manager_InitEvent(Error.Success);
            }

            NewSnapshot().UpdateTransform().SetEnabled(true);

            if (ContinuallyUpdateTransform || IsVideoPlayer)
            {
                if (SyncWithPlayerUpdate)
                {
                    CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
                    CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;
                }
                else
                {
                    StopAllCoroutines();
                    StartCoroutine(UpdateTick());
                }
            }

            if (TrackGaze)
            {
                if (CognitiveVR_Preferences.S_DynamicObjectSearchInParent)
                {
                    if (GetComponentInChildren<Collider>() == null)
                    {
                        Debug.LogWarning("Tracking Gaze on Dynamic Object " + name + " requires a collider!", this);
                    }
                }
                else
                {
                    if (GetComponent<Collider>() == null)
                    {
                        Debug.LogWarning("Tracking Gaze on Dynamic Object " + name + " requires a collider!", this);
                    }
                }
            }
        }

        //post level loaded. also called when cognitive manager first initialized, to make sure onenable registers everything correctly
        private void CognitiveVR_Manager_LevelLoadedEvent()
        {
            OnEnable();
        }

        private void VideoPlayer_loopPointReached(UnityEngine.Video.VideoPlayer source)
        {
            SendVideoTime();

            if (VideoPlayer.isLooping)
            {
                //snapshot at end, then snapshot at beginning
                NewSnapshot().UpdateTransform().SetProperty("videotime", 0);
            }
            else
            {
                NewSnapshot().UpdateTransform().SetProperty("videoplay", false).SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
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

            if (initError != Error.Success)
            {
                StopAllCoroutines();
                return;
            }
        }

        /// <summary>
        /// used to manually enable dynamic object. useful for setting custom properties before first snapshot
        /// </summary>
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
                CheckUpdate(UpdateRate);
                if (IsVideoPlayer)
                    UpdateFrame(UpdateRate);
            }
        }

        //public so snapshot can tie cognitivevr_manager tick event to this. this is for syncing player tick and this tick
        public void CognitiveVR_Manager_TickEvent()
        {
            CheckUpdate(CognitiveVR_Preferences.S_SnapshotInterval);
            if (IsVideoPlayer)
                UpdateFrame(CognitiveVR_Preferences.S_SnapshotInterval);
        }

        void UpdateFrame(float timeSinceLastTick)
        {
            if (VideoPlayer.isPlaying)
            {
                SendFrameTimeRemaining -= timeSinceLastTick;
            }

            if (SendFrameTimeRemaining < 0)
            {
                SendVideoTime();
            }
        }

        /// <summary>
        /// makes a new snapshot and adds the video's current frame as a property. also sets the current transform of the object
        /// </summary>
        /// <returns>returns the new snapshot</returns>
        public DynamicObjectSnapshot SendVideoTime()
        {
            SendFrameTimeRemaining = MaxSendFrameTime;
            return NewSnapshot().UpdateTransform().SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
        }

        //puts outstanding snapshots (from last update) into json
        private static void CognitiveVR_Manager_Update()
        {
            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                if (NewSnapshotQueue.Count + NewObjectManifestQueue.Count > 0)
                {
                    CognitiveVR.Util.logError("Dynamic Object Update - sceneid is empty! do not send Dynamic Objects to sceneexplorer");
                    
                    while(NewSnapshotQueue.Count > 0)
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

                bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.Instance.DynamicSnapshotMinTimer > Time.realtimeSinceStartup;
                bool withinExtremeBatchSize = NewObjectManifestQueue.Count + NewSnapshotQueue.Count < CognitiveVR_Preferences.Instance.DynamicExtremeSnapshotCount;

                //within last send interval and less than extreme count
                if (withinMinTimer && withinExtremeBatchSize)
                {
                    return;
                }
                lastSendTime = Time.realtimeSinceStartup;
                CognitiveVR_Manager.Instance.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
            }
        }

        static float nextSendTime = 0;
        internal static IEnumerator AutomaticSendTimer()
        {
            nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.DynamicSnapshotMaxTimer;
            while (true)
            {
                while (nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.DynamicSnapshotMaxTimer;

                if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                    Util.logDevelopment("check to automatically send dynamics");
                if (NewObjectManifestQueue.Count + NewSnapshotQueue.Count > 0)
                {

                    //don't bother checking min timer here

                    CognitiveVR_Manager.Instance.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
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

            int index=0;
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

            for(int i = 0;i< tempSnapshots.Length;i++)
            {
                tempSnapshots[i].ReturnToPool();
            }
            
            SendSavedSnapshots(manifestEntries, snapshots, trackingSettings, uniqueid, sessiontimestamp, sessionid);
        }

        public void OnGaze(float time)
        {
            if (!TrackGaze) { return; }
            TotalGazeDuration += time;
        }

        /// <summary>
        /// send a snapshot of the position and rotation if the object has moved beyond its threshold
        /// </summary>
        public void CheckUpdate(float timeSinceLastCheck)
        {
            if (!Core.Initialized) { return; }
            if (string.IsNullOrEmpty(Core.TrackingSceneId)) { return; }

            var pos = _t.position;
            var rot = _t.rotation;


            Vector3 heading;
            heading.x = pos.x - lastPosition.x;
            heading.y = pos.y - lastPosition.y;
            heading.z = pos.z - lastPosition.z;

            var distanceSquared = heading.x * heading.x + heading.y * heading.y + heading.z * heading.z;

            bool doWrite = false;
            if (distanceSquared > PositionThreshold * PositionThreshold)
            {
                doWrite = true;
            }
            if (!doWrite)
            {
                float f = Quaternion.Dot(lastRotation, rot);
                if (Mathf.Acos(Mathf.Min(Mathf.Abs(f), 1f)) * 114.59156f > RotationThreshold)
                {
                    doWrite = true;
                }
            }

            DynamicObjectSnapshot snapshot = null;
            if (doWrite)
            {
                snapshot = NewSnapshot();
                snapshot.Position[0] = pos.x;
                snapshot.Position[1] = pos.y;
                snapshot.Position[2] = pos.z;

                snapshot.Rotation[0] = rot.x;
                snapshot.Rotation[1] = rot.y;
                snapshot.Rotation[2] = rot.z;
                snapshot.Rotation[3] = rot.w;
                lastPosition = pos;
                lastRotation = rot;
            }

            if (DirtyEngagements != null)
            {
                if (DirtyEngagements.Count > 0)
                {
                    if (snapshot == null)
                    {
                        snapshot = NewSnapshot();
                        snapshot.Position[0] = pos.x;
                        snapshot.Position[1] = pos.y;
                        snapshot.Position[2] = pos.z;

                        snapshot.Rotation[0] = rot.x;
                        snapshot.Rotation[1] = rot.y;
                        snapshot.Rotation[2] = rot.z;
                        snapshot.Rotation[3] = rot.w;
                        lastPosition = pos;
                        lastRotation = rot;
                    }
                    snapshot.Engagements = new List<EngagementEvent>(DirtyEngagements.Count);
                    for (int i = 0; i < DirtyEngagements.Count; i++)
                    {
                        DirtyEngagements[i].EngagementTime += timeSinceLastCheck;
                        snapshot.Engagements.Add(new EngagementEvent(DirtyEngagements[i]));
                    }
                }
                DirtyEngagements.RemoveAll(delegate (EngagementEvent obj) { return !obj.Active; });
            }
        }

        private static bool HasRegisteredAnyDynamics = false;

        public DynamicObjectSnapshot NewSnapshot()
        {
            //new objectId and manifest entry (if required)
            if (ViewerId == null)
            {
                GenerateDynamicObjectId();
            }

            //create snapshot for this object
            var snapshot = DynamicObjectSnapshot.GetSnapshot(this);

            if (IsVideoPlayer)
            {
                if (!VideoPlayer.isPrepared)
                {
                    snapshot.SetProperty("videoisbuffer", true);
                    wasBufferingVideo = true;
                }
            }
            NewSnapshotQueue.Enqueue(snapshot);

            return snapshot;
        }

        //this should probably be static
        void GenerateDynamicObjectId()
        {
            if (!UseCustomId)
            {
                DynamicObjectId recycledId = ObjectIds.Find(x => !x.Used && x.MeshName == MeshName);

                //do not allow video players to recycle ids - could point to different urls, making the manifest invalid
                //could allow sharing objectids if the url target is the same, but that's not stored in the objectid - need to read objectid from manifest

                if (recycledId != null && !IsVideoPlayer)
                {
                    viewerId = recycledId;
                    viewerId.Used = true;
                    //id is already on manifest
                }
                else
                {
                    viewerId = GetUniqueID(MeshName, this);
                    var manifestEntry = new DynamicObjectManifestEntry(viewerId.Id, gameObject.name, MeshName);
                    if (!string.IsNullOrEmpty(GroupName))
                    {
                        manifestEntry.Properties = new Dictionary<string, object>() { { "groupname", GroupName } };
                    }

                    if (string.Compare(MeshName, "vivecontroller", true) == 0)
                    {
                        string controllerName = "left";
                        if (_t == CognitiveVR_Manager.GetController(true))
                        {
                            controllerName = "right";
                        }
                        else if (_t == CognitiveVR_Manager.GetController(false))
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
                    else if (string.Compare(MeshName, "oculustouchleft", true) == 0)
                    {
                        if (manifestEntry.Properties == null)
                        {
                            manifestEntry.Properties = new Dictionary<string, object>() { { "controller", "left" } };
                        }
                        else
                        {
                            manifestEntry.Properties.Add("controller", "left");
                        }
                    }
                    else if (string.Compare(MeshName, "oculustouchright", true) == 0)
                    {
                        if (manifestEntry.Properties == null)
                        {
                            manifestEntry.Properties = new Dictionary<string, object>() { { "controller", "right" } };
                        }
                        else
                        {
                            manifestEntry.Properties.Add("controller", "right");
                        }
                    }

                    if (!string.IsNullOrEmpty(ExternalVideoSource))
                    {
                        manifestEntry.videoURL = ExternalVideoSource;
                        manifestEntry.videoFlipped = FlipVideo;
                    }

                    ObjectIds.Add(viewerId);
                    NewObjectManifestQueue.Enqueue(manifestEntry);

                    if ((NewObjectManifestQueue.Count + NewSnapshotQueue.Count) > CognitiveVR_Preferences.S_DynamicSnapshotCount)
                    {
                        bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.Instance.DynamicSnapshotMinTimer > Time.realtimeSinceStartup;
                        bool withinExtremeBatchSize = NewObjectManifestQueue.Count + NewSnapshotQueue.Count < CognitiveVR_Preferences.Instance.DynamicExtremeSnapshotCount;

                        //within last send interval and less than extreme count
                        if (withinMinTimer && withinExtremeBatchSize)
                        {
                            return;
                        }
                        lastSendTime = Time.realtimeSinceStartup;
                        CognitiveVR_Manager.Instance.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
                    }
                }
            }
            else
            {
                viewerId = new DynamicObjectId(CustomId, MeshName, this);
                var manifestEntry = new DynamicObjectManifestEntry(viewerId.Id, gameObject.name, MeshName);

                if (string.Compare(MeshName, "vivecontroller", true) == 0)
                {
                    string controllerName = "left";
                    if (_t == CognitiveVR_Manager.GetController(true) || name.Contains("right"))
                    {
                        controllerName = "right";
                    }
                    else if (_t == CognitiveVR_Manager.GetController(false) || name.Contains("left"))
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
                else if (string.Compare(MeshName, "oculustouchleft", true) == 0)
                {
                    if (manifestEntry.Properties == null)
                    {
                        manifestEntry.Properties = new Dictionary<string, object>() { { "controller", "left" } };
                    }
                    else
                    {
                        manifestEntry.Properties.Add("controller", "left");
                    }
                }
                else if (string.Compare(MeshName, "oculustouchright", true) == 0)
                {
                    if (manifestEntry.Properties == null)
                    {
                        manifestEntry.Properties = new Dictionary<string, object>() { { "controller", "right" } };
                    }
                    else
                    {
                        manifestEntry.Properties.Add("controller", "right");
                    }
                }

                if (!string.IsNullOrEmpty(GroupName))
                {
                    manifestEntry.Properties = new Dictionary<string, object>() { { "groupname", GroupName } };
                }
                if (!string.IsNullOrEmpty(ExternalVideoSource))
                {
                    manifestEntry.videoURL = ExternalVideoSource;
                    manifestEntry.videoFlipped = FlipVideo;
                    IsVideoPlayer = true;
                }
                ObjectIds.Add(viewerId);
                NewObjectManifestQueue.Enqueue(manifestEntry);
                if ((NewObjectManifestQueue.Count + NewSnapshotQueue.Count) > CognitiveVR_Preferences.S_DynamicSnapshotCount)
                {
                    bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.Instance.DynamicSnapshotMinTimer > Time.realtimeSinceStartup;
                    bool withinExtremeBatchSize = NewObjectManifestQueue.Count + NewSnapshotQueue.Count < CognitiveVR_Preferences.Instance.DynamicExtremeSnapshotCount;

                    //within last send interval and less than extreme count
                    if (withinMinTimer && withinExtremeBatchSize)
                    {
                        return;
                    }
                    lastSendTime = Time.realtimeSinceStartup;
                    CognitiveVR_Manager.Instance.StartCoroutine(Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
                }
            }

            if (IsVideoPlayer)
            {
                CognitiveVR_Manager.UpdateEvent -= VideoPlayer_Update;
                CognitiveVR_Manager.UpdateEvent += VideoPlayer_Update;
            }
            if (!HasRegisteredAnyDynamics)
            {
                HasRegisteredAnyDynamics = true;
                CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_Update;
                Core.OnSendData += Core_OnSendData;
                CognitiveVR_Manager.Instance.StartCoroutine(AutomaticSendTimer());

                /*for (int i = 0; i < CognitiveVR_Preferences.Instance.DynamicExtremeSnapshotCount; i++)
                {
                    DynamicObjectSnapshot.SnapshotPool.Enqueue(new DynamicObjectSnapshot());
                }*/

            }
        }

        //update on instance of dynamic game obejct
        //only used when dynamic object is written to manifest as a video player
        private void VideoPlayer_Update()
        {
            if (VideoPlayer.isPlaying != wasPlayingVideo)
            {
                if (VideoPlayer.frameRate == 0)
                {
                    //hasn't actually loaded anything yet
                    return;
                }

                SendVideoTime().SetProperty("videoplay", VideoPlayer.isPlaying);
                wasPlayingVideo = VideoPlayer.isPlaying;
            }
        }

        public void UpdateLastPositions()
        {
            lastPosition = _t.position;
            lastRotation = _t.rotation;
        }

        public void UpdateLastPositions(Vector3 pos, Quaternion rot)
        {
            lastPosition = pos;
            lastRotation = rot;
        }

        /// <summary>
        /// used to generate a new unique dynamic object ids at runtime
        /// </summary>
        /// <param name="MeshName"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static DynamicObjectId GetUniqueID(string MeshName, DynamicObject target)
        {
            DynamicObjectId usedObjectId = null;
            while (true) //keep going through objectid list until a new one is reached
            {
                currentUniqueId++;
                usedObjectId = ObjectIds.Find(delegate (DynamicObjectId obj)
                {
                    return obj.Id == "runtime_" + (currentUniqueId + uniqueIdOffset).ToString();
                });
                if (usedObjectId == null)
                {
                    break; //break once we have a currentuniqueid that isn't in objectid list
                }
            }
            return new DynamicObjectId("runtime_" + (currentUniqueId + uniqueIdOffset).ToString(), MeshName, target);
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

            System.Text.StringBuilder sendSnapshotBuilder = new System.Text.StringBuilder(256*CognitiveVR_Preferences.Instance.DynamicExtremeSnapshotCount + 8000);

            //lastSendTime = Time.realtimeSinceStartup;

            sendSnapshotBuilder.Append("{");

            //header
            JsonUtil.SetString("userid", uniqueid, sendSnapshotBuilder);
            sendSnapshotBuilder.Append(",");

            if (!string.IsNullOrEmpty(CognitiveVR_Preferences.LobbyId))
            {
                JsonUtil.SetString("lobbyId", CognitiveVR_Preferences.LobbyId, sendSnapshotBuilder);
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

            string url = Constants.POSTDYNAMICDATA(trackingsettings.SceneId, trackingsettings.VersionNumber);

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

            if (!string.IsNullOrEmpty(entry.videoURL))
            {
                builder.Append(",");
                JsonUtil.SetString("externalVideoSource", entry.videoURL, builder);
                builder.Append(",");
                JsonUtil.SetObject("flipVideo", entry.videoFlipped, builder);
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
            builder.Append("{");

            JsonUtil.SetString("id", snap.Id, builder);
            builder.Append(",");
            JsonUtil.SetDouble("time", snap.Timestamp, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", snap.Position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", snap.Rotation, builder);


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
            if (snap.Engagements != null)
            {
                if (snap.Engagements.Count > 0)
                {
                    builder.Append(",");
                    builder.Append("\"engagements\":[");

                    for (int i = 0; i < snap.Engagements.Count; i++)
                    {
                        builder.Append("{\"engagementtype\":\"");
                        builder.Append(snap.Engagements[i].EngagementType);
                        builder.Append("\",");

                        if (snap.Engagements[i].Parent != "-1")
                        {
                            builder.Append("\"engagementparent\":\"");
                            builder.Append(snap.Engagements[i].Parent);
                            builder.Append("\",");
                        }
                        builder.Append("\"engagement_time\":");
                        builder.Append(snap.Engagements[i].EngagementTime);
                        builder.Append(",");

                        builder.Append("\"engagement_count\":");
                        builder.Append(snap.Engagements[i].EngagementNumber);
                        builder.Append("},");
                    }
                    builder.Remove(builder.Length - 1, 1); //remove last comma
                    builder.Append("]");
                }
            }

            builder.Append("}"); //close object snapshot

            return builder.ToString();
        }

        void OnDisable()
        {
            if (CognitiveVR_Manager.IsQuitting) { return; }
            if (!Application.isPlaying) { return; }
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            CognitiveVR_Manager.LevelLoadedEvent -= CognitiveVR_Manager_LevelLoadedEvent;
            if (IsVideoPlayer)
            {
                VideoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
                CognitiveVR_Manager.UpdateEvent -= VideoPlayer_Update;
                VideoPlayer.loopPointReached -= VideoPlayer_loopPointReached;
            }
            registeredToEvents = false;
            if (string.IsNullOrEmpty(Core.TrackingSceneId)) { return; }
            if (!ReleaseIdOnDisable)
            {
                //don't release id to be used again. makes sure tracked gaze on this will be unique
                NewSnapshot().UpdateTransform().SetEnabled(false);
                new CustomEvent("cvr.objectgaze").SetProperty("object name", gameObject.name).SetProperty("duration", TotalGazeDuration).Send();
                TotalGazeDuration = 0; //reset to not send OnDestroy event
                ViewerId = null;
                return;
            }
            if (CognitiveVR_Manager.Instance != null)
            {
                NewSnapshot().UpdateTransform().SetEnabled(false).ReleaseUniqueId();
            }
        }

        //destroyed, scene unloaded or quit. also called when disabled then destroyed
        void OnDestroy()
        {
            if (TotalGazeDuration > 0)
            {
                new CustomEvent("cvr.objectgaze").SetProperty("object name", gameObject.name).SetProperty("duration", TotalGazeDuration).Send();
                TotalGazeDuration = 0; //reset to not send OnDestroy event
            }

            if (CognitiveVR_Manager.IsQuitting) { return; }
            if (!Application.isPlaying) { return; }
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            CognitiveVR_Manager.LevelLoadedEvent -= CognitiveVR_Manager_LevelLoadedEvent;
            if (string.IsNullOrEmpty(Core.TrackingSceneId)) { return; }
            if (IsVideoPlayer)
            {
                VideoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
                CognitiveVR_Manager.UpdateEvent -= VideoPlayer_Update;
                VideoPlayer.loopPointReached -= VideoPlayer_loopPointReached;
            }
            if (!ReleaseIdOnDestroy)
            {
                //NewSnapshot().SetEnabled(false); //already has a enabled=false snapshot from OnDisable
                return;
            }
            if (CognitiveVR_Manager.Instance != null && viewerId != null) //creates another snapshot to destroy an already probably disabled thing
            {
                NewSnapshot().UpdateTransform().ReleaseUniqueId();
            }
        }

#if UNITY_EDITOR
        public bool HasCollider()
        {
            if (TrackGaze)
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
                else
                {
                    var collider = GetComponent<Collider>();
                    if (collider == null)
                    {
                        return false;
                    }
                    return true;
                }
            }
            return true;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward);
        }

        /// <summary>
        /// parentDynamicObjectId is optional but recommended. it will use a dynamic object id to identify what is engaging with this object - likely a controller
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="parentDynamicObjectId"></param>
        public void BeginEngagement(string engagementName = "default", string parentDynamicObjectId = "-1")
        {
            if (DirtyEngagements == null)
            {
                DirtyEngagements = new List<EngagementEvent>();
            }
            if (Engagements == null)
            {
                Engagements = new List<EngagementEvent>();
            }

            var previousEngagementsOfType = Engagements.FindAll(delegate (EngagementEvent obj)
            {
                return obj.EngagementType == engagementName;
            });

            EngagementEvent newEngagement = new EngagementEvent(engagementName, parentDynamicObjectId, previousEngagementsOfType.Count + 1);

            DirtyEngagements.Add(newEngagement);
            Engagements.Add(newEngagement);
        }

        /// <summary>
        /// parentDynamicObjectId is optional. it is used to identify an engagement if there are multiple engagements with the same type occuring
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="parentDynamicObjectId"></param>
        public void EndEngagement(string engagementName = "default", string parentDynamicObjectId = "-1")
        {
            if (DirtyEngagements == null)
            {
                DirtyEngagements = new List<EngagementEvent>();
            }
            if (Engagements == null)
            {
                Engagements = new List<EngagementEvent>();
            }

            var type = DirtyEngagements.Find(delegate (EngagementEvent obj)
            {
                return obj.EngagementType == engagementName && obj.Active && (obj.Parent == parentDynamicObjectId || parentDynamicObjectId == "-1");
            });

            if (type != null)
            {
                type.Active = false;
            }
            else
            {
                var previousEngagementsOfType = Engagements.FindAll(delegate (EngagementEvent obj)
                {
                    return obj.EngagementType == engagementName;
                });
                EngagementEvent newEngagement = new EngagementEvent(engagementName, parentDynamicObjectId, previousEngagementsOfType.Count + 1);
                newEngagement.Active = false;
                DirtyEngagements.Add(newEngagement);
                Engagements.Add(newEngagement);
            }
        }
    }

    public class DynamicObjectSnapshot
    {
        //public static Queue<DynamicObjectSnapshot> SnapshotPool = new Queue<DynamicObjectSnapshot>();

        public DynamicObjectSnapshot Copy()
        {
            var dyn = GetSnapshot(Dynamic);
            dyn.Timestamp = Timestamp;
            dyn.Id = Id;
            dyn.Position = Position;
            dyn.Rotation = Rotation;
            dyn.Dynamic = null;

            if (Buttons != null)
            {
                dyn.Buttons = new Dictionary<string, ButtonState>(Buttons.Count);
                foreach(var v in Buttons)
                {
                    dyn.Buttons.Add(v.Key, new ButtonState(v.Value));
                }
            }
            if (Engagements != null)
            {
                dyn.Engagements = new List<DynamicObject.EngagementEvent>(Engagements.Count);
                foreach (var v in Engagements)
                {
                    dyn.Engagements.Add(new DynamicObject.EngagementEvent(v));
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
            Dynamic = null;
            Properties = null;
            Buttons = null;
            Engagements = null;
            //SnapshotPool.Enqueue(this);
        }

        public static DynamicObjectSnapshot GetSnapshot(DynamicObject dynamic)
        {
            return new DynamicObjectSnapshot(dynamic);

            /*if (SnapshotPool.Count > 0)
            {
                DynamicObjectSnapshot dos = SnapshotPool.Dequeue();
                if (dos == null)
                {
                    dos = new DynamicObjectSnapshot(dynamic);
                }
                dos.Dynamic = dynamic;
                dos.Id = dynamic.Id;
                dos.Timestamp = Util.Timestamp(CognitiveVR_Manager.frameCount);
                return dos;
            }
            else
            {
                return new DynamicObjectSnapshot(dynamic);
            }*/
        }

        public DynamicObject Dynamic;
        public string Id;
        public Dictionary<string, object> Properties;
        public Dictionary<string, ButtonState> Buttons;
        public List<DynamicObject.EngagementEvent> Engagements;
        public float[] Position = new float[3] { 0, 0, 0 };
        public float[] Rotation = new float[4] { 0, 0, 0, 1 };
        public double Timestamp;

        public DynamicObjectSnapshot(DynamicObject dynamic)
        {
            this.Dynamic = dynamic;
            Id = dynamic.Id;
            Timestamp = Util.Timestamp(CognitiveVR_Manager.frameCount);
        }

        public DynamicObjectSnapshot()
        {
            //empty. only used to fill the pool
        }

        private DynamicObjectSnapshot(DynamicObject dynamic, Vector3 pos, Quaternion rot, Dictionary<string, object> props = null)
        {
            this.Dynamic = dynamic;
            Id = dynamic.Id;
            Properties = props;

            Position[0] = pos.x;
            Position[1] = pos.y;
            Position[2] = pos.z;

            Rotation[0] = rot.x;
            Rotation[1] = rot.y;
            Rotation[2] = rot.z;
            Rotation[3] = rot.w;

            Timestamp = Util.Timestamp(CognitiveVR_Manager.frameCount);
        }

        private DynamicObjectSnapshot(DynamicObject dynamic, float[] pos, float[] rot, Dictionary<string, object> props = null)
        {
            this.Dynamic = dynamic;
            Id = dynamic.Id;
            Properties = props;
            Position = pos;

            Rotation = rot;
            Timestamp = Util.Timestamp(CognitiveVR_Manager.frameCount);
        }

        /// <summary>
        /// Add the position and rotation to the snapshot, even if the dynamic object hasn't moved beyond its threshold
        /// </summary>
        public DynamicObjectSnapshot UpdateTransform()
        {
            //TODO allow using cached _t transform with some compiler flag. default to defensive code rather than faster code
            Vector3 pos = Dynamic._t.position;
            Quaternion rot = Dynamic._t.rotation;

            Position[0] = pos.x;
            Position[1] = pos.y;
            Position[2] = pos.z;

            Rotation[0] = rot.x;
            Rotation[1] = rot.y;
            Rotation[2] = rot.z;
            Rotation[3] = rot.w;

            Dynamic.UpdateLastPositions(pos, rot);

            return this;
        }

        /// <summary>
        /// Add the position and rotation to the snapshot, even if the dynamic object hasn't moved beyond its threshold
        /// </summary>
        public DynamicObjectSnapshot UpdateTransform(Vector3 pos, Quaternion rot)
        {
            Position[0] = pos.x;
            Position[1] = pos.y;
            Position[2] = pos.z;

            Rotation[0] = rot.x;
            Rotation[1] = rot.y;
            Rotation[2] = rot.z;
            Rotation[3] = rot.w;

            Dynamic.lastPosition = pos;
            Dynamic.lastRotation = rot;

            return this;
        }

        //TODO this shouldn't be part of a dynamic obejct snapshot!

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
                    CognitiveVR_Manager.TickEvent -= Dynamic.CognitiveVR_Manager_TickEvent;
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

        //TODO this shouldn't set the dynamic object viewer!
        //releasing an id allows a new object with the same mesh to be used instead of bloating the object manifest
        //also sets the dynamic object to be disabled
        public DynamicObjectSnapshot ReleaseUniqueId()
        {
            var foundId = DynamicObject.ObjectIds.Find(x => x.Id == this.Id);

            if (foundId != null)
            {
                foundId.Used = false;
            }
            this.Dynamic.ViewerId = null;
            return this;
        }
    }

    /// <summary>
    /// <para>holds info about which ids are used and what meshes they are held by</para> 
    /// <para>used to 'release' unique ids so meshes can be pooled in scene explorer</para> 
    /// </summary>
    public class DynamicObjectId
    {
        public string Id;
        public bool Used = true;
        public string MeshName;
        public DynamicObject Target;

        public DynamicObjectId(string id, string meshName, DynamicObject target)
        {
            this.Id = id;
            this.MeshName = meshName;
            Target = target;
        }
    }

    public class DynamicObjectManifestEntry
    {
        public string Id;
        public string Name;
        public string MeshName;
        public Dictionary<string, object> Properties;
        public string videoURL;
        public bool videoFlipped;

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