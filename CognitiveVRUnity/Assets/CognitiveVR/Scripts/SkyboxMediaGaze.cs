using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//dynamic raycast from camera
//if no hit, then set gaze on media displayed on skybox
//requires mesh collider and inverted spheres to get uvs

public class SkyboxMediaGaze : GazeBase
{
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
        Ray ray = new Ray(CameraTransform.position, GetWorldGazeDirection());

        float hitDistance;
        DynamicObject hitDynamic;
        Vector3 hitWorld;
        if (DynamicRaycast(ray.origin,ray.direction,CameraComponent.farClipPlane,0.05f,out hitDistance,out hitDynamic, out hitWorld)) //hit dynamic
        {
            //hit a dynamic object
            string ObjectId = hitDynamic.ObjectId.Id;
            Vector3 LocalGaze = hitDynamic.transform.InverseTransformPointUnscaled(hitWorld);
            hitDynamic.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);
            GazeCore.RecordGazePoint(Util.Timestamp(), ObjectId, LocalGaze, ray.origin, CameraTransform.rotation);
            return;
        }

        //TODO media uv stuff. timestamps
    }

    private void OnDestroy()
    {
        CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
        CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
}
