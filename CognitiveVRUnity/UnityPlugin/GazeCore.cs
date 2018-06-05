using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;
using System.Text;
using CognitiveVR.External;

//TODO static place for receiving data from gaze and sending it

namespace CognitiveVR
{
    public static class GazeCore
    {
        private static int jsonPart = 1;
        //private static Dictionary<string, List<string>> CachedSnapshots = new Dictionary<string, List<string>>();
        private static StringBuilder gazebuilder;
        private static int gazeCount = 0;
        //private static int currentSensorSnapshots = 0;

        static GazeCore()
        {
            Core.OnSendData += SendGazeData;
            Core.CheckSessionId();

            gazebuilder = new StringBuilder(70 * CognitiveVR_Preferences.Instance.GazeSnapshotCount + 200);
            gazebuilder.Append("{\"data\":[");
        }

        public static void RecordGazePoint(double timestamp, Vector3 hmdpoint, Quaternion hmdrotation) //looking at the camera far plane
        {
            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);

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

        public static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation) //looking at a dynamic object
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

        public static void RecordGazePoint(double timestamp, Vector3 gazepoint, Vector3 hmdpoint, Quaternion hmdrotation) //looking at world
        {
            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", gazepoint, gazebuilder);

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
            if (string.IsNullOrEmpty(Core.CurrentSceneId))
            {
                Util.logDebug("Cognitive GazeCore.SendData could not find scene settings for scene! do not upload gaze to sceneexplorer");
                return;
            }

            gazebuilder.Append("],");

            gazeCount =0;

            //add properties!

            //header
            JsonUtil.SetString("userid", Core.UniqueID, gazebuilder);
            gazebuilder.Append(",");

            if (!string.IsNullOrEmpty(CognitiveVR_Preferences.LobbyId))
            {
                JsonUtil.SetString("lobbyId", CognitiveVR_Preferences.LobbyId, gazebuilder);
                gazebuilder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)Core.SessionTimeStamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("sessionid", Core.SessionID, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetInt("part", jsonPart, gazebuilder);
            jsonPart++;
            gazebuilder.Append(",");

#if CVR_FOVE
                    JsonUtil.SetString("hmdtype", "fove", gazebuilder);
#elif CVR_ARKIT
                    JsonUtil.SetString("hmdtype", "arkit", gazebuilder);
#elif CVR_ARCORE
                    JsonUtil.SetString("hmdtype", "arcore", gazebuilder);
#elif CVR_META
                    JsonUtil.SetString("hmdtype", "meta", gazebuilder);
#else
            JsonUtil.SetString("hmdtype", CognitiveVR.Util.GetSimpleHMDName(), gazebuilder);
#endif
            gazebuilder.Append(",");
            JsonUtil.SetFloat("interval", CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval, gazebuilder);
            gazebuilder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", gazebuilder);
            gazebuilder.Append(",");
            
            if (Core.GetNewSessionProperties(false).Count > 0)
            {
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

            var sceneSettings = CognitiveVR_Preferences.FindTrackingScene();
            string url = Constants.POSTGAZEDATA(sceneSettings.SceneId, sceneSettings.VersionNumber);

            CognitiveVR.NetworkManager.Post(url, gazebuilder.ToString());

            gazebuilder.Length = 0;
            gazebuilder.Append("{\"data\":");
        }
    }
}