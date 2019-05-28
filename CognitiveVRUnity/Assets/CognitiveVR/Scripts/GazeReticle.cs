using UnityEngine;
using System.Collections;
using CognitiveVR;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

//debug helper for gaze tracking with Fove and Pupil

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

    void Update()
    {
        if (GameplayReferences.HMD == null) { return; }

        Vector3 newPosition = t.position;
        if (PupilTools.IsGazing)
        {
            var v2 = PupilData._2D.GetEyeGaze("0");
            var ray = GameplayReferences.HMDCameraComponent.ViewportPointToRay(v2);
            newPosition = ray.GetPoint(Distance);
        }

        t.position = Vector3.Lerp(t.position, newPosition, Speed);
        t.LookAt(GameplayReferences.HMD.position);
    }

#elif CVR_FOVE

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
        if (FoveInstance == null)
        {
            return GameplayReferences.HMD.forward;
        }
        var eyeRays = FoveInstance.GetGazeRays();
        Vector3 v = new Vector3(eyeRays.left.direction.x, eyeRays.left.direction.y, eyeRays.left.direction.z);
        return v.normalized;
    }
#elif CVR_TOBIIVR
    private static Tobii.Research.Unity.VREyeTracker _eyeTracker;
    void Start()
    {
        t.position = GameplayReferences.HMD.position + GetLookDirection() * Distance;
        _eyeTracker = Tobii.Research.Unity.VREyeTracker.Instance;
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
        if (_eyeTracker == null)
        {
            return GameplayReferences.HMD.forward;
        }
        return _eyeTracker.LatestProcessedGazeData.CombinedGazeRayWorld.direction;
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
#endif
    }
}