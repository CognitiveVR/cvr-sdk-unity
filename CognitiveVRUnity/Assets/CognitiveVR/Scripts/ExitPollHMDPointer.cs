using System.Collections;
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
        
        PupilLabs.GazeController gazeController;
        Vector3 gazeDirection = Vector3.forward;

        void Start()
        {
            gazeController = FindObjectOfType<PupilLabs.GazeController>();
            if (gazeController != null)
                gazeController.OnReceive3dGaze += ReceiveEyeData;
            else
                Debug.LogError("Pupil Labs GazeController is null!");
            gazeController.OnReceive3dGaze += ReceiveEyeData;
        }

        void ReceiveEyeData(PupilLabs.GazeData data)
        {
            if (!CognitiveVR.Core.IsInitialized) { return; }
            if (data.Confidence < 0.6f) { return; }
            gazeDirection = data.GazeDirection;
        }
        private void OnDisable()
        {
            if (gazeController != null)
                gazeController.OnReceive3dGaze -= ReceiveEyeData;
            else
                Debug.LogError("Pupil Labs GazeController is null!");
        }
#endif
#if CVR_VARJO
        Vector3 lastDir = Vector3.forward;
#endif
#if CVR_VIVEPROEYE
        Vector3 lastDir = Vector3.forward; //vive pro
#endif
#if CVR_TOBIIVR
        private static Tobii.Research.Unity.VREyeTracker _eyeTracker; //tobii
#endif
        Vector3 GetGazeDirection()
        {
#if CVR_PUPIL
            return GameplayReferences.HMD.TransformDirection(gazeDirection);
#elif CVR_FOVE
            if (GameplayReferences.FoveInstance == null)
            {
                return GameplayReferences.HMD.forward;
            }
            var eyeRays = GameplayReferences.FoveInstance.GetGazeRays();
            Vector3 v = new Vector3(eyeRays.left.direction.x, eyeRays.left.direction.y, eyeRays.left.direction.z);
            return v;
#elif CVR_TOBIIVR
            if (_eyeTracker == null)
            {
                return GameplayReferences.HMD.forward;
            }
            return _eyeTracker.LatestProcessedGazeData.CombinedGazeRayWorld.direction;
#elif CVR_NEURABLE
            return Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay().direction;
#elif CVR_AH
            return AdhawkApi.Calibrator.Instance.GetGazeVector(filterType: AdhawkApi.Numerics.Filters.FilterType.ExponentialMovingAverage);
#elif CVR_SNAPDRAGON
            return SvrManager.Instance.EyeDirection;
#elif CVR_VIVEPROEYE
            var ray = new Ray();
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE, out ray))
            {
                lastDir = GameplayReferences.HMD.TransformDirection(ray.direction);
            }
            return lastDir;
#elif CVR_VARJO
            if (Varjo.VarjoPlugin.InitGaze())
            {
                var data = Varjo.VarjoPlugin.GetGaze();
                if (data.status != Varjo.VarjoPlugin.GazeStatus.INVALID)
                {
                    var ray = data.gaze;
                    lastDir = GameplayReferences.HMD.TransformDirection(new Vector3((float)ray.forward[0], (float)ray.forward[1], (float)ray.forward[2]));
                    return lastDir;
                }
            }
            return lastDir;
#else

            return GameplayReferences.HMD.forward;
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