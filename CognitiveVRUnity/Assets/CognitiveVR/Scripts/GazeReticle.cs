using UnityEngine;
using System.Collections;
using CognitiveVR;

//debug helper for gaze tracking with Fove and Pupil

public class GazeReticle : MonoBehaviour
{
    public float Speed = 0.3f;
    public float Distance = 3;

#if CVR_FOVE
    FoveInterface _foveInstance;
    FoveInterface FoveInstance
    {
        get
        {
            if (_foveInstance == null)
            {
                _foveInstance = FindObjectOfType<FoveInterface>();
            }
            return _foveInstance;
        }
    }
#endif //cvr_fove

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
            if (PupilTools.CalibrationMode == Calibration.Mode._2D)
            {
                Vector3 position = PupilData._2D.GazePosition;
                position.z = Distance;
                newPosition = Cam.ViewportToWorldPoint(position);
            }
            else if (PupilTools.CalibrationMode == Calibration.Mode._3D)
            {
                newPosition = PupilData._3D.GazePosition;
            }
        }

        t.position = Vector3.Lerp(t.position, newPosition, Speed);
        t.LookAt(CognitiveVR_Manager.HMD.position);
    }

#elif CVR_FOVE
    void Start()
    {
        t.position = CognitiveVR_Manager.HMD.position + GetLookDirection() * Distance;
        if (CognitiveVR_Manager.HMD == null) { return; }
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
#endif
}