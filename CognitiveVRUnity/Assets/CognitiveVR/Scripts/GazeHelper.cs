using UnityEngine;
using System.Collections;
using CognitiveVR;
using System;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

//utility code to get player's eye ray

namespace CognitiveVR
{
    public static class GazeHelper
    {
        public static Ray GetCurrentWorldGazeRay()
        {
            return new Ray(GameplayReferences.HMD.position, GetLookDirection());
        }

#if CVR_PUPIL
        
        static Vector3 GetLookDirection()
        {
            InitCheck();
            return gazeDirection;
        }

        static PupilLabs.GazeController gazeController;
        static Vector3 gazeDirection = Vector3.forward;

        static void InitCheck()
        {
            if (gazeController != null){return;}

            gazeController = gazeController = GameplayReferences.GazeController;
            if (gazeController != null)
                gazeController.OnReceive3dGaze += ReceiveEyeData;
            else
                Debug.LogError("Pupil Labs GazeController is null!");
            gazeController.OnReceive3dGaze += ReceiveEyeData;
        }

        static void ReceiveEyeData(PupilLabs.GazeData data)
        {
            if (data.Confidence < 0.6f) { return; }
            gazeDirection = data.GazeDirection;
        }

#elif CVR_FOVE
    static Vector3 GetLookDirection()
    {
        Fove.Unity.FoveInterface fi = GameplayReferences.FoveInstance;
        if (fi == null)
        {
            return GameplayReferences.HMD.forward;
        }
        var eyeRays = fi.GetGazeRays();
        Vector3 v = new Vector3(eyeRays.left.direction.x, eyeRays.left.direction.y, eyeRays.left.direction.z);
        return v.normalized;
    }
#elif CVR_TOBIIVR
    static Vector3 lastDirection = Vector3.forward;

    static Vector3 GetLookDirection()
    {
        var provider = Tobii.XR.TobiiXR.Internal.Provider;

        if (provider == null)
        {
            return GameplayReferences.HMD.forward;
        }
        if (provider.EyeTrackingDataLocal.GazeRay.IsValid)
        {
            lastDirection = GameplayReferences.HMD.TransformDirection(provider.EyeTrackingDataLocal.GazeRay.Direction);
        }

        return lastDirection;
    }
#elif CVR_NEURABLE

    static Vector3 GetLookDirection()
    {
        return Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay().direction;
    }
#elif CVR_AH

    static Vector3 GetLookDirection()
    {
        return Calibrator.Instance.GetGazeVector(filterType: FilterType.ExponentialMovingAverage);
    }
#elif CVR_SNAPDRAGON
        static Vector3 GetLookDirection()
        {
            return SvrManager.Instance.EyeDirection;
        }
#elif CVR_VIVEPROEYE

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
#elif CVR_VARJO
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
#elif CVR_PICOVR
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
#elif CVR_PICOXR
        static Vector3 lastDirection = Vector3.forward;

        static Vector3 GetLookDirection()
        {
            var ray = new Ray();

            if (!Unity.XR.PXR.PXR_Manager.Instance.eyeTracking)
            {
                //Debug.Log("CognitiveVR::GazeHelper GetLookDirection FAILED MANAGER NO EYE TRACKING");
                return lastDirection;
            }

            UnityEngine.XR.InputDevice device;
            if (!GameplayReferences.GetEyeTrackingDevice(out device))
            {
                Debug.Log("CognitiveVR::GazeHelper GetLookDirection FAILED TRACKING DEVICE");
                return lastDirection;
            }

            Vector3 headPos;
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out headPos))
            {
                Debug.Log("CognitiveVR::GazeHelper GetLookDirection FAILED HEAD POSITION");
                return lastDirection;
            }
            Quaternion headRot = Quaternion.identity;
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out headRot))
            {
                Debug.Log("CognitiveVR::GazeHelper GetLookDirection FAILED HEAD ROTATION");
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
#elif CVR_OMNICEPT
        static Vector3 lastDirection = Vector3.forward;
        static HP.Omnicept.Unity.GliaBehaviour gb;

        static void DoEyeTracking(HP.Omnicept.Messaging.Messages.EyeTracking data)
        {
            lastDirection = GameplayReferences.HMD.TransformDirection(new Vector3(-data.CombinedGaze.X, data.CombinedGaze.Y, data.CombinedGaze.Z));
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
#elif CVR_XR

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
                return CognitiveVR.GameplayReferences.HMD.forward;
            }
            return lastDirection;
        }
#elif CVR_MRTK
        static Vector3 lastDirection = Vector3.forward;
        static Vector3 GetLookDirection()
        {
            if (Microsoft.MixedReality.Toolkit.CoreServices.InputSystem.EyeGazeProvider.IsEyeTrackingEnabledAndValid)
            {
                lastDirection = Microsoft.MixedReality.Toolkit.CoreServices.InputSystem.EyeGazeProvider.GazeDirection;
            }
            return lastDirection;
        }
#else
        static Vector3 GetLookDirection()
        {
            return GameplayReferences.HMD.forward;
        }
#endif
    }
}