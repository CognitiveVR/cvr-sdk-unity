using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using CognitiveVR.Components;

/// <summary>
/// this tracks the position and gaze point of the player. this also handles the sending data event
/// </summary>

//should only deal with getting the world position of user's gaze and submitting that to the plugin

namespace CognitiveVR
{
    public partial class CognitiveVR_Manager
    {

        //snapshots still 'exist' so the rendertexture can be evaluated
        private List<PlayerSnapshot> playerSnapshots = new List<PlayerSnapshot>();

        //a list of snapshots already formated to string
        //ONLY USED IN THREAD WHEN SENDING
        //private List<string> savedGazeSnapshots = new List<string>();
        private int jsonpart = 1;
        //public bool EvaluateGazeRealtime = true;

        Camera cam;
        PlayerRecorderHelper periodicRenderer;

        bool headsetPresent = true;
        private RenderTexture rt;

#if CVR_FOVE
        static FoveInterfaceBase _foveInstance;
        public static FoveInterfaceBase FoveInstance
        {
            get
            {
                if (_foveInstance == null)
                {
                    _foveInstance = FindObjectOfType<FoveInterfaceBase>();
                }
                return _foveInstance;
            }
        }
#endif

        void PlayerRecorderInit(Error initError)
        {
            if (initError != Error.Success)
            {
                return;
            }
            CheckCameraSettings();

            PlayerSnapshot.colorSpace = QualitySettings.activeColorSpace;
#if CVR_FOVE||CVR_PUPIL
            PlayerSnapshot.tex = new Texture2D(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution);
#else
            PlayerSnapshot.tex = new Texture2D(1, 1);
#endif

            if (CognitiveVR_Preferences.Instance.SendDataOnQuit)
            {
                QuitEvent += Core.SendDataEvent;
                //QuitEvent += DynamicObject.SendAllSnapshots;
            }

            //SendDataEvent += SendPlayerGazeSnapshots;
            //SendDataEvent += Instrumentation.SendTransactions;

#if CVR_PUPIL
            PupilTools.OnCalibrationStarted += PupilGazeTracker_OnCalibrationStarted;
            PupilTools.OnCalibrationEnded += PupilGazeTracker_OnCalibrationDone;
#endif

#if CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_OnPoseEvent; //1.2
            //CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_OnPoseEventOLD; //1.1
#endif
#if CVR_OCULUS
            OVRManager.HMDMounted += OVRManager_HMDMounted;
            OVRManager.HMDUnmounted += OVRManager_HMDUnmounted;
#endif
            //SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR_Preferences.FindTrackingScene();
            if (sceneSettings != null)
            {
                if (!string.IsNullOrEmpty(sceneSettings.SceneId))
                {
                    BeginPlayerRecording();
                    Core.CurrentSceneId = sceneSettings.SceneId;
                    Core.CurrentSceneVersionNumber = sceneSettings.VersionNumber;
                    Util.logDebug("<color=green>PlayerRecorder Init begin recording scene</color> " + sceneSettings.SceneName);
                }
                else
                {
                    Util.logDebug("<color=red>PlayerRecorder Init SceneId is empty for scene " + sceneSettings.SceneName + ". Not recording</color>");
                }
            }
            else
            {
                Util.logDebug("<color=red>PlayerRecorder Init couldn't find scene " + SceneManager.GetActiveScene().name + ". Not recording</color>");
            }
            rt = new RenderTexture(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, 0);
        }

#if CVR_PUPIL

        //TODO these can happen on a separate thread? uses camera.main which will only work on the main thread
        private void PupilGazeTracker_OnCalibrationDone()
        {
            //new CustomEvent("cvr.calibration").Send();
        }

        private void PupilGazeTracker_OnCalibrationStarted()
        {
            //new CustomEvent("cvr.calibration").Send();
        }
#endif

        void CheckCameraSettings()
        {
            var hmd = HMD;
            if (hmd == null)
            {
                Util.logDebug("PlayerRecorder CheckCameraSettings HMD is null");
                return;
            }

            if (periodicRenderer == null) //should get rid of this?
            {
                periodicRenderer = hmd.GetComponent<PlayerRecorderHelper>();
                if (periodicRenderer == null)
                {
                    periodicRenderer = hmd.gameObject.AddComponent<PlayerRecorderHelper>();
                    periodicRenderer.enabled = false;
                }
            }

            if (cam == null)
            {
                cam = hmd.GetComponent<Camera>();
                //do command buffer stuff
                //UnityEngine.Rendering.CommandBuffer buf = new UnityEngine.Rendering.CommandBuffer();
                //buf.name = "Cognitive Analytics Buffer";
            }

#if CVR_FOVE
            if (cam.cullingMask != -1)
                cam.cullingMask = -1;
#endif

            if (cam.depthTextureMode != DepthTextureMode.Depth)
                cam.depthTextureMode = DepthTextureMode.Depth;
        }

#if CVR_STEAMVR
        void CognitiveVR_Manager_OnPoseEvent(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                headsetPresent = true;
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                headsetPresent = false;
                if (CognitiveVR_Preferences.Instance.SendDataOnHMDRemove)
                {
                    Core.SendDataEvent();
                }
            }
        }

