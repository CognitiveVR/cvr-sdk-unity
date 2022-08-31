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
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name.Contains("Rift"))
                gameObject.SetActive(false);
#endif
        }
    }
}