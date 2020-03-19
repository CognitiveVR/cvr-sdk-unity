using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class DisableIfIPDConstant : MonoBehaviour
    {
        void OnEnable()
        {
#if CVR_OCULUS
#if UNITY_2017_2_OR_NEWER
            if (UnityEngine.XR.XRDevice.model == "Oculus Rift S")
                gameObject.SetActive(false);
#else
            if (UnityEngine.VR.VRDevice.model == "Oculus Rift S")
                gameObject.SetActive(false);
#endif
#endif
        }
    }
}