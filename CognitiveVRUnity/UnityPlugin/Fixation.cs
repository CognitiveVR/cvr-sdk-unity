using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class Fixation
    {
        public Fixation() { }
        public Fixation(Fixation src)
        {
            WorldPosition = src.WorldPosition;
            LocalPosition = src.LocalPosition;
            DebugScale = src.DebugScale;
            DurationMs = src.DurationMs;
            StartMs = src.StartMs;

            IsLocal = src.IsLocal;
            LocalTransform = src.LocalTransform;
            DynamicObjectId = src.DynamicObjectId;
            MaxRadius = src.MaxRadius;
        }

        //used for all eye tracking
        public Vector3 WorldPosition;
        public Vector3 LocalPosition;
        public Transform LocalTransform;
        public float DebugScale;

        //timestamp of last assigned valid eye capture. used to 'timeout' from eyes closed
        public long LastUpdated;

        public long DurationMs;
        public long StartMs;

        public long LastNonDiscardedTime;
        public long LastEyesOpen;
        public long LastInRange;
        public long LastOnTransform;

        public float StartDistance;
        //radius in meters that this fixation covers
        public float MaxRadius;
        public bool IsLocal;
        public string DynamicObjectId;

        //called just before new eyecapture is recorded. this has been around 1 second, so 'safe' to actually record
        public void AddEyeCapture(EyeCapture eyeCapture)
        {
            bool validEyeCapture = true;

            if (eyeCapture.Discard)
            {
                validEyeCapture = false;
            }
            else
            {
                LastNonDiscardedTime = eyeCapture.Time;
            }

            if (eyeCapture.EyesClosed)
            {
                validEyeCapture = false;
            }
            else
            {
                LastEyesOpen = eyeCapture.Time;
            }


            if (eyeCapture.OutOfRange)
            {
                validEyeCapture = false;
            }
            else
            {
                LastInRange = eyeCapture.Time;
            }

            if (eyeCapture.OffTransform)
            {
                validEyeCapture = false;
            }
            else
            {
                LastOnTransform = eyeCapture.Time;
            }

            //basically just add duration
            if (validEyeCapture)
            {
                LastUpdated = eyeCapture.Time;
                DurationMs = StartMs - LastUpdated;
            }
        }
    }
}