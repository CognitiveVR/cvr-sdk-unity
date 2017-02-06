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
        public List<PlayerSnapshot> playerSnapshots = new List<PlayerSnapshot>();

        Camera cam;
        PlayerRecorderHelper periodicRenderer;

        public void PlayerRecorderInit(Error initError)
        {
            CheckCameraSettings();

            if (CognitiveVR_Preferences.Instance.SendDataOnQuit)
                QuitEvent += OnSendData;
            
            SendDataEvent += SendPlayerGazeSnapshots;

#if CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_OnPoseEvent;
#endif
#if CVR_OCULUS
            OVRManager.HMDMounted += OVRManager_HMDMounted;
            OVRManager.HMDUnmounted += OVRManager_HMDUnmounted;
#endif
            if (CognitiveVR_Preferences.Instance.SendDataOnLevelLoad)
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            string sceneName = SceneManager.GetActiveScene().name;

            CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR.CognitiveVR_Preferences.Instance.FindScene(sceneName);
            if (sceneSettings != null)
            {
                if (!string.IsNullOrEmpty(sceneSettings.SceneId))
                    BeginPlayerRecording();
            }
            else
            {
                Util.logDebug("PlayerRecorderTracker - startup couldn't find scene -" + sceneName);
            }
            trackingSceneName = SceneManager.GetActiveScene().name;
        }

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

                CognitiveVR_Preferences.SceneSettings sceneSettings = CognitiveVR_Preferences.Instance.FindScene(activeScene.name);
                if (sceneSettings != null)
                {
                    if (!string.IsNullOrEmpty(sceneSettings.SceneId))
                    {
                        CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_OnTick;
                    }
                }
            }

            trackingSceneName = activeScene.name;
        }

        bool headsetPresent = true;
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

                EndPlayerRecording();
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
            //CognitiveVR_Manager.instance.SendPlayerGazeSnapshots();
        }

        public static void EndPlayerRecording()
        {
            CognitiveVR_Manager.TickEvent -= instance.CognitiveVR_Manager_OnTick;
            //instance.SendPlayerGazeSnapshots();
            instance.OnSendData();
            instance.trackingSceneName = SceneManager.GetActiveScene().name;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            CheckCameraSettings();

            if (!headsetPresent || CognitiveVR_Manager.HMD == null) { return; }

#if CVR_FOVE
            if (!Fove.FoveHeadset.GetHeadset().IsEyeTrackingCalibrated()) { return; }
#endif

            RenderTexture rt = null;
            if (CognitiveVR_Preferences.Instance.TrackGazePoint)
            {
                periodicRenderer.enabled = true;
                rt = periodicRenderer.DoRender(new RenderTexture(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution, 0));
                periodicRenderer.enabled = false;
            }

            PlayerSnapshot snapshot = new PlayerSnapshot();

            snapshot.Properties.Add("position", cam.transform.position);
            snapshot.Properties.Add("hmdForward", cam.transform.forward);
            snapshot.Properties.Add("nearDepth", cam.nearClipPlane);
            snapshot.Properties.Add("farDepth", cam.farClipPlane);
            snapshot.Properties.Add("renderDepth", rt);
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
            snapshot.Properties.Add("gazeDirection", worldGazeDirection);


            Vector2 screenGazePoint = Vector2.one * 0.5f;
#if CVR_FOVE //screenpoint
            var normalizedPoint = FoveInterface.GetNormalizedViewportPosition(ray.GetPoint(1000), Fove.EFVR_Eye.Left);

            //Vector2 gazePoint = hmd.GetGazePoint();
            if (float.IsNaN(normalizedPoint.x)) { return; }

            screenGazePoint = new Vector2(normalizedPoint.x, normalizedPoint.y);
#endif //fove screenpoint


            snapshot.Properties.Add("hmdGazePoint", screenGazePoint); //range between 0,0 and 1,1
#endif //gazetracker




#if CVR_STEAMVR
            if (Valve.VR.OpenVR.Chaperone != null)
                snapshot.Properties.Add("chaperoneVisible", Valve.VR.OpenVR.Chaperone.AreBoundsVisible());
#endif
            playerSnapshots.Add(snapshot);
            if (playerSnapshots.Count >= CognitiveVR_Preferences.Instance.SnapshotThreshold)
            {
                OnSendData();
            }
        }

        /// <summary>
        /// registered to OnSendData
        /// </summary>
        public void SendPlayerGazeSnapshots()
        {
            if (playerSnapshots.Count == 0 && InstrumentationSubsystem.CachedTransactions.Count == 0) { return; }

            var sceneSettings = CognitiveVR_Preferences.Instance.FindScene(trackingSceneName);
            if (sceneSettings == null)
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for " + trackingSceneName + "! Cancel Data Upload");
                return;
            }
            Util.logDebug("CognitiveVR_PlayerTracker.SendData " + playerSnapshots.Count + " gaze points " + InstrumentationSubsystem.CachedTransactions.Count + " event points on scene " + trackingSceneName + "(" + sceneSettings.SceneId + ")");

            if (CognitiveVR_Preferences.Instance.TrackGazePoint)
            {
                Texture2D depthTex = new Texture2D(PlayerSnapshot.Resolution, PlayerSnapshot.Resolution);
                for (int i = 0; i < playerSnapshots.Count; i++)
                {
                    Vector3 calcGazePoint = playerSnapshots[i].GetGazePoint(depthTex);
                    if (!float.IsNaN(calcGazePoint.x))
                    {
                        playerSnapshots[i].Properties.Add("gazePoint", calcGazePoint);
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
                }
            }

            if (CognitiveVR_Preferences.Instance.DebugWriteToFile)
            {
                Debug.LogWarning("Player Recorder writing player data to file!");

                if (playerSnapshots.Count > 0)
                    WriteToFile(FormatGazeToString(), "_GAZE_" + trackingSceneName);
                if (InstrumentationSubsystem.CachedTransactions.Count > 0)
                    WriteToFile(FormatEventsToString(), "_EVENTS_" + trackingSceneName);
            }

            if (sceneSettings != null)
            {
                string SceneURLGaze = "https://sceneexplorer.com/api/gaze/" + sceneSettings.SceneId;
                string SceneURLEvents = "https://sceneexplorer.com/api/events/" + sceneSettings.SceneId;

                Util.logDebug("uploading gaze and events to " + sceneSettings.SceneId);

                byte[] bytes;

                if (playerSnapshots.Count > 0)
                {
                    bytes = FormatGazeToString();
                    StartCoroutine(PostJsonRequest(bytes, SceneURLGaze));
                }
                if (InstrumentationSubsystem.CachedTransactions.Count > 0)
                {
                    bytes = FormatEventsToString();
                    StartCoroutine(PostJsonRequest(bytes, SceneURLEvents));
                }
            }
            else
            {
                Util.logError("CogntiveVR PlayerTracker.cs does not have scene key for scene " + trackingSceneName + "!");
            }

            playerSnapshots.Clear();
            InstrumentationSubsystem.CachedTransactions.Clear();
        }

        public IEnumerator PostJsonRequest(byte[] bytes, string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            WWW www = new UnityEngine.WWW(url, bytes, headers);

            yield return www;

            Util.logDebug("request finished - return: " + www.error);

        }

        void OnDestroyPlayerRecorder()
        {
            //unsubscribe events
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

        byte[] FormatEventsToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append("{");

            //header
            builder.Append(Json.Util.SetString("userid", Core.userId));
            builder.Append(",");
            builder.Append(Json.Util.SetObject("timestamp", CognitiveVR_Manager.TimeStamp));
            builder.Append(",");
            builder.Append(Json.Util.SetString("sessionid", CognitiveVR_Manager.SessionID));
            builder.Append(",");
            //builder.Append(SetString("keys", "userdata"));
            //builder.Append(",");


            //events
            builder.Append("\"data\":[");
            for (int i = 0; i<InstrumentationSubsystem.CachedTransactions.Count; i++)
            {
                builder.Append(SetTransaction(InstrumentationSubsystem.CachedTransactions[i]));
                builder.Append(",");
            }
            if (InstrumentationSubsystem.CachedTransactions.Count > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }
            builder.Append("]");

            builder.Append("}");

            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
            return outBytes;
        }

        byte[] FormatGazeToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append("{");

            //header
            builder.Append(Json.Util.SetString("userid", Core.userId));
            builder.Append(",");
            builder.Append(Json.Util.SetObject("timestamp", CognitiveVR_Manager.TimeStamp));
            builder.Append(",");
            builder.Append(Json.Util.SetString("sessionid", CognitiveVR_Manager.SessionID));
            builder.Append(",");

