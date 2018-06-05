using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//physics raycast from camera
//adds gazepoint at hit.point

public class PhysicsGaze : GazeBase {
    

    public override void Initialize()
    {
        base.Initialize();
        CognitiveVR_Manager.InitEvent += CognitiveVR_Manager_InitEvent;
    }

    private void CognitiveVR_Manager_InitEvent(Error initError)
    {
        if (initError == Error.Success)
        {
            CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;
        }
    }

    private void CognitiveVR_Manager_TickEvent()
    {
        RaycastHit hit = new RaycastHit();
        Ray ray = new Ray(CameraTransform.position, CameraTransform.forward);

        //TODO ray origin should include gaze direction

        if (Physics.Raycast(ray,out hit, cam.farClipPlane))
        {
            Vector3 pos = CameraTransform.position;
            Vector3 gazepoint = hit.point;
            Quaternion rot = CameraTransform.rotation;

            DynamicObject dyn = null;
            if (CognitiveVR_Preferences.S_DynamicObjectSearchInParent)
                dyn = hit.collider.GetComponentInParent<DynamicObject>();
            else
                dyn = hit.collider.GetComponent<DynamicObject>();

            if (dyn != null) //hit dynamic object
            {
                string ObjectId = dyn.ObjectId.Id;
                Vector3 LocalGaze = dyn.transform.InverseTransformPointUnscaled(hit.point);
                GazeCore.RecordGazePoint(Util.Timestamp(), ObjectId, LocalGaze, pos, rot);
            }
            else //hit world
            {
                GazeCore.RecordGazePoint(Util.Timestamp(), gazepoint, pos, rot);
            }
        }
        else //hit sky / farclip
        {
            Vector3 pos = CameraTransform.position;
            Quaternion rot = CameraTransform.rotation;
            GazeCore.RecordGazePoint(Util.Timestamp(), pos, rot);
        }
    }

    private void OnDestroy()
    {
        CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
        CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
}
