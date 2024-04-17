using UnityEngine;
using System.Collections;
using Cognitive3D;
using System;

//utility code to get player's eye ray

namespace Cognitive3D
{
    public static class GazeHelper
    {
        public static Ray GetCurrentWorldGazeRay()
        {
            return new Ray(GameplayReferences.HMD.position, GetLookDirection());
        }
#if C3D_SRANIPAL

        static ViveSR.anipal.Eye.SRanipal_Eye_Framework framework;
        static ViveSR.anipal.Eye.SRanipal_Eye_Framework.SupportedEyeVersion version;
        static void InitCheck()
        {
            if (framework != null) { return; }
            framework = ViveSR.anipal.Eye.SRanipal_Eye_Framework.Instance;
            if (framework != null)
            {
                version = framework.EnableEyeVersion;
            }
        }

        static Vector3 lastDir = Vector3.forward;
        static Vector3 GetLookDirection()
        {
            InitCheck();
            var ray = new Ray();
            if (version == ViveSR.anipal.Eye.SRanipal_Eye_Framework.SupportedEyeVersion.version1)
            {
                if (ViveSR.anipal.Eye.SRanipal_Eye.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE, out ray))
                {
                    lastDir = GameplayReferences.HMD.TransformDirection(ray.direction);
                }
            }
            else
            {
                if (ViveSR.anipal.Eye.SRanipal_Eye_v2.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE, out ray))
                {
                    lastDir = GameplayReferences.HMD.TransformDirection(ray.direction);
                }
            }
            return lastDir;
        }
#elif C3D_VARJOVR
        static Vector3 lastDir = Vector3.forward;
        static Vector3 GetLookDirection()
        {
            if (Varjo.XR.VarjoEyeTracking.IsGazeAllowed() && Varjo.XR.VarjoEyeTracking.IsGazeCalibrated())
            {
                var data = Varjo.XR.VarjoEyeTracking.GetGaze();
                if (data.status != Varjo.XR.VarjoEyeTracking.GazeStatus.Invalid)
                {
                    var ray = data.gaze;
                    lastDir = GameplayReferences.HMD.TransformDirection(new Vector3((float)ray.forward[0], (float)ray.forward[1], (float)ray.forward[2]));
                }
            }
            return lastDir;
        }
#elif C3D_VARJOXR
        static Vector3 lastDir = Vector3.forward;
        static Vector3 GetLookDirection()
        {
            if (Varjo.XR.VarjoEyeTracking.IsGazeAllowed() && Varjo.XR.VarjoEyeTracking.IsGazeCalibrated())
            {
                var data = Varjo.XR.VarjoEyeTracking.GetGaze();
                if (data.status != Varjo.XR.VarjoEyeTracking.GazeStatus.Invalid)
                {
                    var ray = data.gaze;
                    lastDir = GameplayReferences.HMD.TransformDirection(new Vector3((float)ray.forward[0], (float)ray.forward[1], (float)ray.forward[2]));
                }
            }
            //TODO CONSIDER defaulting the gaze to be in the forward direction of the HMD, not the world
            return lastDir;
        }
#elif C3D_PICOVR
        static Vector3 lastDirection = Vector3.forward;

        static Vector3 GetLookDirection()
        {
            Pvr_UnitySDKAPI.EyeTrackingGazeRay gazeRay = new Pvr_UnitySDKAPI.EyeTrackingGazeRay();
            if (Pvr_UnitySDKAPI.System.UPvr_getEyeTrackingGazeRayWorld(ref gazeRay))
            {
                if (gazeRay.IsValid && gazeRay.Direction.sqrMagnitude > 0.1f)
                {
                    lastDirection = gazeRay.Direction;
                }
            }
            return lastDirection;
        }
#elif C3D_PICOXR
        static Vector3 lastDirection = Vector3.forward;

        static Vector3 GetLookDirection()
        {
            if (!Unity.XR.PXR.PXR_Manager.Instance.eyeTracking)
            {
                lastDirection = Cognitive3D.GameplayReferences.HMD.rotation * Vector3.forward;
                return lastDirection;
            }

            Vector3 direction;
            if (Unity.XR.PXR.PXR_EyeTracking.GetCombineEyeGazeVector(out direction) && direction.sqrMagnitude > 0.1f)
            {
                lastDirection = Cognitive3D.GameplayReferences.HMD.rotation * direction;
            }
            return lastDirection;
        }
#elif C3D_OMNICEPT
        static Vector3 lastDirection = Vector3.forward;
        static HP.Omnicept.Unity.GliaBehaviour gb;
        static float confidenceThresholdForAcceptedValue = 0.50f;

        static void DoEyeTracking(HP.Omnicept.Messaging.Messages.EyeTracking data)
        {
            if (data.CombinedGaze.Confidence > confidenceThresholdForAcceptedValue)
            {
                lastDirection = GameplayReferences.HMD.TransformDirection(new Vector3(-data.CombinedGaze.X, data.CombinedGaze.Y, data.CombinedGaze.Z));
            }
        }

        static Vector3 GetLookDirection()
        {
            if (gb == null)
            {
                gb = GameplayReferences.GliaBehaviour;
                if (gb != null)
                    gb.OnEyeTracking.AddListener(DoEyeTracking);
            }
            return lastDirection;
        }
