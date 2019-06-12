using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//physics raycast from camera
//adds gazepoint at hit.point

namespace CognitiveVR
{
    [AddComponentMenu("Cognitive3D/Internal/Physics Gaze")]
    public class PhysicsGaze : GazeBase
{
    public override void Initialize()
    {
        base.Initialize();
        Core.InitEvent += CognitiveVR_Manager_InitEvent;
    }

    private void CognitiveVR_Manager_InitEvent(Error initError)
    {
        if (initError == Error.None)
        {
            if (GameplayReferences.HMD == null) { CognitiveVR.Util.logWarning("HMD is null! Physics Gaze will not function"); return; }
            Core.TickEvent += CognitiveVR_Manager_TickEvent;
        }
    }

    private void CognitiveVR_Manager_TickEvent()
    {
        if (GameplayReferences.HMD == null) { return; }

        RaycastHit hit = new RaycastHit();
        Ray ray = new Ray(GameplayReferences.HMD.position, GetWorldGazeDirection());

        Vector3 gpsloc = new Vector3();
        float compass = 0;
        Vector3 floorPos = new Vector3();

        GetOptionalSnapshotData(ref gpsloc, ref compass, ref floorPos);

        float hitDistance;
        DynamicObject hitDynamic;
        Vector3 hitWorld;
        Vector3 hitLocal;
        Vector2 hitcoord;
        if (DynamicRaycast(ray.origin,ray.direction, GameplayReferences.HMDCameraComponent.farClipPlane,0.05f,out hitDistance,out hitDynamic, out hitWorld, out hitLocal, out hitcoord)) //hit dynamic
        {
            string ObjectId = hitDynamic.DataId;
            var mediacomponent = hitDynamic.GetComponent<MediaComponent>();
            if (mediacomponent != null)
            {
                var mediatime = mediacomponent.IsVideo ? (int)((mediacomponent.VideoPlayer.frame / mediacomponent.VideoPlayer.frameRate) * 1000) : 0;
                var mediauvs = hitcoord;
                GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, GameplayReferences.HMD.position, GameplayReferences.HMD.rotation, gpsloc, compass, mediacomponent.MediaSource, mediatime, mediauvs, floorPos);
            }
            else
            {
                GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, ray.origin, GameplayReferences.HMD.rotation, gpsloc, compass, floorPos);
            }

            Debug.DrawLine(GameplayReferences.HMD.position, hitWorld, new Color(1,0,1,0.5f), CognitiveVR_Preferences.Instance.SnapshotInterval);
            Debug.DrawRay(hitWorld, Vector3.right, Color.red, 1);
            Debug.DrawRay(hitWorld, Vector3.forward, Color.blue, 1);
            Debug.DrawRay(hitWorld, Vector3.up, Color.green, 1);
            return;
        }

        if (Physics.Raycast(ray, out hit, GameplayReferences.HMDCameraComponent.farClipPlane, CognitiveVR_Preferences.Instance.GazeLayerMask))
        {
            Vector3 pos = GameplayReferences.HMD.position;
            Vector3 gazepoint = hit.point;
            Quaternion rot = GameplayReferences.HMD.rotation;

            //hit world
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), gazepoint, pos, rot, gpsloc, compass, floorPos);
            Debug.DrawLine(pos, gazepoint, Color.red, CognitiveVR_Preferences.Instance.SnapshotInterval);

                Debug.DrawRay(gazepoint, Vector3.right, Color.red, 10);
                Debug.DrawRay(gazepoint, Vector3.forward, Color.blue, 10);
                Debug.DrawRay(gazepoint, Vector3.up, Color.green, 10);
            }
        else //hit sky / farclip
        {
            Vector3 pos = GameplayReferences.HMD.position;
            Quaternion rot = GameplayReferences.HMD.rotation;
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos, rot, gpsloc, compass, floorPos);
            Debug.DrawRay(pos, GameplayReferences.HMD.forward * GameplayReferences.HMDCameraComponent.farClipPlane, Color.cyan, CognitiveVR_Preferences.Instance.SnapshotInterval);
        }
    }

    private void OnDestroy()
    {
        Core.InitEvent -= CognitiveVR_Manager_InitEvent;
        Core.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
}
}