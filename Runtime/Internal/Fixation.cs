using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public class Fixation
    {
        public Fixation() { }
        public Fixation(Fixation src)
        {
            WorldPosition = src.WorldPosition;
            LocalPosition = src.LocalPosition;
            DurationMs = src.DurationMs;
            StartMs = src.StartMs;
            LastNonDiscardedTime = StartMs;
            LastEyesOpen = StartMs;
            LastInRange = StartMs;
            LastOnTransform = StartMs;

            IsLocal = src.IsLocal;
            DynamicObjectId = src.DynamicObjectId;
            MaxRadius = src.MaxRadius;
            DynamicMatrix = src.DynamicMatrix;
            DynamicTransform = src.DynamicTransform;
        }

        //used for all eye tracking
        public Vector3 WorldPosition;
        public Vector3 LocalPosition;

        //set when starting local fixation. should hold last evaluated eye capture matrix for a dynamic object (updated every frame)
        public Matrix4x4 DynamicMatrix;
        //only used for active session view visualization!
        public Transform DynamicTransform;

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
        internal void AddEyeCapture(EyeCapture eyeCapture)
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
                DurationMs = eyeCapture.Time - StartMs;
            }

            if (IsLocal && eyeCapture.HitDynamicId == DynamicObjectId)
            {
                DynamicMatrix = eyeCapture.CaptureMatrix;
            }
        }
    }
}