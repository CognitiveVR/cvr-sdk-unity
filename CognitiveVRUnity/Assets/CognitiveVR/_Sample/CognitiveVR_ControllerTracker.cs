using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using CognitiveVR;

/// ===================================================
/// put this script on a GameObject with the SteamVR_TrackedController script
/// this tracks some common inputs
/// ===================================================

public class CognitiveVR_ControllerTracker : MonoBehaviour
{
#if CVR_STEAMVR
    SteamVR_TrackedController controller;
    Dictionary<string, string> pendingTransactions = new Dictionary<string, string>();

    void Start()
    {
        controller = GetComponent<SteamVR_TrackedController>();

        if (controller == null)
            controller = gameObject.AddComponent<SteamVR_TrackedController>();

        controller.TriggerClicked += new ClickedEventHandler(OnTriggerClicked);
        controller.TriggerUnclicked += new ClickedEventHandler(OnTriggerUnclicked);
        controller.Gripped += new ClickedEventHandler(OnGripped);
        controller.Ungripped += new ClickedEventHandler(OnUngripped);
        controller.PadClicked += new ClickedEventHandler(OnPadClicked);
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
}


