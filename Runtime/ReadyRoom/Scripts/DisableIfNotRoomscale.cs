using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//if the SDK selected in Scene Setup does not support Room Scale VR
//or the SDK room size is not configured to be Room Scale
//then this gameobject will be disabled immediately

namespace CognitiveVR
{
    public class DisableIfNotRoomscale : MonoBehaviour
    {
        void Awake()
        {
#if CVR_STEAMVR || CVR_STEAMVR2
            float roomX = 0;
            float roomY = 0;
            if (Valve.VR.OpenVR.Chaperone == null || !Valve.VR.OpenVR.Chaperone.GetPlayAreaSize(ref roomX, ref roomY))
            {
                gameObject.SetActive(false);
            }
            else if (Mathf.Approximately(roomX, 1f) && Mathf.Approximately(roomY, 1f))
            {
                gameObject.SetActive(false);
            }
#elif CVR_OCULUS
        if (!OVRPlugin.GetBoundaryConfigured() || OVRPlugin.GetBoundaryDimensions(OVRPlugin.BoundaryType.PlayArea).FromVector3f().magnitude < 1)
        {
            gameObject.SetActive(false);
        }
#else
            gameObject.SetActive(false);
#endif
        }
    }
}