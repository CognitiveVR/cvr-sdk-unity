using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//immediately activate when gazed at
//used by eye tracking assessment

namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Internal/HMD Focus Button")]
    public class HMDFocusButton : MonoBehaviour, IPointerFocus
    {
        bool hasBeenFocused = false;
        public Material focusedMaterial;
        public UnityEngine.Events.UnityEvent OnLook;

        public MonoBehaviour MonoBehaviour { get { return this; } }

        //called from hmd gaze pointer
        public void SetPointerFocus(bool activation, bool fill)
        {
            if (hasBeenFocused) { return; }
            hasBeenFocused = true;
            GetComponentInChildren<Renderer>().material = focusedMaterial;
            if (OnLook != null)
                OnLook.Invoke();
        }
    }
}