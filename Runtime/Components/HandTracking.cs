using UnityEngine;

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Hand Tracking")]
    public class HandTracking : AnalyticsComponentBase
    {

#if C3D_OCULUS
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

        /** MIDDLE */
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

        OVRPlugin.HandState state;
#endif

        // Update is called once per frame
        void Update()
        {
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsBiometricDataAllowed)
#endif
            {
#if C3D_OCULUS
                RecordFingerBoneRotationsOVR(OVRPlugin.Hand.HandLeft);
                RecordFingerBoneRotationsOVR(OVRPlugin.Hand.HandRight);
                RecordWristRotationOVR(OVRPlugin.Hand.HandLeft);
                RecordWristRotationOVR(OVRPlugin.Hand.HandRight);
#endif
            }
        }

#if C3D_OCULUS
        void RecordFingerBoneRotationsOVR(OVRPlugin.Hand hand)
        {
            state = new OVRPlugin.HandState(); // potentiall move to OnSessionBegin?
            OVRPlugin.GetHandState(OVRPlugin.Step.Render, hand, ref state);
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

        void RecordWristRotationOVR(OVRPlugin.Hand hand)
        {
            // WristRotation = // do something to get rotation. Ideally rotation of hand prefab? Wrist_root wasn't working, but maybe we can get it's global rotation?
        }
#endif

    }
}