        void CognitiveVR_Manager_OnPoseEventOLD(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                headsetPresent = true;
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                headsetPresent = false;
                if (CognitiveVR_Preferences.Instance.SendDataOnHMDRemove)
                {
                    Core.SendDataEvent();
                }
            }
        }
#endif

#if CVR_OCULUS
        private void OVRManager_HMDMounted()
        {
            headsetPresent = true;
        }

        private void OVRManager_HMDUnmounted()
        {
            headsetPresent = false;
            if (CognitiveVR_Preferences.Instance.SendDataOnHMDRemove)
            {
                Core.SendDataEvent();
            }
        }
#endif

        static void BeginPlayerRecording()
        {
            var scenedata = CognitiveVR_Preferences.FindTrackingScene();
            //var scenedata = CognitiveVR_Preferences.Instance.FindSceneByPath(SceneManager.GetActiveScene().path);

            if (scenedata == null)
            {
                CognitiveVR.Util.logDebug(CognitiveVR_Preferences.TrackingSceneName + " Scene data is null! Player Recorder has nowhere to upload data");
                return;
            }

            CognitiveVR_Manager.TickEvent += instance.CognitiveVR_Manager_OnTick;
        }

        //delayed by PlayerSnapshotInterval
        private void CognitiveVR_Manager_OnTick()
        {
            //HasRequestedDynamicGazeRaycast = false;
            //hasHitDynamic = false;

            CheckCameraSettings();

            if (!headsetPresent || CognitiveVR_Manager.HMD == null) { return; }

#if CVR_FOVE
            //if (!Fove.FoveHeadset.GetHeadset().IsEyeTrackingCalibrated()) { return; }
            //TODO if eye tracking is not calibrated, use center of view as gaze
#endif
            //doPostRender = true;
            RequestDynamicObjectGaze();

            if (CognitiveVR_Preferences.S_TrackGazePoint)
            {
                periodicRenderer.enabled = true;
                rt = periodicRenderer.DoRender(rt);
                periodicRenderer.enabled = false;
            }
            else
            {
                TickPostRender();
                //StartCoroutine(periodicRenderer.EndOfFrame());
            }
        }

        //only used with !prefs.S_TrackGazePoint
        static DynamicObject VideoSphere;

        //dynamic object
        //static bool HasHitDynamic = true;
        static void RequestDynamicObjectGaze()
        {
            var hmd = HMD;
            RaycastHit hit = new RaycastHit();
            Vector3 gazeDirection = hmd.forward;
#if CVR_FOVE||CVR_PUPIL
#if CVR_FOVE //direction
            var eyeRays = FoveInstance.GetGazeRays();
            var ray = eyeRays.left;
            gazeDirection = new Vector3(ray.direction.x, ray.direction.y, ray.direction.z);
            gazeDirection.Normalize();
#endif //fove direction
#if CVR_PUPIL //direction
            //var v2 = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes); //0-1 screen pos
            var v2 = PupilData._2D.GazePosition;

            //if it doesn't find the eyes, skip this snapshot
            if (PupilTools.FloatFromDictionary(PupilTools.gazeDictionary,"confidence") > 0.1f) //think this is from the right source dict?
            {
                var ray = instance.cam.ViewportPointToRay(v2);
                gazeDirection = ray.direction.normalized;
            } //else uses HMD forward
#endif //pupil direction
#endif
            float maxDistance = 1000;
            DynamicObjectId sphereId = null;
            if (!CognitiveVR_Preferences.S_TrackGazePoint)
            {
                if (!string.IsNullOrEmpty(CognitiveVR_Preferences.S_VideoSphereDynamicObjectId))
                {
                    if (sphereId == null)
                    {
                        //find the object with the preset video sphere id
                        //objectids get cleared and refreshed each scene change

                        sphereId = DynamicObject.ObjectIds.Find(delegate (DynamicObjectId obj)
                        {
                            return obj.Id == CognitiveVR_Preferences.S_VideoSphereDynamicObjectId;
                        });
                    }
                }

                if (sphereId != null)
                {
                    maxDistance = CognitiveVR_Preferences.S_GazeDirectionMultiplier;
                    if (VideoSphere == null)
                    {
                        var dynamics = FindObjectsOfType<DynamicObject>();
                        for (int i = 0; i < dynamics.Length; i++)
                        {
                            if (dynamics[i].ObjectId == sphereId)
                            {
                                VideoSphere = dynamics[i];
                                break;
                            }
                        }
                    }
                }
                else
                {
                    //no video sphere
                }
            }

            Instance.postRenderHitPos = Vector3.zero;
            instance.postRenderHitWorldPos = Vector3.zero;
            Instance.postRenderId = "";
            //Instance.postRenderDist = 999;
            Instance.hitType = DynamicHitType.None;
            
            DynamicObject PhysicsdynamicHit = null;
            float PhysicsHitDistance = Instance.cam.farClipPlane;
            Vector3 PhysicsHitPoint = Vector3.zero;
            bool didHitAnything = false;

            //radius should be the size of the resolution pixel relative to screen size. ie lower resolution snapshot = larger radius
            //this can't work with a spherecast based on pixel size - a pixel represents a different amount of space at distance because of perspective
            var radius = 0.05f;// (1 - ((float)PlayerSnapshot.Resolution / (float)Screen.height)) / ((float)PlayerSnapshot.Resolution*0.25f);
            bool hitDynamic = false;

            if (Physics.Raycast(hmd.position, gazeDirection, out hit, maxDistance))
            {
                if (CognitiveVR_Preferences.S_DynamicObjectSearchInParent)
                {
                    PhysicsdynamicHit = hit.collider.GetComponentInParent<DynamicObject>();
                }
                else
                {
                    PhysicsdynamicHit = hit.collider.GetComponent<DynamicObject>();
                }

                if (PhysicsdynamicHit != null)
                {
                    hitDynamic = true;
                }
            }
            if (!hitDynamic && Physics.SphereCast(HMD.position, radius, gazeDirection, out hit, maxDistance))
            {
                didHitAnything = true;
                if (CognitiveVR_Preferences.Instance.DynamicObjectSearchInParent)
                {
                    PhysicsdynamicHit = hit.collider.GetComponentInParent<DynamicObject>();
                }
                else
                {
                    PhysicsdynamicHit = hit.collider.GetComponent<DynamicObject>();
                }

                if (PhysicsdynamicHit != null)
                {
                    hitDynamic = true;
                }
            }

            if (hitDynamic)
            {
                PhysicsHitDistance = hit.distance;
                PhysicsHitPoint = hit.point;

                //pass an objectid into the snapshot properties
                //instance.TickPostRender(hit.transform.InverseTransformPointUnscaled(hit.point), PhysicsdynamicHit.ObjectId.Id);

                //HasHitDynamic = true;


                PhysicsdynamicHit.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);

                //this gets the object and the 'physical' point on the object
                //TODO this could use the depth buffer to get the point. or maybe average between the raycasthit.point and the world depth point?
                //to do this, defer this into TickPostRender and check EvaluateGazeRealtime

            }

            if (PhysicsdynamicHit != null && PhysicsdynamicHit.ObjectId != null)
            {
                //this is correct because the hit point is correct in world space, regardless of object orientation
                //from this we want to know where it falls relative to the dynamic object transform
                //mesh transform does not matter! meshes are exported as obj with origin as dynamic object - transformations are baked into the exported mesh
                //if the mesh transform is moved/rotated after export, it should be exported again
                instance.postRenderHitPos = PhysicsdynamicHit.transform.InverseTransformPointUnscaled(PhysicsHitPoint);

                instance.postRenderHitWorldPos = PhysicsHitPoint;
                instance.postRenderId = PhysicsdynamicHit.ObjectId.Id;
                //instance.postRenderDist = hit.distance;
                instance.hitType = DynamicHitType.Physics;
                PhysicsdynamicHit.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);