#if CVR_FOVE
            builder.Append(Json.Util.SetString("hmdtype", "fove"));
#else
            builder.Append(Json.Util.SetString("hmdtype", CognitiveVR.Util.GetSimpleHMDName()));
#endif
            builder.Append(",");

            //builder.Append(SetString("keys", "userdata"));
            //builder.Append(",");


            //events
            builder.Append("\"data\":[");
            for (int i = 0; i<playerSnapshots.Count; i++)
            {
                if (playerSnapshots[i] == null) { continue; }
                builder.Append(SetGazePont(playerSnapshots[i]));
                builder.Append(",");
            }
            //KNOWN BUG json format invalid if 0 gaze points are sent - not that there's anything to record, though
            builder.Remove(builder.Length - 1, 1);
            builder.Append("]");

            builder.Append("}");

            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(builder.ToString());
            return outBytes;
        }

        static void WriteToFile(byte[] bytes, string appendFileName = "")
        {
            if (!System.IO.Directory.Exists("CognitiveVR_SceneExplorerExport"))
            {
                System.IO.Directory.CreateDirectory("CognitiveVR_SceneExplorerExport");
            }

            string playerID = System.DateTime.Now.ToShortTimeString().Replace(':', '_').Replace(" ", "") + '_' + System.DateTime.Now.ToShortDateString().Replace('/', '_');
            string path = System.IO.Directory.GetCurrentDirectory() +Path.DirectorySeparatorChar+ "CognitiveVR_SceneExplorerExport"+ Path.DirectorySeparatorChar+"player" + playerID + appendFileName + ".json";

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

        /// <returns>{gaze point}</returns>
        public static string SetGazePont(PlayerSnapshot snap)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");

            builder.Append(Json.Util.SetObject("time", snap.timestamp));
            builder.Append(",");
            builder.Append(Json.Util.SetPos("p", (Vector3)snap.Properties["position"]));
            builder.Append(",");
            builder.Append(Json.Util.SetQuat("r", (Quaternion)snap.Properties["hmdRotation"]));
            builder.Append(",");
            builder.Append(Json.Util.SetPos("g", (Vector3)snap.Properties["gazePoint"]));

            builder.Append("}");

            return builder.ToString();
        }

        /// <returns>{snapshotstuff}</returns>
        public static string SetTransaction(TransactionSnapshot snap)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("{");

            builder.Append(Json.Util.SetString("name", snap.category));
            builder.Append(",");
            builder.Append(Json.Util.SetObject("time", snap.timestamp));
            builder.Append(",");
            builder.Append(Json.Util.SetVector("point", snap.position));


            if (snap.properties != null && snap.properties.Keys.Count > 0)
            {
                builder.Append(",");
                builder.Append("\"properties\":{");
                foreach (var v in snap.properties)
                {
                    if (v.Value.GetType() == typeof(string))
                    {
                        builder.Append(Json.Util.SetString(v.Key, (string)v.Value));
                    }
                    else
                    {
                        builder.Append(Json.Util.SetObject(v.Key, v.Value));
                    }
                    builder.Append(",");
                }
                builder.Remove(builder.Length - 1, 1); //remove last comma
                builder.Append("}"); //close properties object
            }

            builder.Append("}"); //close transaction object

            return builder.ToString();
        }
