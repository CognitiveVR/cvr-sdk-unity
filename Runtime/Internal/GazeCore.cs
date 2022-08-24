using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cognitive3D;
using System.Text;
using Cognitive3D.External;

namespace Cognitive3D
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
            Cognitive3D_Manager.OnSendData += SendGazeData;
            gazebuilder = new StringBuilder(70 * Cognitive3D_Preferences.Instance.GazeSnapshotCount + 1200);
            gazebuilder.Append("{\"data\":[");
        }

        public static event Cognitive3D_Manager.onSendData OnGazeSend;

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
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);

            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (Cognitive3D_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }

            gazebuilder.Append("}");
            gazeCount++;
            if (gazeCount >= Cognitive3D_Preferences.Instance.GazeSnapshotCount)
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
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

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
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (Cognitive3D_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }
            gazebuilder.Append("}");

            gazeCount++;
            if (gazeCount >= Cognitive3D_Preferences.Instance.GazeSnapshotCount)
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
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", gazepoint, gazebuilder);
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (Cognitive3D_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }
            gazebuilder.Append("}");

            gazeCount++;
            if (gazeCount >= Cognitive3D_Preferences.Instance.GazeSnapshotCount)
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
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

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

            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", gpsloc, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", compass, gazebuilder);
            }
            if (Cognitive3D_Preferences.Instance.RecordFloorPosition)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }

            gazebuilder.Append("}");
            gazeCount++;
            if (gazeCount >= Cognitive3D_Preferences.Instance.GazeSnapshotCount)
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
            if (Cognitive3D_Preferences.Instance.EnableGaze == false)
            {
                SendSessionProperties(copyDataToCache);
                return;
            }

            if (gazeCount == 0) { return; }

            if (!Cognitive3D_Manager.IsInitialized)
            {
                return;
            }

            if (string.IsNullOrEmpty(Cognitive3D_Manager.TrackingSceneId))
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
            JsonUtil.SetString("userid", Cognitive3D_Manager.DeviceId, gazebuilder);
            gazebuilder.Append(",");

            if (!string.IsNullOrEmpty(Cognitive3D_Manager.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Cognitive3D_Manager.LobbyId, gazebuilder);
                gazebuilder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)Cognitive3D_Manager.SessionTimeStamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("sessionid", Cognitive3D_Manager.SessionID, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetInt("part", jsonPart, gazebuilder);
            jsonPart++;
            gazebuilder.Append(",");

            JsonUtil.SetString("hmdtype", HMDName, gazebuilder);

            gazebuilder.Append(",");
            JsonUtil.SetFloat("interval", Cognitive3D.Cognitive3D_Preferences.Instance.SnapshotInterval, gazebuilder);
            gazebuilder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", gazebuilder);
            
            if (Cognitive3D_Manager.ForceWriteSessionMetadata) //if scene changed and haven't sent metadata recently
            {
                Cognitive3D_Manager.ForceWriteSessionMetadata = false;
                gazebuilder.Append(",");
                gazebuilder.Append("\"properties\":{");
                foreach (var kvp in Cognitive3D_Manager.GetAllSessionProperties(true))
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
            else if (Cognitive3D_Manager.GetNewSessionProperties(false).Count > 0) //if a session property has changed
            {
                gazebuilder.Append(",");
                gazebuilder.Append("\"properties\":{");
                foreach (var kvp in Cognitive3D_Manager.GetNewSessionProperties(true))
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

            var sceneSettings = Cognitive3D_Manager.TrackingScene;
            string url = CognitiveStatics.POSTGAZEDATA(sceneSettings.SceneId, sceneSettings.VersionNumber);
            string content = gazebuilder.ToString();

            if (copyDataToCache)
            {
                if (Cognitive3D_Manager.NetworkManager.runtimeCache != null && Cognitive3D_Manager.NetworkManager.runtimeCache.CanWrite(url,content))
                {
                    Cognitive3D_Manager.NetworkManager.runtimeCache.WriteContent(url, content);
                }
            }

            Cognitive3D_Manager.NetworkManager.Post(url, content);
            if (OnGazeSend != null)
                OnGazeSend.Invoke(copyDataToCache);

            //gazebuilder = new StringBuilder(70 * Cognitive3D_Preferences.Instance.GazeSnapshotCount + 200);
            gazebuilder.Length = 9;
            //gazebuilder.Append("{\"data\":[");
        }

        public static void SendSessionProperties(bool copyDataToCache)
        {
            if (!Cognitive3D_Manager.IsInitialized)
            {
                return;
            }

            if (!Cognitive3D_Manager.ForceWriteSessionMetadata) //if scene has not changed
            {
                if (Cognitive3D_Manager.GetNewSessionProperties(false).Count == 0) //and there are no new properties
                {
                    return;
                }
            }
            //if the scene has changed, send

            if (string.IsNullOrEmpty(Cognitive3D_Manager.TrackingSceneId))
            {
                Util.logDebug("Cognitive GazeCore.SendData could not find scene settings for scene! do not upload gaze to sceneexplorer");
                //dump gaze data
                gazebuilder.Length = 9;
                gazeCount = 0;
                return;
            }

            StringBuilder propertyBuilder = new StringBuilder();

            propertyBuilder.Append("{");

            //header
            JsonUtil.SetString("userid", Cognitive3D_Manager.DeviceId, propertyBuilder);
            propertyBuilder.Append(",");

            if (!string.IsNullOrEmpty(Cognitive3D_Manager.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Cognitive3D_Manager.LobbyId, propertyBuilder);
                propertyBuilder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)Cognitive3D_Manager.SessionTimeStamp, propertyBuilder);
            propertyBuilder.Append(",");
            JsonUtil.SetString("sessionid", Cognitive3D_Manager.SessionID, propertyBuilder);
            propertyBuilder.Append(",");
            JsonUtil.SetInt("part", jsonPart, propertyBuilder);
            jsonPart++;
            propertyBuilder.Append(",");

            JsonUtil.SetString("hmdtype", HMDName, propertyBuilder);

            propertyBuilder.Append(",");
            JsonUtil.SetFloat("interval", Cognitive3D.Cognitive3D_Preferences.Instance.SnapshotInterval, propertyBuilder);
            propertyBuilder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", propertyBuilder);

            if (Cognitive3D_Manager.ForceWriteSessionMetadata) //if scene changed and haven't sent metadata recently
            {
                Cognitive3D_Manager.ForceWriteSessionMetadata = false;
                propertyBuilder.Append(",");
                propertyBuilder.Append("\"properties\":{");
                foreach (var kvp in Cognitive3D_Manager.GetAllSessionProperties(true))
                {
                    if (kvp.Value == null) { Util.logDevelopment("Session Property " + kvp.Key + " is NULL "); continue; }
                    if (kvp.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(kvp.Key, (string)kvp.Value, propertyBuilder);
                    }
                    else if (kvp.Value.GetType() == typeof(float))
                    {
                        JsonUtil.SetFloat(kvp.Key, (float)kvp.Value, propertyBuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(kvp.Key, kvp.Value, propertyBuilder);
                    }
                    propertyBuilder.Append(",");
                }
                propertyBuilder.Remove(propertyBuilder.Length - 1, 1); //remove comma
                propertyBuilder.Append("}");
            }
            else if (Cognitive3D_Manager.GetNewSessionProperties(false).Count > 0) //if a session property has changed
            {
                propertyBuilder.Append(",");
                propertyBuilder.Append("\"properties\":{");
                foreach (var kvp in Cognitive3D_Manager.GetNewSessionProperties(true))
                {
                    if (kvp.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(kvp.Key, (string)kvp.Value, propertyBuilder);
                    }
                    else if (kvp.Value.GetType() == typeof(float))
                    {
                        JsonUtil.SetFloat(kvp.Key, (float)kvp.Value, propertyBuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(kvp.Key, kvp.Value, propertyBuilder);
                    }
                    propertyBuilder.Append(",");
                }
                propertyBuilder.Remove(propertyBuilder.Length - 1, 1); //remove comma
                propertyBuilder.Append("}");
            }

            propertyBuilder.Append("}");

            var sceneSettings = Cognitive3D_Manager.TrackingScene;
            string url = CognitiveStatics.POSTGAZEDATA(sceneSettings.SceneId, sceneSettings.VersionNumber);
            string content = propertyBuilder.ToString();

            if (copyDataToCache)
            {
                if (Cognitive3D_Manager.NetworkManager.runtimeCache != null && Cognitive3D_Manager.NetworkManager.runtimeCache.CanWrite(url, content))
                {
                    Cognitive3D_Manager.NetworkManager.runtimeCache.WriteContent(url, content);
                }
            }

            Cognitive3D_Manager.NetworkManager.Post(url, content);
            if (OnGazeSend != null)
                OnGazeSend.Invoke(copyDataToCache);
        }
    }
}