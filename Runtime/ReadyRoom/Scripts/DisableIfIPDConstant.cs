using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public class DisableIfIPDConstant : MonoBehaviour
    {
        void OnEnable()
        {
#if C3D_OCULUS
#if UNITY_2019_1_OR_NEWER
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name.Contains("Rift"))
                gameObject.SetActive(false);
#elif UNITY_2017_2_OR_NEWER
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