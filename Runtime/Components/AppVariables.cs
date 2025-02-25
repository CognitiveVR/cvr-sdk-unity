using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Retrieves configuration variables from the Cognitive3D backend to customize your app.
/// </summary>

// If the participantId is set at session start, variables are fetched immediately.
// Otherwise, it waits up to 5 seconds for a participantId to be set.
// If the timer runs out, it defaults to using the deviceId.
// If fetchVariablesAutomatically is false, call Cognitive3D.Components.AppVariables.FetchVariables manually.

//CONSIDER custom editor to display app variables

namespace Cognitive3D.Components
{
#region Json
    internal class AppVariableCollection
    {
        public List<AppVariableItem> appVariables = new List<AppVariableItem>();
    }
#endregion

    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/App Variables")]
    public class AppVariables : AnalyticsComponentBase
    {
        static bool hasFetchedVariables;
        /// <summary>
        /// the delay to hear a response from our backend. If there is no response in this time, try to use a local cache of variables
        /// </summary>
        const float requestAppVariablesTimeout = 3;
        /// <summary>
        /// the delay waiting for participant id to be set (if not already set at the start of the session)
        /// </summary>
        public float waitForParticipantIdTimeout = 5;
        /// <summary>
        /// if true, uses the participant id (possibly with a delay) to get app variables. Otherwise, use the device id
        /// </summary>
        public bool useParticipantId = true;
        /// <summary>
        /// if true, sends identifying data to retrieve variables as soon as possible
        /// </summary>
        public bool fetchVariablesAutomatically = true;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;

            if (fetchVariablesAutomatically)
            {
                if (useParticipantId)
                {
                    //get variables if participant id is already set
                    if (!string.IsNullOrEmpty(Cognitive3D_Manager.ParticipantId))
                    {
                        FetchVariables(Cognitive3D_Manager.ParticipantId);
                    }
                    else
                    {
                        //listen for event
                        Cognitive3D_Manager.OnParticipantIdSet += Cognitive3D_Manager_OnParticipantIdSet;
                        //also start a timer
                        StartCoroutine(DelayFetch());
                    }
                }
                else //just use the hardware id to identify the user
                {
                    FetchVariables(Cognitive3D_Manager.DeviceId);
                }
            }
        }

        IEnumerator DelayFetch()
        {
            yield return new WaitForSeconds(waitForParticipantIdTimeout);
            FetchVariables(Cognitive3D_Manager.DeviceId);
        }

        private void Cognitive3D_Manager_OnParticipantIdSet(string participantId)
        {
            FetchVariables(participantId);
        }

        public static void FetchVariables()
        {
            if (!hasFetchedVariables)
            {
                NetworkManager.GetAppVariables(Cognitive3D_Manager.DeviceId, AppVariableResponse, requestAppVariablesTimeout);
            }
        }

        public static void FetchVariables(string participantId)
        {
            if (!hasFetchedVariables)
            {
                NetworkManager.GetAppVariables(participantId, AppVariableResponse, requestAppVariablesTimeout);
            }
        }

        static void AppVariableResponse(int responsecode, string error, string text)
        {
            if (hasFetchedVariables)
            {
                Util.logDevelopment("AppVariableResponse called multiple times!");
                return;
            }

            if (responsecode != 200)
            {
                Util.logDevelopment("App Variable reponse code " + responsecode + "  " + error);
            }
            
            try
            {
                var tvc = JsonUtility.FromJson<AppVariableCollection>(text);
                if (tvc != null)
                {
                    AppVariableManager.Reset();
                    foreach (var entry in tvc.appVariables)
                    {
                        AppVariableManager.SetVariable(entry);

                        if (entry.type == "string")
                        {
                            Cognitive3D_Manager.SetSessionProperty(entry.name, entry.valueString);
                        }
                        else if (entry.type == "int")
                        {
                            Cognitive3D_Manager.SetSessionProperty(entry.name, entry.valueInt);
                        }
                        else if (entry.type == "boolean")
                        {
                            Cognitive3D_Manager.SetSessionProperty(entry.name, entry.valueBool);
                        }
                    }
                }
                else
                {
                    Util.logDevelopment("AppVariableCollection is null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            hasFetchedVariables = true;
            AppVariableManager.InvokeOnAppVariablesAvailable();
        }

        private void Cognitive3D_Manager_OnPostSessionEnd()
        {
            hasFetchedVariables = false;
            Cognitive3D_Manager.OnParticipantIdSet -= Cognitive3D_Manager_OnParticipantIdSet;
            Cognitive3D_Manager.OnPostSessionEnd -= Cognitive3D_Manager_OnPostSessionEnd;
        }

        public override string GetDescription()
        {
            return "Retrieves variables from the Cognitive3D server to adjust the user's experience";
        }

        public override bool GetWarning()
        {
            return false;
        }

        [ContextMenu("Test Fetch with DeviceId")]
        void TestFetch()
        {
            FetchVariables(Cognitive3D_Manager.DeviceId);
        }
        [ContextMenu("Test Fetch with Random GUID")]
        void TestFetchGUID()
        {
            FetchVariables(System.Guid.NewGuid().ToString());
        }
    }
}
