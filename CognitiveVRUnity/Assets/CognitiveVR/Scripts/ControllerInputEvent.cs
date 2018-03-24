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
namespace CognitiveVR.Components
{
    public class ControllerInputEvent : CognitiveVRAnalyticsComponent
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
                BeginTransaction("primarytrigger", "trigger", false);
            }

            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            {
                BeginTransaction("secondarytrigger", "trigger", true);
            }

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
            {
                BeginTransaction("primarygrip", "grip", false);
            }

            if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
            {
                BeginTransaction("secondarygrip", "grip", true);
            }


            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
            {
                EndTransaction("primarytrigger","trigger",false);
            }
            if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
            {
                EndTransaction("secondarytrigger","trigger",true);
            }

            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger))
            {
                EndTransaction("primarygrip","grip",false);
            }

            if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
            {
                EndTransaction("secondarygrip","grip",true);
            }
        }
#endif

#if CVR_OCULUS || CVR_STEAMVR
        void BeginTransaction(string transactionKey, string type, bool rightController)
        {
            Vector3 pos = CognitiveVR_Manager.GetControllerPosition(rightController);

            string transactionID = Util.GetUniqueId();
            var inTransaction = new CustomEvent("cvr.input");
            inTransaction.SetProperty("type", type).SetProperty("device", rightController?"right controller": "left controller").SetProperty("state","begin");
            inTransaction.Send(pos);

            if (!pendingTransactions.ContainsKey(transactionKey))
            { pendingTransactions.Add(transactionKey, transactionID); }
        }

        void EndTransaction(string transactionKey, string type, bool rightController)
        {
            string transactionID;
            if (pendingTransactions.TryGetValue(transactionKey, out transactionID))
            {
                Vector3 pos = CognitiveVR_Manager.GetControllerPosition(rightController);
                new CustomEvent("cvr.input").SetProperty("type",type).SetProperty("device", rightController ? "right controller" : "left controller").SetProperty("state","end").Send(pos);
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
                bool right = i == 0 ? true : false;
                if (CognitiveVR_Manager.GetController(right) == null){continue;}
                controller = CognitiveVR_Manager.GetController(right).GetComponent<SteamVR_TrackedController>();

                if (controller == null)
                    controller = CognitiveVR_Manager.GetController(right).gameObject.AddComponent<SteamVR_TrackedController>();

                controller.TriggerClicked += new ClickedEventHandler(OnTriggerClicked);
                controller.TriggerUnclicked += new ClickedEventHandler(OnTriggerUnclicked);
                controller.Gripped += new ClickedEventHandler(OnGripped);
                controller.Ungripped += new ClickedEventHandler(OnUngripped);
                controller.PadClicked += new ClickedEventHandler(OnPadClicked);
            }
        }

        private void OnGripped(object sender, ClickedEventArgs e)
        {
            CognitiveVR_Manager.ControllerInfo cont = CognitiveVR_Manager.GetControllerInfo((int)e.controllerIndex);

            BeginTransaction("grip" + e.controllerIndex, "grip", cont.isRight);
        }

        private void OnUngripped(object sender, ClickedEventArgs e)
        {
            CognitiveVR_Manager.ControllerInfo cont = CognitiveVR_Manager.GetControllerInfo((int)e.controllerIndex);

            EndTransaction("grip" + e.controllerIndex, "grip", cont.isRight);
        }

        private void OnPadClicked(object sender, ClickedEventArgs e)
        {
            var padTransaction = new CustomEvent("cvr.input");
            CognitiveVR_Manager.ControllerInfo cont = CognitiveVR_Manager.GetControllerInfo((int)e.controllerIndex);
            if (cont == null) { return; }
            
            Vector3 pos = CognitiveVR_Manager.GetControllerPosition(cont.isRight);
            padTransaction.SetProperties(new Dictionary<string, object>
            {
                { "type","pad" },
                { "device", cont.isRight?"right controller":"left controller"},
                { "x",e.padX },
                { "y",e.padY }
            });
            padTransaction.Send(pos);
        }

        void OnTriggerClicked(object sender, ClickedEventArgs e)
        {
            CognitiveVR_Manager.ControllerInfo cont = CognitiveVR_Manager.GetControllerInfo((int)e.controllerIndex);
            BeginTransaction("trigger"+e.controllerIndex, "trigger", cont.isRight);
        }

        void OnTriggerUnclicked(object sender, ClickedEventArgs e)
        {
            CognitiveVR_Manager.ControllerInfo cont = CognitiveVR_Manager.GetControllerInfo((int)e.controllerIndex);

            EndTransaction("trigger" + e.controllerIndex, "trigger", cont.isRight);
        }
#endif

        public static string GetDescription()
        {
            return "Sends a transaction when a controller detects certain inputs. This should only be used for testing!\nRequires SteamVR or Oculus Touch controllers";
        }

        public static bool GetWarning()
        {
#if (!CVR_OCULUS && !CVR_STEAMVR) || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        void OnDestroy()
        {
            if (!Application.isPlaying) { return; }
            if (!Core.Initialized) { return; }
#if CVR_STEAMVR
            SteamVR_TrackedController controller;
            for (int i = 0; i < 2; i++)
            {
                bool right = i == 0 ? true : false;
                if (CognitiveVR_Manager.GetController(right) == null) { continue; }
                controller = CognitiveVR_Manager.GetController(right).GetComponent<SteamVR_TrackedController>();

                if (controller == null) { continue; }

                controller.TriggerClicked -= new ClickedEventHandler(OnTriggerClicked);
                controller.TriggerUnclicked -= new ClickedEventHandler(OnTriggerUnclicked);
                controller.Gripped -= new ClickedEventHandler(OnGripped);
                controller.Ungripped -= new ClickedEventHandler(OnUngripped);
                controller.PadClicked -= new ClickedEventHandler(OnPadClicked);
            }
#endif
        }
    }
}