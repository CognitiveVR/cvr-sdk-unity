﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//raycasts from hmd gaze direction to hit gaze button

namespace CognitiveVR
{
    [AddComponentMenu("Cognitive3D/Internal/Exit Poll HMD Pointer")]
    public class ExitPollHMDPointer : MonoBehaviour
    {
        public GameObject visualTarget;

        //sets the curve to the target
        void Update()
        {
            if (GameplayReferences.HMD == null) { return; }
            if (visualTarget != null)
            {
                visualTarget.transform.position = transform.position + GetGazeDirection() * 4;
                visualTarget.transform.LookAt(GameplayReferences.HMD.position);
            }

            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(transform.position,GetGazeDirection(), out hit, 10, LayerMask.GetMask("UI")))
            {
                var button = hit.collider.GetComponent<GazeButton>();
                if (button != null)
                {
                    button.SetFocus();
                }
            }
        }


#if CVR_PUPIL
    void Start()
    {
        if (GameplayReferences.HMD == null) { return; }
        PupilTools.OnCalibrationEnded += PupilTools_OnCalibrationEnded;
    }

    private void PupilTools_OnCalibrationEnded()
    {
        PupilTools.IsGazing = true;
        PupilTools.SubscribeTo("gaze");
    }
#endif
#if CVR_VIVEPROEYE
        Vector3 lastDir = Vector3.forward; //vive pro
#endif
#if CVR_TOBIIVR
        private static Tobii.Research.Unity.VREyeTracker _eyeTracker; //tobii
#endif

#if CVR_FOVE
        FoveInterfaceBase _foveInstance;
        FoveInterfaceBase FoveInstance
        {
            get
            {
                if (_foveInstance == null)
                {
                    _foveInstance = FindObjectOfType<FoveInterfaceBase>();
                }
                return _foveInstance;
            }
        }
#endif

        Vector3 GetGazeDirection()
        {
#if CVR_PUPIL
            if (PupilTools.IsGazing)
            {
                var v2 = PupilData._2D.GetEyeGaze("0");
                var ray = GameplayReferences.HMDCameraComponent.ViewportPointToRay(v2);
                return ray.direction;
            }
#elif CVR_FOVE
            if (FoveInstance == null)
            {
                return GameplayReferences.HMD.forward;
            }
            var eyeRays = FoveInstance.GetGazeRays();
            Vector3 v = new Vector3(eyeRays.left.direction.x, eyeRays.left.direction.y, eyeRays.left.direction.z);
#elif CVR_TOBIIVR
            if (_eyeTracker == null)
            {
                return GameplayReferences.HMD.forward;
            }
            return _eyeTracker.LatestProcessedGazeData.CombinedGazeRayWorld.direction;
#elif CVR_NEURABLE
            return Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay().direction;
#elif CVR_AH
            return Calibrator.Instance.GetGazeVector(filterType: FilterType.ExponentialMovingAverage);
#elif CVR_SNAPDRAGON
            return SvrManager.Instance.EyeDirection;
#elif CVR_VIVEPROEYE
            var ray = new Ray();
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE, out ray))
            {
                lastDir = GameplayReferences.HMD.TransformDirection(ray.direction);
            }
            return lastDir;
#else

            return Vector3.forward;
#endif
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 5);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 0.3f);
        }
    }
}