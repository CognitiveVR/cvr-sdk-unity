using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class EyeCapture
    {
        public Vector3 WorldPosition;

        //this should be true if gazing at sky. within fixation angle, but position will just mess up the average
        public bool SkipPositionForFixationAverage = false;
        public Vector3 HmdPosition;
        public long Time;

        public bool Discard; //empty or impossible values
        public bool EyesClosed; //blinking or eyes closed
        public bool OutOfRange; //compared to linkedFixation
        public bool OffTransform; //compared to linkedFixation

        public Transform HitDynamicTransform;
    }
}