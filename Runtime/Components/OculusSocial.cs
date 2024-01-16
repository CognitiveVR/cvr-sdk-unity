using UnityEngine;

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
        [Tooltip("Used to record user data like username, id, and display name. Sessions will be named as users' display name in the session list.")]
        [SerializeField]
        private bool RecordOculusUserData = true;
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
}