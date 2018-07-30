using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//physics raycast from camera
//adds gazepoint at hit.point
//TODO media

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
        Ray ray = new Ray(CameraTransform.position, GetWorldGazeDirection());

        Vector3 gpsloc = new Vector3();
        float compass = 0;
        Vector3 floorPos = new Vector3();

        GetOptionalSnapshotData(ref gpsloc, ref compass, ref floorPos);

        float hitDistance;
        DynamicObject hitDynamic;
        Vector3 hitWorld;
        if (DynamicRaycast(ray.origin,ray.direction,CameraComponent.farClipPlane,0.05f,out hitDistance,out hitDynamic, out hitWorld)) //hit dynamic
        {
            string ObjectId = hitDynamic.ObjectId.Id;
            Vector3 LocalGaze = hitDynamic.transform.InverseTransformPointUnscaled(hitWorld);
            hitDynamic.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);
            GazeCore.RecordGazePoint(Util.Timestamp(), ObjectId, LocalGaze, ray.origin, CameraTransform.rotation, gpsloc, compass, floorPos);
            return;
        }

        if (Physics.Raycast(ray,out hit, cam.farClipPlane))
        {
            Vector3 pos = CameraTransform.position;
            Vector3 gazepoint = hit.point;
            Quaternion rot = CameraTransform.rotation;

            //hit world
            GazeCore.RecordGazePoint(Util.Timestamp(), gazepoint, pos, rot, gpsloc, compass, floorPos);
        }
        else //hit sky / farclip
        {
            Vector3 pos = CameraTransform.position;
            Quaternion rot = CameraTransform.rotation;
            GazeCore.RecordGazePoint(Util.Timestamp(), pos, rot, gpsloc, compass, floorPos);
        }
    }

    private void OnDestroy()
    {
        CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
        CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
}
