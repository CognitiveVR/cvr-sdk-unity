using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//spawns and updates a 'feed' of events
//optimized to avoid enabling/disabling gameobjects since this causes lots of UI updates

namespace CognitiveVR.ActiveSession
{
    public class EventCanvas : MonoBehaviour
    {
        public RectTransform EventFeedRoot;
        public GameObject EventFeedPrefab;
        EventFeedEntry[] EventEntryPool = new EventFeedEntry[10];


        public Color PrimaryBackgroundColor;
        public Color SecondaryBackgroundColor;
        WaitForEndOfFrame endOfFrame;

        void Start()
        {
            CognitiveVR.CustomEvent.OnCustomEventRecorded += Instrumentation_OnCustomEventRecorded;
            for (int i = 0; i < EventEntryPool.Length; i++)
            {
                var go = Instantiate(EventFeedPrefab, EventFeedRoot);
                EventEntryPool[i] = go.GetComponent<EventFeedEntry>();
                go.SetActive(false);
            }
            StartCoroutine(CalcSizeEndOfFrame());
        }

        int eventCount = 0;
        private void Instrumentation_OnCustomEventRecorded(string name, Vector3 pos, List<KeyValuePair<string, object>> properties, string dynamicObjectId, double time)
        {
            var entrygo = GetPrefab(EventFeedRoot);
            var entry = entrygo.GetComponent<EventFeedEntry>();
            entry.SetEvent(name, pos, properties, dynamicObjectId, time);
            entry.SetBackgroundColor(eventCount % 2 == 0? PrimaryBackgroundColor: SecondaryBackgroundColor);
            
            eventCount++;
        }

        IEnumerator CalcSizeEndOfFrame()
        {
            endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return endOfFrame;
                for (int i = 0; i < EventEntryPool.Length; i++)
                {
                    if (EventEntryPool[i].DirtySize)
                    {
                        EventEntryPool[i].CalcSize();
                        EventEntryPool[i].DirtySize = false;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            CognitiveVR.CustomEvent.OnCustomEventRecorded -= Instrumentation_OnCustomEventRecorded;
        }

        public GameObject GetPrefab(Transform parent)
        {
            GameObject go = null;

            //check if all prefabs are enabled. if not, use first disabled one
            for (int i = 0; i < EventEntryPool.Length; i++)
            {
                if (EventEntryPool[i].gameObject.activeSelf)
                    continue;
                go = EventEntryPool[i].gameObject;
                go.SetActive(true);
                EventEntryPool[i].DirtySize = true;
                break;
            }
            //otherwise, get last prefab
            if (go == null)
            {
                go = EventFeedRoot.GetChild(9).gameObject;
                go.GetComponent<EventFeedEntry>().DirtySize = true;
            }
            go.transform.SetAsFirstSibling();
            return go;
        }

        public void ReturnToPool(GameObject go)
        {
            go.SetActive(false);
        }
    }
}