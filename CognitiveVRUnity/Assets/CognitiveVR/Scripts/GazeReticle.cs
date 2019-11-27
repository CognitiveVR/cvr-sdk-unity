using UnityEngine;
using System.Collections;
using CognitiveVR;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

//debug helper for gaze tracking with Fove, Pupil, Tobii, Vive Pro Eye, Adhawk, Varjo

namespace CognitiveVR
{
    [AddComponentMenu("Cognitive3D/Testing/Gaze Reticle")]
    public class GazeReticle : MonoBehaviour
{
    public float Speed = 0.3f;
    public float Distance = 3;

    Transform _transform;
    Transform t
    {
        get
        {
            if (_transform == null)
            {
                _transform = transform;
            }
            return _transform;
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
            if (data.Confidence < 0.6f) { return; }
            gazeDirection = data.GazeDirection;
        }

        void Update()
        {
            if (GameplayReferences.HMD == null) { return; }

            Vector3 newPosition = t.position;
            var worldDir = GameplayReferences.HMD.TransformDirection(gazeDirection);
            //var v2 = PupilData._2D.GetEyeGaze("0");
            //var ray = GameplayReferences.HMDCameraComponent.ViewportPointToRay(v2);
            var ray = new Ray(GameplayReferences.HMDCameraComponent.transform.position, worldDir);
            newPosition = ray.GetPoint(Distance);

            t.position = Vector3.Lerp(t.position, newPosition, Speed);
            t.LookAt(GameplayReferences.HMD.position);
        }

        private void OnDisable()
        {
            gazeController.OnReceive3dGaze -= ReceiveEyeData;
        }

#elif CVR_FOVE

    void Start()
    {
        t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
    }

    void Update()
    {
        if (GameplayReferences.HMD == null){return;}

        t.position = Vector3.Lerp(t.position, GameplayReferences.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(GameplayReferences.HMD.position);
    }

    Vector3 GetLookDirection()
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
    public Vector3 lastDirection = Vector3.forward;
    void Start()
    {
        t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
        if (GameplayReferences.HMD == null) { return; }
    }

    void Update()
    {
        if (GameplayReferences.HMD == null) { return; }

        t.position = Vector3.Lerp(t.position, GameplayReferences.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(GameplayReferences.HMD.position);
    }

    Vector3 GetLookDirection()
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
    void Start()
    {
        t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
        if (GameplayReferences.HMD == null) { return; }
    }

    void Update()
    {
        if (GameplayReferences.HMD == null) { return; }

        t.position = Vector3.Lerp(t.position, GameplayReferences.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(GameplayReferences.HMD.position);
    }

    Vector3 GetLookDirection()
    {
        return Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay().direction;
    }
#elif CVR_AH
    void Start()
    {
        t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
        if (GameplayReferences.HMD == null) { return; }
    }
    void Update()
    {
        if (GameplayReferences.HMD == null) { return; }
         t.position = Vector3.Lerp(t.position, GameplayReferences.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(GameplayReferences.HMD.position);
    }
    Vector3 GetLookDirection()
    {
        return Calibrator.Instance.GetGazeVector(filterType: FilterType.ExponentialMovingAverage);
    }
#elif CVR_SNAPDRAGON
        void Start()
        {
            t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
            if (GameplayReferences.HMD == null) { return; }
        }
        void Update()
        {
            if (GameplayReferences.HMD == null) { return; }
            t.position = Vector3.Lerp(t.position, GameplayReferences.HMD.position + GetLookDirection() * Distance, Speed);
            t.LookAt(GameplayReferences.HMD.position);
        }
        Vector3 GetLookDirection()
        {
            return SvrManager.Instance.EyeDirection;
        }
#elif CVR_VIVEPROEYE
        void Start()
        {
            t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
            if (GameplayReferences.HMD == null) { return; }
        }

        void Update()
        {
            if (GameplayReferences.HMD == null) { return; }

            t.position = Vector3.Lerp(t.position, GameplayReferences.HMD.position + GetLookDirection() * Distance, Speed);
            t.LookAt(GameplayReferences.HMD.position);
        }

        Vector3 lastDir = Vector3.forward;
        Vector3 GetLookDirection()
        {
            var ray = new Ray();
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE, out ray))
            {
                lastDir = GameplayReferences.HMD.TransformDirection(ray.direction);
            }
            return lastDir;
        }
#elif CVR_VARJO
        void Start()
        {
            t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
            if (GameplayReferences.HMD == null) { return; }
        }

        void Update()
        {
            if (GameplayReferences.HMD == null) { return; }

            t.position = Vector3.Lerp(t.position, GameplayReferences.HMD.position + GetLookDirection() * Distance, Speed);
            t.LookAt(GameplayReferences.HMD.position);
        }

        Vector3 lastDir = Vector3.forward;
        Vector3 GetLookDirection()
        {
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
        }
#endif
    }
}