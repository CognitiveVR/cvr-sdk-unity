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
        public static int CachedFixations { get { return Fixations.Count; } }

        static FixationCore()
        {
            Core.OnSendData += Core_OnSendData;
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
                if (Core.IsInitialized)
                {
                    if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                        Util.logDevelopment("check to automatically send fixations");
                    Core_OnSendData();
                }
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
            if (newFixation.IsLocal)
            {
                //apply scale to fixation
                newFixation.LocalPosition /= newFixation.DynamicMatrix.GetColumn(0).magnitude;
            }
            Fixation f = new Fixation(newFixation);
            Fixations.Add(f);
        }

        public delegate void onFixationRecord(Fixation fixation);
        public static event onFixationRecord OnFixationRecord;
        public static void FixationRecordEvent(Fixation fixation)
        {
            if (OnFixationRecord != null)
                OnFixationRecord.Invoke(fixation);
        }

        //happens after the network has sent the request, before any response
        public static event Core.onDataSend OnFixationSend;

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
            JsonUtil.SetString("userid", Core.DeviceId, sb);
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
                sb.Append("{");
                JsonUtil.SetDouble("time", System.Convert.ToDouble((double)Fixations[i].StartMs / 1000.0),sb);
                sb.Append(",");
                JsonUtil.SetLong("duration", Fixations[i].DurationMs, sb);
                sb.Append(",");
                JsonUtil.SetFloat("maxradius", Fixations[i].MaxRadius, sb);
                sb.Append(",");

                if (Fixations[i].IsLocal)
                {
                    JsonUtil.SetString("objectid", Fixations[i].DynamicObjectId, sb);
                    sb.Append(",");
                    JsonUtil.SetVector("p", Fixations[i].LocalPosition, sb);
                }
                else
                {
                    JsonUtil.SetVector("p", Fixations[i].WorldPosition, sb);
                }
                sb.Append("},");
            }
            if (Fixations.Count > 0)
            {
                sb.Remove(sb.Length - 1, 1); //remove last comma from fixation object
            }

            sb.Append("]}");

            Fixations.Clear();

            string url = CognitiveStatics.POSTFIXATIONDATA(Core.TrackingSceneId, Core.TrackingSceneVersionNumber);
            NetworkManager.Post(url, sb.ToString());
            if (OnFixationSend != null)
            {
                OnFixationSend.Invoke();
            }
        }
    }
}