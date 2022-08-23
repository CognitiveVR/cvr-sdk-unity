using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//if the SDK selected in Scene Setup does not support Eye Tracking
//then this gameobject will be disabled immediately

namespace Cognitive3D
{
    public class DisableIfNotEyeTracking : MonoBehaviour
    {
        void Awake()
        {
            if (GameplayReferences.SDKSupportsEyeTracking)
            {

            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}