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
        string trackingSceneName;
        
        //snapshots still 'exist' so the rendertexture can be evaluated
        private List<PlayerSnapshot> playerSnapshots = new List<PlayerSnapshot>();

        //a list of snapshots already formated to string
        private List<string> savedGazeSnapshots = new List<string>();
        private int jsonpart = 1;
        //public bool EvaluateGazeRealtime = true;

        Camera cam;
        PlayerRecorderHelper periodicRenderer;

        bool headsetPresent = true;
        private RenderTexture rt;

        public void PlayerRecorderInit(Error initError)
        {
            if (initError != Error.Success) { return; }
            CheckCameraSettings();

            if (CognitiveVR_Preferences.Instance.SendDataOnQuit)
                QuitEvent += OnSendData;

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
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            string sceneName = SceneManager.GetActiveScene().name;

            CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR.CognitiveVR_Preferences.Instance.FindScene(sceneName);
            if (sceneSettings != null)
            {
                if (!string.IsNullOrEmpty(sceneSettings.SceneId))
                {
                    BeginPlayerRecording();
                    CoreSubsystem.CurrentSceneId = sceneSettings.SceneId;
                }
            }
            else
            {
                Util.logDebug("PlayerRecorderTracker - startup couldn't find scene -" + sceneName);
            }
            trackingSceneName = SceneManager.GetActiveScene().name;
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
            if (CognitiveVR_Manager.HMD == null) { return; }

            if (periodicRenderer == null)
            {
                periodicRenderer = CognitiveVR_Manager.HMD.GetComponent<PlayerRecorderHelper>();
                if (periodicRenderer == null)
                {
                    periodicRenderer = CognitiveVR_Manager.HMD.gameObject.AddComponent<PlayerRecorderHelper>();
                    periodicRenderer.enabled = false;
                }
            }
            if (cam == null)
                cam = CognitiveVR_Manager.HMD.GetComponent<Camera>();

            if (cam.depthTextureMode != DepthTextureMode.Depth)
                cam.depthTextureMode = DepthTextureMode.Depth;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (!CognitiveVR_Preferences.Instance.SendDataOnLevelLoad) { return; }

            Scene activeScene = arg0;

            if (!string.IsNullOrEmpty(trackingSceneName))
            {
                CognitiveVR_Preferences.SceneSettings lastSceneSettings = CognitiveVR_Preferences.Instance.FindScene(trackingSceneName);
                if (lastSceneSettings != null)
                {
                    if (!string.IsNullOrEmpty(lastSceneSettings.SceneId))
                    {
                        OnSendData();
                        //SendPlayerGazeSnapshots();
                        CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
                    }
                }

                CoreSubsystem.CurrentSceneId = string.Empty;

                CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR_Preferences.Instance.FindScene(activeScene.name);
                if (sceneSettings != null)
                {
                    if (!string.IsNullOrEmpty(sceneSettings.SceneId))
                    {
                        CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_OnTick;
                        CoreSubsystem.CurrentSceneId = sceneSettings.SceneId;
                    }
                }
            }

            trackingSceneName = activeScene.name;
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
            if (!CognitiveVR_Preferences.Instance.SendDataOnHotkey) { return; }
            if (Input.GetKeyDown(CognitiveVR_Preferences.Instance.SendDataHotkey))
            {
                CognitiveVR_Preferences prefs = CognitiveVR_Preferences.Instance;

                if (prefs.HotkeyShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) { return; }
                if (prefs.HotkeyAlt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) { return; }
                if (prefs.HotkeyCtrl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) { return; }

                SendPlayerRecording();
            }
        }

        public static void BeginPlayerRecording()
        {
            var scenedata = CognitiveVR_Preferences.Instance.FindSceneByPath(SceneManager.GetActiveScene().path);

            if (scenedata == null)
            {
                CognitiveVR.Util.logDebug(SceneManager.GetActiveScene().name + " Scene data is null! Player Recorder has nowhere to upload data");
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
            instance.trackingSceneName = SceneManager.GetActiveScene().name;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            HasRequestedDynamicGazeRaycast = false;
            //hasHitDynamic = false;

            CheckCameraSettings();

            if (!headsetPresent || CognitiveVR_Manager.HMD == null) { return; }

#if CVR_FOVE
            if (!Fove.FoveHeadset.GetHeadset().IsEyeTrackingCalibrated()) { return; }
#endif

            if (CognitiveVR_Preferences.Instance.TrackGazePoint)
            {
                if (CognitiveVR_Preferences.Instance.EvaluateGazeRealtime)
                {
                    periodicRenderer.enabled = true;
                    rt = periodicRenderer.DoRender(rt);
                    periodicRenderer.enabled = false;
                    return;
                }
                else
                {
                    TickPostRender(Vector3.zero);
                }
            }
            else
            {
                TickPostRender(Vector3.zero);
            }
        }

        //dynamic object
        public static bool HasRequestedDynamicGazeRaycast { get; private set; }
        public static bool HasHitDynamic = true;
        public static void RequestDynamicObjectGaze()
        {
            HasRequestedDynamicGazeRaycast = true;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(HMD.position, HMD.forward, out hit))
            {
                var dynamicHit = hit.collider.GetComponent<DynamicObject>();
                if (dynamicHit == null) { return; }

                //pass an objectid into the snapshot properties
                instance.TickPostRender(hit.transform.InverseTransformPoint(hit.point), dynamicHit.ObjectId.Id);

                HasHitDynamic = true;

                Debug.DrawRay(hit.point, Vector3.up, Color.green, 1);
                Debug.DrawRay(hit.point, Vector3.right, Color.red, 1);
                Debug.DrawRay(hit.point, Vector3.forward, Color.blue, 1);

                //this gets the object and the 'physical' point on the object
                //TODO this could use the depth buffer to get the point. or maybe average between the raycasthit.point and the world depth point?
                //to do this, defer this into TickPostRender and check EvaluateGazeRealtime
            }
        }



        //called from periodicrenderer OnPostRender or immediately after on tick if realtime gaze eval is disabled
        public void TickPostRender(Vector3 localPos, int objectId = -1)
        {
            if (CognitiveVR_Preferences.Instance.EvaluateGazeRealtime && HasHitDynamic) { return; }

            PlayerSnapshot snapshot = new PlayerSnapshot();
            if (objectId >= 0)
            {
                snapshot.Properties.Add("objectId", objectId);
                snapshot.Properties.Add("localGaze", localPos);
            }

            snapshot.Properties.Add("position", cam.transform.position);
            snapshot.Properties.Add("hmdForward", cam.transform.forward);
            snapshot.Properties.Add("nearDepth", cam.nearClipPlane);
            snapshot.Properties.Add("farDepth", cam.farClipPlane);
            if (CognitiveVR_Preferences.Instance.EvaluateGazeRealtime)
            {
                snapshot.Properties.Add("renderDepth", rt); //this is constantly getting overwritten
            }
            else
            {
                periodicRenderer.enabled = true;
                RenderTexture newrt = new RenderTexture(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, 0);
                newrt = periodicRenderer.DoRender(newrt);
                periodicRenderer.enabled = false;
                snapshot.Properties.Add("renderDepth", newrt);
            }

            snapshot.Properties.Add("hmdRotation", cam.transform.rotation);

#if CVR_GAZETRACK

            //gaze tracking sdks need to return a v3 direction "gazeDirection" and a v2 point "hmdGazePoint"
            //the v2 point is used to get a pixel from the render texture

            Vector3 worldGazeDirection = Vector3.forward;

#if CVR_FOVE //direction
            var eyeRays = FoveInterface.GetEyeRays();
            var ray = eyeRays.left;
            worldGazeDirection = new Vector3(ray.direction.x, ray.direction.y, ray.direction.z);
            worldGazeDirection.Normalize();
#endif //fove direction
#if CVR_PUPIL //direction
            var v2 = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes); //0-1 screen pos

            //if it doesn't find the eyes, skip this snapshot
            if (PupilGazeTracker.Instance.Confidence < 0.5f) { return; }

            var ray = cam.ViewportPointToRay(v2);
            worldGazeDirection = ray.direction.normalized;