#if UNITY_EDITOR
                if (CognitiveVR_Preferences.Instance.EnableLogging)
                {
                    //world position
                    //Debug.DrawRay(PhysicsdynamicHit.transform.TransformPointUnscaled(instance.postRenderHitPos), Vector3.up, Color.green, 1);
                    //Debug.DrawRay(PhysicsdynamicHit.transform.TransformPointUnscaled(instance.postRenderHitPos), Vector3.right, Color.red, 1);
                    //Debug.DrawRay(PhysicsdynamicHit.transform.TransformPointUnscaled(instance.postRenderHitPos), Vector3.forward, Color.blue, 1);

                    //local position
                    Debug.DrawRay(instance.postRenderHitPos, Vector3.up, Color.green, 1);
                    Debug.DrawRay(instance.postRenderHitPos, Vector3.right, Color.red, 1);
                    Debug.DrawRay(instance.postRenderHitPos, Vector3.forward, Color.blue, 1);
                }
#endif
            }

            if (!didHitAnything)// && results.Count == 0) //nothing hit
            {
                if (sphereId != null)
                {
                    //instance.TickPostRender(gazeDirection * maxDistance, CognitiveVR_Preferences.Instance.VideoSphereDynamicObjectId, WorldHitDistance);
                    instance.postRenderHitPos = gazeDirection * maxDistance;
                    instance.postRenderId = CognitiveVR_Preferences.Instance.VideoSphereDynamicObjectId;
                    //instance.postRenderDist = WorldHitDistance;
                    if (VideoSphere != null)
                    {
                        VideoSphere.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);
                    }
                    //HasHitDynamic = true;
                }
            }
        }

        //0 is none, 1 is dynamic. used to distinguish if depth point is a dynamic object
        enum DynamicHitType
        {
            None,
            Physics
        }
        DynamicHitType hitType = DynamicHitType.None;
        //local position of gaze relative to dynamic object
        Vector3 postRenderHitPos;
        //used for debugging and calculating distance of UI compared to world gaze point
        Vector3 postRenderHitWorldPos;
        string postRenderId = "";
        //float postRenderDist;
        //DynamicObject uiDynamicHit;

        //TODO make this private and tie a delegate to this
        //called from periodicrenderer OnPostRender or immediately after on tick if realtime gaze eval is disabled
        public void TickPostRender()
        {
            //TODO pool player snapshots
            PlayerSnapshot snapshot = new PlayerSnapshot(frameCount);
            if (!string.IsNullOrEmpty(postRenderId) && hitType == DynamicHitType.Physics)
            {
                //snapshot.Properties.Add("objectId", objectId);
                snapshot.ObjectId = postRenderId;
                //snapshot.Properties.Add("localGaze", localPos);
                snapshot.LocalGaze = postRenderHitPos;
            }

            //update ensures camera is not null. if null here, this means something changes before post render, possibly scene change
            if (cam == null)
            {
                return;
            }



            var camTransform = cam.transform;
            var camPos = camTransform.position;
            var camRot = camTransform.rotation;

            //snapshot.Properties.Add("position", camPos);
            snapshot.Position = camPos;
            //snapshot.Properties.Add("hmdForward", camTransform.forward);
            //snapshot.HMDForward = camTransform.forward;
            snapshot.HMDForward = camRot * Util.vector_forward;

            //timestamp set in snapshot constructor
            //snapshot.timestamp = Util.Timestamp(frameCount);


            //snapshot.Properties.Add("nearDepth", cam.nearClipPlane);
            snapshot.NearDepth = cam.nearClipPlane;
            //snapshot.Properties.Add("farDepth", cam.farClipPlane);
            snapshot.FarDepth = cam.farClipPlane;
            if (CognitiveVR_Preferences.S_EvaluateGazeRealtime)
            {
                //snapshot.Properties.Add("renderDepth", rt); //this is constantly getting overwritten
                snapshot.RTex = rt;
            }
            else
            {
                //make a copy of rt and save to napshot
                RenderTexture newrt = new RenderTexture(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, 0);
                //Graphics.CopyTexture(rt, newrt);
                Graphics.Blit(rt, newrt);

                //periodicRenderer.enabled = true;
                //RenderTexture newrt = new RenderTexture(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, 0);
                //newrt = periodicRenderer.DoRender(newrt);
                //periodicRenderer.enabled = false;
                snapshot.RTex = newrt;
            }

            //snapshot.Properties.Add("hmdRotation", camRot);
            snapshot.HMDRotation = camRot;

#if CVR_FOVE||CVR_PUPIL

            //gaze tracking sdks need to return a v3 direction "gazeDirection" and a v2 point "hmdGazePoint"
            //the v2 point is used to get a pixel from the render texture

            Vector3 worldGazeDirection = Vector3.forward;

#if CVR_FOVE //direction
            
            var eyeRays = FoveInstance.GetGazeRays();
            var ray = eyeRays.left;
            worldGazeDirection = new Vector3(ray.direction.x, ray.direction.y, ray.direction.z);
            //Debug.DrawRay(HMD.position, worldGazeDirection * 100, Color.cyan, 2);
            worldGazeDirection.Normalize();
#endif //fove direction
#if CVR_PUPIL //direction
            var v2 = PupilData._2D.GazePosition;//.GetEyeGaze(Pupil.GazeSource.BothEyes);

            //if it doesn't find the eyes, skip this snapshot
            if (PupilTools.FloatFromDictionary(PupilTools.gazeDictionary, "confidence") < 0.5f) { return; }
            //if (PupilTools.Confidence(PupilData.rightEyeID) < 0.5f){return;}

            var ray = cam.ViewportPointToRay(v2);
            worldGazeDirection = ray.direction.normalized;
#endif //pupil direction

            //snapshot.Properties.Add("gazeDirection", worldGazeDirection);
            snapshot.GazeDirection = worldGazeDirection;


            Vector2 screenGazePoint = Vector2.one * 0.5f;
#if CVR_FOVE //screenpoint

            //var normalizedPoint = FoveInterface.GetNormalizedViewportPosition(ray.GetPoint(1000), Fove.EFVR_Eye.Left); //Unity Plugin Version 1.3.1
            var normalizedPoint = cam.WorldToViewportPoint(ray.GetPoint(1000));

            //Vector2 gazePoint = hmd.GetGazePoint();
            if (float.IsNaN(normalizedPoint.x))
            {
                return;
            }

            screenGazePoint = new Vector2(normalizedPoint.x, normalizedPoint.y);
#endif //fove screenpoint
#if CVR_PUPIL//screenpoint
            screenGazePoint = PupilData._2D.GazePosition;//.GetEyeGaze(Pupil.GazeSource.BothEyes);
#endif //pupil screenpoint

            //snapshot.Properties.Add("hmdGazePoint", screenGazePoint); //range between 0,0 and 1,1
            snapshot.HMDGazePoint = screenGazePoint;
#endif //gazetracker


            //get gaze point (unless snapshot is whatever. maybe write the type of snapshot here? world, dynamic, sky)
            //wait until enough have been done, then batch the string.write or whatever in a thread

            if (CognitiveVR_Preferences.S_EvaluateGazeRealtime)
            {
                if (!string.IsNullOrEmpty(snapshot.ObjectId) && hitType == DynamicHitType.Physics)
                {
                    snapshot.snapshotType = PlayerSnapshot.SnapshotType.Dynamic;

                    Debug.DrawLine(snapshot.Position, postRenderHitWorldPos, Color.yellow, 1);
                }
                else
                {
                    Vector3 calcGazePoint;
                    bool validPoint = snapshot.GetGazePoint(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, out calcGazePoint);

                    //float gazeDistance = Vector3.SqrMagnitude(calcGazePoint - snapshot.Position);
                    //float uiDistance = Vector3.SqrMagnitude(snapshot.Position - postRenderHitWorldPos);

                    /*if (hitType == DynamicHitType.UI && gazeDistance > uiDistance)
                    {
                        Debug.DrawLine(snapshot.Position, postRenderHitWorldPos, Color.cyan, 1);
                        snapshot.snapshotType = PlayerSnapshot.SnapshotType.Dynamic;
                        if (uiDynamicHit != null)
                            uiDynamicHit.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);
                        //KNOWN BUG if not evaluating world hit point at the end of the frame (and waiting for later) this check cannot happen - dynamic possibly destroyed
                    }
                    else
                    {*/
                    if (!validPoint)
                    {
                        snapshot.snapshotType = PlayerSnapshot.SnapshotType.Sky;
                        Debug.DrawRay(snapshot.Position, HMD.forward * 1000, Color.white, 1);
                    }
                    else if (!float.IsNaN(calcGazePoint.x))
                    {
                        snapshot.snapshotType = PlayerSnapshot.SnapshotType.World;
                        snapshot.GazePoint = calcGazePoint;
#if UNITY_EDITOR
                        if (CognitiveVR_Preferences.Instance.EnableLogging)
                        {
                            Debug.DrawRay(snapshot.GazePoint, Vector3.up, Color.green, 1);
                            Debug.DrawRay(snapshot.GazePoint, Vector3.right, Color.red, 1);
                            Debug.DrawRay(snapshot.GazePoint, Vector3.forward, Color.blue, 1);
                            Debug.DrawLine(snapshot.Position, snapshot.GazePoint, Color.magenta, 1);
                        }
#endif
                    }
                    else
                    {
                        //looked at world, but invalid gaze point
                        return;
                    }
                    //}
                }
            }

            playerSnapshots.Add(snapshot);

            if (playerSnapshots.Count >= CognitiveVR_Preferences.S_GazeSnapshotCount)
            {
                PlayerSnapshot[] tempSnapshots = new PlayerSnapshot[playerSnapshots.Count];
                playerSnapshots.CopyTo(tempSnapshots);

                playerSnapshots.Clear();

                StartCoroutine(Threaded_SendGaze(tempSnapshots, CognitiveVR_Preferences.FindTrackingScene(), Core.UniqueID, Core.SessionTimeStamp, Core.SessionID, CognitiveVR_Manager.GetNewSessionProperties(true)));

                //SendPlayerGazeSnapshots();
                //OnSendData();
            }
        }
        //bool doneSendGaze = false;
        //bool waitingForThread = false;
        IEnumerator Threaded_SendGaze(PlayerSnapshot[] tempSnapshots, CognitiveVR_Preferences.SceneSettings trackingsettings, string uniqueid, double sessiontimestamp, string sessionid, Dictionary<string, object> sessionProperties)
        {
            //var sceneSettings = CognitiveVR_Preferences.FindTrackingScene();
            if (trackingsettings == null)
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for " + CognitiveVR_Preferences.TrackingSceneName + "! Cancel Data Upload");
                yield break;
            }
            if (string.IsNullOrEmpty(trackingsettings.SceneId))
            {
                //playerSnapshots.Clear();
                CognitiveVR.Util.logDebug("sceneid is empty. do not send gaze objects to sceneexplorer");
                yield break;
            }

            bool doneSendGaze = false;
            if (!CognitiveVR_Preferences.S_EvaluateGazeRealtime)
            {
                foreach (var snapshot in tempSnapshots)
                {
                    if (!string.IsNullOrEmpty(snapshot.ObjectId))
                    {
                        snapshot.snapshotType = PlayerSnapshot.SnapshotType.Dynamic;
                    }
                    else
                    {
                        Vector3 calcGazePoint;
                        bool validPoint = snapshot.GetGazePoint(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, out calcGazePoint);
                        if (!validPoint)
                        {
                            snapshot.snapshotType = PlayerSnapshot.SnapshotType.Sky;
                            Debug.DrawRay(snapshot.Position, HMD.forward * 1000, Color.cyan, 1);
                        }
                        else if (!float.IsNaN(calcGazePoint.x))
                        {
                            snapshot.snapshotType = PlayerSnapshot.SnapshotType.World;
                            snapshot.GazePoint = calcGazePoint;

#if UNITY_EDITOR
                            if (CognitiveVR_Preferences.Instance.EnableLogging)
                            {
                                Debug.DrawRay(snapshot.GazePoint, Vector3.up, Color.green, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.right, Color.red, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.forward, Color.blue, 1);
                                Debug.DrawLine(snapshot.Position, snapshot.GazePoint, Color.magenta, 1);
                            }
#endif
                        }
                        else
                        {
                            //looked at world, but invalid gaze point
                            continue;
                        }
                    }
                }
            }

            string contents = null;
            string url = Constants.POSTGAZEDATA(trackingsettings.SceneId, trackingsettings.VersionNumber);

            string hmdmodel = CognitiveVR.Util.GetSimpleHMDName();


            new System.Threading.Thread(() =>
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder(70 * CognitiveVR_Preferences.S_GazeSnapshotCount + 200);
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
                jsonpart++;
                builder.Append(",");

#if CVR_FOVE
                    JsonUtil.SetString("hmdtype", "fove", builder);
#elif CVR_ARKIT
                    JsonUtil.SetString("hmdtype", "arkit", builder);
#elif CVR_ARCORE
                    JsonUtil.SetString("hmdtype", "arcore", builder);
#elif CVR_META
                    JsonUtil.SetString("hmdtype", "meta", builder);
#else
                JsonUtil.SetString("hmdtype", hmdmodel, builder);
#endif
                builder.Append(",");
                JsonUtil.SetFloat("interval", CognitiveVR.CognitiveVR_Preferences.S_SnapshotInterval, builder);
                builder.Append(",");

                JsonUtil.SetString("formatversion", "1.0", builder);
                builder.Append(",");

                //var deviceProperties = CognitiveVR_Manager.GetNewDeviceProperties(true);
                if (sessionProperties.Count > 0)
                {
                    builder.Append("\"properties\":{");
                    foreach (var kvp in sessionProperties)
                    {
                        if (kvp.Value.GetType() == typeof(string))
                        {
                            JsonUtil.SetString(kvp.Key, (string)kvp.Value, builder);
                        }
                        else
                        {
                            JsonUtil.SetObject(kvp.Key, kvp.Value, builder);
                        }
                        builder.Append(",");
                    }
                    builder.Remove(builder.Length - 1, 1); //remove comma
                    builder.Append("},");
                }

                //events
                builder.Append("\"data\":[");


                for (int i = 0; i < tempSnapshots.Length; i++)
                {
                    if (tempSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.Dynamic)
                    {
                        SetDynamicGazePoint(tempSnapshots[i].timestamp, tempSnapshots[i].Position, tempSnapshots[i].HMDRotation, tempSnapshots[i].LocalGaze, tempSnapshots[i].ObjectId, builder);
                    }
                    else if (tempSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.World)
                    {
                        SetPreGazePoint(tempSnapshots[i].timestamp, tempSnapshots[i].Position, tempSnapshots[i].HMDRotation, tempSnapshots[i].GazePoint, builder);
                    }
                    else// if (t_snaphots[i].snapshotType == PlayerSnapshot.SnapshotType.Sky)
                    {
                        SetFarplaneGazePoint(tempSnapshots[i].timestamp, tempSnapshots[i].Position, tempSnapshots[i].HMDRotation, builder);
                    }
                    builder.Append(",");
                }
                if (tempSnapshots.Length > 0)
                    builder.Remove(builder.Length - 1, 1);

                builder.Append("]");

                builder.Append("}");

                contents = builder.ToString();

                doneSendGaze = true;
            }).Start();

            while (!doneSendGaze)
            {
                yield return null;
            }

            CognitiveVR.NetworkManager.Post(url, contents);
        }

        /// <summary>
        /// registered to OnSendData. synchronous
        /// </summary>
        void SendPlayerGazeSnapshots()
        {
            if (playerSnapshots.Count == 0)
            {
                return;
            }

            List<string> savedGazeSnapshots = new List<string>();

            var sceneSettings = CognitiveVR_Preferences.FindTrackingScene();
            if (sceneSettings == null)
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for " + CognitiveVR_Preferences.TrackingSceneName + "! Cancel Data Upload");
                return;
            }
            if (string.IsNullOrEmpty(sceneSettings.SceneId))
            {
                CognitiveVR.Util.logDebug("sceneid is empty. do not send gaze objects to sceneexplorer");
                return;
            }

            if (!CognitiveVR_Preferences.S_EvaluateGazeRealtime)
            {
                //evaluate gaze from snapshots then send
                foreach (var snapshot in playerSnapshots)
                {
                    if (!string.IsNullOrEmpty(snapshot.ObjectId))
                    {
                        snapshot.snapshotType = PlayerSnapshot.SnapshotType.Dynamic;
                    }
                    else
                    {
                        Vector3 calcGazePoint;
                        bool validPoint = snapshot.GetGazePoint(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, out calcGazePoint);
                        if (!validPoint)
                        {
                            snapshot.snapshotType = PlayerSnapshot.SnapshotType.Sky;
                            Debug.DrawRay(snapshot.Position, HMD.forward * 1000, Color.cyan, 1);
                        }
                        else if (!float.IsNaN(calcGazePoint.x))
                        {
                            snapshot.snapshotType = PlayerSnapshot.SnapshotType.World;
                            snapshot.GazePoint = calcGazePoint;

#if UNITY_EDITOR
                            if (CognitiveVR_Preferences.Instance.EnableLogging)
                            {
                                Debug.DrawRay(snapshot.GazePoint, Vector3.up, Color.green, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.right, Color.red, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.forward, Color.blue, 1);
                                Debug.DrawLine(snapshot.Position, snapshot.GazePoint, Color.magenta, 1);
                            }
#endif
                        }
                        else
                        {
                            //looked at world, but invalid gaze point
                            continue;
                        }
                    }
                }
            }

            if (sceneSettings != null)
            {
                if (savedGazeSnapshots.Count > 0)
                {
                    System.Text.StringBuilder builder = new System.Text.StringBuilder(70 * CognitiveVR_Preferences.S_GazeSnapshotCount + 200);

                    builder.Append("{");

                    //header
                    JsonUtil.SetString("userid", Core.UniqueID, builder);
                    builder.Append(",");

                    if (!string.IsNullOrEmpty(CognitiveVR_Preferences.LobbyId))
                    {
                        JsonUtil.SetString("lobbyId", CognitiveVR_Preferences.LobbyId, builder);
                        builder.Append(",");
                    }

                    JsonUtil.SetDouble("timestamp", (int)Core.SessionTimeStamp, builder);
                    builder.Append(",");
                    JsonUtil.SetString("sessionid", Core.SessionID, builder);
                    builder.Append(",");
                    JsonUtil.SetInt("part", jsonpart, builder);
                    jsonpart++;
                    builder.Append(",");

#if CVR_FOVE
                    JsonUtil.SetString("hmdtype", "fove", builder);
#elif CVR_ARKIT
                    JsonUtil.SetString("hmdtype", "arkit", builder);
#elif CVR_ARCORE
                    JsonUtil.SetString("hmdtype", "arcore", builder);
#elif CVR_META
                    JsonUtil.SetString("hmdtype", "meta", builder);
#else
                    JsonUtil.SetString("hmdtype", CognitiveVR.Util.GetSimpleHMDName(), builder);
#endif
                    builder.Append(",");
                    JsonUtil.SetFloat("interval", CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval, builder);
                    builder.Append(",");


                    //events
                    builder.Append("\"data\":[");
                    for (int i = 0; i < playerSnapshots.Count; i++)
                    {
                        if (playerSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.Dynamic)
                        {
                            SetDynamicGazePoint(playerSnapshots[i].timestamp, playerSnapshots[i].Position, playerSnapshots[i].HMDRotation, playerSnapshots[i].LocalGaze, playerSnapshots[i].ObjectId, builder);
                        }
                        else if (playerSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.World)
                        {
                            SetPreGazePoint(playerSnapshots[i].timestamp, playerSnapshots[i].Position, playerSnapshots[i].HMDRotation, playerSnapshots[i].GazePoint, builder);
                        }
                        else// if (t_snaphots[i].snapshotType == PlayerSnapshot.SnapshotType.Sky)
                        {
                            SetFarplaneGazePoint(playerSnapshots[i].timestamp, playerSnapshots[i].Position, playerSnapshots[i].HMDRotation, builder);
                            Debug.DrawRay(playerSnapshots[i].Position, Util.vector_forward, Color.magenta);
                        }
                        builder.Append(",");
                    }
                    if (playerSnapshots.Count > 0)
                        builder.Remove(builder.Length - 1, 1);
                    builder.Append("]");

                    builder.Append("}");

                    //byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
                    string url = Constants.POSTGAZEDATA(sceneSettings.SceneId, sceneSettings.VersionNumber);

                    //CognitiveVR.Util.logDebug(sceneSettings.SceneId + " gaze " + builder.ToString());

                    CognitiveVR.NetworkManager.Post(url, builder.ToString());
                }
            }
            else
            {
                Util.logError("CogntiveVR PlayerTracker.cs does not have scene key for scene " + CognitiveVR_Preferences.TrackingSceneName + "!");
            }

            playerSnapshots.Clear();
        }

        void CleanupPlayerRecorderEvents()
        {
            //unsubscribe events
            //should i set all these events to null?
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
            //SendDataEvent -= SendPlayerGazeSnapshots;
            //SendDataEvent -= Instrumentation.SendTransactions;
            //CognitiveVR_Manager.QuitEvent -= OnSendData;
            //SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
#if CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent -= CognitiveVR_Manager_OnPoseEvent;
#endif
#if CVR_OCULUS
            OVRManager.HMDMounted -= OVRManager_HMDMounted;
            OVRManager.HMDUnmounted -= OVRManager_HMDUnmounted;
#endif
        }

        #region json

        private static string SetPreGazePoint(double time, Vector3 position, Quaternion rotation)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
            builder.Append("{");

            JsonUtil.SetDouble("time", time, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", rotation, builder);
            builder.Append(",");
            builder.Append("GAZE");

            builder.Append("}");

            return builder.ToString();
        }

        //EvaluateGazeRealtime
        private static void SetPreGazePoint(double time, Vector3 position, Quaternion rotation, Vector3 gazepos, System.Text.StringBuilder builder)
        {
            builder.Append("{");

            JsonUtil.SetDouble("time", time, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", rotation, builder);
            builder.Append(",");
            JsonUtil.SetVector("g", gazepos, builder);

            builder.Append("}");
        }

        private static void SetFarplaneGazePoint(double time, Vector3 position, Quaternion rotation, System.Text.StringBuilder builder)
        {
            builder.Append("{");

            JsonUtil.SetDouble("time", time, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", rotation, builder);

            builder.Append("}");
        }

        //EvaluateGaze on a dynamic object
        private static void SetDynamicGazePoint(double time, Vector3 position, Quaternion rotation, Vector3 localGazePos, string objectId, System.Text.StringBuilder builder)
        {
            builder.Append("{");

            JsonUtil.SetDouble("time", time, builder);
            builder.Append(",");
            JsonUtil.SetString("o", objectId, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", rotation, builder);
            builder.Append(",");
            JsonUtil.SetVector("g", localGazePos, builder);

            builder.Append("}");
        }

        #endregion
    }
}

//scale points extention menthods
public static class UnscaledTransformPoints
{
    public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var localToWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        return localToWorldMatrix.MultiplyPoint3x4(position);
    }

    public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
        return worldToLocalMatrix.MultiplyPoint3x4(position);
    }
}