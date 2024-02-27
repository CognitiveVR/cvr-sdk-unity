using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
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
        private bool isResponseJsonValid;
        private const string URL_FOR_SUBSCRIPTION = "https://graph.oculus.com/application/subscriptions";
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
        IEnumerator Get(string url)
        {
            var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            switch (req.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    // Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    new CustomEvent("Connection or Data Error").Send();
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    // Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    new CustomEvent("Protocol Error").Send();
                    break;
                case UnityWebRequest.Result.Success:
                    // Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                    var data = req.downloadHandler.text;
                    SubscriptionContextResponseText response = null;

                    try
                    {
                        response = JsonUtility.FromJson<SubscriptionContextResponseText>(data);
                        isResponseJsonValid = true;
                    }
                    catch
                    {
                        isResponseJsonValid = false;
                        Debug.LogError("Invalid JSON response");
                    }

                    if (isResponseJsonValid)
                    {
                        SetSubscriptionProperties(response);
                    }
                    break;
            }
        }

        /// <summary>
        /// Populates session properties with subscription context details
        /// </summary>
        /// <param name="response">The subscription response returne by the API</param>
        private void SetSubscriptionProperties(SubscriptionContextResponseText response)
        {
            if (!string.IsNullOrEmpty(response.data[0].sku))
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.app.meta.sku", response.data[0].sku);
            }
            if (!string.IsNullOrEmpty(response.data[0].isActive))
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.app.meta.isActive", response.data[0].isActive);
            }
            if (!string.IsNullOrEmpty(response.data[0].isTrial))
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.app.meta.isTrial", response.data[0].isTrial);
            }
            if (!string.IsNullOrEmpty(response.data[0].periodStartDate))
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.app.meta.periodStartDate", response.data[0].periodStartDate);
            }
            if (!string.IsNullOrEmpty(response.data[0].periodEndDate))
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.app.meta.periodEndDate", response.data[0].periodEndDate);
            }
            if (!string.IsNullOrEmpty(response.data[0].nextRenewalTime))
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.app.meta.nextRenewalTime", response.data[0].nextRenewalTime);
            }
        }

        /// <summary>
        /// Callback to handle the user access token
        /// </summary>
        /// <param name="message">The response from GetAccessToken - message.Data.ToString to get the token</param>
        private void DoSubscriptionStuff(Message<string> message)
        {
            string userAccessToken = message.Data.ToString();
            Cognitive3D_Manager.SetParticipantProperty("c3d.app.meta.accessToken", userAccessToken);
            string url = URL_FOR_SUBSCRIPTION +
                "?access_token=" + userAccessToken +
                "&fields=sku,period_start_time,period_end_time,is_trial,is_active,next_renewal_time";
            StartCoroutine(Get(url));
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
    class SubscriptionContextResponseText
    {
        internal SubscriptionContextData[] data;

        internal class SubscriptionContextData
        {
            internal string sku;
            internal Owner owner;
            internal string isActive;
            internal string isTrial;
            internal string periodStartDate;
            internal string periodEndDate;
            internal string nextRenewalTime;

            internal class Owner
            {
                internal string id;
            }
        }
    }
}