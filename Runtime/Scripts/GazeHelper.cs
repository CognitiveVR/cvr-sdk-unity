using UnityEngine;
using System.Collections;
using Cognitive3D;
using System;

//utility code to get player's eye ray

namespace Cognitive3D
{
    public static class GazeHelper
    {
        /*
         * EYE-TRACKED gaze direction
         */
        public static Ray GetCurrentWorldGazeRay()
        {
            return new Ray(GameplayReferences.HMD.position, GetLookDirection());
        }

        /*
         * NAIVE HEAD-FORWARD gaze direction
         */
        public static Ray GetCurrentWorldGazeRayWithoutEyeTracking()
        {
            return new Ray(GameplayReferences.HMD.position, GameplayReferences.HMD.forward);
        }

        // TODO - this is my frantic hackathon garbage, do not allow to be merged anywhere without thorough review (derya)
        /*
         * HEAD MOVEMENT PREDICTIVE gaze direction
         */
        public static Ray GetCurrentWorldGazeRayPredictive()
        {
            Vector3 angleNow = PhysicsGaze.sampleNow.headAngle;
            Vector3 angle200msAgo = PhysicsGaze.sample200MsAgo.headAngle;

            double scaled_headrotx = scaleRotation(angleNow.x);
            double scaled_headroty = scaleRotation(angleNow.y);
            double scaled_headrotz = scaleRotation(angleNow.z);
            double scaled_headrotx_200ms_delta = scaled_headrotx - scaleRotation(angle200msAgo.x);
            double scaled_headroty_200ms_delta = scaled_headroty - scaleRotation(angle200msAgo.y);
            double scaled_headrotz_200ms_delta = scaled_headrotz - scaleRotation(angle200msAgo.z);

            /*
                x <- lm(scaled_gazerotx ~ scaled_headrotx + scaled_headroty + scaled_headrotz + scaled_headrotx_200ms_delta + scaled_headroty_200ms_delta + scaled_headrotz_200ms_delta, data = dat)

                                              Estimate
                (Intercept)                 138.5635724
                scaled_headrotx               0.1614473
                scaled_headroty               0.0007960
                scaled_headrotz              -0.0007333
                scaled_headrotx_200ms_delta   1.2913775
                scaled_headroty_200ms_delta  -0.0166752
                scaled_headrotz_200ms_delta   0.0175271
             */

            double scaled_predicted_x = 138.5635724 + (0.1614473) * scaled_headrotx + (0.0007960) * scaled_headroty + (-0.0007333) * scaled_headrotz +
                (1.2913775) * scaled_headrotx_200ms_delta + (-0.0166752) * scaled_headroty_200ms_delta + (0.0175271) * scaled_headrotz_200ms_delta;
            double predicted_x = unscaleRotation(scaled_predicted_x);

            /*
                y <- lm(scaled_gazeroty ~ scaled_headrotx + scaled_headroty + scaled_headrotz + scaled_headrotx_200ms_delta + scaled_headroty_200ms_delta + scaled_headrotz_200ms_delta, data = dat)

                                              Estimate
                (Intercept)                 134.076373
                scaled_headrotx               0.009656
                scaled_headroty               0.123380
                scaled_headrotz               0.038370
                scaled_headrotx_200ms_delta  -0.012187
                scaled_headroty_200ms_delta   0.518784
                scaled_headrotz_200ms_delta  -0.294554
             */

            double scaled_predicted_y = 134.076373 + (0.009656) * scaled_headrotx + (0.123380) * scaled_headroty + (0.038370) * scaled_headrotz +
                (-0.012187) * scaled_headrotx_200ms_delta + (0.518784) * scaled_headroty_200ms_delta + (-0.294554) * scaled_headrotz_200ms_delta;
            double predicted_y = unscaleRotation(scaled_predicted_y);

            // predicted angle as per rotation model
            Vector3 predicted_angle = new Vector3((float)predicted_x, (float)predicted_y, 0);

            // convert to direction vector
            Vector3 predicted_direction = (Quaternion.Euler(predicted_angle) * Vector3.forward).normalized;

            // relative to head
            Vector3 direction_from_HMD = GameplayReferences.HMD.transform.TransformDirection(predicted_direction).normalized;

            return new Ray(GameplayReferences.HMD.position, direction_from_HMD);
        }

        // model used angles with 160 added to them
        private static double ANGLE_SCALE = 160;
        private static double scaleRotation(double rotDegrees)
        {
            return (rotDegrees + ANGLE_SCALE) % 360;
        }
        private static double unscaleRotation(double rotDegrees)
        {
            var ret = rotDegrees - ANGLE_SCALE;
            while (ret < 0) ret += 360d;
            return ret;
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
            var ray = new Ray();

            if (!Unity.XR.PXR.PXR_Manager.Instance.eyeTracking)
            {
                //Debug.Log("Cognitive3D::GazeHelper GetLookDirection FAILED MANAGER NO EYE TRACKING");
                return lastDirection;
            }

            UnityEngine.XR.InputDevice device;
            device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
            Vector3 headPos;
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out headPos))
            {
                Debug.Log("Cognitive3D::GazeHelper GetLookDirection FAILED HEAD POSITION");
                return lastDirection;
            }
            Quaternion headRot = Quaternion.identity;
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out headRot))
            {
                Debug.Log("Cognitive3D::GazeHelper GetLookDirection FAILED HEAD ROTATION");
                return lastDirection;
            }

            Vector3 direction;
            if (Unity.XR.PXR.PXR_EyeTracking.GetCombineEyeGazeVector(out direction) && direction.sqrMagnitude > 0.1f)
            {
                Matrix4x4 matrix = Matrix4x4.identity;
                matrix = Matrix4x4.TRS(Vector3.zero, headRot, Vector3.one);
                direction = matrix.MultiplyPoint3x4(direction);
                ray.origin = headPos;
                ray.direction = direction;
                lastDirection = direction;
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
#else
        static Vector3 lastDirection = Vector3.forward;
        static Vector3 GetLookDirection()
        {
            UnityEngine.XR.Eyes eyes;
            var centereye = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);

            if (centereye.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
            {
                Vector3 convergancePoint;
                if (eyes.TryGetFixationPoint(out convergancePoint))
                {
                    Vector3 leftPos = Vector3.zero;
                    eyes.TryGetLeftEyePosition(out leftPos);
                    Vector3 rightPos = Vector3.zero;
                    eyes.TryGetRightEyePosition(out rightPos);

                    //TEST possible optimization. reduces math
                    //Vector3 centerPointPosition = Vector3.zero;
                    //centereye.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out centerPointPosition);

                    Vector3 centerPos = (rightPos + leftPos) / 2f;

                    var worldGazeDirection = (convergancePoint - centerPos).normalized;
                    //openxr implementation returns a direction adjusted by the HMD's transform, but not by the parent transformations
                    if (GameplayReferences.HMD.parent != null)
                        worldGazeDirection = GameplayReferences.HMD.parent.TransformDirection(worldGazeDirection);
                    lastDirection = worldGazeDirection;
                    return worldGazeDirection;
                }
            }
            else //hmd doesn't have eye data (ie, eye tracking)
            {
                //use center point of hmd
                return Cognitive3D.GameplayReferences.HMD.forward;
            }
            return lastDirection;
        }
#endif
    }
}