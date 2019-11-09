using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR.ActiveSession
{
    public class SetCameraMask : MonoBehaviour
    {
        public int Mask = 8;

        [ContextMenu("Set Mask")]
        void Start()
        {
            GetComponent<Camera>().cullingMask = Mask;
        }
    }
}