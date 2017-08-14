using System;
using System.Collections.Generic;
using CognitiveVR.External.MiniJSON; 

namespace CognitiveVR
{
    internal static class EventDepot
    {
        private static Uri sUri;
        private static int sReqTimeout;
        private static HttpRequest.Listener sRequestListener;

        private static List<string> savedTransactions = new List<string>();
        internal static int maxCachedTransactions = 16;

        /**
         * Initialize the event depot.
         *
         * @param host          The host name of the data collector
         * @param queryParams   Query parameters to send along with the request
         * @param reqTimeout    A timeout, in milliseconds, representing the maxmimum amount of time one should wait for CognitiveVR network requests to complete.
         */
        internal static void init(string host, string queryParams, int reqTimeout)
        {
            // Save off the parameters needed to submit requests to send the events to the data collector
            sReqTimeout = reqTimeout;
            sUri = new Uri(host + "/isos-personalization/ws/interface/datacollector_batch" + queryParams);
            sRequestListener = new SendEventRequestListener();
        }

        static System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

        internal static void SendCachedTransactions()
        {
            if (savedTransactions.Count == 0)
            {
                Util.logDebug("EventDepot SendCachedTransactions - no saved transactions. do not send");
                return;
            }

            builder.Length = 0;
            builder.Append("[");
            builder.Append(Util.Timestamp());
            builder.Append(",[");

            foreach (var v in savedTransactions)
            {
                builder.Append(v);
                builder.Append(",");
            }
            if (savedTransactions.Count > 0)
            {
                builder = builder.Remove(builder.Length - 1, 1);
                //sendJson = sendJson.Remove(sendJson.Length-1, 1);
            }
            builder.Append("],\"");
            builder.Append(CoreSubsystem.SessionID);
            builder.Append("\"]");

            HttpRequest.executeAsync(sUri, sReqTimeout, builder.ToString(), sRequestListener);
            savedTransactions.Clear();
            InstrumentationSubsystem.SendTransactionsToSceneExplorer();
        }

        /**
         * Store an event in the depot.
         *
         * @param event The event we wish to store
         */
        internal static Error store(IDictionary<string, object> eventData)
        {
            // Build up the batch
            List<object> allArgs = new List<object>(2);
            allArgs.Add(Util.Timestamp());
            allArgs.Add(new List<object> { eventData });
            
            string data = Json.Serialize(new List<object> { eventData });
            data = data.Remove(data.Length-1, 1);
            data = data.Remove(0, 1);

            savedTransactions.Add(data);

            if (savedTransactions.Count >= maxCachedTransactions)
            {
                SendCachedTransactions();
            }

            return Error.Success;
        }

        [System.Obsolete("EventDepot.pause() is no longer used")]
        internal static void pause()
        {
            // Do nothing for native unity...
        }

        [System.Obsolete("EventDepot.resume() is no longer used")]
        internal static void resume()
        {
            // Do nothing for native unity...
        }

        #region private helper classes
        private class SendEventRequestListener : HttpRequest.Listener
        {
            void HttpRequest.Listener.onComplete(HttpRequest.Result result)
            {
                // At least log the error response
                logErrorResponse(result.Response);
            }
        }
        #endregion

        #region private helper functions
        private static void logErrorResponse(string responseStr)
        {
            if (null != responseStr)
            {
                try
                {
                    Dictionary<string, object> response = Json.Deserialize(responseStr) as Dictionary<string, object>;
                    if (response.ContainsKey("error"))
                    {
                        Error err = (Error)Enum.ToObject(typeof(Error), response["error"]);
                        if (Error.Success != err)
                        {
                            Util.logError("Top-level error [" + err.ToString() + "] returned from data collector");
                        }
                        else
                        {
                            // Got a successful top-level response, now check the datacollector_batch context
                            if (response.ContainsKey("data"))
                            {
                                Dictionary<string, object> data = response["data"] as Dictionary<string, object>;

                                if (data.ContainsKey("datacollector_batch"))
                                {
                                    Dictionary<string, object> context = data["datacollector_batch"] as Dictionary<string, object>;
                                    if (context.ContainsKey("error"))
                                    {
                                        Error contextErr = (Error)Enum.ToObject(typeof(Error), context["error"]);
                                        if (Error.Success != contextErr)
                                        {
                                            Util.logError("datacollector_batch error [" + contextErr.ToString() + "] returned from data collector");
                                        }
                                    }
                                    else
                                    {
                                        Util.logError("Unexpected response returned from data collector, context error missing");
                                    }
                                }
                                else
                                {
                                    Util.logError("Unexpected response returned from data collector, context missing");
                                }
                            }
                            else
                            {
                                Util.logError("Unexpected response returned from data collector, data missing");
                            }
                        }
                    }
                    else
                    {
                        Util.logError("Unexpected response returned from data collector, error missing");
                    }
                }
                catch (Exception)
                {
                    Util.logError("Exception parsing server response: " + responseStr);
                }
            }
        }
        #endregion
    }
}