#elif C3D_MRTK
        static Vector3 lastDirection = Vector3.forward;
        static Vector3 GetLookDirection()
        {
            if (Microsoft.MixedReality.Toolkit.CoreServices.InputSystem.EyeGazeProvider.IsEyeTrackingEnabledAndValid)
            {
                lastDirection = Microsoft.MixedReality.Toolkit.CoreServices.InputSystem.EyeGazeProvider.GazeDirection;
            }
            return lastDirection;
        }
#elif C3D_VIVEWAVE
        static Vector3 lastDirection = Vector3.forward;
        static Vector3 GetLookDirection()
        {
            Wave.Essence.Eye.EyeManager.Instance.GetCombindedEyeDirectionNormalized(out lastDirection);
            return lastDirection;
        }
#elif C3D_OCULUS
        static Vector3 lastDirection = Vector3.forward;
        private static OVRPlugin.EyeGazesState _currentEyeGazesState;
        private static float ConfidenceThreshold = 0.5f;
        static Vector3 GetLookDirection()
        {
            if (GameplayReferences.SDKSupportsEyeTracking && GameplayReferences.EyeTrackingEnabled)
            {
                if (!OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref _currentEyeGazesState))
                    return lastDirection;

                float lblinkweight;
                float rblinkweight;
                GameplayReferences.OVRFaceExpressions.TryGetFaceExpressionWeight(OVRFaceExpressions.FaceExpression.EyesClosedL, out lblinkweight);
                GameplayReferences.OVRFaceExpressions.TryGetFaceExpressionWeight(OVRFaceExpressions.FaceExpression.EyesClosedR, out rblinkweight);

                var eyeGazeRight = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Right];
                var eyeGazeLeft = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Left];

                if (eyeGazeRight.IsValid && rblinkweight < ConfidenceThreshold && eyeGazeLeft.IsValid && lblinkweight < ConfidenceThreshold)
                {
                    //average directions
                    var poseR = eyeGazeRight.Pose.ToOVRPose();
                    poseR = poseR.ToWorldSpacePose(GameplayReferences.HMDCameraComponent);
                    var poseL = eyeGazeRight.Pose.ToOVRPose();
                    poseL = poseL.ToWorldSpacePose(GameplayReferences.HMDCameraComponent);

                    Quaternion q = Quaternion.Slerp(poseR.orientation, poseL.orientation, 0.5f);
                    lastDirection = q * Vector3.forward;
                    return lastDirection;
                }
                else if (eyeGazeRight.IsValid && rblinkweight < ConfidenceThreshold)
                {
                    var pose = eyeGazeRight.Pose.ToOVRPose();
                    pose = pose.ToWorldSpacePose(GameplayReferences.HMDCameraComponent);
                    lastDirection = pose.orientation * Vector3.forward;
                    return lastDirection;
                }
                else if (eyeGazeLeft.IsValid && lblinkweight < ConfidenceThreshold)
                {
                    var pose = eyeGazeLeft.Pose.ToOVRPose();
                    pose = pose.ToWorldSpacePose(GameplayReferences.HMDCameraComponent);
                    lastDirection = pose.orientation * Vector3.forward;
                    return lastDirection;
                }
                return lastDirection;
            }
            else
            {
                return Cognitive3D.GameplayReferences.HMD.forward;
            }
        }
#else
        static Vector3 GetLookDirection()
        {
            UnityEngine.XR.Eyes eyes;
            var centereye = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);

            if (centereye.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
            {
                Vector3 convergencePoint;
                if (eyes.TryGetFixationPoint(out convergencePoint))
                {
                    //Some devices return true, but with 0,0,0 convergencePoint
                    //Compare magnitude of convergencePoint to origin with threshold of 1e-5
                    //https://docs.unity3d.com/ScriptReference/Vector3-operator_eq.html
                    if (convergencePoint != Vector3.zero)
                    {
                        Vector3 leftPos;
                        Vector3 rightPos;
                        eyes.TryGetLeftEyePosition(out leftPos);
                        eyes.TryGetRightEyePosition(out rightPos);
                        Vector3 centerPos = (rightPos + leftPos) / 2f;

                        var worldGazeDirection = (convergencePoint - centerPos).normalized;
                        //openxr implementation returns a direction adjusted by the HMD's transform, but not by the parent transformations
                        if (GameplayReferences.HMD.parent != null)
                            worldGazeDirection = GameplayReferences.HMD.parent.TransformDirection(worldGazeDirection);
                        return worldGazeDirection;
                    }
                }
            }
            return Cognitive3D.GameplayReferences.HMD.forward;
        }
#endif
    }
}