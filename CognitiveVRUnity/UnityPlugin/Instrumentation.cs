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

        static int maxCachedEvents = 0;
        static int cachedEvents = 16;

        private static System.Text.StringBuilder TransactionBuilder = new System.Text.StringBuilder();
        private static System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

        static void SendTransactions()
        {
            cachedEvents = 0;
            //bundle up header stuff and transaction data

            //clear the transaction builder
            builder.Length = 0;

            //PackageData(CoreSubsystem.UniqueID, CoreSubsystem.SessionTimeStamp, CoreSubsystem.SessionID);
            string userid = Core.UniqueID;
            double timestamp = Core.SessionTimeStamp;
            string sessionId = Core.SessionID;

            //CognitiveVR.Util.logDebug("package transaction event data " + partCount);
            //when thresholds are reached, etc

            builder.Append("{");

            //header
            builder.Append(JsonUtil.SetString("userid", Core.UniqueID));
            builder.Append(",");

            builder.Append(JsonUtil.SetObject("timestamp", timestamp));
            builder.Append(",");
            builder.Append(JsonUtil.SetString("sessionid", sessionId));
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
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for scene! do not upload transactions to sceneexplorer");
                return;
            }

            //sends all packaged transaction events from instrumentaiton subsystem to events endpoint on scene explorer
            string url = Constants.POSTEVENTDATA(Core.CurrentSceneId, Core.CurrentSceneVersionNumber);
            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(packagedEvents);

            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            NetworkManager.Post(url, outBytes, headers);
        }

        //writes json to display the transaction in sceneexplorer
        private static void AppendTransaction(string category, Dictionary<string, object> properties, float[] position, double timestamp)
        {
            TransactionBuilder.Append("{");
            TransactionBuilder.Append(JsonUtil.SetString("name", category));
            TransactionBuilder.Append(",");
            TransactionBuilder.Append(JsonUtil.SetObject("time", timestamp));
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

        public static void beginTransaction(string category, Dictionary<string, object> properties, float[] position)
        {
            AppendTransaction(category, properties, position, Util.Timestamp());
        }

        public static void updateTransaction(string category, int progress, Dictionary<string, object> properties)
        {

        }

        public static void endTransaction(string category, Dictionary<string, object> properties, float[] position)
        {
            AppendTransaction(category, properties, position, Util.Timestamp());
        }
    }
}