#endregion
    }

    namespace Json
    {
        public static class Util
        {
            /// <returns>"name":["obj","obj","obj"]</returns>
            public static string SetListString(string name, List<string> list)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":[");
                for (int i = 0; i < list.Count; i++)
                {
                    builder.Append("\"" + list[i] + "\"");
                    builder.Append(",");
                }
                builder.Remove(builder.Length - 1, 1);
                builder.Append("]");
                return builder.ToString();
            }

            /// <returns>"name":[obj,obj,obj]</returns>
            public static string SetListObject<T>(string name, List<T> list)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":{");
                for (int i = 0; i < list.Count; i++)
                {
                    builder.Append(list[i].ToString());
                    builder.Append(",");
                }
                builder.Remove(builder.Length - 1, 1);
                builder.Append("}");
                return builder.ToString();
            }

            /// <returns>"name":"stringval"</returns>
            public static string SetString(string name, string stringValue)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":");
                builder.Append("\"" + stringValue + "\"");

                return builder.ToString();
            }

            /// <returns>"name":objectValue.ToString()</returns>
            public static string SetObject(string name, object objectValue)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":");

                if (objectValue.GetType() == typeof(bool))
                    builder.Append(objectValue.ToString().ToLower());
                else
                    builder.Append(objectValue.ToString());

                return builder.ToString();
            }

            /// <returns>"name":[0.1,0.2,0.3]</returns>
            public static string SetVector(string name, float[] pos, bool centimeterLimit = true)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":[");

                if (centimeterLimit)
                {
                    builder.Append(string.Format("{0:0.00}", pos[0]));
                    builder.Append(",");
                    builder.Append(string.Format("{0:0.00}", pos[1]));
                    builder.Append(",");
                    builder.Append(string.Format("{0:0.00}", pos[2]));
                }
                else
                {
                    builder.Append(pos[0]);
                    builder.Append(",");
                    builder.Append(pos[1]);
                    builder.Append(",");
                    builder.Append(pos[2]);
                }

                builder.Append("]");
                return builder.ToString();
            }

            /// <returns>"name":[0.1,0.2,0.3]</returns>
            public static string SetPos(string name, Vector3 pos, bool centimeterLimit = true)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":[");

                if (centimeterLimit)
                {
                    builder.Append(string.Format("{0:0.00}", pos.x));
                    builder.Append(",");
                    builder.Append(string.Format("{0:0.00}", pos.y));
                    builder.Append(",");
                    builder.Append(string.Format("{0:0.00}", pos.z));
                }
                else
                {
                    builder.Append(pos.x);
                    builder.Append(",");
                    builder.Append(pos.y);
                    builder.Append(",");
                    builder.Append(pos.z);
                }

                builder.Append("]");
                return builder.ToString();
            }

            /// <returns>"name":[0.1,0.2,0.3,0.4]</returns>
            public static string SetQuat(string name, Quaternion quat)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":[");

                builder.Append(string.Format("{0:0.000}", quat.x));
                builder.Append(",");
                builder.Append(string.Format("{0:0.000}", quat.y));
                builder.Append(",");
                builder.Append(string.Format("{0:0.000}", quat.z));
                builder.Append(",");
                builder.Append(string.Format("{0:0.000}", quat.w));

                builder.Append("]");
                return builder.ToString();
            }

            /// <returns>"name":[0.1,0.2,0.3,0.4]</returns>
            public static string SetQuat(string name, float[] quat)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.Append("\"" + name + "\":[");

                builder.Append(string.Format("{0:0.000}", quat[0]));
                builder.Append(",");
                builder.Append(string.Format("{0:0.000}", quat[1]));
                builder.Append(",");
                builder.Append(string.Format("{0:0.000}", quat[2]));
                builder.Append(",");
                builder.Append(string.Format("{0:0.000}", quat[3]));

                builder.Append("]");
                return builder.ToString();
            }
        }
    }
}
