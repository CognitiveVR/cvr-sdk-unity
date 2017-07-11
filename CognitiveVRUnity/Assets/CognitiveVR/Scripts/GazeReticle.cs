using UnityEngine;
using System.Collections;
using CognitiveVR;

//debug helper for gaze tracking with Fove and Pupil

public class GazeReticle : MonoBehaviour
{
    public float Speed = 0.3f;
    public float Distance = 3;

#if CVR_GAZETRACK
    Vector3 LastLookDirection = Vector3.zero;
#endif //cvr_gazetrack

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
    void Start()
    {
        if (CognitiveVR_Manager.HMD == null) { return; }
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
#if CVR_FOVE
        if (FoveInstance == null)
        {
            return CognitiveVR_Manager.HMD.forward;
        }
        var eyeRays = FoveInstance.GetGazeRays();
        Vector3 v = new Vector3(eyeRays.left.direction.x, eyeRays.left.direction.y, eyeRays.left.direction.z);
        LastLookDirection = v.normalized;
        return LastLookDirection;
#elif CVR_PUPIL
        //TODO position for pupil labs gaze
        return CognitiveVR_Manager.HMD.forward;
#else
        return CognitiveVR_Manager.HMD.forward;
#endif
    }
}