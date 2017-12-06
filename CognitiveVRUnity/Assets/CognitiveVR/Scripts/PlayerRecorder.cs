using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using CognitiveVR.Components;

/// <summary>
/// this tracks the position and gaze point of the player. this also handles the sending data event
/// </summary>

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
        static FoveInterface _foveInstance;
        public static FoveInterface FoveInstance
        {
            get
            {
                if (_foveInstance == null)
                {
                    _foveInstance = FindObjectOfType<FoveInterface>();
                }
                return _foveInstance;
            }
        }
#endif

        public void PlayerRecorderInit(Error initError)
        {
            if (initError != Error.Success)
            {
                return;
            }
            CheckCameraSettings();

            PlayerSnapshot.colorSpace = QualitySettings.activeColorSpace;
#if CVR_GAZETRACK
            PlayerSnapshot.tex = new Texture2D(Resolution, Resolution);
#else
            PlayerSnapshot.tex = new Texture2D(1, 1);
#endif

            if (CognitiveVR_Preferences.Instance.SendDataOnQuit)
            {
                QuitEvent += OnSendData;
                //QuitEvent += DynamicObject.SendAllSnapshots;
            }

            SendDataEvent += SendPlayerGazeSnapshots;
            SendDataEvent += InstrumentationSubsystem.SendCachedTransactions;

#if CVR_PUPIL
            PupilGazeTracker.Instance.OnCalibrationStarted += PupilGazeTracker_OnCalibrationStarted;
            PupilGazeTracker.Instance.OnCalibrationDone += PupilGazeTracker_OnCalibrationDone;
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
                    CoreSubsystem.CurrentSceneId = sceneSettings.SceneId;
                    CoreSubsystem.CurrentSceneVersion = sceneSettings.Version;
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
        private void PupilGazeTracker_OnCalibrationDone(PupilGazeTracker manager)
        {
            //Instrumentation.Transaction("cvr.calibration").end();
        }

        private void PupilGazeTracker_OnCalibrationStarted(PupilGazeTracker manager)
        {
            //Instrumentation.Transaction("cvr.calibration").begin();
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
                    OnSendData();
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
                    OnSendData();
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
                OnSendData();
            }
        }
