using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class EyeCapture
    {
        public Vector3 WorldPosition;
        public Vector3 LocalPosition;
        //captured transform matrix of the dynamic object
        public Matrix4x4 CaptureMatrix;
        //if the transform matrix was correctly set from local capture
        public bool UseCaptureMatrix;
        public string HitDynamicId;

        //this should be true if gazing at sky. within fixation angle, but position will just mess up the average
        public bool SkipPositionForFixationAverage = false;
        public Vector3 HmdPosition;
        public long Time;

        public bool Discard; //empty or impossible values
        public bool EyesClosed; //blinking or eyes closed
        public bool OutOfRange; //compared to linkedFixation
        public bool OffTransform; //compared to linkedFixation
        public Vector2 ScreenPos;

        //TODO remove this
        [System.Obsolete]
        public Transform HitDynamicTransform;
    }
}