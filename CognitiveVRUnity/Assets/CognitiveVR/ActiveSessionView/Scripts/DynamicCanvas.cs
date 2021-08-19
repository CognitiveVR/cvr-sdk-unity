using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CognitiveVR.ActiveSession
{
    [AddComponentMenu("")]
    public class DynamicCanvas : MonoBehaviour
    {
        public GameObject DynamicSortablePrefab;
        public RectTransform DynamicListRoot;

        public Color PrimaryBackgroundColor;
        public Color SecondaryBackgroundColor;

        public Image SequenceSortIcon;
        public Image DurationSortIcon;
        public Image VisitsSortIcon;

        void Start()
        {
            UpdateSortIcons();
            if (GameplayReferences.SDKSupportsEyeTracking)
            {
                FixationCore.OnFixationRecord += FixationCore_OnFixationRecord;
            }
            else
            {
                GazeCore.OnDynamicGazeRecord += GazeCore_OnDynamicGazeRecord;
                GazeCore.OnSkyGazeRecord += GazeCore_OnSkyGazeRecord;
                GazeCore.OnWorldGazeRecord += GazeCore_OnWorldGazeRecord;
            }
        }

        private void GazeCore_OnWorldGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            lastGazed = null;
        }

        private void GazeCore_OnSkyGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            lastGazed = null;
        }

        List<DynamicSortable> DynamicSeqenceList = new List<DynamicSortable>();

        //the order of which dynamics are looked at
        int Sequence = 1;
        //number of items on the list. used for background colours
        int entryCount = 0;

        //listen for dynamic objects getting fixated on
        private void FixationCore_OnFixationRecord(Fixation fixation)
        {
            if (!fixation.IsLocal) { return; }

            //IMPROVEMENT faster lookup than a list.find
            var DynamicSequenceDisplay = DynamicSeqenceList.Find(delegate (DynamicSortable obj) { return obj.Id == fixation.DynamicObjectId; });
            if (DynamicSequenceDisplay != null)
            {
                //update display time
                DynamicSequenceDisplay.Visits++;
                DynamicSequenceDisplay.DurationMs += fixation.DurationMs;
                DynamicSequenceDisplay.SetDirty();
            }
            else
            {
                //add a new entry
                string name;
                if (DynamicManager.GetDynamicObjectName(fixation.DynamicObjectId, out name))
                {
                    var entrygo = GetPrefab(DynamicListRoot);
                    var entry = entrygo.GetComponent<DynamicSortable>();
                    entry.SetDynamic(fixation.DynamicObjectId, name, fixation.DurationMs, Sequence, 1);
                    entry.SetBackgroundColor(entryCount % 2 == 0 ? SecondaryBackgroundColor : PrimaryBackgroundColor);
                    DynamicSeqenceList.Add(entry);

                    entryCount++;
                    Sequence++;
                }
            }
            if (SortMethod == SortByMethod.Duration || SortMethod == SortByMethod.ReverseDuration)
            {
                SortByDuration();
            }
            if (SortMethod == SortByMethod.Visits || SortMethod == SortByMethod.ReverseVisits)
            {
                SortByVisits();
            }
            if (SortMethod == SortByMethod.Sequence || SortMethod == SortByMethod.ReverseSequence)
            {
                SortBySequence();
            }
        }

        DynamicSortable lastGazed = null;

        private void GazeCore_OnDynamicGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (string.IsNullOrEmpty(objectid))
            {
                lastGazed = null;
                return;
            }

            //ms
            int gazeInterval = (int)(CognitiveVR_Preferences.Instance.SnapshotInterval * 1000f);

            var DynamicSequenceDisplay = DynamicSeqenceList.Find(delegate (DynamicSortable obj) { return obj.Id == objectid; });
            if (DynamicSequenceDisplay != null)
            {
                //update display time
                DynamicSequenceDisplay.DurationMs += gazeInterval;
                if (lastGazed != DynamicSequenceDisplay)
                {
                    DynamicSequenceDisplay.Visits++;
                }
                lastGazed = DynamicSequenceDisplay;
                DynamicSequenceDisplay.SetDirty();
            }
            else
            {
                //add a new entry
                string name;
                if (DynamicManager.GetDynamicObjectName(objectid, out name))
                {
                    var entrygo = GetPrefab(DynamicListRoot);
                    var entry = entrygo.GetComponent<DynamicSortable>();
                    entry.SetDynamic(objectid, name, gazeInterval, Sequence, 1);
                    entry.SetBackgroundColor(entryCount % 2 == 0 ? SecondaryBackgroundColor : PrimaryBackgroundColor);
                    DynamicSeqenceList.Add(entry);
                    lastGazed = entry;

                    entryCount++;
                    Sequence++;
                }
            }
            if (SortMethod == SortByMethod.Duration || SortMethod == SortByMethod.ReverseDuration)
            {
                SortByDuration();
            }
            if (SortMethod == SortByMethod.Visits || SortMethod == SortByMethod.ReverseVisits)
            {
                SortByVisits();
            }
            if (SortMethod == SortByMethod.Sequence || SortMethod == SortByMethod.ReverseSequence)
            {
                SortBySequence();
            }
        }

        enum SortByMethod
        {
            Sequence,
            ReverseSequence,
            Duration,
            ReverseDuration,
            Visits,
            ReverseVisits
        }
        SortByMethod SortMethod;

        public void Button_SortByDuration()
        {
            if (SortMethod != SortByMethod.Duration)
                SortMethod = SortByMethod.Duration;
            else
                SortMethod = SortByMethod.ReverseDuration;
            SortByDuration();
        }

        void SortByDuration()
        {
            DynamicSeqenceList.Sort(delegate (DynamicSortable x, DynamicSortable y)
            {
                if (x.DurationMs == y.DurationMs)
                {
                    //when duration match sorting flips list around in unintended way. compare to sequence, which is always unique
                    if (x.Sequence == y.Sequence) { return 0; }
                    if (x.Sequence < y.Sequence) { return 1; }
                    return -1;
                }
                if (x.DurationMs > y.DurationMs) { return 1; }
                return -1;
            });
            if (SortMethod == SortByMethod.ReverseDuration)
                DynamicSeqenceList.Reverse();
            RefreshListOrder();
            UpdateSortIcons();
        }

        public void Button_SortByVisits()
        {
            if (SortMethod != SortByMethod.Visits)
                SortMethod = SortByMethod.Visits;
            else
                SortMethod = SortByMethod.ReverseVisits;
            SortByVisits();
        }

        void SortByVisits()
        {
            DynamicSeqenceList.Sort(delegate (DynamicSortable x, DynamicSortable y)
            {
                if (x.Visits == y.Visits)
                {
                    //when # visits match sorting flips list around in unintended way. compare to sequence, which is always unique
                    if (x.Sequence == y.Sequence) { return 0; }
                    if (x.Sequence < y.Sequence) { return 1; }
                    return -1;
                }
                if (x.Visits > y.Visits) { return 1; }
                return -1;
            });
            if (SortMethod == SortByMethod.ReverseVisits)
                DynamicSeqenceList.Reverse();
            RefreshListOrder();
            UpdateSortIcons();
        }

        public void Button_SortBySequence()
        {
            if (SortMethod != SortByMethod.Sequence)
                SortMethod = SortByMethod.Sequence;
            else
                SortMethod = SortByMethod.ReverseSequence;
            SortBySequence();
        }

        void SortBySequence()
        {
            DynamicSeqenceList.Sort(delegate (DynamicSortable x, DynamicSortable y)
            {
                if (x.Sequence == y.Sequence) { return 0; }
                if (x.Sequence > y.Sequence) { return 1; }
                return -1;
            });
            if (SortMethod == SortByMethod.ReverseSequence)
                DynamicSeqenceList.Reverse();
            RefreshListOrder();
            UpdateSortIcons();
        }

        void UpdateSortIcons()
        {
            SequenceSortIcon.enabled = false;
            DurationSortIcon.enabled = false;
            VisitsSortIcon.enabled = false;

            if (SortMethod == SortByMethod.Sequence)
            {
                SequenceSortIcon.enabled = true;
                SequenceSortIcon.transform.rotation = Quaternion.identity;
            }
            else if (SortMethod == SortByMethod.ReverseSequence)
            {
                SequenceSortIcon.enabled = true;
                SequenceSortIcon.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            else if (SortMethod == SortByMethod.Duration)
            {
                DurationSortIcon.enabled = true;
                DurationSortIcon.transform.rotation = Quaternion.identity;
            }
            else if (SortMethod == SortByMethod.ReverseDuration)
            {
                DurationSortIcon.enabled = true;
                DurationSortIcon.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            else if (SortMethod == SortByMethod.Visits)
            {
                VisitsSortIcon.enabled = true;
                VisitsSortIcon.transform.rotation = Quaternion.identity;
            }
            else if (SortMethod == SortByMethod.ReverseVisits)
            {
                VisitsSortIcon.enabled = true;
                VisitsSortIcon.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
        }

        void RefreshListOrder()
        {
            entryCount = 0;
            //put list in order of DynamicSeqenceList
            for(int i = 0; i< DynamicSeqenceList.Count;i++)
            {
                DynamicSeqenceList[i].transform.SetAsFirstSibling();

                //change background white/blue colours
                DynamicSeqenceList[i].SetBackgroundColor(entryCount % 2 == 0 ? SecondaryBackgroundColor : PrimaryBackgroundColor);
                entryCount++;
            }
        }

        public GameObject GetPrefab(Transform parent)
        {
            //instantaite for now. maybe pool later
            var go = Instantiate(DynamicSortablePrefab,parent);
            return go;
        }

        public void ReturnToPool(GameObject go)
        {
            go.SetActive(false);
        }
    }
}