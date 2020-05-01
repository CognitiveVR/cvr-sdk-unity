using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//if the SDK selected in Scene Setup does not support Eye Tracking
//then this gameobject will be disabled immediately

namespace CognitiveVR
{
    public class DisableIfNotEyeTracking : MonoBehaviour
    {
        void Awake()
        {
#if CVR_TOBIIVR || CVR_FOVE || CVR_NEURABLE || CVR_PUPIL || CVR_AH || CVR_SNAPDRAGON || CVR_VIVEPROEYE || CVR_PICONEO2EYE

#else
            gameObject.SetActive(false);
#endif
        }
    }
}