using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
#if C3D_OCULUS
using Oculus.Platform;
using Oculus.Platform.Models;
#endif

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Oculus Social")]
    public class OculusSocial : AnalyticsComponentBase
    {

#if C3D_OCULUS
        [Tooltip("Used to record user data like username, id, and display name. Sessions will be named as users' display name in the session list. Allows tracking users across different sessions.")]
        [SerializeField]
        private bool RecordOculusUserData = true;

        /// <summary>
        /// A comma separated list of query parameters for meta API call
        /// </summary>
        List<string> subscriptionQueryParams = new List<string>() {"sku", "is_trial", "is_active", "period_start_time", "period_end_time", "next_renewal_time" };

        internal bool GetRecordOculusUserData()
        {
            return RecordOculusUserData;
        }

        internal void SetRecordOculusUserData(bool value)
        {
            RecordOculusUserData = value;
        }

        public enum InitializeType
        {
            Automatic,
            Delayed,
            Manual
        }
        [Tooltip("The behaviour to handle getting user data from the Oculus Platform.\n- Automatic will Initialize the platform with the current Platform.AppID.\n- Delayed will wait until you've checked Entitlement yourself.\n- Manual requires calling the code to record these session properties.")]
        public InitializeType initializeType = InitializeType.Automatic;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();

            if (initializeType == InitializeType.Automatic)
            {
                string appID = GetAppIDFromConfig();
                BeginOculusEntitlementCheck(appID);
            }
            else if (initializeType == InitializeType.Delayed)
            {
                //the developer is doing their own entitlement elsewhere, wait until that completes
                StartCoroutine(WaitForInitialize());
            }
            else if (initializeType == InitializeType.Manual)
            {
                //call this code when you've initialized everything:
                //var oculusSocial = FindObjectOfType<Cognitive3D.Components.OculusSocial>();
                //if (oculusSocial != null) {oculusSocial.BeginOculusEntitlementCheck(Cognitive3D.Components.OculusSocial.GetAppIDFromConfig());}
            }
        }
        IEnumerator WaitForInitialize()
        {
            yield return new WaitUntil(Core.IsInitialized);
            string appID = GetAppIDFromConfig();
            BeginOculusEntitlementCheck(appID);
        }

        /// <summary>
        /// Completes the entitlement check and callbacks if check is successful
        /// Should be called within first 10 seconds of launching app
        /// </summary>
        /// <param name="appID"> The Oculus AppID found in your Oculus dev dashboard </param>
        public void BeginOculusEntitlementCheck(string appID)
        {
            if (!Core.IsInitialized())
            {
                // Initialize will throw error if appid is invalid/missing
                try
                {
                    Core.Initialize(appID);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (Core.IsInitialized())
            {
                Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
            }

            if (!string.IsNullOrEmpty(appID))
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.app.oculus.appid", appID);
            }
        }

        /// <summary>
        /// Returns the oculus AppID from oculus platform settings
        /// </summary>
        /// <returns>A string representing the oculus AppID</returns>
        private static string GetAppIDFromConfig()
        {
            if (UnityEngine.Application.platform == RuntimePlatform.Android)
            {
                return PlatformSettings.MobileAppID;
            }
            else
            {
                return PlatformSettings.AppID;
            }
        }

        /// <summary>
        /// Callback for entitlement check
        /// Tries to get the logged in user and goes to the next callback
        /// </summary>
        /// <param name="message"> The response message </param>
        private void EntitlementCallback(Message message)
        {
            if (message.IsError) // User failed entitlement check
            {
                Debug.LogError("You are NOT entitled to use this app.");
            }
            else // User passed entitlement check
            {
                // Log the succeeded entitlement check for debugging.
                Debug.Log("You are entitled to use this app.");
                Users.GetLoggedInUser().OnComplete(UserCallback);
            }
        }

        /// <summary>
        /// Callback for getting details on the logged in user
        /// </summary>
        /// <param name="message"> The User object representing the current logged in user </param>
        private void UserCallback(Message<User> message)
        {
            string id;
            string oculusID;
            if (!message.IsError)
            {
                id = message.Data.ID.ToString();
                oculusID = message.Data.OculusID;
#if XRPF
                if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSocialDataAllowed)
#endif
                {
                    Cognitive3D_Manager.SetParticipantProperty("oculusId", id);
                    Cognitive3D_Manager.SetParticipantProperty("oculusUsername", oculusID);
                }

                Users.Get(message.Data.ID).OnComplete(DisplayNameCallback);
                if (RecordOculusUserData)
                {
                    Cognitive3D_Manager.SetParticipantId(id);
                }
            }
           Users.GetAccessToken().OnComplete(GetSubscriptionContext);
        }

        /// <summary>
        /// Callback to handle the user access token
        /// </summary>
        /// <param name="message">The response from GetAccessToken - message.Data.ToString to get the token</param>
        private void GetSubscriptionContext(Message<string> message)
        {
            string userAccessToken = message.Data.ToString();
            Cognitive3D_Manager.NetworkManager.Get
                (CognitiveStatics.MetaSubscriptionContextEndpoint
                    (userAccessToken,subscriptionQueryParams),
                DeserializeResponseAndSetSessionProperties);
        }

        /// <summary>
        /// Deserializes a json string into SubscriptionContextResponseText object and sets session properties
        /// </summary>
        /// <param name="data">The json string to be deserialized</param>
        private void DeserializeResponseAndSetSessionProperties(string data)
        {
            if (data != null)
            {
                SubscriptionContextResponseText subscriptionContextResponse = JsonUtility.FromJson<SubscriptionContextResponseText>(data);
                if (subscriptionContextResponse != null)
                {
                    for (int i = 0; i < subscriptionContextResponse.data.Length; i++)
                    {
                        if (subscriptionContextResponse.data[i] != null)
                        {
                            List<KeyValuePair<string, object>> subscription = new List<KeyValuePair<string, object>>();
                            subscription.Add(new KeyValuePair<string, object>("sku", subscriptionContextResponse.data[i].sku));
                            subscription.Add(new KeyValuePair<string, object>("is_active", subscriptionContextResponse.data[i].is_active));
                            subscription.Add(new KeyValuePair<string, object>("is_trial", subscriptionContextResponse.data[i].is_trial));
                            subscription.Add(new KeyValuePair<string, object>("period_start_date", TimeStringToUnix(subscriptionContextResponse.data[i].period_start_time)));
                            subscription.Add(new KeyValuePair<string, object>("period_end_date", TimeStringToUnix(subscriptionContextResponse.data[i].period_end_time)));
                            subscription.Add(new KeyValuePair<string, object>("next_renewal_date", TimeStringToUnix(subscriptionContextResponse.data[i].next_renewal_time)));
                            CoreInterface.WriteMetaSubscriptionProperty(i, subscription);
                        }
                    }
                    CoreInterface.SetSubscriptionDetailsReadyToSerialize(true);
                }
            }
        }

        /// <summary>
        /// Callback to get the display name
        /// apparently a second request is required
        /// https://stackoverflow.com/questions/76038469/oculus-users-getloggedinuser-return-empty-string-for-displayname-field)
        /// </summary>
        /// <param name="message"> The response for the callback </param>
        private void DisplayNameCallback(Message message)
        {
            string displayName = message.GetUser().DisplayName;
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSocialDataAllowed)
#endif
            {
                Cognitive3D_Manager.SetParticipantProperty("oculusDisplayName", displayName);
                if (RecordOculusUserData)
                {
                    Cognitive3D_Manager.SetParticipantFullName(displayName);
                }
            }
        }

