using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Retrieves configuration variables from the Cognitive3D backend to customize your app.
/// </summary>

// If the participantId is set at session start, variables are fetched immediately.
// Otherwise, it waits up to 5 seconds for a participantId to be set.
// If the timer runs out, it defaults to using the deviceId.
// If fetchVariablesAutomatically is false, call Cognitive3D.Components.RemoteControls.FetchVariables manually.

//CONSIDER custom editor to display remote variables

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Remote Controls")]
    public class RemoteControls : AnalyticsComponentBase
    {
        /// <summary>
        /// List to store remote variables
        /// </summary>
        static List<RemoteVariableItem> remoteVariables = new List<RemoteVariableItem>();

        /// <summary>
        /// Flag indicating if variables have been fetched
        /// </summary>
        static bool hasFetchedVariables;

        /// <summary>
        /// the delay to hear a response from our backend. If there is no response in this time, try to use a local cache of variables
        /// </summary>
        const float requestRemoteControlsTimeout = 3;

        /// <summary>
        /// the delay waiting for participant id to be set (if not already set at the start of the session)
        /// </summary>
        public float waitForParticipantIdTimeout = 5;

        /// <summary>
        /// if true, uses the participant id (possibly with a delay) to get remote variables. Otherwise, use the device id
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

        /// <summary>
        /// Delay fetching of remote variables if the participant ID is not set within the specified timeout
        /// </summary>
        IEnumerator DelayFetch()
        {
            yield return new WaitForSeconds(waitForParticipantIdTimeout);
            FetchVariables(Cognitive3D_Manager.DeviceId);
        }

        private void Cognitive3D_Manager_OnParticipantIdSet(string participantId)
        {
            FetchVariables(participantId);
        }

        /// <summary>
        /// Fetches remote variables if not already fetched, using the device ID.
        /// </summary>
        public static void FetchVariables()
        {
            if (!hasFetchedVariables)
            {
                NetworkManager.GetRemoteControls(Cognitive3D_Manager.DeviceId, RemoteControlResponse, requestRemoteControlsTimeout);
            }
        }

        /// <summary>
        /// Fetches remote variables using a participant ID if not already fetched.
        /// </summary>
        /// <param name="participantId"></param>
        public static void FetchVariables(string participantId)
        {
            if (!hasFetchedVariables)
            {
                NetworkManager.GetRemoteControls(participantId, RemoteControlResponse, requestRemoteControlsTimeout);
            }
        }

        static void RemoteControlResponse(int responsecode, string error, string text)
        {
            if (hasFetchedVariables)
            {
                Util.logDevelopment("RemoteControlResponse called multiple times!");
                return;
            }

            if (responsecode != 200)
            {
                Util.logDevelopment("remote variable response code " + responsecode + "  " + error);
            }
            
            try
            {
                var tvc = JsonUtility.FromJson<RemoteVariableCollection>(text);
                if (tvc == null)
                {
                    Util.logError("No remote variable found!");
                    return;
                }

                RemoteControlManager.Reset();
                if (tvc.abTests.Count > 0)
                {
                    ProcessRemoteVariables(tvc.abTests, RemoteControlManager.SetABTest);
                }

                if (tvc.remoteConfigurations.Count > 0)
                {
                    ProcessRemoteVariables(tvc.remoteConfigurations, RemoteControlManager.SetRemoteConfiguration);
                }

                foreach (var item in remoteVariables)
                {
                    if (item.type == "string")
                    {
                        Cognitive3D_Manager.SetSessionProperty(item.appVariableName, RemoteControlManager.GetValue<string>(item.appVariableName, ""));
                    }
                    else if (item.type == "bool")
                    {
                        Cognitive3D_Manager.SetSessionProperty(item.appVariableName, RemoteControlManager.GetValue<bool>(item.appVariableName, false));
                    }
                    else if (item.type == "int")
                    {
                        Cognitive3D_Manager.SetSessionProperty(item.appVariableName, RemoteControlManager.GetValue<int>(item.appVariableName, 0));
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            hasFetchedVariables = true;
            RemoteControlManager.InvokeOnRemoteControlsAvailable();
        }

        /// <summary>
        /// Processes a list of remote variables and applies the provided setter function to each entry.
        /// </summary>
        /// <param name="variables"></param>
        /// <param name="setter"></param>
        private static void ProcessRemoteVariables(List<RemoteVariableItem> variables, Action<RemoteVariableItem> setter)
        {
            if (variables == null || variables.Count == 0) return;

            foreach (var entry in variables)
            {
                setter(entry);
                remoteVariables.Add(entry);
            }
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
