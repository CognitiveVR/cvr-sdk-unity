using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;

//goes on the player's HMD to track stuff

namespace CognitiveVR
{
    public class PlayerTracker : MonoBehaviour
    {
        //Player Snapshots
        YieldInstruction playerSnapshotInverval;

        //HMD Height
        float averageHMDHeight;
        List<float> lastHMDHeights = new List<float>();
        int hmdHeightSamples = 20;

#if CVR_STEAMVR
        List<SteamVR_TrackedController> controllers = new List<SteamVR_TrackedController>();
#endif

        //Teleportation
        Transform _root;
        Transform root { get { if (_root == null) _root = transform.root; return _root; } }
        Vector3 lastRootPosition;

        Transform _transform;
        Transform myTransform { get { if (_transform == null) _transform = transform; return _transform; } }


        void Start()
        {
            playerSnapshotInverval = new WaitForSeconds(CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval);
            lastRootPosition = root.position;
            StartCoroutine(Capture());//TODO if tracking something that happens on snapshots else yield break;

            //arm tracking
        }

        IEnumerator Capture()
        {
            while (true)
            {
                yield return playerSnapshotInverval;
                if (CognitiveVR_Preferences.Instance.TrackHMDHeight) UpdateHMDHeight();
                if (CognitiveVR_Preferences.Instance.HMDCollision) HMDCollisionCheck();
            }
        }

        void Update()
        {
            if (CognitiveVR_Preferences.Instance.TrackTeleport) UpdateTeleport();
        }

        void HMDCollisionCheck()
        {
            bool hit = Physics.CheckSphere(myTransform.position, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
            if (hit)
            {
                Instrumentation.Transaction("collision").beginAndEnd();
            }
        }

        void UpdateTeleport()
        {
            if (Vector3.SqrMagnitude(lastRootPosition - root.position) > 0.1f)
            {
                string transactionID = System.Guid.NewGuid().ToString();
                Vector3 newPosition = root.position;

                Instrumentation.Transaction("teleport", transactionID).begin();
                if (CognitiveVR_Preferences.Instance.TrackTeleportDistance)
                {
                    Instrumentation.Transaction("teleport", transactionID).setProperty("distance", Vector3.Distance(newPosition, lastRootPosition));
                }
                Instrumentation.Transaction("teleport", transactionID).end();

                lastRootPosition = root.position;
            }
        }

        void UpdateHMDHeight()
        {
            float hmdHeight = 1.8f;
#if CVR_STEAMVR

#endif

#if CVR_OCULUS

#endif
            lastHMDHeights.Add(hmdHeight);
            if (lastHMDHeights.Count > hmdHeightSamples)
            {
                lastHMDHeights.RemoveAt(0);
            }
            for (int i = 0; i < lastHMDHeights.Count; i++)
                averageHMDHeight = lastHMDHeights[i];
            averageHMDHeight /= lastHMDHeights.Count;
        }
    }
}