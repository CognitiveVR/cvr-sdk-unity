using UnityEngine;
using UnityEngine.XR;

public class HMDPresence : MonoBehaviour
{
    InputDevice currentHmd;
    bool isUserPresent;
    bool lastPresenceVal;

    // Update is called once per frame
    void Update()
    {
        if (!currentHmd.isValid)
        {
            currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            lastPresenceVal = true;
        }
        else
        {
            CheckUserPresence();
        }
    }

    void CheckUserPresence()
    {
        if (currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out isUserPresent))
        {
            if (isUserPresent && !lastPresenceVal) // put on headset after removing
            {
                new Cognitive3D.CustomEvent("c3d.Headset Reworn by User").Send();
                lastPresenceVal = true;
            }
            else if (!isUserPresent && lastPresenceVal) // removing headset
            {
                new Cognitive3D.CustomEvent("c3d.Headset Removed by User").Send();
                lastPresenceVal = false;
            }
        }
    }
}
