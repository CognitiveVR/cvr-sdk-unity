using UnityEngine;
using UnityEngine.XR;
using Cognitive3D;

public class ControllerTracking : MonoBehaviour
{
    InputDevice rightController;
    InputDevice leftController;

    // Update is called once per frame
    void Update()
    {
        if (rightController.isValid == false || leftController.isValid == false)
        {
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputTracking.trackingLost += OnTrackingLost;
            Cognitive3D_Manager.OnPreSessionEnd += Cleanup;
        } 
    }

    public void OnTrackingLost(XRNodeState xrNodeState)
    {
        if (!xrNodeState.tracked)
        {
            if (xrNodeState.nodeType == XRNode.RightHand)
            {
                new CustomEvent("c3d.Right Controller Lost tracking").Send();
            }
            if (xrNodeState.nodeType == XRNode.LeftHand)
            {
                new CustomEvent("c3d.Left Controller Lost tracking").Send();
            }
        }
    }

    private void Cleanup()
    {
        InputTracking.trackingLost -= OnTrackingLost;
    }
}
