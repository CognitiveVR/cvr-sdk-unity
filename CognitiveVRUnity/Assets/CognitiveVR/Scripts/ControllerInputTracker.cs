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
#if CVR_OCULUS || CVR_STEAMVR
        Dictionary<string, string> pendingTransactions = new Dictionary<string, string>();
#endif

#if CVR_OCULUS
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
        }

        public void Update()
        {
            //near touch stuff
            //var leftPoint = !OVRInput.Get(OVRInput.NearTouch.SecondaryIndexTrigger,OVRInput.Controller.LTouch);
            //var rightPoint = !OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

            //var leftThumb = !OVRInput.Get(OVRInput.NearTouch.SecondaryThumbButtons, OVRInput.Controller.LTouch);
            //var rightThumb = !OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, OVRInput.Controller.RTouch);

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            {
                BeginTransaction("primarytrigger", "trigger", 0);
            }

            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            {
                BeginTransaction("secondarytrigger", "trigger", 1);
            }

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
            {
                BeginTransaction("primarygrip", "grip", 0);
            }

            if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
            {
                BeginTransaction("secondarygrip", "grip", 1);
            }


            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
            {
                EndTransaction("primarytrigger");
            }
            if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
            {
                EndTransaction("secondarytrigger");
            }

            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger))
            {
                EndTransaction("primarygrip");
            }

            if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
            {
                EndTransaction("secondarygrip");
            }
        }
#endif

#if CVR_OCULUS || CVR_STEAMVR
        void BeginTransaction(string transactionKey, string type, int controller)
        {
            string transactionID = System.Guid.NewGuid().ToString();
            Transaction inTransaction = Instrumentation.Transaction("cvr.input", transactionID);
            inTransaction.setProperty("controllerindex", controller).setProperty("type", type);
            inTransaction.begin();

            if (!pendingTransactions.ContainsKey(transactionKey))
            { pendingTransactions.Add(transactionKey, transactionID); }
        }

        void EndTransaction(string transactionKey)
        {
            string transactionID;
            if (pendingTransactions.TryGetValue(transactionKey, out transactionID))
            {
                Instrumentation.Transaction("cvr.input", transactionID).end();
                pendingTransactions.Remove(transactionID);
            }
        }
#endif

#if CVR_STEAMVR
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
            BeginTransaction("grip"+e.controllerIndex, "grip", e.controllerIndex);
        }

        private void OnUngripped(object sender, ClickedEventArgs e)
        {
            EndTransaction("grip"+e.controllerIndex);
        }

        private void OnPadClicked(object sender, ClickedEventArgs e)
        {
            Transaction padTransaction = Instrumentation.Transaction("cvr.input");
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
            BeginTransaction("trigger"+e.controllerIndex, "trigger", e.controllerIndex);
        }

        void OnTriggerUnclicked(object sender, ClickedEventArgs e)
        {
            EndTransaction("trigger"+e.controllerIndex);
        }
#endif

        public static string GetDescription()
        {
            return "Sends a transaction when the player does an input with a SteamVR controller or Oculus Touch";
        }
    }
}