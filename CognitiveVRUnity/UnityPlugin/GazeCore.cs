using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;
using System.Text;
using CognitiveVR.External;

namespace CognitiveVR
{
    public enum GazeType
    {
        Physics, //raycast
        Command //command buffer
    }

    public static class GazeCore
    {
        private static int jsonPart = 1;
        private static StringBuilder gazebuilder;
        private static int gazeCount = 0;
        private static string HMDName;
        public static int CachedGaze { get { return gazeCount; } }

        static GazeCore()
        {
            Core.OnSendData += SendGazeData;
            gazebuilder = new StringBuilder(70 * CognitiveVR_Preferences.Instance.GazeSnapshotCount + 1200);
            gazebuilder.Append("{\"data\":[");
        }

        public static event Core.onDataSend OnGazeSend;

        public delegate void onGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation);
        public static event onGazeRecord OnDynamicGazeRecord;
        public static void DynamicGazeRecordEvent(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnDynamicGazeRecord != null)
                OnDynamicGazeRecord.Invoke(timestamp,objectid,localgazepoint,hmdpoint,hmdrotation);
        }
        public static event onGazeRecord OnWorldGazeRecord;
        public static void WorldGazeRecordEvent(double timestamp, string ignored, Vector3 worldgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnWorldGazeRecord != null)
                OnWorldGazeRecord.Invoke(timestamp, ignored, worldgazepoint, hmdpoint, hmdrotation);
        }
        public static event onGazeRecord OnSkyGazeRecord;
        public static void SkyGazeRecordEvent(double timestamp, string ignored, Vector3 ignored2, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnSkyGazeRecord != null)
                OnSkyGazeRecord.Invoke(timestamp, ignored, ignored2, hmdpoint, hmdrotation);
        }

        public static void SetHMDType(string hmdname)
        {
            HMDName = hmdname;
        }

        ///sky position
        public static void RecordGazePoint(double timestamp, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos) //looking at the camera far plane
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);

            if (CognitiveVR_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (CognitiveVR_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }

            gazebuilder.Append("}");
            gazeCount++;
            if (gazeCount >= CognitiveVR_Preferences.Instance.GazeSnapshotCount)
            {
                SendGazeData(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
            SkyGazeRecordEvent(timestamp, string.Empty, Vector3.zero, hmdpoint, hmdrotation);
        }

        //gaze on dynamic object
        public static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos) //looking at a dynamic object
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("o", objectid, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", localgazepoint, gazebuilder);
            if (CognitiveVR_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (CognitiveVR_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }
            gazebuilder.Append("}");

            gazeCount++;
            if (gazeCount >= CognitiveVR_Preferences.Instance.GazeSnapshotCount)
            {
                SendGazeData(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
            DynamicGazeRecordEvent(timestamp, objectid, localgazepoint, hmdpoint, hmdrotation);
        }

        //world position
        public static void RecordGazePoint(double timestamp, Vector3 gazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos) //looking at world
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", gazepoint, gazebuilder);
            if (CognitiveVR_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (CognitiveVR_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }
            gazebuilder.Append("}");

            gazeCount++;
            if (gazeCount >= CognitiveVR_Preferences.Instance.GazeSnapshotCount)
            {
                SendGazeData(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
            WorldGazeRecordEvent(timestamp, string.Empty, gazepoint, hmdpoint, hmdrotation);
        }

        //looking at a media dynamic object
        //mediatime is milliseconds since the start of the video
        public static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, string mediasource, int mediatimeMs, Vector2 uvs, Vector3 floorPos)
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("o", objectid, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", localgazepoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("mediaId", mediasource, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetInt("mediatime", mediatimeMs, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector2("uvs", uvs, gazebuilder);

            if (CognitiveVR_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (CognitiveVR_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }

            gazebuilder.Append("}");
            gazeCount++;
            if (gazeCount >= CognitiveVR_Preferences.Instance.GazeSnapshotCount)
            {
                SendGazeData(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
            DynamicGazeRecordEvent(timestamp, objectid, localgazepoint, hmdpoint, hmdrotation);
        }

        private static void SendGazeData(bool copyDataToCache)
        {
            if (gazeCount == 0) { return; }

            if (!Core.IsInitialized)
            {
                return;
            }

            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                Util.logDebug("Cognitive GazeCore.SendData could not find scene settings for scene! do not upload gaze to sceneexplorer");
                //dump gaze data
                gazebuilder.Length = 9;
                gazeCount = 0;
                return;
            }

            if (gazebuilder[gazebuilder.Length-1] == ',')
            {
                gazebuilder = gazebuilder.Remove(gazebuilder.Length-1, 1);
            }

            gazebuilder.Append("],");

            gazeCount =0;

            //header
            JsonUtil.SetString("userid", Core.DeviceId, gazebuilder);
            gazebuilder.Append(",");

            if (!string.IsNullOrEmpty(Core.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Core.LobbyId, gazebuilder);
                gazebuilder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)Core.SessionTimeStamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("sessionid", Core.SessionID, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetInt("part", jsonPart, gazebuilder);
            jsonPart++;
            gazebuilder.Append(",");

            JsonUtil.SetString("hmdtype", HMDName, gazebuilder);

            gazebuilder.Append(",");
            JsonUtil.SetFloat("interval", CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval, gazebuilder);
            gazebuilder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", gazebuilder);
            
            if (Core.ForceWriteSessionMetadata) //if scene changed and haven't sent metadata recently
            {
                Core.ForceWriteSessionMetadata = false;
                gazebuilder.Append(",");
                gazebuilder.Append("\"properties\":{");
                foreach (var kvp in Core.GetAllSessionProperties(true))
                {
                    if (kvp.Value == null) { Util.logDevelopment("Session Property " +kvp.Key+" is NULL "); continue; }
                    if (kvp.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(kvp.Key, (string)kvp.Value, gazebuilder);
                    }
                    else if (kvp.Value.GetType() == typeof(float))
                    {
                        JsonUtil.SetFloat(kvp.Key, (float)kvp.Value, gazebuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(kvp.Key, kvp.Value, gazebuilder);
                    }
                    gazebuilder.Append(",");
                }
                gazebuilder.Remove(gazebuilder.Length - 1, 1); //remove comma
                gazebuilder.Append("}");
            }
            else if (Core.GetNewSessionProperties(false).Count > 0) //if a session property has changed
            {
                gazebuilder.Append(",");
                gazebuilder.Append("\"properties\":{");
                foreach (var kvp in Core.GetNewSessionProperties(true))
                {
                    if (kvp.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(kvp.Key, (string)kvp.Value, gazebuilder);
                    }
                    else if (kvp.Value.GetType() == typeof(float))
                    {
                        JsonUtil.SetFloat(kvp.Key, (float)kvp.Value, gazebuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(kvp.Key, kvp.Value, gazebuilder);
                    }
                    gazebuilder.Append(",");
                }
                gazebuilder.Remove(gazebuilder.Length - 1, 1); //remove comma
                gazebuilder.Append("}");
            }

            gazebuilder.Append("}");

            var sceneSettings = Core.TrackingScene;
            string url = CognitiveStatics.POSTGAZEDATA(sceneSettings.SceneId, sceneSettings.VersionNumber);
            string content = gazebuilder.ToString();

            if (copyDataToCache)
            {
                if (NetworkManager.lc != null && NetworkManager.lc.CanAppend(url, content))
                {
                    NetworkManager.lc.Append(url, content);
                }
            }

            CognitiveVR.NetworkManager.Post(url, content);
            if (OnGazeSend != null)
                OnGazeSend.Invoke();

            //gazebuilder = new StringBuilder(70 * CognitiveVR_Preferences.Instance.GazeSnapshotCount + 200);
            gazebuilder.Length = 9;
            //gazebuilder.Append("{\"data\":[");
        }
    }
}