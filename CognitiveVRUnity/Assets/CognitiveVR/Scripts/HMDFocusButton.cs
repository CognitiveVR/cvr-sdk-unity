using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//immediately activate when gazed at

namespace CognitiveVR
{
    public class HMDFocusButton : MonoBehaviour, IGazeFocus
    {
        bool hasBeenFocused = false;
        public Material focusedMaterial;
        public UnityEngine.Events.UnityEvent OnLook;

        //called from hmd gaze pointer
        public void SetGazeFocus()
        {
            if (hasBeenFocused) { return; }
            hasBeenFocused = true;
            GetComponentInChildren<Renderer>().material = focusedMaterial;
            if (OnLook != null)
                OnLook.Invoke();
        }
    }
}