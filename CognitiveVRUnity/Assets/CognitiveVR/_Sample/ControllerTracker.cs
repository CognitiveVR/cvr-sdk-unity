using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using CognitiveVR;

/// ===================================================
/// put this script on a GameObject with the SteamVR_TrackedController script
/// this tracks some common inputs
/// ===================================================
namespace CognitiveVR
{
    public class ControllerTracker : MonoBehaviour
    {
        //arm length tracking
        Transform HMDTransform;
        float maxDistance;
        int sampleCount = 50;
        int samples = 0;

#if CVR_STEAMVR

        YieldInstruction playerSnapshotInverval = new WaitForSeconds(CognitiveVR.CognitiveVR_Preferences.Instance.SnapshotInterval);

        SteamVR_TrackedController controller;
        Dictionary<string, string> pendingTransactions = new Dictionary<string, string>();

        void Start()
        {
            if (CognitiveVR_Preferences.Instance.TrackArmLength)
            {
                HMDTransform = Camera.main.transform;
            }

            controller = GetComponent<SteamVR_TrackedController>();

            if (controller == null)
                controller = gameObject.AddComponent<SteamVR_TrackedController>();

            controller.TriggerClicked += new ClickedEventHandler(OnTriggerClicked);
            controller.TriggerUnclicked += new ClickedEventHandler(OnTriggerUnclicked);
            controller.Gripped += new ClickedEventHandler(OnGripped);
            controller.Ungripped += new ClickedEventHandler(OnUngripped);
            controller.PadClicked += new ClickedEventHandler(OnPadClicked);
            StartCoroutine(Capture()); //TODO if tracking something that happens on snapshots else yield break;
        }

        IEnumerator Capture()
        {
            while (true)
            {
                yield return playerSnapshotInverval;
                if (CognitiveVR_Preferences.Instance.ControllerCollision) ControllerCollisionCheck();
                SampleArmLength();
            }
        }

        void ControllerCollisionCheck()
        {
            bool hit = Physics.CheckSphere(transform.position, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
            if (hit)
            {
                Instrumentation.Transaction("collision").beginAndEnd();
            }
        }

        private void OnGripped(object sender, ClickedEventArgs e)
        {
            if (CognitiveVR_Preferences.Instance.TrackArmLength && samples < sampleCount){maxDistance = Mathf.Max(Vector3.Distance(transform.position,HMDTransform.position));samples++;}
            string transactionDescription = "input";

            string transactionID = System.Guid.NewGuid().ToString();
            Transaction inTransaction = Instrumentation.Transaction(transactionDescription, transactionID);
            inTransaction.setProperty("controllerindex", e.controllerIndex).setProperty("type", "grip");
            inTransaction.begin();

            if (!pendingTransactions.ContainsKey(transactionDescription))
            { pendingTransactions.Add(transactionDescription, transactionID); }
        }

        private void OnUngripped(object sender, ClickedEventArgs e)
        {
            string transactionID;
            string transactionDescription = "input";
            if (pendingTransactions.TryGetValue(transactionDescription, out transactionID))
            {
                Instrumentation.Transaction(transactionDescription, transactionID).end();
                pendingTransactions.Remove(transactionID);
            }
        }

        private void OnPadClicked(object sender, ClickedEventArgs e)
        {
            Transaction padTransaction = Instrumentation.Transaction("input");
            padTransaction.setProperties(new Dictionary<string, object>
            {
                { "type","pad" },
                { "controllerindex",e.controllerIndex },
                { "x",e.padX },
                { "y",e.padY }
            });
            padTransaction.beginAndEnd();
        }

        void OnTriggerClicked(object sender, ClickedEventArgs e)
        {
            string transactionDescription = "input";

            string transactionID = System.Guid.NewGuid().ToString();
            Transaction inTransaction = Instrumentation.Transaction(transactionDescription, transactionID);
            inTransaction.setProperty("controllerindex", e.controllerIndex).setProperty("type", "trigger");
            inTransaction.begin();

            if (!pendingTransactions.ContainsKey(transactionDescription))
            { pendingTransactions.Add(transactionDescription, transactionID); }
        }

        void OnTriggerUnclicked(object sender, ClickedEventArgs e)
        {
            string transactionID;
            string transactionDescription = "input";
            if (pendingTransactions.TryGetValue(transactionDescription, out transactionID))
            {
                Instrumentation.Transaction(transactionDescription, transactionID).end();
                pendingTransactions.Remove(transactionID);
            }
        }

        void SampleArmLength()
        {
            if (CognitiveVR_Preferences.Instance.TrackArmLength && samples < sampleCount)
            {
                maxDistance = Mathf.Max(Vector3.Distance(transform.position, HMDTransform.position));
                samples++;
                if (samples >= sampleCount)
                {
                    Instrumentation.updateUserState(new Dictionary<string, object> { { "armlength", maxDistance } });
                }
            }
        }
#endif
    }
}