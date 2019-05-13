using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CognitiveVR
{
	/// <summary>
	/// For logging application telemetry
	/// </summary>
	public static class Instrumentation
	{
        //public functions
        //add transaction
        //send transactions
        
        static Instrumentation()
        {
            Core.OnSendData += Core_OnSendData;
            Core.CheckSessionId();
            autoTimer_nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.TransactionSnapshotMaxTimer;
            NetworkManager.Sender.StartCoroutine(AutomaticSendTimer());
        }

        private static void Core_OnSendData()
        {
            SendTransactions();
        }
        

        //used for unique identifier for sceneexplorer file names
        private static int partCount = 1;
        
        static int cachedEvents = 0;

        private static System.Text.StringBuilder eventBuilder = new System.Text.StringBuilder(512);
        private static System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

        static float autoTimer_nextSendTime = 0;
        internal static IEnumerator AutomaticSendTimer()
        {
            while (true)
            {
                while (autoTimer_nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                autoTimer_nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.TransactionSnapshotMaxTimer;
                if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                    Util.logDevelopment("check to automatically send events");
                SendTransactions();
            }
        }

        //checks for min send time and extreme batch size before calling send
        static void TrySendTransactions()
        {

            bool withinMinTimer = minTimer_lastSendTime + CognitiveVR_Preferences.Instance.TransactionSnapshotMinTimer > Time.realtimeSinceStartup;
            bool withinExtremeBatchSize = cachedEvents < CognitiveVR_Preferences.Instance.TransactionExtremeSnapshotCount;

            //within last send interval and less than extreme count
            if (withinMinTimer && withinExtremeBatchSize)
            {
                //Util.logDebug("instrumentation less than timer, less than extreme batch size");
                return;
            }
            SendTransactions();
        }

        static float minTimer_lastSendTime = -60;
        static void SendTransactions()
        {
            if (cachedEvents == 0)
            {
                return;
            }

            //TODO should hold until extreme batch size reached
            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                Util.logDebug("Instrumentation.SendTransactions could not find CurrentSceneId! has scene been uploaded and CognitiveVR_Manager.Initialize been called?");
                cachedEvents = 0;
                eventBuilder.Length = 0;
                return;
            }


            autoTimer_nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.DynamicSnapshotMaxTimer;
            minTimer_lastSendTime = Time.realtimeSinceStartup;

            cachedEvents = 0;
            //bundle up header stuff and transaction data

            //clear the transaction builder
            builder.Length = 0;

            //CognitiveVR.Util.logDebug("package transaction event data " + partCount);
            //when thresholds are reached, etc

            builder.Append("{");

            //header
            JsonUtil.SetString("userid", Core.UniqueID, builder);
            builder.Append(",");

            if (!string.IsNullOrEmpty(Core.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Core.LobbyId, builder);
                builder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", Core.SessionTimeStamp, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionid", Core.SessionID, builder);
            builder.Append(",");
            JsonUtil.SetInt("part", partCount, builder);
            partCount++;
            builder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", builder);
            builder.Append(",");

            //events
            builder.Append("\"data\":[");

            builder.Append(eventBuilder.ToString());

            if (eventBuilder.Length > 0)
                builder.Remove(builder.Length - 1, 1); //remove the last comma
            builder.Append("]");

            builder.Append("}");

            eventBuilder.Length = 0;

            //send transaction contents to scene explorer

            string packagedEvents = builder.ToString();

            //sends all packaged transaction events from instrumentaiton subsystem to events endpoint on scene explorer
            string url = CognitiveStatics.POSTEVENTDATA(Core.TrackingSceneId, Core.TrackingSceneVersionNumber);

            NetworkManager.Post(url, packagedEvents);
        }

        public static void SendCustomEvent(string category, float[] position, string dynamicObjectId = "")
        {
            eventBuilder.Append("{");
            JsonUtil.SetString("name", category, eventBuilder);
            eventBuilder.Append(",");
            JsonUtil.SetDouble("time", Util.Timestamp(Time.frameCount), eventBuilder);
            if (!string.IsNullOrEmpty(dynamicObjectId))
            {
                eventBuilder.Append(',');
                JsonUtil.SetString("dynamicId", dynamicObjectId, eventBuilder);
            }
            eventBuilder.Append(",");
            JsonUtil.SetVector("point", position, eventBuilder);
            eventBuilder.Append("}"); //close transaction object
            eventBuilder.Append(",");

            cachedEvents++;
            if (cachedEvents >= CognitiveVR_Preferences.Instance.TransactionSnapshotCount)
            {
                TrySendTransactions();
            }
        }

        public static void SendCustomEvent(string category, Vector3 position, string dynamicObjectId = "")
        {
            eventBuilder.Append("{");
            JsonUtil.SetString("name", category, eventBuilder);
            eventBuilder.Append(",");
            JsonUtil.SetDouble("time", Util.Timestamp(Time.frameCount), eventBuilder);
            if (!string.IsNullOrEmpty(dynamicObjectId))
            {
                eventBuilder.Append(',');
                JsonUtil.SetString("dynamicId", dynamicObjectId, eventBuilder);
            }
            eventBuilder.Append(",");
            JsonUtil.SetVector("point", position, eventBuilder);

            eventBuilder.Append("}"); //close transaction object
            eventBuilder.Append(",");

            cachedEvents++;
            if (cachedEvents >= CognitiveVR_Preferences.Instance.TransactionSnapshotCount)
            {
                TrySendTransactions();
            }
        }

        //writes json to display the transaction in sceneexplorer
        public static void SendCustomEvent(string category, List<KeyValuePair<string, object>> properties, Vector3 position, string dynamicObjectId = "")
        {
            SendCustomEvent(category, properties, new float[3]{ position.x,position.y,position.z},dynamicObjectId);
        }

        public static void SendCustomEvent(string category, List<KeyValuePair<string, object>> properties, float[] position, string dynamicObjectId = "")
        {
            eventBuilder.Append("{");
            JsonUtil.SetString("name", category, eventBuilder);
            eventBuilder.Append(",");
            JsonUtil.SetDouble("time", Util.Timestamp(Time.frameCount), eventBuilder);
            if (!string.IsNullOrEmpty(dynamicObjectId))
            {
                eventBuilder.Append(',');
                JsonUtil.SetString("dynamicId", dynamicObjectId, eventBuilder);
            }
            eventBuilder.Append(",");
            JsonUtil.SetVector("point", position, eventBuilder);

            if (properties != null && properties.Count > 0)
            {
                eventBuilder.Append(",");
                eventBuilder.Append("\"properties\":{");
                for(int i = 0; i<properties.Count;i++)
                {
                    if (i != 0) { eventBuilder.Append(","); }
                    if (properties[i].Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(properties[i].Key, (string)properties[i].Value, eventBuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(properties[i].Key, properties[i].Value, eventBuilder);
                    }
                }
                eventBuilder.Append("}"); //close properties object
            }

            eventBuilder.Append("}"); //close transaction object
            eventBuilder.Append(",");

            cachedEvents++;
            if (cachedEvents >= CognitiveVR_Preferences.Instance.TransactionSnapshotCount)
            {
                TrySendTransactions();
            }
        }
    }
}