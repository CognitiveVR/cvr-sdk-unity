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


//write DynamicRepresentation struct
//id
//position
//rotation

//list of basic objects
//list of objects with properties
//list of video objects
//list of controller objects

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
            VideoSphereCubemap
        }

        [HideInInspector]
        public Transform _t;

        public bool SnapshotOnEnable = true;
        public bool UpdateTicksOnEnable = true;

        //[Header("Thresholds")]
        public float PositionThreshold = 0.001f;
        public Vector3 lastPosition;
        public float RotationThreshold = 0.1f;
        public Quaternion lastRotation;

        //[Header("IDs")]
        public bool UseCustomId = true;
        public string CustomId = "";
        public bool ReleaseIdOnDestroy = true; //only release the id for reuse if not tracking gaze
        public bool ReleaseIdOnDisable = true; //only release the id for reuse if not tracking gaze

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

        //[Header("Updates")]
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

#if CVR_STEAMVR
        //used to append changes in button states to snapshots
        private DynamicObjectButtonStates ButtonStates = null;
#endif

        //engagement name, engagement event. cleared when snapshots sent
        List<EngagementEvent> DirtyEngagements = null;

        //engagement name, engagement event
        List<EngagementEvent> Engagements = null;

        //each engagement event
        //public List<EngagementEvent> Engagements;
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
        //public static HashSet<DynamicObjectId> ObjectIdsHash = new HashSet<DynamicObjectId>();
        
        ///don't recycle object ids between scenes - otherwise ids wont be written into new scene's manifest
        ///disconnect all the objectids from dynamics. they will make new objectids in the scene when they write a new snapshot
        public static void ClearObjectIds()
        {
            foreach(var v in ObjectIds)
            {
                if (v == null) { continue; }
                if (v.Target == null) { continue; }
                v.Target.ViewerId = null;
            }

            //Util.logDebug("========================clear object ids");

            ObjectIds.Clear();
        }

        //cumulative. all objects.
        //public static List<DynamicObjectManifestEntry> ObjectManifest = new List<DynamicObjectManifestEntry>();
        //public static Dictionary<int, DynamicObjectManifestEntry> ObjectManifestDict = new Dictionary<int, DynamicObjectManifestEntry>();

        //new until they are packaged into json
        //public static List<DynamicObjectSnapshot> NewSnapshots = new List<DynamicObjectSnapshot>();
        //private static List<DynamicObjectManifestEntry> NewObjectManifest = new List<DynamicObjectManifestEntry>();

        private static Queue<DynamicObjectSnapshot> NewSnapshotQueue = new Queue<DynamicObjectSnapshot>();
        private static Queue<DynamicObjectManifestEntry> NewObjectManifestQueue = new Queue<DynamicObjectManifestEntry>();

        //private static List<string> savedDynamicManifest = new List<string>();
       // private static List<string> savedDynamicSnapshots = new List<string>();
        //private static int maxSnapshotBatchCount = 64;
        private static int jsonpart = 1;

        public UnityEngine.Video.VideoPlayer VideoPlayer;
        bool IsVideoPlayer;

        bool registeredToEvents = false;

        /// <summary>
        /// called on enable and after scene load
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

            //PositionThresholdSqr = Mathf.Pow(PositionThreshold, 2);
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
                return;

            if (CognitiveVR_Manager.InitResponse == Error.Success)
            {
                CognitiveVR_Manager_InitEvent(Error.Success);
            }
            //else if (CognitiveVR_Manager.InitResponse == Error.NotInitialized)
            //{
            //    CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            //    CognitiveVR_Manager.InitEvent += CognitiveVR_Manager_InitEvent;
            //}

            NewSnapshot().UpdateTransform().SetEnabled(true);
            //DynamicObjectSnapshot enableSnapshot = NewSnapshot().UpdateTransform().SetEnabled(true);
            //if (SnapshotOnEnable)
            //{
            //    if (CognitiveVR_Manager.Instance != null)
            //    {
            //        enableSnapshot = NewSnapshot().UpdateTransform().SetEnabled(true);
            //    }
            //}

            if (UpdateTicksOnEnable || IsVideoPlayer)
            {
                //if (enableSnapshot == null)
                //{
                    
                    
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
                //}
                //else
                //{
                //    enableSnapshot.SetTick(true);
                //}
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

        //level loaded. also called when cognitive manager first initialized, to make sure onenable registers everything correctly
        private void CognitiveVR_Manager_LevelLoadedEvent()
        {
            //CognitiveVR_Manager.LevelLoadedEvent -= CognitiveVR_Manager_LevelLoadedEvent;
            //StopAllCoroutines();
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

            if (CommonMesh != CommonDynamicMesh.ViveController) { return; }
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
                    NewSnapshotQueue.Clear();
                    NewObjectManifestQueue.Clear();
                }
                return;
            }

            //only need this because dynamic objects don't have a clear 'send' function
            //queue
            if ((NewObjectManifestQueue.Count + NewSnapshotQueue.Count) > CognitiveVR_Preferences.S_DynamicSnapshotCount)
            {
                CognitiveVR_Manager.Instance.StartCoroutine(CognitiveVR_Manager.Instance.Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene,Core.UniqueID,Core.SessionTimeStamp,Core.SessionID));
            }
        }

        public void OnGaze(float time)
        {
            if (!TrackGaze){ return; }
            TotalGazeDuration += time;
        }

        //float PositionThresholdSqr;
        /// <summary>
        /// send a snapshot of the position and rotation if the object has moved beyond its threshold
        /// </summary>
        public void CheckUpdate(float timeSinceLastCheck)
        {
            if (!Core.Initialized) { return; }
            if (string.IsNullOrEmpty(Core.TrackingSceneId)) { return; }

            //it might actually be slower to check the threshold than to just write a snapshot
            //what this does well is reduce the network bandwidth, though

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
                snapshot = NewSnapshot();//.UpdateTransform(pos,rot);
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
                        snapshot = NewSnapshot();//.UpdateTransform();
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
            bool needObjectId = false;
            //add object to manifest and set ObjectId
            if (ViewerId == null)
            {
                needObjectId = true;
            }
            else
            {
                /*if (!ObjectManifestDict.ContainsKey(ObjectId.Id))
                {
                    needObjectId = true;
                }*/

                /*var manifestEntry = ObjectManifest.Find(x => x.Id == ObjectId.Id);
                if (manifestEntry == null)
                {
                    needObjectId = true;
                }*/
            }

            //new objectId and manifest entry (if required)
            if (needObjectId)
            {
                GenerateDynamicObjectId();
            }

            //create snapshot for this object
            //var snapshot = new DynamicObjectSnapshot(this);
            var snapshot = DynamicObjectSnapshot.GetSnapshot(this);
#if CVR_STEAMVR
            if (ButtonStates != null)
            {
                //snapshot.Buttons = ButtonStates.GetDirtyStates();
                snapshot.Buttons = new Dictionary<string, DynamicObjectButtonStates.ButtonState>();
                var dirtyButtonStates = ButtonStates.GetDirtyStates();
                foreach (var v in dirtyButtonStates)
                {
                    snapshot.Buttons.Add(v.Key, v.Value);
                }
            }
#endif
            //            if (DirtyEngagements != null)
            //            {
            //                if (DirtyEngagements.Count > 0)
            //                {
            //                    snapshot.Engagements = new List<EngagementEvent>(DirtyEngagements);
            //                }
            //                DirtyEngagements.RemoveAll(delegate (EngagementEvent obj) { return !obj.Active; });
            //            }

            if (IsVideoPlayer)
            {
                if (!VideoPlayer.isPrepared)
                {
                    snapshot.SetProperty("videoisbuffer", true);
                    wasBufferingVideo = true;
                }
            }
            //NewSnapshots.Add(snapshot);
            NewSnapshotQueue.Enqueue(snapshot);

            return snapshot;
        }

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

                    if (!string.IsNullOrEmpty(ExternalVideoSource))
                    {
                        manifestEntry.videoURL = ExternalVideoSource;
                        manifestEntry.videoFlipped = FlipVideo;
                    }

                    ObjectIds.Add(viewerId);
                    //ObjectManifest.Add(manifestEntry);
                    //ObjectManifestDict.Add(manifestEntry.Id, manifestEntry);
                    //NewObjectManifest.Add(manifestEntry);
                    NewObjectManifestQueue.Enqueue(manifestEntry);

                    if ((NewObjectManifestQueue.Count + NewSnapshotQueue.Count) > CognitiveVR_Preferences.S_DynamicSnapshotCount)
                    {
                        CognitiveVR_Manager.Instance.StartCoroutine(CognitiveVR_Manager.Instance.Thread_StringThenSend(NewObjectManifestQueue, NewSnapshotQueue, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID));
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
                //ObjectManifest.Add(manifestEntry);
                //ObjectManifestDict.Add(manifestEntry.Id, manifestEntry);
                //NewObjectManifest.Add(manifestEntry);
                NewObjectManifestQueue.Enqueue(manifestEntry);
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

                //NewSnapshot().SetProperty("videoplay", VideoPlayer.isPlaying).SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
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

        //from SendDataEvent. either manual 'send all data' or onquit
        static void Core_OnSendData()
        {
            //WriteAllSnapshots();

            List<string> savedDynamicManifest = new List<string>();
            List<string> savedDynamicSnapshots = new List<string>();

            DynamicObjectSnapshot snap = null;
            //write new dynamic object snapshots to strings
            while (NewSnapshotQueue.Count > 0)
            //for (int i = 0; i < NewSnapshotQueue.Count; i++)
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
                    SendSavedSnapshotsForce(true,savedDynamicManifest,savedDynamicSnapshots, Core.TrackingScene,Core.UniqueID,Core.SessionTimeStamp,Core.SessionID);
                    savedDynamicManifest.Clear();
                    savedDynamicSnapshots.Clear();
                }
            }
            //if (NewSnapshotQueue.Count > 0)
                //NewSnapshotQueue.Clear();

            DynamicObjectManifestEntry entry = null;
            while (NewObjectManifestQueue.Count > 0)
            //for (int i = 0; i < NewObjectManifestQueue.Count; i++)
            {
                entry = NewObjectManifestQueue.Dequeue();
                if (entry == null) { continue; }
                savedDynamicManifest.Add(SetManifestEntry(entry));
                //entry = null;
                if (savedDynamicSnapshots.Count + savedDynamicManifest.Count >= CognitiveVR_Preferences.S_DynamicSnapshotCount)
                {
                    SendSavedSnapshotsForce(true, savedDynamicManifest, savedDynamicSnapshots, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID);
                    savedDynamicManifest.Clear();
                    savedDynamicSnapshots.Clear();
                }
            }
            //if (NewObjectManifestQueue.Count > 0)
                //NewObjectManifestQueue.Clear();

            SendSavedSnapshotsForce(true, savedDynamicManifest, savedDynamicSnapshots, Core.TrackingScene, Core.UniqueID, Core.SessionTimeStamp, Core.SessionID);
        }

        //from thread
        public static void SendSavedSnapshots(List<string> stringEntries, List<string> stringSnapshots, CognitiveVR_Preferences.SceneSettings trackingsettings, string uniqueid, double sessiontimestamp, string sessionid)
        {
            if (stringEntries.Count == 0 && stringSnapshots.Count == 0) { return; }

            System.Text.StringBuilder sendSnapshotBuilder = new System.Text.StringBuilder();

            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                CognitiveVR.Util.logError("SceneId is empty. Do not send Dynamic Objects to SceneExplorer");
                /*for (int i = 0; i < NewSnapshots.Count; i++)
                {
                    NewSnapshots[i].ReturnToPool();
                }*/
                //NewSnapshots.Clear();
                NewSnapshotQueue.Clear();
                //NewObjectManifest.Clear();
                NewObjectManifestQueue.Clear();
                //savedDynamicManifest.Clear();
                //savedDynamicSnapshots.Clear();
                sendSnapshotBuilder.Length = 0;
                return;
            }

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

            //if (CognitiveVR_Manager.Instance.isActiveAndEnabled)
            {
                CognitiveVR.NetworkManager.Post(url, content);
            }
        }

        //called immediately if event with dynamic has been recorded, then cognitive manager initializes

        /// <summary>
        /// it is recommended that you use PlayerRecorder.SendData instead. that will send all outstanding data
        /// </summary>
        public static void SendSavedSnapshotsForce(bool forceSend, List<string> stringEntries, List<string> stringSnapshots, CognitiveVR_Preferences.SceneSettings trackingsettings, string uniqueid, double sessiontimestamp, string sessionid)
        {
            //put all this into http request
            if (stringEntries.Count == 0 && stringSnapshots.Count == 0) { Util.logWarning("DynamicObject SendSavedSnapshotsForce - no string entries or snapshots"); return; }

            //redundant? checked when writing snapshots to string
            //CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR.CognitiveVR_Preferences.FindTrackingScene();
            if (trackingsettings == null)
            {
                CognitiveVR.Util.logError("DynamicObject send snapshots. scene settings are null " + Core.TrackingSceneName);
                int count = NewSnapshotQueue.Count;
                for (int i = 0; i< count; i++)
                {
                    NewSnapshotQueue.Dequeue().ReturnToPool();
                    //NewSnapshots[i].ReturnToPool();
                }
                //NewSnapshots.Clear();
                NewSnapshotQueue.Clear();
                //NewObjectManifest.Clear();
                NewObjectManifestQueue.Clear();
                //savedDynamicManifest.Clear();
                //savedDynamicSnapshots.Clear();
                return;
            }
            if (string.IsNullOrEmpty(trackingsettings.SceneId))
            {
                CognitiveVR.Util.logError("SceneId is empty. Do not send Dynamic Objects to SceneExplorer");
                int count = NewSnapshotQueue.Count;
                for (int i = 0; i < count; i++)
                {
                    NewSnapshotQueue.Dequeue().ReturnToPool();
                    //NewSnapshots[i].ReturnToPool();
                }
                //NewSnapshots.Clear();
                NewSnapshotQueue.Clear();
                //NewObjectManifest.Clear();
                NewObjectManifestQueue.Clear();
                //savedDynamicManifest.Clear();
                //savedDynamicSnapshots.Clear();
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(512);

            builder.Append("{");

            //header
            JsonUtil.SetString("userid", uniqueid, builder);
            builder.Append(",");
            if (!string.IsNullOrEmpty(CognitiveVR_Preferences.LobbyId))
            {
                JsonUtil.SetString("lobbyId", CognitiveVR_Preferences.LobbyId, builder);
                builder.Append(",");
            }
            JsonUtil.SetDouble("timestamp", (int)sessiontimestamp, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionid", sessionid, builder);
            builder.Append(",");
            JsonUtil.SetInt("part", jsonpart, builder);
            builder.Append(",");
            jsonpart++;

            JsonUtil.SetString("formatversion", "1.0", builder);
            builder.Append(",");

            //format all the savedmanifest entries

            if (stringEntries.Count > 0)
            {
                //manifest
                builder.Append("\"manifest\":{");
                for (int i = 0; i < stringEntries.Count; i++)
                {
                    builder.Append(stringEntries[i]);
                    builder.Append(",");
                }
                if (stringEntries.Count > 0)
                {
                    builder.Remove(builder.Length - 1, 1);
                }
                builder.Append("},");
            }

            if (stringSnapshots.Count > 0)
            {
                //snapshots
                builder.Append("\"data\":[");
                for (int i = 0; i < stringSnapshots.Count; i++)
                {
                    builder.Append(stringSnapshots[i]);
                    builder.Append(",");
                }
                if (stringSnapshots.Count > 0)
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

            string url = Constants.POSTDYNAMICDATA(trackingsettings.SceneId, trackingsettings.VersionNumber);

            string content = builder.ToString();

            if (CognitiveVR_Manager.Instance.isActiveAndEnabled)
            {
                CognitiveVR.NetworkManager.Post(url, content);
            }
        }

        public static string SetManifestEntry(DynamicObjectManifestEntry entry)
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

        public static string SetSnapshot(DynamicObjectSnapshot snap)
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
                //var dirtyButtons = snap.Dynamic.ButtonStates.GetDirtyStates();

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

                    for(int i = 0; i<snap.Engagements.Count; i++)
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
            if (string.IsNullOrEmpty(Core.TrackingSceneId)) { return; }
            if (IsVideoPlayer)
            {
                VideoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
                CognitiveVR_Manager.UpdateEvent -= VideoPlayer_Update;
                VideoPlayer.loopPointReached -= VideoPlayer_loopPointReached;
            }
            registeredToEvents = false;
            if (!ReleaseIdOnDisable)
            {
                //don't release id to be used again. makes sure tracked gaze on this will be unique
                NewSnapshot().SetEnabled(false);
                new CustomEvent("cvr.objectgaze").SetProperty("object name", gameObject.name).SetProperty("duration", TotalGazeDuration).Send();
                TotalGazeDuration = 0; //reset to not send OnDestroy event
                ViewerId = null;
                return;
            }
            if (CognitiveVR_Manager.Instance != null)
            {
                NewSnapshot().SetEnabled(false).ReleaseUniqueId();
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
                //if (CognitiveVR_Manager.Instance != null)
                //{
                //    NewSnapshot().SetEnabled(false); //already has a enabled=false snapshot from OnDisable
                //}
                return;
            }
            if (CognitiveVR_Manager.Instance != null && viewerId != null) //creates another snapshot to destroy an already probably disabled thing
            {
                NewSnapshot().ReleaseUniqueId();
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

            EngagementEvent newEngagement = new EngagementEvent(engagementName, parentDynamicObjectId, previousEngagementsOfType.Count+1);

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
        public static Queue<DynamicObjectSnapshot> snapshotQueue = new Queue<DynamicObjectSnapshot>();
        //public static List<DynamicObjectSnapshot> snapshotPool = new List<DynamicObjectSnapshot>(128);

        public void ReturnToPool()
        {
            Dynamic = null;
            Properties = null;
            Buttons = null;
            Engagements = null;
            snapshotQueue.Enqueue(this);
        }

        public static DynamicObjectSnapshot GetSnapshot(DynamicObject dynamic)
        {
            if (snapshotQueue.Count > 0)
            {
                DynamicObjectSnapshot dos = snapshotQueue.Dequeue();
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
            }
        }

        public DynamicObject Dynamic;
        public string Id;
        public Dictionary<string, object> Properties;
        public Dictionary<string, DynamicObjectButtonStates.ButtonState> Buttons;
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

            Dynamic.UpdateLastPositions(pos,rot);

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

            //Dynamic.UpdateLastPositions(pos, rot);
            Dynamic.lastPosition = pos;
            Dynamic.lastRotation = rot;

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

        //releasing an id allows a new object with the same mesh to be used instead of bloating the object manifest
        //also sets the dynamic object to be disabled
        public DynamicObjectSnapshot ReleaseUniqueId()
        {
            var foundId = DynamicObject.ObjectIds.Find(x => x.Id == this.Id);
            //var foundId = DynamicObject.ObjectIds.Find(delegate (DynamicObjectId obj) { return obj.Id == this.Id; });

            if (foundId != null)
            {
                foundId.Used = false;
            }
            this.Dynamic.ViewerId = null;
            //this.SetEnabled(false);
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



        /*public override bool Equals(object obj)
        {
            return Id == ((DynamicObjectId)obj).Id;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }*/
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

        public DynamicObjectManifestEntry(string id, string name, string meshName,Dictionary<string,object>props)
        {
            this.Id = id;
            this.Name = name;
            this.MeshName = meshName;
            this.Properties = props;
        }
    }

    //deals with writing new controller inputs into snapshot
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
        int Id;
        //TODO if controller is not present at cognitivevrmanager init, it doesn't initialize input tracking
        public void ButtonStateInit(Transform transform)
        {
            SteamVR_TrackedController controller;
            for (int i = 0; i<2; i++)
            {
                bool right = i == 0 ? true : false;
                if (CognitiveVR_Manager.GetController(right) == null)
                {
                    Util.logDebug("Dynamic Object Controller - Button State Init cannot get "+ (right?"right":"left") + " controller");
                    continue;
                }
                if (CognitiveVR_Manager.GetController(right) != transform)
                {
                    continue;
                }

                controller = CognitiveVR_Manager.GetController(right).GetComponent<SteamVR_TrackedController>();

                if (controller == null)
                {
                    Util.logDebug("------------------Must have a SteamVR_TrackedController component to capture inputs!");
                    continue;
                }
                //controller = CognitiveVR_Manager.GetController(right).gameObject.AddComponent<SteamVR_TrackedController>(); //need to have start called and set controllerindex

                Id = (int)controller.controllerIndex;

                controller.SteamClicked -= Controller_SteamClicked;

                controller.MenuButtonClicked -= Controller_MenuButtonClicked;
                controller.MenuButtonUnclicked -= Controller_MenuButtonUnclicked;

                controller.TriggerClicked -= Controller_TriggerClicked;
                controller.TriggerUnclicked -= Controller_TriggerUnclicked;

                controller.Gripped -= Controller_Gripped;
                controller.Ungripped -= Controller_Ungripped;

                controller.PadTouched -= Controller_PadTouched;
                controller.PadUntouched -= Controller_PadUntouched;

                controller.PadClicked -= Controller_PadClicked;
                controller.PadUnclicked -= Controller_PadUnclicked;


                controller.SteamClicked += Controller_SteamClicked;

                controller.MenuButtonClicked += Controller_MenuButtonClicked;
                controller.MenuButtonUnclicked += Controller_MenuButtonUnclicked;

                controller.TriggerClicked += Controller_TriggerClicked;
                controller.TriggerUnclicked += Controller_TriggerUnclicked;

                controller.Gripped += Controller_Gripped;
                controller.Ungripped += Controller_Ungripped;

                controller.PadTouched += Controller_PadTouched;
                controller.PadUntouched += Controller_PadUntouched;

                controller.PadClicked += Controller_PadClicked;
                controller.PadUnclicked += Controller_PadUnclicked;

                //CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;
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

        private void Controller_TriggerUnclicked(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_trigger"].ButtonPercent = 0;//trigger
        }

        private void Controller_TriggerClicked(object sender, ClickedEventArgs e)
        {
            CurrentStates["vive_trigger"].ButtonPercent = 100;//trigger
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