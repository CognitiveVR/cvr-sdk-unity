using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cognitive3D;
using System.Text;
using Cognitive3D.External;

namespace Cognitive3D
{
    public static class FixationCore
    {
        private static int jsonPart = 1;
        static List<Fixation> Fixations = new List<Fixation>();
        public static int CachedFixations { get { return Fixations.Count; } }

        static FixationCore()
        {
            Cognitive3D_Manager.OnSendData += Core_OnSendData;
            nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.FixationSnapshotMaxTimer;
            Cognitive3D_Manager.OnSessionBegin += Core_InitEvent;
        }

        private static void Core_InitEvent()
        {
            Cognitive3D_Manager.OnSessionBegin -= Core_InitEvent;
            Cognitive3D_Manager.NetworkManager.StartCoroutine(AutomaticSendTimer());
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
                nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.FixationSnapshotMaxTimer;
                if (Cognitive3D_Manager.IsInitialized)
                {
                    if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                        Util.logDevelopment("check to automatically send fixations");
                    Core_OnSendData(false);
                }
            }
        }

        static void TrySendData()
        {
            if (Fixations.Count > Cognitive3D_Preferences.Instance.FixationSnapshotCount)
            {
                Core_OnSendData(false);
            }
        }

        public static void RecordFixation(Fixation newFixation)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Fixation cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Fixation recorded without SceneId"); return; }

            if (newFixation.IsLocal)
            {
                //apply scale to fixation
                //newFixation.LocalPosition /= newFixation.DynamicMatrix.GetColumn(0).magnitude;
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
        public static event Cognitive3D_Manager.onSendData OnFixationSend;

        static float lastSendTime = -60;
        private static void Core_OnSendData(bool copyDataToCache)
        {
            if (Fixations.Count <= 0) { Cognitive3D.Util.logDebug("Fixations.SendData found no data"); return; }

            //TODO should hold until extreme batch size reached
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                Cognitive3D.Util.logDebug("Fixations.SendData could not find scene settings for scene! do not upload fixations to sceneexplorer");
                Fixations.Clear();
                return;
            }


            nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.FixationSnapshotMaxTimer;
            lastSendTime = Time.realtimeSinceStartup;


            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{");
            JsonUtil.SetString("userid", Cognitive3D_Manager.DeviceId, sb);
            sb.Append(",");
            JsonUtil.SetString("sessionid", Cognitive3D_Manager.SessionID, sb);
            sb.Append(",");
            JsonUtil.SetInt("timestamp", (int)Cognitive3D_Manager.SessionTimeStamp, sb);
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

            string url = CognitiveStatics.POSTFIXATIONDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);
            string content = sb.ToString();
            
            if (copyDataToCache)
            {
                if (Cognitive3D_Manager.NetworkManager.runtimeCache != null && Cognitive3D_Manager.NetworkManager.runtimeCache.CanWrite(url, content))
                {
                    Cognitive3D_Manager.NetworkManager.runtimeCache.WriteContent(url, content);
                }
            }

            Cognitive3D_Manager.NetworkManager.Post(url, content);
            if (OnFixationSend != null)
            {
                OnFixationSend.Invoke(copyDataToCache);
            }
        }
    }
}