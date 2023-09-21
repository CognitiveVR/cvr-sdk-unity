using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Oculus Social")]
    public class OculusSocial : AnalyticsComponentBase
    {
        public string appID;
#if C3D_OCULUS
        [Tooltip("Used to automatically associate a profile to a participant. Allows tracking between different sessions")]
        [SerializeField]
        private bool AssignOculusProfileToParticipant = true;

        [Tooltip("Sets a session property with the size of the user's party (skipped if playing alone)")]
        [SerializeField]
        private bool RecordPartySize = true;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
#if C3D_OCULUS
            if (!Core.IsInitialized())
            {
                //Initialize will throw error if appid is invalid/missing
                try
                {
                    Core.Initialize(appID);
                    Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
                    if (RecordPartySize)
                    {
                        CheckPartySize();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
#endif
        }

#if C3D_OCULUS
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
                    Cognitive3D_Manager.SetParticipantProperty("c3d.user.oculus.id", id);
                    Cognitive3D_Manager.SetParticipantProperty("c3d.user.oculus.oculusID", oculusID);
                }

                Users.Get(message.Data.ID).OnComplete(DisplayNameCallback);
                if (AssignOculusProfileToParticipant)
                {
                    Cognitive3D_Manager.SetParticipantId(id);
                }
            }
        }

        private void DisplayNameCallback(Message message)
        {
            string displayName = message.GetUser().DisplayName;
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSocialDataAllowed)
#endif
            {
                Cognitive3D_Manager.SetParticipantProperty("c3d.user.oculus.displayName", displayName);
            }
        }

        void CheckPartySize()
        {
#if C3D_OCULUS
            Oculus.Platform.Parties.GetCurrent().OnComplete(delegate (Oculus.Platform.Message<Oculus.Platform.Models.Party> message)
            {
                if (message.IsError)
                {
                    Util.logDebug(message.GetError().Message);
                }
                else if (message.Data != null)
                {
                    if (message.Data.UsersOptional != null)
                    {
#if XRPF
                        if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSocialDataAllowed)
#endif
                        {
                            Cognitive3D_Manager.SetSessionProperty("Party Size", message.Data.UsersOptional.Count);
                        }
                    }
                }
                else
                {
                    //no party
                }
            });
#endif
        }
#endif
        public override string GetDescription()
        {
#if C3D_OCULUS
            return "Set a property for the user's ID, name, and party size.";
#else
            return "Oculus Social properties can only be accessed when using the Oculus Platform";
#endif
        }
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