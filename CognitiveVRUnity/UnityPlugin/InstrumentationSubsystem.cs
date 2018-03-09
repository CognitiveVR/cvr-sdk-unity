using System.Collections.Generic;

namespace CognitiveVR
{
    public static class InstrumentationSubsystem
    {
        //used for unique identifier for sceneexplorer file names
        private static int partCount = 1;

        static int maxCachedEvents = 0;
        static int cachedEvents = 16;
        public static void SetMaxTransactions(int max)
        {
            maxCachedEvents = max;
        }
        
        private static System.Text.StringBuilder TransactionBuilder = new System.Text.StringBuilder();
        private static System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

        internal static UnityEngine.GameObject wwwSendGameObject;
        internal static WWWSender wwwSender;

        public static void SendTransactions()
        {
            cachedEvents = 0;
            //bundle up header stuff and transaction data
            
            //clear the transaction builder
            builder.Length = 0;

            //PackageData(CoreSubsystem.UniqueID, CoreSubsystem.SessionTimeStamp, CoreSubsystem.SessionID);
            string userid = CoreSubsystem.UniqueID;
            double timestamp = CoreSubsystem.SessionTimeStamp;
            string sessionId = CoreSubsystem.SessionID;

            //CognitiveVR.Util.logDebug("package transaction event data " + partCount);
            //when thresholds are reached, etc

            builder.Append("{");

            //header
            builder.Append(JsonUtil.SetString("userid", CoreSubsystem.UniqueID));
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

            if (string.IsNullOrEmpty(CoreSubsystem.CurrentSceneId))
            {
                Util.logDebug("CognitiveVR_PlayerTracker.SendData could not find scene settings for scene! do not upload transactions to sceneexplorer");
                return;
            }
            
            //sends all packaged transaction events from instrumentaiton subsystem to events endpoint on scene explorer
            string url = Constants.POSTEVENTDATA(CoreSubsystem.CurrentSceneId, CoreSubsystem.CurrentSceneVersionNumber);
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
            //TODO add dedicated network interface
            wwwSender.StartCoroutine(GetWWWReponse(url, outBytes, headers));
            //UnityEngine.WWW www = new UnityEngine.WWW(url, outBytes, headers);

            //Util.logDebug("sent transaction event data. clear packaged bundles");
        }

        public static System.Collections.IEnumerator GetWWWReponse(string url, byte[] outBytes, Dictionary<string,string> headers)
        {
            UnityEngine.WWW www = new UnityEngine.WWW(url, outBytes, headers);
            yield return www; //have to wait until this is finished, otherwise it can get removed without finishing request?
            //Util.logDebug("response" + www.text);
        }

        //writes json to display the transaction in sceneexplorer
        private static void AppendTransaction(string category, Dictionary<string, object>  properties, float[] position, double timestamp)
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
