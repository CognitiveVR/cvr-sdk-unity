using System.Collections;
using System.Collections.Generic;
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
        Debug.Log(currentHmd);
        Debug.Log("Is it valid? " + currentHmd.isValid);
        // currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        
        if (currentHmd != null)
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

            }
            else if (!isUserPresent && lastPresenceVal) // removing headset
            {

            }
        }
    }
}
