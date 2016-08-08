using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using CognitiveVR;

/// ===================================================
/// This script should only be used for testing or as a reference
///
/// Sending Transactions for these common events may not be very helpful, depending on what you are trying to get from your data.
/// For example, it may be easier to send one Transaction at the end of a level to see how many bullets a player had fired.
/// These Transactions could be more valuable if you are trying to determine if certain gestures are difficult for some players to perform
/// ===================================================
namespace CognitiveVR
{
    public class ControllerInputTracker : CognitiveVRAnalyticsComponent
    {
#if CVR_STEAMVR
        Dictionary<string, string> pendingTransactions = new Dictionary<string, string>();

        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

            SteamVR_TrackedController controller;
            for (int i = 0; i<2; i++)
            {
                //TODO run this when a controller becomes active, not just on Init
                if (CognitiveVR_Manager.GetController(i) == null){continue;}
                controller = CognitiveVR_Manager.GetController(i).GetComponent<SteamVR_TrackedController>();

                if (controller == null)
                    controller = CognitiveVR_Manager.GetController(i).gameObject.AddComponent<SteamVR_TrackedController>();

                controller.TriggerClicked += new ClickedEventHandler(OnTriggerClicked);
                controller.TriggerUnclicked += new ClickedEventHandler(OnTriggerUnclicked);
                controller.Gripped += new ClickedEventHandler(OnGripped);
                controller.Ungripped += new ClickedEventHandler(OnUngripped);
                controller.PadClicked += new ClickedEventHandler(OnPadClicked);
            }
        }

        private void OnGripped(object sender, ClickedEventArgs e)
        {
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
#endif
        public static string GetDescription()
        {
            return "Sends a transaction when the player does an input with a SteamVR controller";
        }
    }
}