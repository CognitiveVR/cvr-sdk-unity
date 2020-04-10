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

            gazeController = UnityEngine.Object.FindObjectOfType<PupilLabs.GazeController>();
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
            if (Varjo.VarjoPlugin.InitGaze())
            {
                var data = Varjo.VarjoPlugin.GetGaze();
                if (data.status != Varjo.VarjoPlugin.GazeStatus.INVALID)
                {
                    var ray = data.gaze;
                    lastDir = GameplayReferences.HMD.TransformDirection(new Vector3((float)ray.forward[0], (float)ray.forward[1], (float)ray.forward[2]));
                }
            }
            return lastDir;
        }
#elif CVR_PICONEO2EYE
        static Vector3 lastDirection = Vector3.forward;

        static Vector3 GetLookDirection()
        {
            Pvr_UnitySDKAPI.EyeTrackingGazeRay gazeRay = new Pvr_UnitySDKAPI.EyeTrackingGazeRay();
            var t = Pvr_UnitySDKManager.SDK.HeadPose.Matrix;
            if (Pvr_UnitySDKAPI.System.UPvr_getEyeTrackingGazeRayWorld(ref gazeRay))
            {
                if (gazeRay.IsValid && gazeRay.Direction.sqrMagnitude > 0.1f)
                {
                    lastDirection = gazeRay.Direction;
                }
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