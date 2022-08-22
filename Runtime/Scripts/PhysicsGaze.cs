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
        Core.InitEvent += CognitiveVR_Manager_InitEvent;
        base.Initialize();
    }

    private void CognitiveVR_Manager_InitEvent(Error initError)
    {
        if (initError == Error.None)
        {
            if (GameplayReferences.HMD == null) { CognitiveVR.Util.logWarning("HMD is null! Physics Gaze needs a camera to function"); }
            Core.TickEvent += CognitiveVR_Manager_TickEvent;
            Core.EndSessionEvent += OnEndSessionEvent;
        }
    }

    private void CognitiveVR_Manager_TickEvent()
    {
        if (GameplayReferences.HMD == null) { return; }

        RaycastHit hit;
        Ray ray = GazeHelper.GetCurrentWorldGazeRay();

        Vector3 gpsloc = new Vector3();
        float compass = 0;
        Vector3 floorPos = new Vector3();

        GetOptionalSnapshotData(ref gpsloc, ref compass, ref floorPos);

        float hitDistance;
        DynamicObject hitDynamic;
        Vector3 hitWorld;
        Vector3 hitLocal;
        Vector2 hitcoord;
        if (CognitiveVR_Preferences.Instance.EnableGaze == true && DynamicRaycast(ray.origin,ray.direction, GameplayReferences.HMDCameraComponent.farClipPlane,0.05f,out hitDistance,out hitDynamic, out hitWorld, out hitLocal, out hitcoord)) //hit dynamic
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
            //Debug.DrawRay(hitWorld, Vector3.right, Color.red, 1);
            //Debug.DrawRay(hitWorld, Vector3.forward, Color.blue, 1);
            //Debug.DrawRay(hitWorld, Vector3.up, Color.green, 1);
            if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();

            DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = hitWorld;
            DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = hitLocal;
            DisplayGazePoints[DisplayGazePoints.Count].Transform = hitDynamic.transform;
            DisplayGazePoints[DisplayGazePoints.Count].IsLocal = true;
            DisplayGazePoints.Update();
            return;
        }

        if (CognitiveVR_Preferences.Instance.EnableGaze == true && Physics.Raycast(ray, out hit, GameplayReferences.HMDCameraComponent.farClipPlane, CognitiveVR_Preferences.Instance.GazeLayerMask, CognitiveVR_Preferences.Instance.TriggerInteraction))
        {
            Vector3 pos = GameplayReferences.HMD.position;
            Vector3 gazepoint = hit.point;
            Quaternion rot = GameplayReferences.HMD.rotation;

            //hit world
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), gazepoint, pos, rot, gpsloc, compass, floorPos);
            Debug.DrawLine(pos, gazepoint, Color.red, CognitiveVR_Preferences.Instance.SnapshotInterval);

            //Debug.DrawRay(gazepoint, Vector3.right, Color.red, 10);
            //Debug.DrawRay(gazepoint, Vector3.forward, Color.blue, 10);
            //Debug.DrawRay(gazepoint, Vector3.up, Color.green, 10);
            if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();

            DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = hit.point;
            DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = Vector3.zero;
            DisplayGazePoints[DisplayGazePoints.Count].Transform = null;
            DisplayGazePoints[DisplayGazePoints.Count].IsLocal = false;
            DisplayGazePoints.Update();
        }
        else //hit sky / farclip / gaze disabled. record HMD position and rotation
        {
            Vector3 pos = GameplayReferences.HMD.position;
            Quaternion rot = GameplayReferences.HMD.rotation;
            Vector3 displayPosition = GameplayReferences.HMD.forward * GameplayReferences.HMDCameraComponent.farClipPlane;
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos, rot, gpsloc, compass, floorPos);
            //Debug.DrawRay(pos, displayPosition, Color.cyan, CognitiveVR_Preferences.Instance.SnapshotInterval);
            if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();

            DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = displayPosition;
            DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = Vector3.zero;
            DisplayGazePoints[DisplayGazePoints.Count].Transform = null;
            DisplayGazePoints[DisplayGazePoints.Count].IsLocal = false;
            DisplayGazePoints.Update();
        }
    }

    private void OnDestroy()
    {
        Core.InitEvent -= CognitiveVR_Manager_InitEvent;
        Core.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
    private void OnEndSessionEvent()
    {
        Core.EndSessionEvent -= OnEndSessionEvent;
        Destroy(this);
    }
}
}