#endif

        void UpdatePlayerRecorder()
        {
            //TimeSinceLastObjectGazeRequest += Time.deltaTime;

            CognitiveVR_Preferences prefs = CognitiveVR_Preferences.Instance;

            if (!prefs.SendDataOnHotkey) { return; }
            if (Input.GetKeyDown(prefs.SendDataHotkey))
            {
                if (prefs.HotkeyShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) { return; }
                if (prefs.HotkeyAlt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) { return; }
                if (prefs.HotkeyCtrl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) { return; }

                OnSendData();
            }
        }

        public static void BeginPlayerRecording()
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

        public static void SendPlayerRecording()
        {
            instance.OnSendData();
        }

        public static void EndPlayerRecording()
        {
            CognitiveVR_Manager.TickEvent -= instance.CognitiveVR_Manager_OnTick;
            instance.OnSendData();
            //instance.SetTrackingScene(SceneManager.GetActiveScene().name);
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

        //only used with gazedirection
        static DynamicObject VideoSphere;

        //dynamic object
        //static bool HasHitDynamic = true;
        static void RequestDynamicObjectGaze()
        {
            var hmd = HMD;
            RaycastHit hit = new RaycastHit();
            Vector3 gazeDirection = hmd.forward;
#if CVR_GAZETRACK
#if CVR_FOVE //direction
            var eyeRays = FoveInstance.GetGazeRays();
            var ray = eyeRays.left;
            gazeDirection = new Vector3(ray.direction.x, ray.direction.y, ray.direction.z);
            gazeDirection.Normalize();
#endif //fove direction
#if CVR_PUPIL //direction
            var v2 = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes); //0-1 screen pos

            //if it doesn't find the eyes, skip this snapshot
            if (PupilGazeTracker.Instance.Confidence > 0.1f)
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
                if (CognitiveVR_Preferences.S_VideoSphereDynamicObjectId > -1)
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
            Instance.postRenderId = -1;
            Instance.postRenderDist = 999;
            Instance.hitType = DynamicHitType.None;

            DynamicObject UIdynamicHit = null;
            DynamicObject PhysicsdynamicHit = null;

            //check UI
            float UIHitDistance = Instance.cam.farClipPlane;
            Vector3 UIHitPoint = Vector3.zero;
            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();

            //maybe salvagable, but at the moment UI gaze should be done with a collider on the canvas

            /*float camNearDistance = Instance.cam.nearClipPlane;
            UnityEngine.EventSystems.PointerEventData gazeData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            gazeData.position = Instance.cam.WorldToScreenPoint(gazeDirection);

            gazeData.position = new Vector2(Screen.width / 2, Screen.height / 2);

            UnityEngine.EventSystems.EventSystem.current.RaycastAll(gazeData, results);
            if (results.Count > 0)
            {
                UIdynamicHit = results[0].gameObject.GetComponentInParent<DynamicObject>();
                if (UIdynamicHit != null)
                {
                    UIHitDistance = results[0].distance + camNearDistance;
                    UIHitPoint = HMD.position + gazeDirection * (results[0].distance + camNearDistance);
                }
            }*/

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
                //Debug.Log("spherecast did hit anything");
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
                    //Debug.Log("spherecast did hit DYNAMIC");
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
                if (Util.IsLoggingEnabled)
                {
                    //Debug.DrawRay(hit.point, Vector3.up, Color.green, 1);
                    //Debug.DrawRay(hit.point, Vector3.right, Color.red, 1);
                    //Debug.DrawRay(hit.point, Vector3.forward, Color.blue, 1);
                }

                //this gets the object and the 'physical' point on the object
                //TODO this could use the depth buffer to get the point. or maybe average between the raycasthit.point and the world depth point?
                //to do this, defer this into TickPostRender and check EvaluateGazeRealtime

            }

            instance.uiDynamicHit = null;
            if (UIHitDistance < PhysicsHitDistance) //hit some UI thing first
            {
                if (UIdynamicHit != null)
                {
                    //Debug.Log("save hit from dynamic to world!");
                    //instance.TickPostRender(results[0].gameObject.transform.InverseTransformPointUnscaled(UIHitPoint), UIdynamicHit.ObjectId.Id, UIHitDistance);
                    instance.postRenderHitPos = results[0].gameObject.transform.InverseTransformPointUnscaled(UIHitPoint);
                    instance.postRenderHitWorldPos = UIHitPoint;
                    instance.postRenderId = UIdynamicHit.ObjectId.Id;
                    instance.postRenderDist = UIHitDistance;
                    instance.hitType = DynamicHitType.UI;
                    instance.uiDynamicHit = UIdynamicHit;
                }
            }
            else
            {
                if (PhysicsdynamicHit != null)
                {
                    //instance.TickPostRender(hit.transform.InverseTransformPointUnscaled(WorldHitPoint), PhysicsdynamicHit.ObjectId.Id);
                    instance.postRenderHitPos = hit.transform.InverseTransformPointUnscaled(PhysicsHitPoint);
                    instance.postRenderHitWorldPos = PhysicsHitPoint;
                    instance.postRenderId = PhysicsdynamicHit.ObjectId.Id;
                    instance.postRenderDist = hit.distance;
                    instance.hitType = DynamicHitType.Physics;
                    PhysicsdynamicHit.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);
                }
            }

            if (!didHitAnything && results.Count == 0) //nothing hit
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

        //0 is none, 1 is dynamic, 2 is ui
        enum DynamicHitType
        {
            None,
            Physics,
            UI
        }
        DynamicHitType hitType = DynamicHitType.None;
        //local position of gaze relative to dynamic object
        Vector3 postRenderHitPos;
        //used for debugging and calculating distance of UI compared to world gaze point
        Vector3 postRenderHitWorldPos;
        int postRenderId = -1;
        float postRenderDist;
        DynamicObject uiDynamicHit;

        //called from periodicrenderer OnPostRender or immediately after on tick if realtime gaze eval is disabled
        public void TickPostRender()
        {
            //TODO pool player snapshots
            PlayerSnapshot snapshot = new PlayerSnapshot(frameCount);
            if (postRenderId >= 0 && hitType == DynamicHitType.Physics)
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

#if CVR_GAZETRACK

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
            var v2 = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes); //0-1 screen pos

            //if it doesn't find the eyes, skip this snapshot
            if (PupilGazeTracker.Instance.Confidence < 0.5f) { return; }

            var ray = cam.ViewportPointToRay(v2);
            worldGazeDirection = ray.direction.normalized;
