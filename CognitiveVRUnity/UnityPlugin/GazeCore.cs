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
        Command, //command buffer
        Depth, //classic depth rendering
        //Sphere, //inverted sphere media
    }

    public static class GazeCore
    {
        private static int jsonPart = 1;
        //private static Dictionary<string, List<string>> CachedSnapshots = new Dictionary<string, List<string>>();
        private static StringBuilder gazebuilder;
        private static int gazeCount = 0;
        private static string HMDName;
        //private static int currentSensorSnapshots = 0;

        static GazeCore()
        {
            Core.OnSendData += SendGazeData;
            Core.CheckSessionId();

            gazebuilder = new StringBuilder(70 * CognitiveVR_Preferences.Instance.GazeSnapshotCount + 1200);
            gazebuilder.Append("{\"data\":[");
        }

        public static void SetHMDType(string hmdname)
        {
            HMDName = hmdname;
        }

        ///sky position
        public static void RecordGazePoint(double timestamp, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos) //looking at the camera far plane
        {
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
                SendGazeData();
            }
            else
            {
                gazebuilder.Append(",");
            }
        }

        //gaze on dynamic object
        public static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos) //looking at a dynamic object
        {
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
                SendGazeData();
            }
            else
            {
                gazebuilder.Append(",");
            }
        }

        //world position
        public static void RecordGazePoint(double timestamp, Vector3 gazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos) //looking at world
        {
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
                SendGazeData();
            }
            else
            {
                gazebuilder.Append(",");
            }
        }

        //looking at a media dynamic object
        //mediatime is milliseconds since the start of the video
        public static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, string mediasource, int mediatimeMs, Vector2 uvs, Vector3 floorPos)
        {
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
                SendGazeData();
            }
            else
            {
                gazebuilder.Append(",");
            }
        }

        private static void SendGazeData()
        {
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
            JsonUtil.SetString("userid", Core.UniqueID, gazebuilder);
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
            
            if (Core.GetNewSessionProperties(false).Count > 0)
            {
                gazebuilder.Append(",");
                gazebuilder.Append("\"properties\":{");
                foreach (var kvp in Core.GetNewSessionProperties(true))
                {
                    if (kvp.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(kvp.Key, (string)kvp.Value, gazebuilder);
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

            CognitiveVR.NetworkManager.Post(url, gazebuilder.ToString());

            //gazebuilder = new StringBuilder(70 * CognitiveVR_Preferences.Instance.GazeSnapshotCount + 200);
            gazebuilder.Length = 9;
            //gazebuilder.Append("{\"data\":[");
        }
    }
}