#endif //pupil direction

            snapshot.Properties.Add("gazeDirection", worldGazeDirection);


            Vector2 screenGazePoint = Vector2.one * 0.5f;
#if CVR_FOVE //screenpoint
            var normalizedPoint = FoveInterface.GetNormalizedViewportPosition(ray.GetPoint(1000), Fove.EFVR_Eye.Left);

            //Vector2 gazePoint = hmd.GetGazePoint();
            if (float.IsNaN(normalizedPoint.x)) { return; }

            screenGazePoint = new Vector2(normalizedPoint.x, normalizedPoint.y);
#endif //fove screenpoint
#if CVR_PUPIL//screenpoint
            screenGazePoint = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes);
#endif //pupil screenpoint

            snapshot.Properties.Add("hmdGazePoint", screenGazePoint); //range between 0,0 and 1,1
#endif //gazetracker



            playerSnapshots.Add(snapshot);
            if (CognitiveVR_Preferences.Instance.EvaluateGazeRealtime)
            {
                if (CognitiveVR_Preferences.Instance.TrackGazePoint)
                {
                    if (snapshot.Properties.ContainsKey("objectId"))
                    {
                        savedGazeSnapshots.Add(SetDynamicGazePoint(Util.Timestamp(), cam.transform.position, cam.transform.rotation, (Vector3)snapshot.Properties["localGaze"], (int)snapshot.Properties["objectId"]));
                    }
                    else
                    {
                        Vector3 calcGazePoint = snapshot.GetGazePoint(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution);

                        if (!float.IsNaN(calcGazePoint.x))
                        {
                            savedGazeSnapshots.Add(SetPreGazePoint(Util.Timestamp(), cam.transform.position, cam.transform.rotation, calcGazePoint));
#if CVR_DEBUG
                        Debug.DrawLine(HMD.position, calcGazePoint, Color.yellow, 5);
                        Debug.DrawRay(calcGazePoint, Vector3.up, Color.green, 5);
                        Debug.DrawRay(calcGazePoint, Vector3.right, Color.red, 5);
                        Debug.DrawRay(calcGazePoint, Vector3.forward, Color.blue, 5);
#endif
                        }
                        else
                        {
                            snapshot = null;
                        }
                    }
                }
                else if (CognitiveVR_Preferences.Instance.GazePointFromDirection)
                {
                    Vector3 position = (Vector3)snapshot.Properties["position"] + (Vector3)snapshot.Properties["hmdForward"] * CognitiveVR_Preferences.Instance.GazeDirectionMultiplier;
                    savedGazeSnapshots.Add(SetPreGazePoint(Util.Timestamp(), cam.transform.position, cam.transform.rotation, position));
                }
            }
            else //cache and save evaluation for later
            {
                if (snapshot.Properties.ContainsKey("objectId"))
                {
                    savedGazeSnapshots.Add(SetDynamicGazePoint(Util.Timestamp(), cam.transform.position, cam.transform.rotation, (Vector3)snapshot.Properties["localGaze"], (int)snapshot.Properties["objectId"]));
                }
                else
                {
                    savedGazeSnapshots.Add(SetPreGazePoint(Util.Timestamp(), cam.transform.position, cam.transform.rotation));
                }
            }

            if (playerSnapshots.Count >= CognitiveVR_Preferences.Instance.GazeSnapshotCount)
            {
                OnSendData();
            }
        }

        /// <summary>
        /// registered to OnSendData
        /// </summary>
        public void SendPlayerGazeSnapshots()
        {
            if (playerSnapshots.Count == 0) { return; }

            var sceneSettings = CognitiveVR_Preferences.Instance.FindScene(trackingSceneName);
            if (sceneSettings == null)
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for " + trackingSceneName + "! Cancel Data Upload");
                return;
            }
            if (string.IsNullOrEmpty(sceneSettings.SceneId))
            {
                playerSnapshots.Clear();
                CognitiveVR.Util.logDebug("sceneid is empty. do not send gaze objects to sceneexplorer");
                return;
            }

            if (!CognitiveVR_Preferences.Instance.EvaluateGazeRealtime)
            {
                if (CognitiveVR_Preferences.Instance.TrackGazePoint)
                {
                    for (int i = 0; i < playerSnapshots.Count; i++)
                    {
                        Vector3 calcGazePoint = playerSnapshots[i].GetGazePoint(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution);
                        if (!float.IsNaN(calcGazePoint.x))
                        {
                            playerSnapshots[i].Properties.Add("gazePoint", calcGazePoint);
                            savedGazeSnapshots[i] = savedGazeSnapshots[i].Replace("GAZE", JsonUtil.SetVector("g", calcGazePoint));
#if CVR_DEBUG
                            Debug.DrawLine((Vector3)playerSnapshots[i].Properties["position"], (Vector3)playerSnapshots[i].Properties["gazePoint"], Color.yellow, 5);
                            Debug.DrawRay((Vector3)playerSnapshots[i].Properties["gazePoint"], Vector3.up, Color.green, 5);
                            Debug.DrawRay((Vector3)playerSnapshots[i].Properties["gazePoint"], Vector3.right, Color.red, 5);
                            Debug.DrawRay((Vector3)playerSnapshots[i].Properties["gazePoint"], Vector3.forward, Color.blue, 5);
#endif
                        }
                        else
                        {
                            playerSnapshots[i] = null;
                        }
                    }
                }
                else if (CognitiveVR_Preferences.Instance.GazePointFromDirection)
                {
                    for (int i = 0; i < playerSnapshots.Count; i++)
                    {
                        Vector3 position = (Vector3)playerSnapshots[i].Properties["position"] + (Vector3)playerSnapshots[i].Properties["hmdForward"] * CognitiveVR_Preferences.Instance.GazeDirectionMultiplier;

                        Debug.DrawRay((Vector3)playerSnapshots[i].Properties["position"], (Vector3)playerSnapshots[i].Properties["hmdForward"] * CognitiveVR_Preferences.Instance.GazeDirectionMultiplier, Color.yellow, 5);

                        playerSnapshots[i].Properties.Add("gazePoint", position);
                        savedGazeSnapshots[i] = savedGazeSnapshots[i].Replace("GAZE", JsonUtil.SetVector("g", position));
                    }
                }
            }

            if (sceneSettings != null)
            {
                Util.logDebug("uploading gaze and events to " + sceneSettings.SceneId);

                if (playerSnapshots.Count > 0)
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
                    builder.Append(JsonUtil.SetObject("part", jsonpart));
                    jsonpart++;
                    builder.Append(",");

#if CVR_FOVE
            builder.Append(JsonUtil.SetString("hmdtype", "fove"));
#else
                    builder.Append(JsonUtil.SetString("hmdtype", CognitiveVR.Util.GetSimpleHMDName()));
#endif
                    builder.Append(",");
                    builder.Append(JsonUtil.SetObject("interval", CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval));
                    builder.Append(",");


                    //events
                    builder.Append("\"data\":[");
                    for (int i = 0; i < playerSnapshots.Count; i++)
                    {
                        if (playerSnapshots[i] == null) { continue; }
                        //builder.Append(SetGazePont(playerSnapshots[i]));
                        builder.Append(savedGazeSnapshots[i]);
                        builder.Append(",");
                    }
                    if (playerSnapshots.Count > 0)
                        builder.Remove(builder.Length - 1, 1);
                    builder.Append("]");

                    builder.Append("}");



                    byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
                    string SceneURLGaze = "https://sceneexplorer.com/api/gaze/" + sceneSettings.SceneId;

                    if (CognitiveVR_Preferences.Instance.DebugWriteToFile)
                    {
                        Debug.LogWarning("Player Recorder writing player data to file!");

                        if (playerSnapshots.Count > 0)
                            WriteToFile(outBytes, "_GAZE_" + trackingSceneName);
                    }

                    CognitiveVR.Util.logDebug(builder.ToString());

                    StartCoroutine(PostJsonRequest(outBytes, SceneURLGaze));
                }
            }
            else
            {
                Util.logError("CogntiveVR PlayerTracker.cs does not have scene key for scene " + trackingSceneName + "!");
            }

            playerSnapshots.Clear();
            savedGazeSnapshots.Clear();
        }

        public IEnumerator PostJsonRequest(byte[] bytes, string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            WWW www = new UnityEngine.WWW(url, bytes, headers);

            yield return www;

            Util.logDebug(url + " PostJsonRequest response - " + (string.IsNullOrEmpty(www.error) ? "" : "<color=red>return error: " + www.error) + "</color> <color=green>return text: " + www.text + "</color>");
        }

        void OnDestroyPlayerRecorder()
        {
            //unsubscribe events
            //should i set all these events to null?
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
            SendDataEvent -= SendPlayerGazeSnapshots;
            CognitiveVR_Manager.QuitEvent -= OnSendData;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
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
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");

            builder.Append(JsonUtil.SetObject("time", time));
            builder.Append(",");
            builder.Append(JsonUtil.SetVector("p", position));
            builder.Append(",");
            builder.Append(JsonUtil.SetQuat("r", rotation));
            builder.Append(",");
            builder.Append("GAZE");

            builder.Append("}");

            return builder.ToString();
        }

        //EvaluateGazeRealtime
        private static string SetPreGazePoint(double time, Vector3 position, Quaternion rotation, Vector3 gazepos)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");

            builder.Append(JsonUtil.SetObject("time", time));
            builder.Append(",");
            builder.Append(JsonUtil.SetVector("p", position));
            builder.Append(",");
            builder.Append(JsonUtil.SetQuat("r", rotation));
            builder.Append(",");
            builder.Append(JsonUtil.SetVector("g", gazepos));

            builder.Append("}");

            return builder.ToString();
        }

        //EvaluateGaze on a dynamic object
        private static string SetDynamicGazePoint(double time, Vector3 position, Quaternion rotation, Vector3 localGazePos, int objectId)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");

            builder.Append(JsonUtil.SetObject("time", time));
            builder.Append(",");
            builder.Append(JsonUtil.SetObject("o", objectId));
            builder.Append(",");
            builder.Append(JsonUtil.SetVector("p", position));
            builder.Append(",");
            builder.Append(JsonUtil.SetQuat("r", rotation));
            builder.Append(",");
            builder.Append(JsonUtil.SetVector("g", localGazePos));

            builder.Append("}");

            return builder.ToString();
        }

#endregion
    }
}
