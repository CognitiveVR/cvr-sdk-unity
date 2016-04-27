using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using CognitiveVR.External.MiniJSON;

namespace CognitiveVR
{
    /**
     * <p>Tuning Subsystem</p>
     *
     * @author Copyright 2015 Knetik, Inc.
     * @version 1.0
     */
    public class TuningSubsystem
    {
        private const string CACHE_FILENAME = "cognitivevr_tuningCache";

        private static TuningValues sCacheVars;

        /**
         * Performs required initialization for the tuning subsystem
         *
         * @param context Application context to use for caching tuning variable information
         */
        public static void init(Callback cb)
        {
            sCacheVars = LocalStorage.Load<TuningValues>(CACHE_FILENAME, false);
            if (null == sCacheVars)
            {
                sCacheVars = new TuningValues();
            }

            if (null != cb) cb(Error.Success);
        }

        public static void refresh(Callback cb)
        {
            Error ret = Error.Success;

            if (CoreSubsystem.Initialized)
            {
                String url = CoreSubsystem.Host + "/isos-personalization/ws/interface/tuner_refresh" + CoreSubsystem.getQueryParms();

                IList allArgs = new List<object>(4);
				double curTimeStamp = Util.Timestamp();
                allArgs.Add(curTimeStamp);
                allArgs.Add(curTimeStamp);
                allArgs.Add(CoreSubsystem.DeviceId);
                allArgs.Add(CoreSubsystem.getRegisteredUsers());

                try
                {
                    // Create an (async) request to retrieve the tuning variables.  The callback will be triggered when the request is completed
                    HttpRequest.executeAsync(new Uri(url), CoreSubsystem.ReqTimeout, Json.Serialize(allArgs), new RefreshRequestListener(cb));
                }
                catch (WebException e)
				{
					Util.logError("WebException during the HttpRequest.  Check your host and customerId values: " + e.Message);
					ret = Error.InvalidArgs;
				}
				catch (Exception e)
				{
					Util.logError("Error during HttpRequest: " + e.Message);
					ret = Error.Generic;
				}
            }
            else
            {
                Util.logError("Cannot refresh tuning because CognitiveVR is not initialized");
                ret = Error.NotInitialized;
            }

            // if we have an error at this point, then the callback will not get called through the HttpRequest, so call it now
            if (Error.Success != ret && null != cb)
            {
                cb(ret);
            }
        }

        /**
         * Get the value of a named variable from CognitiveVR.  If {@link CognitiveVR#cacheVariables cacheVariables} has not been called or has not finished, this function will return the default value provided.
         *
         * @param userId        The user id, or null
         * @param deviceId      The device id
         * @param varName       The name of the variable to retrieve.
         * @param defaultValue  Java Object representing the default value to use.
         *
         * @return The value of the variable (or the default value)
         * <p><b>Note:</b> The return value is guaranteed to match the type of the defaultValue passed in.</p>
         */
        public static T getVar<T>(string userId, string deviceId, string varName, T defaultValue)
        {
            string entityType = Constants.ENTITY_TYPE_DEVICE;
            string entityId = deviceId;
            if (null != userId)
            {
                entityType = Constants.ENTITY_TYPE_USER;
                entityId = userId;
            }

            // Grab the value from the cache
            return sCacheVars.getValue(entityType, entityId, varName, defaultValue);
        }

        public class Updater : TuningUpdater
        {
            private bool mDirty = false;

            public void onUpdate(string type, string id, IDictionary<string, object> values)
            {
                sCacheVars.updateEntity(type, id, values);

                mDirty = true;
            }

            public void onClear(string type, string id)
            {
                sCacheVars.removeEntity(type, id);

                mDirty = true;
            }

            public void commit()
            {
                if (mDirty)
                {
                    // Cache off the tuning variables
                    LocalStorage.Save(CACHE_FILENAME, sCacheVars);
                    mDirty = false;
                }
            }
        }

        #region private helper classes
        private class RefreshRequestListener : HttpRequest.Listener
        {
            private Callback mCallback;

            internal RefreshRequestListener(Callback cb)
            {
                mCallback = cb;
            }

