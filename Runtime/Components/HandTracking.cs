using System.Collections.Generic;
using UnityEngine;
using static OVRHand;
using TMPro;
using static OVRInput;
using System;

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Hand Tracking")]
    public class HandTracking : AnalyticsComponentBase
    {
        bool isHandTracking = false;
        int startTime;
        int currentTime;
        public DynamicObject rightHand;
        public DynamicObject leftHand;
        private OVRPlugin.Quatf WristRotation;

        /** THUMB **/
        private OVRPlugin.Quatf Thumb0Rotation; // Tumb Trapezium
        private OVRPlugin.Quatf Thumb1Rotation; // Thumb Metacarpal
        private OVRPlugin.Quatf Thumb2Rotation; // Thumb Proximal Phalange
        private OVRPlugin.Quatf Thumb3Rotation; // Thumb Distal Phalange

        /** INDEX **/
        private OVRPlugin.Quatf Index1Rotation; // Index Proximal Phalange
        private OVRPlugin.Quatf Index2Rotation; // Index Intermediate Phalange
        private OVRPlugin.Quatf Index3Rotation; // Index Distal Phalange

        /** MIDDLE **/
        private OVRPlugin.Quatf Middle1Rotation; // Middle Proximal Phalange
        private OVRPlugin.Quatf Middle2Rotation; // Middle Intermediate Phalange
        private OVRPlugin.Quatf Middle3Rotation; // Middle Distal Phalange

        /** RING **/
        private OVRPlugin.Quatf Ring1Rotation; // Ring Proximal Phalange
        private OVRPlugin.Quatf Ring2Rotation; // Ring Intermediate Phalange
        private OVRPlugin.Quatf Ring3Rotation; // Ring Distal Phalange

        /** PINKY **/
        private OVRPlugin.Quatf Pinky0Rotation; // Pinky Metacarpal
        private OVRPlugin.Quatf Pinky1Rotation; // Pinky Proximal Phalange
        private OVRPlugin.Quatf Pinky2Rotation; // Pinky Intermediate Phalange
        private OVRPlugin.Quatf Pinky3Rotation; // Pinky Distal Phalange
        public TextMeshProUGUI debug;
        public TextMeshProUGUI debug2;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            startTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            Cognitive3D_Manager.SetSessionProperty("c3d.app.handtracking.enabled", true);
        }

        // Update is called once per frame
        void Update()
        {
            if (OVRInput.GetActiveController() == OVRInput.Controller.None)
            {
                debug2.text = "NO TRACKING";
            }
            else if (OVRInput.GetActiveController() == OVRInput.Controller.Hands
                || OVRInput.GetActiveController() == OVRInput.Controller.LHand
                || OVRInput.GetActiveController() == OVRInput.Controller.RHand)
            {
                debug2.text = "HAND";
            }
            else
            {
                debug2.text = "CONTROLLER";
            }

            currentTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds - startTime;
            debug.text = currentTime.ToString();
            CaptureHandTrackingEvents();
            if (isHandTracking)
            {
                RecordFingerBoneRotationsOVR(OVRPlugin.Hand.HandLeft);
                RecordFingerBoneRotationsOVR(OVRPlugin.Hand.HandRight);
                RecordWristRotationOVR(OVRPlugin.Hand.HandLeft);
                RecordWristRotationOVR(OVRPlugin.Hand.HandRight);
            }
        }

        void CaptureHandTrackingEvents()
        {
            if (isHandTracking)
            {
                if (!OVRInput.IsControllerConnected(OVRInput.Controller.Hands))
                {
                    new CustomEvent("c3d.hands.stopped.tracking").Send();
                    isHandTracking = false;
                }
            }
            else
            {
                if (OVRInput.IsControllerConnected(OVRInput.Controller.Hands))
                {
                    new CustomEvent("c3d.hands.resumed.tracking").Send();
                    isHandTracking = true;
                }
            }
        }

        void RecordFingerBoneRotationsOVR(OVRPlugin.Hand hand)
        {
            OVRPlugin.HandState state;
            state = new OVRPlugin.HandState(); // potentially move to OnSessionBegin?
            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, hand, ref state))
            {
                Thumb0Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Thumb0]; // DOES THIS WORK? I DON'T WANT TO USE MAGIC NUMBER FOR BONE ID
                Thumb1Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Thumb1];
                Thumb2Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Thumb2];
                Thumb3Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Thumb3];

                Index1Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Index1];
                Index2Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Index2];
                Index3Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Index3];

                Middle1Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Middle1];
                Middle2Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Middle2];
                Middle3Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Middle3];

                Ring1Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Ring1];
                Ring2Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Ring2];
                Ring3Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Ring3];

                Pinky0Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Pinky0];
                Pinky1Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Pinky1];
                Pinky2Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Pinky2];
                Pinky3Rotation = state.BoneRotations[(int)OVRPlugin.BoneId.Hand_Pinky3];
            }

            // new CustomEvent("HAND").SetProperties(CreateDictionaryFromHand()).Send();
            
            if (hand == OVRPlugin.Hand.HandLeft)
            {
                leftHand.RecordSnapshot(CreateDictionaryFromHand());
            }
            else if (hand == OVRPlugin.Hand.HandRight)
            {
                rightHand.RecordSnapshot(CreateDictionaryFromHand());
            }
        }

        void RecordWristRotationOVR(OVRPlugin.Hand hand)
        {
            // WristRotation = // do something to get rotation. Ideally rotation of hand prefab? Wrist_root wasn't working, but maybe we can get it's global rotation?
        }

        List<KeyValuePair<string, object>> CreateDictionaryFromHand()
        {
            List<KeyValuePair<string, object>> handTrackingData = new List<KeyValuePair<string, object>>();

            handTrackingData.Add(new KeyValuePair<string, object>("thumb0-rotation-x", Thumb0Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb0-rotation-y", Thumb0Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb0-rotation-z", Thumb0Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb0-rotation-w", Thumb0Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("thumb1-rotation-x", Thumb1Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb1-rotation-y", Thumb1Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb1-rotation-z", Thumb1Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb1-rotation-w", Thumb1Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("thumb2-rotation-x", Thumb2Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb2-rotation-y", Thumb2Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb2-rotation-z", Thumb2Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb2-rotation-w", Thumb2Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-x", Thumb3Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-y", Thumb3Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-z", Thumb3Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-w", Thumb3Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-x", Thumb3Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-y", Thumb3Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-z", Thumb3Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("thumb3-rotation-w", Thumb3Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("index1-rotation-x", Index1Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("index1-rotation-y", Index1Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("index1-rotation-z", Index1Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("index1-rotation-w", Index1Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("index2-rotation-x", Index2Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("index2-rotation-y", Index2Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("index2-rotation-z", Index2Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("index2-rotation-w", Index2Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("index3-rotation-x", Index3Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("index3-rotation-y", Index3Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("index3-rotation-z", Index3Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("index3-rotation-w", Index3Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("middle1-rotation-x", Middle1Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("middle1-rotation-y", Middle1Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("middle1-rotation-z", Middle1Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("middle1-rotation-w", Middle1Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("middle2-rotation-x", Middle2Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("middle2-rotation-y", Middle2Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("middle2-rotation-z", Middle2Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("middle2-rotation-w", Middle2Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("middle3-rotation-x", Middle3Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("middle3-rotation-y", Middle3Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("middle3-rotation-z", Middle3Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("middle3-rotation-w", Middle3Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("ring1-rotation-x", Ring1Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("ring1-rotation-y", Ring1Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("ring1-rotation-z", Ring1Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("ring1-rotation-w", Ring1Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("ring2-rotation-x", Ring2Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("ring2-rotation-y", Ring2Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("ring2-rotation-z", Ring2Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("ring2-rotation-w", Ring2Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("ring3-rotation-x", Ring3Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("ring3-rotation-y", Ring3Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("ring3-rotation-z", Ring3Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("ring3-rotation-w", Ring3Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("pinky0-rotation-x", Pinky0Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky0-rotation-y", Pinky0Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky0-rotation-z", Pinky0Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky0-rotation-w", Pinky0Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("pinky1-rotation-x", Pinky1Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky1-rotation-y", Pinky1Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky1-rotation-z", Pinky1Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky1-rotation-w", Pinky1Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("pinky2-rotation-x", Pinky2Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky2-rotation-y", Pinky2Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky2-rotation-z", Pinky2Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky2-rotation-w", Pinky2Rotation.w));

            handTrackingData.Add(new KeyValuePair<string, object>("pinky3-rotation-x", Pinky3Rotation.x));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky3-rotation-y", Pinky3Rotation.y));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky3-rotation-z", Pinky3Rotation.z));
            handTrackingData.Add(new KeyValuePair<string, object>("pinky3-rotation-w", Pinky3Rotation.w));

            return handTrackingData;
        }
    }
}