#endif //pupil direction

            //snapshot.Properties.Add("gazeDirection", worldGazeDirection);
            snapshot.GazeDirection = worldGazeDirection;


            Vector2 screenGazePoint = Vector2.one * 0.5f;
#if CVR_FOVE //screenpoint
            //var normalizedPoint = FoveInterface.GetNormalizedViewportPosition(ray.GetPoint(1000), Fove.EFVR_Eye.Left);

            var normalizedPoint = FoveInstance.GetNormalizedViewportPointForEye(ray.GetPoint(1000), Fove.EFVR_Eye.Left);

            //Vector2 gazePoint = hmd.GetGazePoint();
            if (float.IsNaN(normalizedPoint.x))
            {
                return;
            }

            screenGazePoint = new Vector2(normalizedPoint.x, normalizedPoint.y);
#endif //fove screenpoint
#if CVR_PUPIL//screenpoint
            screenGazePoint = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes);
#endif //pupil screenpoint

            //snapshot.Properties.Add("hmdGazePoint", screenGazePoint); //range between 0,0 and 1,1
            snapshot.HMDGazePoint = screenGazePoint;
#endif //gazetracker


            //get gaze point (unless snapshot is whatever. maybe write the type of snapshot here? world, dynamic, sky)
            //wait until enough have been done, then batch the string.write or whatever in a thread

            if (CognitiveVR_Preferences.S_EvaluateGazeRealtime)
            {
                if (snapshot.ObjectId > -1 && hitType == DynamicHitType.Physics)
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

                            if (Util.IsLoggingEnabled)
                            {
                                Debug.DrawRay(snapshot.GazePoint, Vector3.up, Color.green, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.right, Color.red, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.forward, Color.blue, 1);
                                Debug.DrawLine(snapshot.Position, snapshot.GazePoint, Color.magenta, 1);
                            }
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

                StartCoroutine(Threaded_SendGaze(tempSnapshots));

                //SendPlayerGazeSnapshots();
                //OnSendData();
            }
        }
        //bool doneSendGaze = false;
        //bool waitingForThread = false;
        IEnumerator Threaded_SendGaze(PlayerSnapshot[] tempSnapshots)
        {
            bool doneSendGaze = false;
            if (!CognitiveVR_Preferences.S_EvaluateGazeRealtime)
            {
                foreach (var snapshot in tempSnapshots)
                {
                    if (snapshot.ObjectId > -1)
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

                            if (Util.IsLoggingEnabled)
                            {
                                Debug.DrawRay(snapshot.GazePoint, Vector3.up, Color.green, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.right, Color.red, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.forward, Color.blue, 1);
                                Debug.DrawLine(snapshot.Position, snapshot.GazePoint, Color.magenta, 1);
                            }
                        }
                        else
                        {
                            //looked at world, but invalid gaze point
                            continue;
                        }
                    }
                }
            }

            List<string> stringGazeSnapshots = new List<string>();
            new System.Threading.Thread(() =>
            {
                for (int i = 0; i < tempSnapshots.Length; i++)
                {
                    if (tempSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.Dynamic)
                    {
                        stringGazeSnapshots.Add(SetDynamicGazePoint(tempSnapshots[i].timestamp, tempSnapshots[i].Position, tempSnapshots[i].HMDRotation, tempSnapshots[i].LocalGaze, tempSnapshots[i].ObjectId));
                    }
                    else if (tempSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.World)
                    {
                        stringGazeSnapshots.Add(SetPreGazePoint(tempSnapshots[i].timestamp, tempSnapshots[i].Position, tempSnapshots[i].HMDRotation, tempSnapshots[i].GazePoint));
                    }
                    else// if (t_snaphots[i].snapshotType == PlayerSnapshot.SnapshotType.Sky)
                    {
                        stringGazeSnapshots.Add(SetFarplaneGazePoint(tempSnapshots[i].timestamp, tempSnapshots[i].Position, tempSnapshots[i].HMDRotation));
                    }
                }
                System.GC.Collect();
                doneSendGaze = true;
            }).Start();

            while (!doneSendGaze)
            {
                yield return null;
            }
            
            SendPlayerGazeSnapshots(stringGazeSnapshots);
        }

        public void SendPlayerGazeSnapshots(List<string> stringGazeSnapshots)
        {
            /*if (playerSnapshots.Count == 0)
            {
                Util.logDebug("PlayerRecord SendPlayerGazeSnapshots has no snapshots to send");
                return;
            }*/

            var sceneSettings = CognitiveVR_Preferences.FindTrackingScene();
            if (sceneSettings == null)
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for " + CognitiveVR_Preferences.TrackingSceneName + "! Cancel Data Upload");
                return;
            }
            if (string.IsNullOrEmpty(sceneSettings.SceneId))
            {
                //playerSnapshots.Clear();
                CognitiveVR.Util.logDebug("sceneid is empty. do not send gaze objects to sceneexplorer");
                return;
            }

            if (sceneSettings != null)
            {
                Util.logDebug("uploading gaze and events to " + sceneSettings.SceneId);

                if (stringGazeSnapshots.Count > 0)
                {
                    System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

                    builder.Append("{");

                    //header
                    JsonUtil.SetString("userid", Core.userId, builder);
                    builder.Append(",");

                    JsonUtil.SetDouble("timestamp", (int)CognitiveVR_Preferences.TimeStamp, builder);
                    builder.Append(",");
                    JsonUtil.SetString("sessionid", CognitiveVR_Preferences.SessionID, builder);
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
#else
                    JsonUtil.SetString("hmdtype", CognitiveVR.Util.GetSimpleHMDName(), builder);
#endif
                    builder.Append(",");
                    JsonUtil.SetFloat("interval", CognitiveVR.CognitiveVR_Preferences.S_SnapshotInterval, builder);
                    builder.Append(",");


                    //events
                    builder.Append("\"data\":[");
                    for (int i = 0; i < stringGazeSnapshots.Count; i++)
                    {
                        //if (playerSnapshots[i] == null) { continue; }
                        //builder.Append(SetGazePont(playerSnapshots[i]));
                        builder.Append(stringGazeSnapshots[i]);
                        builder.Append(",");
                    }
                    if (stringGazeSnapshots.Count > 0)
                        builder.Remove(builder.Length - 1, 1);
                    builder.Append("]");

                    builder.Append("}");

                    byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
                    string SceneURLGaze = Constants.GAZE_URL + sceneSettings.SceneId + "?version=" + sceneSettings.Version;
                    
                    //Debug.Log(stringGazeSnapshots.Count +" gaze " + builder.ToString());

                    StartCoroutine(PostJsonRequest(outBytes, SceneURLGaze));
                }
            }
            else
            {
                Util.logError("CogntiveVR PlayerTracker.cs does not have scene key for scene " + CognitiveVR_Preferences.TrackingSceneName + "!");
            }

            playerSnapshots.Clear();
            //savedGazeSnapshots.Clear();
        }

        /// <summary>
        /// registered to OnSendData
        /// </summary>
        public void SendPlayerGazeSnapshots()
        {
            if (playerSnapshots.Count == 0)
            {
                return;
            }

            List<string> savedGazeSnapshots = new List<string>();

            if (!CognitiveVR_Preferences.S_EvaluateGazeRealtime)
            {
                //evaluate gaze from snapshots then send
                foreach( var snapshot in playerSnapshots)
                {
                    if (snapshot.ObjectId > -1)
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

                            if (Util.IsLoggingEnabled)
                            {
                                Debug.DrawRay(snapshot.GazePoint, Vector3.up, Color.green, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.right, Color.red, 1);
                                Debug.DrawRay(snapshot.GazePoint, Vector3.forward, Color.blue, 1);
                                Debug.DrawLine(snapshot.Position, snapshot.GazePoint, Color.magenta, 1);
                            }
                        }
                        else
                        {
                            //looked at world, but invalid gaze point
                            continue;
                        }
                    }
                }
            }

            for (int i = 0; i < playerSnapshots.Count; i++)
            {
                if (playerSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.Dynamic)
                {
                    savedGazeSnapshots.Add(SetDynamicGazePoint(playerSnapshots[i].timestamp, playerSnapshots[i].Position, playerSnapshots[i].HMDRotation, playerSnapshots[i].LocalGaze, playerSnapshots[i].ObjectId));

                    //Debug.DrawRay(hit.point, Vector3.up, Color.green, 1);
                    //Debug.DrawRay(hit.point, Vector3.right, Color.red, 1);
                    //Debug.DrawRay(hit.point, Vector3.forward, Color.blue, 1);
                }
                else if (playerSnapshots[i].snapshotType == PlayerSnapshot.SnapshotType.World)
                {
                    savedGazeSnapshots.Add(SetPreGazePoint(playerSnapshots[i].timestamp, playerSnapshots[i].Position, playerSnapshots[i].HMDRotation, playerSnapshots[i].GazePoint));
                }
                else// if (t_snaphots[i].snapshotType == PlayerSnapshot.SnapshotType.Sky)
                {
                    savedGazeSnapshots.Add(SetFarplaneGazePoint(playerSnapshots[i].timestamp, playerSnapshots[i].Position, playerSnapshots[i].HMDRotation));
                    Debug.DrawRay(playerSnapshots[i].Position, Util.vector_forward, Color.magenta);
                }
            }

            /*if (playerSnapshots.Count == 0)
            {
                Util.logDebug("PlayerRecord SendPlayerGazeSnapshots has no snapshots to send");
                return;
            }*/

            var sceneSettings = CognitiveVR_Preferences.FindTrackingScene();
            if (sceneSettings == null)
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for " + CognitiveVR_Preferences.TrackingSceneName + "! Cancel Data Upload");
                return;
            }
            if (string.IsNullOrEmpty(sceneSettings.SceneId))
            {
                //playerSnapshots.Clear();
                CognitiveVR.Util.logDebug("sceneid is empty. do not send gaze objects to sceneexplorer");
                return;
            }

            if (sceneSettings != null)
            {
                //Util.logDebug("uploading gaze and events to " + sceneSettings.SceneId);

                if (savedGazeSnapshots.Count > 0)
                {
                    System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

                    builder.Append("{");

                    //header
                    JsonUtil.SetString("userid", Core.UniqueID, builder);
                    builder.Append(",");

                    JsonUtil.SetDouble("timestamp", (int)CognitiveVR_Preferences.TimeStamp, builder);
                    builder.Append(",");
                    JsonUtil.SetString("sessionid", CognitiveVR_Preferences.SessionID, builder);
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
#else
                    JsonUtil.SetString("hmdtype", CognitiveVR.Util.GetSimpleHMDName(), builder);
#endif
                    builder.Append(",");
                    JsonUtil.SetFloat("interval", CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval, builder);
                    builder.Append(",");


                    //events
                    builder.Append("\"data\":[");
                    for (int i = 0; i < savedGazeSnapshots.Count; i++)
                    {
                        //if (playerSnapshots[i] == null) { continue; }
                        //builder.Append(SetGazePont(playerSnapshots[i]));
                        builder.Append(savedGazeSnapshots[i]);
                        builder.Append(",");
                    }
                    if (playerSnapshots.Count > 0)
                        builder.Remove(builder.Length - 1, 1);
                    builder.Append("]");

                    builder.Append("}");

                    byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
                    string SceneURLGaze = Constants.GAZE_URL + sceneSettings.SceneId + "?version=" + sceneSettings.Version;

                    CognitiveVR.Util.logDebug(sceneSettings.SceneId + " gaze " + builder.ToString());

                    StartCoroutine(PostJsonRequest(outBytes, SceneURLGaze));
                }
            }
            else
            {
                Util.logError("CogntiveVR PlayerTracker.cs does not have scene key for scene " + CognitiveVR_Preferences.TrackingSceneName + "!");
            }

            playerSnapshots.Clear();
        }

        Dictionary<string, string> headers = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "X-HTTP-Method-Override", "POST" } };
        
        public IEnumerator PostJsonRequest(byte[] bytes, string url)
        {
            WWW www = new UnityEngine.WWW(url, bytes, headers);
            
            yield return www;

            if (Util.IsLoggingEnabled)
            {
                Util.logDebug(url + " PostJsonRequest response - " + (string.IsNullOrEmpty(www.error) ? "" : "<color=red>return error: " + www.error + "</color>") + " <color=green>return text: " + www.text + "</color>");
            }
        }

        void CleanupPlayerRecorderEvents()
        {
            //unsubscribe events
            //should i set all these events to null?
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
            SendDataEvent -= SendPlayerGazeSnapshots;
            SendDataEvent -= InstrumentationSubsystem.SendCachedTransactions;
            CognitiveVR_Manager.QuitEvent -= OnSendData;
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

        private static void WriteToFile(byte[] bytes, string appendFileName = "")
        {
            if (!System.IO.Directory.Exists("CognitiveVR_SceneExplorerExport"))
            {
                System.IO.Directory.CreateDirectory("CognitiveVR_SceneExplorerExport");
            }

            string playerID = System.DateTime.Now.ToString("d").Replace(':', '_').Replace(" ", "") + '_' + System.DateTime.Now.ToString("t").Replace('/', '_');

            string path = System.IO.Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "player" + playerID + appendFileName + ".json";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            //write file, using some kinda stream writer
            using (FileStream fs = File.Create(path))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }

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
        private static string SetPreGazePoint(double time, Vector3 position, Quaternion rotation, Vector3 gazepos)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
            builder.Append("{");

            JsonUtil.SetDouble("time", time, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", rotation, builder);
            builder.Append(",");
            JsonUtil.SetVector("g", gazepos, builder);

            builder.Append("}");

            return builder.ToString();
        }

        private static string SetFarplaneGazePoint(double time, Vector3 position, Quaternion rotation)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
            builder.Append("{");

            JsonUtil.SetDouble("time", time, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", rotation, builder);

            builder.Append("}");

            return builder.ToString();
        }

        //EvaluateGaze on a dynamic object
        private static string SetDynamicGazePoint(double time, Vector3 position, Quaternion rotation, Vector3 localGazePos, int objectId)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
            builder.Append("{");

            JsonUtil.SetDouble("time", time, builder);
            builder.Append(",");
            JsonUtil.SetInt("o", objectId, builder);
            builder.Append(",");
            JsonUtil.SetVector("p", position, builder);
            builder.Append(",");
            JsonUtil.SetQuat("r", rotation, builder);
            builder.Append(",");
            JsonUtil.SetVector("g", localGazePos, builder);

            builder.Append("}");

            return builder.ToString();
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