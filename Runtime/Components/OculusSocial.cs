using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Oculus Social")]
    public class OculusSocial : AnalyticsComponentBase
    {
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
            if (!Oculus.Platform.Core.IsInitialized())
            {
                //Initialize will throw error if appid is invalid/missing
                try
                {
                    Oculus.Platform.Core.Initialize();
                    if (AssignOculusProfileToParticipant)
                    {
                        AssignParticipant();
                    }
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

        void AssignParticipant()
        {
#if C3D_OCULUS
            Oculus.Platform.Users.GetLoggedInUser().OnComplete(delegate (Oculus.Platform.Message<Oculus.Platform.Models.User> message)
            {
                if (message.IsError)
                {
                    Util.logDebug(message.GetError().Message);
                }
                else
                {
                    Oculus.Platform.Users.GetOrgScopedID(message.Data.ID).OnComplete(delegate (Oculus.Platform.Message<Oculus.Platform.Models.OrgScopedID> message2)
                    {
#if XRPF
                        if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSocialDataAllowed)
#endif
                        {
                            Cognitive3D_Manager.SetParticipantId(message2.Data.ID.ToString());
                        }
                    });
                }
            });
#endif
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
            return "Set a property for the user's party size and an Id to associate the user across your organization";
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