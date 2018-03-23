using UnityEngine;
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
        }

        private static void Core_OnSendData()
        {
            SendTransactions();
        }

        public static void SetMaxTransactions(int max)
        {
            maxCachedEvents = max;
        }

        //used for unique identifier for sceneexplorer file names
        private static int partCount = 1;

        static int maxCachedEvents = 16;
        static int cachedEvents = 0;

        private static System.Text.StringBuilder TransactionBuilder = new System.Text.StringBuilder();
        private static System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

        static void SendTransactions()
        {
            cachedEvents = 0;
            //bundle up header stuff and transaction data

            //clear the transaction builder
            builder.Length = 0;

            //CognitiveVR.Util.logDebug("package transaction event data " + partCount);
            //when thresholds are reached, etc

            builder.Append("{");

            //header
            builder.Append(JsonUtil.SetString("userid", Core.UniqueID));
            builder.Append(",");

            builder.Append(JsonUtil.SetObject("timestamp", Core.SessionTimeStamp));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("sessionid", Core.SessionID));
            builder.Append(",");
            builder.Append(JsonUtil.SetObject("part", partCount));
            partCount++;
            builder.Append(",");

            //events
            builder.Append("\"data\":[");

            builder.Append(TransactionBuilder.ToString());

            if (TransactionBuilder.Length > 0)
                builder.Remove(builder.Length - 1, 1); //remove the last comma
            builder.Append("]");

            builder.Append("}");

            TransactionBuilder.Length = 0;

            //send transaction contents to scene explorer

            string packagedEvents = builder.ToString();

            if (string.IsNullOrEmpty(Core.CurrentSceneId))
            {
                Util.logDebug("Instrumentation.SendTransactions could not find CurrentSceneId! has scene been uploaded and CognitiveVR_Manager.Initialize been called?");
                return;
            }

            //sends all packaged transaction events from instrumentaiton subsystem to events endpoint on scene explorer
            string url = Constants.POSTEVENTDATA(Core.CurrentSceneId, Core.CurrentSceneVersionNumber);
            //byte[] outBytes = System.Text.UTF8Encoding.UTF8.GetBytes();

            //var headers = new Dictionary<string, string>();
            //headers.Add("Content-Type", "application/json");
            //headers.Add("X-HTTP-Method-Override", "POST");

            NetworkManager.Post(url, packagedEvents);
        }

        //writes json to display the transaction in sceneexplorer
        public static void SendCustomEvent(string category, Dictionary<string, object> properties, Vector3 position)
        {
            SendCustomEvent(category, properties, new float[3]{ position.x,position.y,position.z});
        }

        public static void SendCustomEvent(string category, Dictionary<string, object> properties, float[] position)
        {
            TransactionBuilder.Append("{");
            TransactionBuilder.Append(JsonUtil.SetString("name", category));
            TransactionBuilder.Append(",");
            TransactionBuilder.Append(JsonUtil.SetObject("time", Util.Timestamp()));
            TransactionBuilder.Append(",");
            TransactionBuilder.Append(JsonUtil.SetVector("point", position));


            if (properties != null && properties.Keys.Count > 0)
            {
                TransactionBuilder.Append(",");
                TransactionBuilder.Append("\"properties\":{");
                foreach (var v in properties)
                {
                    if (v.Value.GetType() == typeof(string))
                    {
                        TransactionBuilder.Append(JsonUtil.SetString(v.Key, (string)v.Value));
                    }
                    else
                    {
                        TransactionBuilder.Append(JsonUtil.SetObject(v.Key, v.Value));
                    }
                    TransactionBuilder.Append(",");
                }
                TransactionBuilder.Remove(TransactionBuilder.Length - 1, 1); //remove last comma
                TransactionBuilder.Append("}"); //close properties object
            }

            TransactionBuilder.Append("}"); //close transaction object
            TransactionBuilder.Append(",");

            cachedEvents++;
            if (cachedEvents >= maxCachedEvents)
            {
                SendTransactions();
            }
        }

#pragma warning disable 618
        public static Transaction Transaction(string category)
        {
            return new CognitiveVR.Transaction(category);
        }
#pragma warning restore 618
    }
}