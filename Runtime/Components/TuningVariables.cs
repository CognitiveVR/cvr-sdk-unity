using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;

/// <summary>
/// gets variables from the cognitive3d backend to configure your app
/// </summary>

//TODO debug startup flow
//TODO delay for some amount of time - if no participantId set, it should fetch using deviceId

//set session properties DONE
//static generic funciton to get values DONE
//locally cache in network class DONE
//read from local cache if it fails DONE

namespace Cognitive3D.Components
{
    #region Json
    [System.Serializable]
    internal class TuningVariableItem
    {
        public string name;
        public string description;
        public string type;
        public int valueInt;
        public string valueString;
        public bool valueBool;

        public override string ToString()
        {
            if (type == "string")
            {
                return string.Format("name:{0}, description:{1}, type:{2}, value:{3}", name, description, type,valueString);
            }
            if (type == "int")
            {
                return string.Format("name:{0}, description:{1}, type:{2}, value:{3}", name, description, type,valueInt);
            }
            if (type == "bool")
            {
                return string.Format("name:{0}, description:{1}, type:{2}, value:{3}", name, description, type,valueBool);
            }

            return string.Format("name:{0}, description:{1}, type:{2}", name, description, type);
        }
    }

    internal class TuningVariableCollection
    {
        public List<TuningVariableItem> tuningVariables = new List<TuningVariableItem>();
    }
    #endregion

    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Tuning Variables")]
    public class TuningVariables : AnalyticsComponentBase
    {
        bool hasFetchedVariables;
        public float timeout = 3;

        static Dictionary<string, TuningVariableItem> tuningVariables = new Dictionary<string, TuningVariableItem>();

        public delegate void onTuninVariablesAvailable();
        /// <summary>
        /// Called after tuning variables are available (also called after a delay if no response)
        /// </summary>
        public static event onTuninVariablesAvailable OnTuninVariablesAvailable;
        private static void InvokeOnTuninVariablesAvailable() { if (OnTuninVariablesAvailable != null) { OnTuninVariablesAvailable.Invoke(); } }

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnParticipantIdSet += Cognitive3D_Manager_OnParticipantIdSet;
            Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;
        }

        private void Cognitive3D_Manager_OnParticipantIdSet(string participantId)
        {
            FetchVariables(participantId);
        }

        void FetchVariables(string participantId)
        {
            NetworkManager.GetTuningVariables(participantId, TuningVariableResponse, timeout);
        }

        void TuningVariableResponse(int responsecode, string error, string text)
        {
            if (hasFetchedVariables)
            {
                Util.logDevelopment("TuningVariableResponse called multiple times!");
                return;
            }

            Util.logDevelopment("Tuning Variable reponse code " + responsecode);
            try
            {
                var tvc = JsonUtility.FromJson<TuningVariableCollection>(text);
                if (tvc == null)
                {
                    tuningVariables.Clear();
                    foreach (var entry in tvc.tuningVariables)
                    {
                        tuningVariables.Add(entry.name, entry);

                        if (entry.type == "string")
                        {
                            Cognitive3D_Manager.SetSessionProperty(entry.name, entry.valueString);
                        }
                        else if (entry.type == "int")
                        {
                            Cognitive3D_Manager.SetSessionProperty(entry.name, entry.valueInt);
                        }
                        else if (entry.type == "bool")
                        {
                            Cognitive3D_Manager.SetSessionProperty(entry.name, entry.valueBool);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            hasFetchedVariables = true;
            InvokeOnTuninVariablesAvailable();
        }

        //bool onboardingType2 = TuningVariables.GetValue<bool>(variableName: "onboarding_type", defaultValue: false);
        public static T GetValue<T>(string variableName, T defaultValue)
        {
            TuningVariableItem returnItem;
            if (tuningVariables.TryGetValue(variableName,out returnItem))
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)returnItem.valueString;
                }
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)returnItem.valueBool;
                }
                if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return (T)(object)returnItem.valueInt;
                }
            }
            return defaultValue;
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
    }
}
