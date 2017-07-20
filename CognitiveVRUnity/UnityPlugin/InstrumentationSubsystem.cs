using System.Collections.Generic;

namespace CognitiveVR
{
    public static class InstrumentationSubsystem
    {
        //used for unique identifier for sceneexplorer file names
        private static int partCount = 1;

        public static void SetMaxTransactions(int max)
        {
            EventDepot.maxCachedTransactions = max;
        }

        public static void SendCachedTransactions()
        {
            EventDepot.SendCachedTransactions();
        }

        public static void init()
        {
            Util.cacheCurrencyInfo();
        }

        private static System.Text.StringBuilder TransactionBuilder = new System.Text.StringBuilder();
        private static System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);
        /// <summary>
        /// call this when threshold for transactions is reached
        /// format transactions to be sent as the body of a webrequest
        /// this can be called manually, but it is automatically called when
        /// </summary>
        private static string PackageData()
        {
            //clear the transaction builder
            builder.Length = 0;
            
            //PackageData(CoreSubsystem.UniqueID, CoreSubsystem.SessionTimeStamp, CoreSubsystem.SessionID);
            string userid = CoreSubsystem.UniqueID;
            double timestamp = CoreSubsystem.SessionTimeStamp;
            string sessionId = CoreSubsystem.SessionID;

            CognitiveVR.Util.logDebug("package transaction event data " + partCount);
            //when thresholds are reached, etc
            
            builder.Append("{");

            //header
            builder.Append(JsonUtil.SetString("userid", userid));
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
                builder.Remove(builder.Length-1, 1); //remove the last comma
            builder.Append("]");

            builder.Append("}");

            TransactionBuilder.Length = 0;

            return builder.ToString();
        }

        internal static UnityEngine.GameObject wwwSendGameObject;
        internal static WWWSender wwwSender;

        internal static void SendTransactionsToSceneExplorer()
        {
            string packagedEvents = PackageData();

            if (string.IsNullOrEmpty(CoreSubsystem.CurrentSceneId))
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for scene! do not upload transactions to sceneexplorer");
                return;
            }

            //sends all packaged transaction events from instrumentaiton subsystem to events endpoint on scene explorer
            string url = "https://sceneexplorer.com/api/events/" + CoreSubsystem.CurrentSceneId;
            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(packagedEvents);

            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            if (wwwSendGameObject == null)
            {
                wwwSendGameObject = new UnityEngine.GameObject("CognitiveVR Transaction Helper");
                UnityEngine.GameObject.DontDestroyOnLoad(wwwSendGameObject);
                wwwSender = wwwSendGameObject.AddComponent<WWWSender>();
            }
            wwwSender.StartCoroutine(GetWWWReponse(url, outBytes, headers));
            //UnityEngine.WWW www = new UnityEngine.WWW(url, outBytes, headers);

            Util.logDebug("sent transaction event data. clear packaged bundles");
        }

        public static System.Collections.IEnumerator GetWWWReponse(string url, byte[] outBytes, Dictionary<string,string> headers)
        {
            UnityEngine.WWW www = new UnityEngine.WWW(url, outBytes, headers);
            yield return www; //have to wait until this is finished, otherwise it can get removed without finishing request?
            //Util.logDebug("response" + www.text);
        }

        private static void SetTransaction(string category, Dictionary<string, object>  properties, float[] position, double timestamp)
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
        }

        public static void beginTransaction(string category, string timeoutMode, double timeout, string transactionId, Dictionary<string, object> properties, float[] position)
        {
            SetTransaction(category, properties, position, Util.Timestamp());

            new CoreSubsystem.DataPointBuilder("datacollector_beginTransaction")
            .setArg(category)
            .setArg(timeoutMode)
            .setArg(timeout)
            .setArg(transactionId)
            .setArg(properties)
            .send();
        }

        public static void updateTransaction(string category, int progress, string transactionId, Dictionary<string, object> properties)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateTransaction")
            .setArg(category)
            .setArg(progress)
            .setArg(transactionId)
            .setArg(properties)
            .send();
        }

        public static void endTransaction(string category, string result, string transactionId, Dictionary<string, object> properties, float[] position)
        {
            SetTransaction(category, properties, position, Util.Timestamp());

            new CoreSubsystem.DataPointBuilder("datacollector_endTransaction")
            .setArg(category)
            .setArg(result)
            .setArg(transactionId)
            .setArg(properties)
            .send();
        }

        /**
         * Updates state information about the user.
         *
         * @param properties A key-value object representing the user state we want to update. This can be a nested object structure.
         */
        public static void updateUserState(IDictionary<string, object> properties)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateUserState").setArg(properties).send();
        }

        /**
         * Updates state information about a device.
         *
         * @param properties A key-value object representing the device state we want to update. This can be a nested object structure.
         */
        public static void updateDeviceState(IDictionary<string, object> properties)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateDeviceState").setArg(properties).send();
        }

        /**
         * Update a collection balance for a user.
         *
         * @param name                  The name of the collection.
         * @param balance               The new balance of the collection.
         * @param balanceModification   The change in balance being recorded.  To reduce the balance, specify a negative number.
         * @param isCurrency            Whether or not this collection represents a currency in the application.
         */
        public static void updateCollection(string name, double balance, double balanceModification, bool isCurrency)
        {
            new CoreSubsystem.DataPointBuilder("datacollector_updateCollection")
            .setArg(name)
            .setArg(balance)
            .setArg(balanceModification)
            .setArg(isCurrency)
            .send();
        }
    }
}
