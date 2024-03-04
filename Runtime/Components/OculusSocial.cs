using UnityEngine;
using System;
using System.Collections.Generic;
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
#endif

    protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
#if C3D_OCULUS
            string appID = GetAppIDFromConfig();
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
#endif
        }

#if C3D_OCULUS

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
#endif

#if C3D_OCULUS
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
        }

#endif
        
        /// <summary>
        /// Populates session properties with subscription context details
        /// </summary>
        /// <param name="response">The subscription response returne by the API</param>
        private void SetSubscriptionProperties(SubscriptionContextResponseText response)
        {
            int numActiveSubscriptions = 0;

            // use string instead of bool so we can check if they are actually there with isNullOrEmpty
            // bool would just default to false
            for (int i = 0; i < response.data.Length; i++)
            {
                if (response.data[i] != null)
                {
                    Cognitive3D_Manager.SetSessionProperty($"c3d.user.meta.subscription{i + 1}.sku", response.data[i].sku);
                    Cognitive3D_Manager.SetSessionProperty($"c3d.user.meta.subscription{i + 1}.is_active", response.data[i].is_active);
                    if (response.data[i].is_active == "true") { numActiveSubscriptions++; }
                    Cognitive3D_Manager.SetSessionProperty($"c3d.user.meta.subscription{i + 1}.is_trial", response.data[i].is_trial);
                    Cognitive3D_Manager.SetSessionProperty($"c3d.user.meta.subscription{i + 1}.period_start_date", DateTime.Parse(response.data[i].period_start_time).ToUniversalTime().ToString());
                    Cognitive3D_Manager.SetSessionProperty($"c3d.user.meta.subscription{i + 1}.period_end_date", DateTime.Parse(response.data[i].period_end_time).ToUniversalTime().ToString());
                    Cognitive3D_Manager.SetSessionProperty($"c3d.user.meta.subscription{i + 1}.next_renewal_time", DateTime.Parse(response.data[i].next_renewal_time).ToUniversalTime().ToString());
                }
            }
            Cognitive3D_Manager.SetSessionProperty("c3d.user.meta.numberOfActiveSubscriptions", numActiveSubscriptions);
        }

        /// <summary>
        /// Callback to handle the user access token
        /// </summary>
        /// <param name="message">The response from GetAccessToken - message.Data.ToString to get the token</param>
        private void DoSubscriptionStuff(Message<string> message)
        {
            string userAccessToken = message.Data.ToString();
            Cognitive3D_Manager.SetParticipantProperty("c3d.app.meta.accessToken", userAccessToken);
            Cognitive3D_Manager.NetworkManager.Get
                (CognitiveStatics.METASUBSCRPTIONCONTEXT
                    (userAccessToken,subscriptionQueryParams),
                DeserializeAndSetSessionProperties);
        }

        /// <summary>
        /// Deserializes a json string into SubscriptionContextResponseText object and sets session properties
        /// </summary>
        /// <param name="data">The json string to be deserialized</param>
        private void DeserializeAndSetSessionProperties(string data)
        {
            SubscriptionContextResponseText subscriptionContextResponse = JsonUtility.FromJson<SubscriptionContextResponseText>(data);
            if (subscriptionContextResponse != null)
            {
                SetSubscriptionProperties(subscriptionContextResponse);
            }
        }

#if C3D_OCULUS
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
                Users.GetAccessToken().OnComplete(DoSubscriptionStuff);
                Cognitive3D_Manager.SetParticipantProperty("oculusDisplayName", displayName);
                if (RecordOculusUserData)
                {
                    Cognitive3D_Manager.SetParticipantFullName(displayName);
                }
            }
        }
#endif

        /// <summary>
        /// Description to display in inspector
        /// </summary>
        /// <returns> A string representing the description </returns>
        public override string GetDescription()
        {
#if C3D_OCULUS
            return "Set a property for the user's oculus id and display name";
#else
            return "Oculus Social properties can only be accessed when using the Oculus Platform";
#endif
        }

        /// <summary>
        /// Warning for incompatible platform to display on inspector
        /// </summary>
        public override bool GetWarning()
        {
#if C3D_OCULUS
            return false;
#else
            return true;
#endif
        }
    }

    /// <summary>
    /// A class defining the structure of the json response for subscription <br/>
    /// Example of the json structure can be found here:https://developer.oculus.com/documentation/unity/ps-subscriptions-s2s/   
    /// </summary>
    [System.Serializable]
    public class SubscriptionContextResponseText
    {
        public SubscriptionContextData[] data;

        [System.Serializable]
        public class SubscriptionContextData
        {
            /// <summary>
            /// String representing the product stock keeping unit
            /// </summary>
            public string sku;

            /// <summary>
            /// Set to true when a subscription is active
            /// </summary>
            public string is_active;

            /// <summary>
            /// Set to true when the most recent subscription period is a free trial (7d, 14d, 30d). <br/>
            /// Does not indicate that the subscription itself is active.
            /// </summary>
            public string is_trial;

            /// <summary>
            /// Timestamp for when subscription started (format: ISO 8601/RFC 3339)
            /// </summary>
            public string period_start_time;

            /// <summary>
            /// Timestamp for when subscription will end (format: ISO 8601/RFC 3339)
            /// </summary>
            public string period_end_time;

            /// <summary>
            /// Timestamp for when subscription will next be billed (format: ISO 8601/RFC 3339)
            /// </summary>
            public string next_renewal_time;
        }
    }
}