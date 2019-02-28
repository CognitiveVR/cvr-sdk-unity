using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//physics raycast from camera
//adds gazepoint at hit.point

namespace CognitiveVR
{
public class PhysicsGaze : GazeBase
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
        RaycastHit hit = new RaycastHit();
        Ray ray = new Ray(CameraTransform.position, GetWorldGazeDirection());

        Vector3 gpsloc = new Vector3();
        float compass = 0;
        Vector3 floorPos = new Vector3();

        GetOptionalSnapshotData(ref gpsloc, ref compass, ref floorPos);

        float hitDistance;
        DynamicObject hitDynamic;
        Vector3 hitWorld;
        Vector3 hitLocal;
        Vector2 hitcoord;
        if (DynamicRaycast(ray.origin,ray.direction,CameraComponent.farClipPlane,0.05f,out hitDistance,out hitDynamic, out hitWorld, out hitLocal, out hitcoord)) //hit dynamic
        {
            string ObjectId = hitDynamic.Id;
            var mediacomponent = hitDynamic.GetComponent<MediaComponent>();
            if (mediacomponent != null)
            {
                var mediatime = mediacomponent.IsVideo ? (int)((mediacomponent.VideoPlayer.frame / mediacomponent.VideoPlayer.frameRate) * 1000) : 0;
                var mediauvs = hitcoord;
                GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, CameraTransform.position, CameraTransform.rotation, gpsloc, compass, mediacomponent.MediaSource, mediatime, mediauvs, floorPos);
            }
            else
            {
                GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, ray.origin, CameraTransform.rotation, gpsloc, compass, floorPos);
            }

            Debug.DrawLine(CameraTransform.position, hitWorld, new Color(1,0,1,0.5f), CognitiveVR_Preferences.Instance.SnapshotInterval);

            return;
        }

        if (Physics.Raycast(ray,out hit, cam.farClipPlane))
        {
            Vector3 pos = CameraTransform.position;
            Vector3 gazepoint = hit.point;
            Quaternion rot = CameraTransform.rotation;

            //hit world
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), gazepoint, pos, rot, gpsloc, compass, floorPos);
            Debug.DrawLine(pos, pos + gazepoint, Color.red, CognitiveVR_Preferences.Instance.SnapshotInterval);
        }
        else //hit sky / farclip
        {
            Vector3 pos = CameraTransform.position;
            Quaternion rot = CameraTransform.rotation;
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos, rot, gpsloc, compass, floorPos);
            Debug.DrawRay(pos, CameraTransform.forward * CameraComponent.farClipPlane, Color.cyan, CognitiveVR_Preferences.Instance.SnapshotInterval);
        }
    }

    private void OnDestroy()
    {
        CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
        CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
}
}