using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;
using System.Text;
using CognitiveVR.External;

namespace CognitiveVR
{
    public static class FixationCore
    {
        private static int jsonPart = 1;
        static List<Fixation> Fixations = new List<Fixation>();

        static FixationCore()
        {
            Core.OnSendData += Core_OnSendData;
            Core.CheckSessionId();
            nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.FixationSnapshotMaxTimer;
            NetworkManager.Sender.StartCoroutine(AutomaticSendTimer());
        }

        static float nextSendTime = 0;
        internal static IEnumerator AutomaticSendTimer()
        {
            while (true)
            {
                while (nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.FixationSnapshotMaxTimer;
                if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                    Util.logDevelopment("check to automatically send fixations");
                Core_OnSendData();
            }
        }

        static void TrySendData()
        {
            bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.Instance.FixationSnapshotMinTimer > Time.realtimeSinceStartup;
            bool withinExtremeBatchSize = Fixations.Count < CognitiveVR_Preferences.Instance.FixationExtremeSnapshotCount;

            //within last send interval and less than extreme count
            if (withinMinTimer && withinExtremeBatchSize)
            {
                return;
            }
            Core_OnSendData();
        }

        public static void RecordFixation(Fixation newFixation)
        {
            Fixations.Add(newFixation);
        }

        static float lastSendTime = -60;
        private static void Core_OnSendData()
        {
            if (Fixations.Count <= 0) { CognitiveVR.Util.logDebug("Fixations.SendData found no data"); return; }

            //TODO should hold until extreme batch size reached
            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                CognitiveVR.Util.logDebug("Fixations.SendData could not find scene settings for scene! do not upload fixations to sceneexplorer");
                Fixations.Clear();
                return;
            }


            nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.FixationSnapshotMaxTimer;
            lastSendTime = Time.realtimeSinceStartup;


            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{");
            JsonUtil.SetString("userid", Core.UniqueID, sb);
            sb.Append(",");
            JsonUtil.SetString("sessionid", Core.SessionID, sb);
            sb.Append(",");
            JsonUtil.SetInt("timestamp", (int)Core.SessionTimeStamp, sb);
            sb.Append(",");
            JsonUtil.SetInt("part", jsonPart, sb);
            sb.Append(",");
            jsonPart++;

            sb.Append("\"data\":[");
            for(int i = 0; i<Fixations.Count;i++)
            {
                JsonUtil.SetDouble("starttime", Fixations[i].StartMs * 1000,sb);
                sb.Append(",");
                JsonUtil.SetDouble("duration", Fixations[i].DurationMs * 1000, sb);
                sb.Append(",");
                JsonUtil.SetFloat("maxradius", Fixations[i].MaxRadius, sb);
                sb.Append(",");
                JsonUtil.SetVector("p", Fixations[i].WorldPosition, sb);
                sb.Append(",");

                if (Fixations[i].IsLocal)
                {
                    JsonUtil.SetString("dynamicid", Fixations[i].DynamicObjectId, sb);
                    sb.Append(",");
                }
            }

            if (Fixations.Count > 0)
            {
                sb.Remove(sb.Length - 1, 1); //remove last comma from fixation object
            }

            sb.Append("]}");

            Fixations.Clear();

            string url = Constants.POSTFIXATIONDATA(Core.TrackingSceneId, Core.TrackingSceneVersionNumber);
            //byte[] outBytes = System.Text.UTF8Encoding.UTF8.GetBytes();
            //CognitiveVR_Manager.Instance.StartCoroutine(CognitiveVR_Manager.Instance.PostJsonRequest(outBytes, url));
            NetworkManager.Post(url, sb.ToString());
        }
    }
}