            void HttpRequest.Listener.onComplete(HttpRequest.Result result)
            {
                Error error = result.ErrorCode;

                if (Error.Success == error)
                {
                    TuningUpdater updater = new Updater();
                    try
                    {
                        var dict = Json.Deserialize(result.Response) as Dictionary<string, object>;
                        error = Error.Unknown;
                        if (dict.ContainsKey("error"))
                        {
                            error = (Error)Enum.ToObject(typeof(Error), dict["error"]);
                        }

                        if (Error.Success == error)
                        {
                            if (dict.ContainsKey("data"))
                            {
                                var data = dict["data"] as Dictionary<string, object>;

                                if ((null != data) && data.ContainsKey("deviceTuning"))
                                {
                                    var deviceTuning = data["deviceTuning"] as Dictionary<string, object>;

                                    if ((null != deviceTuning) && deviceTuning.ContainsKey("data"))
                                    {
                                        var tuningData = deviceTuning["data"] as Dictionary<string, object>;
                                        if ((null != tuningData) && tuningData.ContainsKey("value"))
                                        {
                                            var theData = tuningData["value"] as Dictionary<string, object>;
                                            // NOTE:  This may return null if no tuning data is available (returns null because the value data is an empty array not a Dictionary)
                                            if (null != theData)
                                            {
                                                updater.onUpdate(Constants.ENTITY_TYPE_DEVICE, CoreSubsystem.DeviceId, theData);
                                            }
                                        }
                                        else
                                        {
                                            Util.logError("Unexpected response from server. Missing device tuning data");
                                        }
                                    }
                                    else
                                    {
                                        Util.logError("Error processing device tuning:");
                                        if (deviceTuning.ContainsKey("error") && deviceTuning.ContainsKey("description"))
                                        {
                                            Util.logError(((Error)deviceTuning["error"]).ToString() + "(" + deviceTuning["description"] + ")");
                                        }
                                    }
                                }
                                else
                                {
                                    Util.logError("Missing device tuning");
                                }

                                if (data.ContainsKey("userTuning"))
                                {
                                    var userTuning = data["userTuning"] as Dictionary<string, object>;

                                    if ((null != userTuning) && userTuning.ContainsKey("data"))
                                    {
                                        var tuningData = userTuning["data"] as Dictionary<string, object>;
                                        if ((null != tuningData) && tuningData.ContainsKey("value"))
                                        {
                                            var values = tuningData["value"] as Dictionary<string, object>;
                                            if (null != values)
                                            {
                                                foreach (KeyValuePair<string, object> entry in values)
                                                {
                                                    if (null != entry.Value)
                                                    {
                                                        var theData = entry.Value as Dictionary<string, object>;
                                                        // NOTE:  This may return null if no tuning data is available (returns null because the value data is an empty array not a Dictionary)
                                                        if (null != theData)
                                                        {
                                                            updater.onUpdate(Constants.ENTITY_TYPE_USER, entry.Key, theData);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Util.logError("User tuning data missing for user " + entry.Key + " in refresh response");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Util.logError("Unexpected response from server. Missing user tuning data");
                                        }
                                    }
                                    else
                                    {
                                        Util.logError("Error processing user tuning:");
                                        if (userTuning.ContainsKey("error") && userTuning.ContainsKey("description"))
                                        {
                                            Util.logError(((Error)userTuning["error"]).ToString() + "(" + userTuning["description"] + ")");
                                        }
                                    }
                                }
                                else
                                {
                                    Util.logError("Missing user tuning");
                                }
                            }
                            else
                            {
                                Util.logError("Unexpected response from server.  Missing data");
                            }
                        }
                        else
                        {
                            Util.logError("Error received in refresh response from server: ");
                            string desc = null;
                            if (dict.ContainsKey("description"))
                            {
                                desc = dict["description"] as string;
                            }

                            Util.logError(error.ToString() + "(" + ((null != desc) ? desc : "Unknown") + ")");
                        }
                    }
                    catch (Exception e)
                    {
                        Util.logError("Exception parsing refresh response: " + e.Message);
                        error = Error.Generic;
                    }
                    finally
                    {
                        updater.commit();
                    }
                }
                else
                {
                    Util.logError("refresh() failed on server.  SSF Error: " + error);
                }

                // Call the callback
                if (null != mCallback)
                {
                    mCallback(error);
                }
            }
        }

        private class TuningValues
        {
            public Dictionary<string, Dictionary<string, object>> Storage { get; set; }
            public Dictionary<string, double> Used { get; set; }

            public TuningValues()
            {
                Storage = new Dictionary<string, Dictionary<string, object>>();
                Used = new Dictionary<string, double>();
            }

            internal void updateEntity(string type, string id, IDictionary<string, object> values)
            {
                if(!Storage.ContainsKey(type))
                {
                    Storage.Add(type, new Dictionary<string, object>());
                }

                Dictionary<string, object> typeStorage = Storage[type];
                typeStorage[id] = values;
            }

            internal void removeEntity(string type, string id)
            {
                if(!Storage.ContainsKey(type))
                {
                    Storage.Add(type, new Dictionary<string, object>());
                }

                Dictionary<string, object> typeStorage = Storage[type];
                if(typeStorage.ContainsKey(id))
                {
                    typeStorage.Remove(id);
                }
            }

            internal T getValue<T>(string type, string id, string var, T defaultValue)
            {
                T ret = defaultValue;

                if (null != var)
                {
                    // let the cognitivevr backend know that this request took place
                    double curTimeStamp = Util.Timestamp();
                    if (!Used.ContainsKey(var) || curTimeStamp > Used[var] + Constants.TIME_RECORDAGAIN)
                    {
                        Used[var] = curTimeStamp;
                        new CoreSubsystem.DataPointBuilder("tuner_recordUsed")
                            .setArg(var)
                            .setArg(defaultValue)
                            .send();
                    }

                    if (Storage.ContainsKey(type))
                    {
                        Dictionary<string, object> typeStorage = Storage[type];
                        if ((null != id) && typeStorage.ContainsKey(id))
                        {
                            Dictionary<string, object> entityData = (Dictionary<string, object>)typeStorage[id];
                            if (entityData.ContainsKey(var))
                            {
                                try
                                {
                                    if (typeof(T).IsEnum && entityData[var] is string)
                                    {
                                        ret = (T)Enum.Parse(typeof(T), entityData[var] as string);
                                    }
                                    else
                                    {
                                        ret = (T)Convert.ChangeType(entityData[var], typeof(T));
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                return ret;
            }
        }
        #endregion
    }
}
