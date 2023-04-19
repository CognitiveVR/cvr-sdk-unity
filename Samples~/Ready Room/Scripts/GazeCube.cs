using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.ReadyRoom
{
    public class GazeCube : MonoBehaviour
    {
        int sidesRemaining = 6;
        List<HMDFocusButton> Sides;
        public UnityEngine.Events.UnityEvent OnComplete;

        void Start()
        {
            Sides = new List<HMDFocusButton>();
            var sides = GetComponentsInChildren<HMDFocusButton>();
            foreach (var s in sides)
            {
                Sides.Add(s);
            }
        }

        //this should only be called once per 'side'. does not check if a side has been 'gazed' at already
        public void GazeAtSide()
        {
            sidesRemaining--;
            if (sidesRemaining == 0)
            {
                OnComplete.Invoke();
            }
        }
    }
}