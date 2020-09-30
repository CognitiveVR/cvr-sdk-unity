using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CognitiveVR
{
    namespace ActiveSession
    {
        public class DynamicCanvas : MonoBehaviour
        {
            public struct DynamicObjectDisplay
            {
                public string Name;
                public float Time;
                public DynamicObjectDisplay(string name, float time)
                {
                    Name = name;
                    Time = time;
                }
            }

            [System.Serializable]
            public class DynamicObjectEntry
            {
                public Text Name;
                public Text FixationTime;
            }

            public DynamicObjectEntry[] TopDynamicObjects;
            public Text FocusTimeText;

            void Start()
            {
                if (GameplayReferences.SDKSupportsEyeTracking)
                {
                    FocusTimeText.text = "Fixation Time";
                    FixationCore.OnFixationRecord += FixationCore_OnFixationRecord;
                }
                else
                {
                    FocusTimeText.text = "Gaze Time";
                    GazeCore.OnDynamicGazeRecord += GazeCore_OnDynamicGazeRecord;
                }
            }

            Dictionary<string, DynamicObjectDisplay> DynamicFocusTimes = new Dictionary<string, DynamicObjectDisplay>();

            private void GazeCore_OnDynamicGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
            {
                float durationMs = CognitiveVR_Preferences.S_SnapshotInterval * 1000;
                if (DynamicFocusTimes.ContainsKey(objectid))
                {
                    //update this key
                    var display = DynamicFocusTimes[objectid];
                    display.Time += durationMs;
                    DynamicFocusTimes[objectid] = display;
                }
                else
                {
                    //add new key to dictionary
                    string name;
                    if (DynamicManager.GetDynamicObjectName(objectid, out name))
                    {
                        DynamicFocusTimes.Add(objectid, new DynamicObjectDisplay(name, durationMs));
                    }
                }
            }

            private void FixationCore_OnFixationRecord(Fixation fixation)
            {
                if (!fixation.IsLocal) { return; }

                if (DynamicFocusTimes.ContainsKey(fixation.DynamicObjectId))
                {
                    //update this key
                    var display = DynamicFocusTimes[fixation.DynamicObjectId];
                    display.Time += fixation.DurationMs;
                    DynamicFocusTimes[fixation.DynamicObjectId] = display;
                }
                else
                {
                    //add new key to dictionary
                    string name;
                    if (DynamicManager.GetDynamicObjectName(fixation.DynamicObjectId, out name))
                    {
                        DynamicFocusTimes.Add(fixation.DynamicObjectId, new DynamicObjectDisplay(name, fixation.DurationMs));
                    }
                }
            }

            //keep dictionary of all dynamics (key) fixation time (value)

            int frame = 0;
            private void Update()
            {
                frame++;
                if (frame < 10) { return; }
                frame = 0;


                //sort dictionary
                List<DynamicObjectDisplay> display = new List<DynamicObjectDisplay>(DynamicFocusTimes.Values);

                display.Sort(delegate (DynamicObjectDisplay x, DynamicObjectDisplay y)
                {
                    if (x.Time > y.Time) return -1;
                    return 1;
                });

                for (int i = 0; i<TopDynamicObjects.Length;i++)
                {
                    if (display.Count <= i)
                    {
                        TopDynamicObjects[i].Name.text = string.Empty;
                        TopDynamicObjects[i].FixationTime.text = string.Empty;
                    }
                    else
                    {
                        TopDynamicObjects[i].Name.text = display[i].Name;
                        float focusTimeSec = display[i].Time * 0.001f;
                        string time = (focusTimeSec).ToString("0.00");
                        TopDynamicObjects[i].FixationTime.text = time+"s";
                    }
                }
            }
        }
    }
}