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

    Camera _cam;
    Camera Cam
    {
        get
        {
            if (_cam == null)
            {
                _cam = Camera.main;
            }
            return _cam;
        }
    }


    void Start()
    {
        if (CognitiveVR_Manager.HMD == null) { return; }
        PupilTools.OnCalibrationEnded += PupilTools_OnCalibrationEnded;
    }

    private void PupilTools_OnCalibrationEnded()
    {
        PupilTools.IsGazing = true;
        PupilTools.SubscribeTo("gaze");
    }

    void Update()
    {
        if (CognitiveVR_Manager.HMD == null) { return; }

        Vector3 newPosition = t.position;
        if (PupilTools.IsGazing)
        {
            var v2 = PupilData._2D.GetEyeGaze("0");
            var ray = cam.ViewportPointToRay(v2);
            newPosition = ray.GetPoint(Distance);
        }

        t.position = Vector3.Lerp(t.position, newPosition, Speed);
        t.LookAt(CognitiveVR_Manager.HMD.position);
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
        t.position = CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance;
    }

    void Update()
    {
        if (CognitiveVR_Manager.HMD == null){return;}

        t.position = Vector3.Lerp(t.position, CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(CognitiveVR_Manager.HMD.position);
    }

    Vector3 GetLookDirection()
    {
        if (FoveInstance == null)
        {
            return CognitiveVR_Manager.HMD.forward;
        }
        var eyeRays = FoveInstance.GetGazeRays();
        Vector3 v = new Vector3(eyeRays.left.direction.x, eyeRays.left.direction.y, eyeRays.left.direction.z);
        return v.normalized;
    }
#elif CVR_TOBIIVR
    private static Tobii.Research.Unity.VREyeTracker _eyeTracker;
    void Start()
    {
        t.position = CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance;
        _eyeTracker = Tobii.Research.Unity.VREyeTracker.Instance;
        if (CognitiveVR_Manager.HMD == null) { return; }
    }

    void Update()
    {
        if (CognitiveVR_Manager.HMD == null) { return; }

        t.position = Vector3.Lerp(t.position, CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(CognitiveVR_Manager.HMD.position);
    }

    Vector3 GetLookDirection()
    {
        if (_eyeTracker == null)
        {
            return CognitiveVR_Manager.HMD.forward;
        }
        return _eyeTracker.LatestProcessedGazeData.CombinedGazeRayWorld.direction;
    }
#elif CVR_NEURABLE
    void Start()
    {
        t.position = CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance;
        if (CognitiveVR_Manager.HMD == null) { return; }
    }

    void Update()
    {
        if (CognitiveVR_Manager.HMD == null) { return; }

        t.position = Vector3.Lerp(t.position, CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(CognitiveVR_Manager.HMD.position);
    }

    Vector3 GetLookDirection()
    {
        return Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay().direction;
    }
#elif CVR_AH
    void Start()
    {
        t.position = CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance;
        if (CognitiveVR_Manager.HMD == null) { return; }
    }
    void Update()
    {
        if (CognitiveVR_Manager.HMD == null) { return; }
         t.position = Vector3.Lerp(t.position, CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance, Speed);
        t.LookAt(CognitiveVR_Manager.HMD.position);
    }
    Vector3 GetLookDirection()
    {
        return Calibrator.Instance.GetGazeVector(filterType: FilterType.ExponentialMovingAverage);
    }
#elif CVR_SNAPDRAGON
        void Start()
        {
            t.position = CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance;
            if (CognitiveVR_Manager.HMD == null) { return; }
        }
        void Update()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            t.position = Vector3.Lerp(t.position, CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance, Speed);
            t.LookAt(CognitiveVR_Manager.HMD.position);
        }
        Vector3 GetLookDirection()
        {
            return SvrManager.Instance.eyeDirection;
        }
#endif
    }
}