#endif
        /// <summary>
        /// Converts ISO 8601 timestamp to unix timestamp in seconds
        /// </summary>
        /// <param name="timeString">A timestamp in a ISO 8601 format </param>
        /// <returns> The unix timestamp in seconds</returns>
        private long TimeStringToUnix(string timeString)
        {
            return ((DateTimeOffset) DateTime.Parse(timeString)).ToUnixTimeSeconds();
        }

#if C3D_OCULUS
        public override string GetDescription()
        {
            return "Set a property for the user's Oculus ID and display name";
        }

        /// <summary>
        /// Warning for incompatible platform to display on inspector
        /// </summary>
        public override bool GetWarning()
        {
            return false;
        }

#else //not C3D_OCULUS

        /// <summary>
        /// Description to display in inspector
        /// </summary>
        /// <returns> A string representing the description </returns>
        public override string GetDescription()
        {
            return "Oculus Social properties can only be accessed when using the Oculus Platform";
        }

        /// <summary>
        /// Warning for incompatible platform to display on inspector
        /// </summary>
        public override bool GetWarning()
        {
            return true;
        }
#endif
    }

    /// <summary>
    /// A class defining the structure of the json response for subscription <br/>
    /// Example of the json structure can be found here:https://developer.oculus.com/documentation/unity/ps-subscriptions-s2s/   
    /// </summary>
    [Serializable]
    public class SubscriptionContextResponseText
    {
        public SubscriptionContextData[] data;

        [Serializable]
        public class SubscriptionContextData
        {
            /// <summary>
            /// String representing the product stock keeping unit
            /// </summary>
            public string sku;

            /// <summary>
            /// Set to true when a subscription is active
            /// </summary>
            public bool is_active;

            /// <summary>
            /// Set to true when the most recent subscription period is a free trial (7d, 14d, 30d). <br/>
            /// Does not indicate that the subscription itself is active.
            /// </summary>
            public bool is_trial;

            /// <summary>
            /// Timestamp for when subscription started
            /// </summary>
            public string period_start_time;

            /// <summary>
            /// Timestamp for when subscription will end
            /// </summary>
            public string period_end_time;

            /// <summary>
            /// Timestamp for when subscription will next be billed
            /// </summary>
            public string next_renewal_time;
        }
    }
}