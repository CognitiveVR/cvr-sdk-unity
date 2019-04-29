using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CognitiveVR
{
    [HelpURL("https://docs.cognitive3d.com/unity/dynamic-objects/")]
    [AddComponentMenu("Cognitive3D/Common/Dynamic Object")]
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

        //original scale, set on enable
        //assuming that this is the scale used to export this mesh and that this should be divided by current scale to get 'relative scale from upload'
        private bool HasSetScale = false;
        [System.NonSerialized]
        public Vector3 StartingScale;

        public float ScaleThreshold = 0.1f;
        Vector3 lastRelativeScale = Vector3.one;

        public bool UseCustomId = true;
        public string CustomId = "";
        public bool ReleaseIdOnDestroy = false; //only release the id for reuse if not tracking gaze
        public bool ReleaseIdOnDisable = false; //only release the id for reuse if not tracking gaze

        public bool IsController = false;
        public string ControllerType;
        public bool IsRight = false;

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

        public bool RequiresManualEnable = false;

        //custom events with a uniqueid + dynamicid
        Dictionary<string, CustomEvent> EngagementsDict;

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

        public UnityEngine.Video.VideoPlayer VideoPlayer;
        bool IsVideoPlayer;

        bool registeredToEvents = false;

#if UNITY_EDITOR
        private void Reset()
        {
            //set name is not set otherwise
            if (UseCustomMesh && string.IsNullOrEmpty(MeshName))
            {
                MeshName = gameObject.name.ToLower().Replace(" ", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_").Replace("?", "_").Replace("*", "_").Replace("\"", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            //set custom id if not set otherwise
            if (UseCustomId && string.IsNullOrEmpty(CustomId))
            {
                string s = System.Guid.NewGuid().ToString();
                CustomId = "editor_" + s;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }
#endif

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
            if (!HasSetScale)
            {
                HasSetScale = true;
                StartingScale = _t.lossyScale;
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

            if (CognitiveVR_Manager.InitResponse == Error.None)
            {
                CognitiveVR_Manager_InitEvent(Error.None);
            }

            NewSnapshot().UpdateTransform(transform.position, transform.rotation).SetEnabled(true);
            lastPosition = transform.position;
            lastRotation = transform.rotation;

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
#if UNITY_EDITOR
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
#endif
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
                NewSnapshot().UpdateTransform(transform.position, transform.rotation).SetProperty("videotime", 0);
                lastPosition = transform.position;
                lastRotation = transform.rotation;
            }
            else
            {
                NewSnapshot().UpdateTransform(transform.position, transform.rotation).SetProperty("videoplay", false).SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
                lastPosition = transform.position;
                lastRotation = transform.rotation;
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

            if (initError != Error.None)
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

        /// <summary>
        /// this coroutine 'ticks' at a different interval than the CognitiveVR_Manager tick event
        /// </summary>
        public IEnumerator UpdateTick()
        {
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;

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
            var snap = NewSnapshot().UpdateTransform(transform.position, transform.rotation).SetProperty("videotime", (int)((VideoPlayer.frame / VideoPlayer.frameRate) * 1000));
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            return snap;
        }

        /// <summary>
        /// send a snapshot of the position and rotation if the object has moved beyond its threshold
        /// </summary>
        public void CheckUpdate(float timeSinceLastCheck)
        {
            if (!Core.IsInitialized) { return; }
            if (string.IsNullOrEmpty(Core.TrackingSceneId)) { return; }

            var lossyScale = _t.lossyScale;
            var pos = _t.position;
            var rot = _t.rotation;
            float scaleX = lossyScale.x / StartingScale.x;
            float scaleY = lossyScale.y / StartingScale.y;
            float scaleZ = lossyScale.z / StartingScale.z;

            Vector3 heading;
            heading.x = pos.x - lastPosition.x;
            heading.y = pos.y - lastPosition.y;
            heading.z = pos.z - lastPosition.z;

            var distanceSquared = heading.x * heading.x + heading.y * heading.y + heading.z * heading.z;

            bool doWrite = false;
            bool writeScale = false;
            if (distanceSquared > PositionThreshold * PositionThreshold)
            {
                doWrite = true;
            }
            if (!doWrite)
            {
                float f = Quaternion.Dot(lastRotation, rot);

                float fabs = System.Math.Abs(f);
                float min = fabs < 1 ? fabs : 1;

                if (System.Math.Acos(min) * 114.59156f > RotationThreshold)
                {
                    doWrite = true;
                }
            }

            float sqrmag = scaleX * lastRelativeScale.x - scaleY * lastRelativeScale.y - scaleZ * lastRelativeScale.z;
            if (sqrmag > ScaleThreshold * ScaleThreshold)
            {
                writeScale = true;
            }

            DynamicObjectSnapshot snapshot = null;
            if (doWrite || writeScale)
            {
                snapshot = NewSnapshot();
                snapshot.posX = pos.x;
                snapshot.posY = pos.y;
                snapshot.posZ = pos.z;

                snapshot.rotX = rot.x;
                snapshot.rotY = rot.y;
                snapshot.rotZ = rot.z;
                snapshot.rotW = rot.w;
                lastPosition = pos;
                lastRotation = rot;
                if (writeScale)
                {
                    snapshot.DirtyScale = true;

                    snapshot.scaleX = scaleX;
                    snapshot.scaleY = scaleY;
                    snapshot.scaleZ = scaleZ;
                    lastRelativeScale.x = scaleX;
                    lastRelativeScale.y = scaleY;
                    lastRelativeScale.z = scaleZ;
                }
            }
        }

        public DynamicObjectSnapshot NewSnapshot()
        {
            //new objectId and manifest entry (if required)
            if (ViewerId == null)
            {
                GenerateDynamicObjectId();
            }

            //create snapshot for this object
            var snapshot = DynamicObjectSnapshot.GetSnapshot(Id);

            if (IsVideoPlayer)
            {
                if (!VideoPlayer.isPrepared)
                {
                    snapshot.SetProperty("videoisbuffer", true);
                    wasBufferingVideo = true;
                }
            }
            DynamicObjectCore.AddSnapshot(snapshot);

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

                    if (IsController)
                    {
                        manifestEntry.isController = true;
                        manifestEntry.controllerType = ControllerType;
                        string controllerName = "left";

                        if (IsRight)
                            controllerName = "right";
                        
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
                    DynamicObjectCore.AddManifestEntry(manifestEntry);
                }
            }
            else
            {
                viewerId = new DynamicObjectId(CustomId, MeshName, this);
                var manifestEntry = new DynamicObjectManifestEntry(viewerId.Id, gameObject.name, MeshName);

                if (IsController)
                {
                    manifestEntry.isController = true;
                    manifestEntry.controllerType = ControllerType;
                    string controllerName = "left";

                    if (IsRight)
                        controllerName = "right";

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
                    IsVideoPlayer = true;
                }
                
                ObjectIds.Add(viewerId);

                DynamicObjectCore.AddManifestEntry(manifestEntry);
            }

            if (IsVideoPlayer)
            {
                CognitiveVR_Manager.UpdateEvent -= VideoPlayer_Update;
                CognitiveVR_Manager.UpdateEvent += VideoPlayer_Update;
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

            if (EngagementsDict != null)
            {
                foreach (var engagement in EngagementsDict)
                { engagement.Value.Send(transform.position); }
                EngagementsDict = null;
            }

            if (!ReleaseIdOnDisable)
            {
                //don't release id to be used again. makes sure tracked gaze on this will be unique
                NewSnapshot().UpdateTransform(transform.position, transform.rotation).SetEnabled(false);
                lastPosition = transform.position;
                lastRotation = transform.rotation;
                ViewerId = null;
                return;
            }
            if (CognitiveVR_Manager.Instance != null)
            {
                NewSnapshot().UpdateTransform(transform.position, transform.rotation).SetEnabled(false);
                lastPosition = transform.position;
                lastRotation = transform.rotation;
                var foundId = DynamicObject.ObjectIds.Find(x => x.Id == this.Id);

                if (foundId != null)
                {
                    foundId.Used = false;
                }
                ViewerId = null;
            }
        }

        //destroyed, scene unloaded or quit. also called when disabled then destroyed
        void OnDestroy()
        {
            if (CognitiveVR_Manager.IsQuitting) { return; }
            if (!Application.isPlaying) { return; }
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
            CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
            CognitiveVR_Manager.LevelLoadedEvent -= CognitiveVR_Manager_LevelLoadedEvent;
            if (string.IsNullOrEmpty(Core.TrackingSceneId)) { return; }

            if (EngagementsDict != null)
            {
                foreach (var engagement in EngagementsDict)
                { engagement.Value.Send(transform.position); }
                EngagementsDict = null;
            }

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
                NewSnapshot().UpdateTransform(transform.position,transform.rotation);
                lastPosition = transform.position;
                lastRotation = transform.rotation;
                var foundId = DynamicObject.ObjectIds.Find(x => x.Id == this.Id);

                if (foundId != null)
                {
                    foundId.Used = false;
                }
                ViewerId = null;
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
        /// begin an engagement on this dynamic object with a name 'engagementName'. if multiple engagements with the same name may be active at once on this dynamic, uniqueEngagementId should be set
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="uniqueEngagementId"></param>
        public void BeginEngagement(string engagementName = "default", string uniqueEngagementId = null, Dictionary<string,object> properties = null)
        {
            if (EngagementsDict == null) EngagementsDict = new Dictionary<string, CustomEvent>();

            if (uniqueEngagementId == null)
            {
                CustomEvent ce = new CustomEvent(engagementName).SetProperties(properties).SetDynamicObject(Id);
                if (!EngagementsDict.ContainsKey(engagementName))
                {
                    EngagementsDict.Add(engagementName, ce);
                }
                else
                {
                    //send old engagement, record this new one
                    EngagementsDict[engagementName].Send(transform.position);
                    EngagementsDict[engagementName] = ce;
                }
            }
            else
            {
                CustomEvent ce = new CustomEvent(engagementName).SetProperties(properties).SetDynamicObject(Id);
                string key = uniqueEngagementId + Id;
                if (!EngagementsDict.ContainsKey(key))
                    EngagementsDict.Add(key,ce);
                else
                {
                    //send existing engagement and start a new one. this uniqueEngagementId isn't very unique
                    EngagementsDict[key].Send(transform.position);
                    EngagementsDict[key] = ce;
                }
            }
        }

        /// <summary>
        /// ends an engagement on this dynamic object with the matchign uniqueEngagementId. if this is not set, ends an engagement with a name 'engagementName'
        /// </summary>
        /// <param name="engagementName"></param>
        /// <param name="uniqueEngagementId"></param>
        public void EndEngagement(string engagementName = "default", string uniqueEngagementId = null, Dictionary<string, object> properties = null)
        {
            if (EngagementsDict == null) EngagementsDict = new Dictionary<string, CustomEvent>();

            if (uniqueEngagementId == null)
            {
                CustomEvent ce = null;
                if (EngagementsDict.TryGetValue(engagementName, out ce))
                {
                    ce.SetProperties(properties).Send(transform.position);
                    EngagementsDict.Remove(engagementName);
                }
                else
                {
                    //create and send immediately
                    new CustomEvent(engagementName).SetProperties(properties).SetDynamicObject(Id).Send(transform.position);
                }
            }
            else
            {
                CustomEvent ce = null;
                string key = uniqueEngagementId + Id;
                if (EngagementsDict.TryGetValue(key, out ce))
                {
                    ce.SetProperties(properties).Send(transform.position);
                    EngagementsDict.Remove(key);
                }
                else
                {
                    //create and send immediately
                    new CustomEvent(engagementName).SetProperties(properties).SetDynamicObject(Id).Send(transform.position);
                }
            }
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
}