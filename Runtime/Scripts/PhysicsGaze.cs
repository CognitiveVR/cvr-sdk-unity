using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;

//physics raycast from camera
//adds gazepoint at hit.point

namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Internal/Physics Gaze")]
    public class PhysicsGaze : GazeBase
    {
        public delegate void onGazeTick();
        /// <summary>
        /// Called on a 0.1 second interval
        /// </summary>
        public static event onGazeTick OnGazeTick;
        private static void InvokeGazeTickEvent() { if (OnGazeTick != null) { OnGazeTick(); } }

        public override void Initialize()
        {
            base.Initialize();
            if (GameplayReferences.HMD == null) { Cognitive3D.Util.logWarning("HMD is null! Physics Gaze needs a camera to function"); }
            StartCoroutine(Tick());
            Cognitive3D_Manager.OnPreSessionEnd += OnEndSessionEvent;
        }

        IEnumerator Tick()
        {
            if (GameplayReferences.HMD == null) { yield return null; }

            while (Cognitive3D_Manager.IsInitialized)
            {
                yield return Cognitive3D_Manager.PlayerSnapshotInverval;
                
                try
                {
                    InvokeGazeTickEvent();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }

                Ray ray = GazeHelper.GetCurrentWorldGazeRay();

                if (Cognitive3D_Preferences.Instance.EnableGaze == true && DynamicRaycast(ray.origin, ray.direction, GameplayReferences.HMDCameraComponent.farClipPlane, 0.05f, out var hitDistance, out var hitDynamic, out var hitWorld, out var hitLocal, out var hitcoord)) //hit dynamic
                {
                    string ObjectId = hitDynamic.GetId();
                    var mediacomponent = hitDynamic.GetComponent<MediaComponent>();
                    if (mediacomponent != null)
                    {
                        var mediatime = mediacomponent.IsVideo ? (int)((mediacomponent.VideoPlayerFrame / mediacomponent.VideoPlayerFrameRate) * 1000) : 0;
                        var mediauvs = hitcoord;
                        GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, GameplayReferences.HMD.position, GameplayReferences.HMD.rotation, mediacomponent.MediaId, mediatime, mediauvs);
                    }
                    else
                    {
                        GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, ray.origin, GameplayReferences.HMD.rotation);
                    }

                    //debugging
                    //Debug.DrawLine(GameplayReferences.HMD.position, hitWorld, new Color(1, 0, 1, 0.5f), Cognitive3D_Preferences.SnapshotInterval);
                    //Debug.DrawRay(hitWorld, Vector3.right, Color.red, 1);
                    //Debug.DrawRay(hitWorld, Vector3.forward, Color.blue, 1);
                    //Debug.DrawRay(hitWorld, Vector3.up, Color.green, 1);
                    
                    //active session view
                    if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                        DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();
                    DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = hitWorld;
                    DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = hitLocal;
                    DisplayGazePoints[DisplayGazePoints.Count].Transform = hitDynamic.transform;
                    DisplayGazePoints[DisplayGazePoints.Count].IsLocal = true;
                    DisplayGazePoints.Update();
                    yield return null;
                }

                if (Cognitive3D_Preferences.Instance.EnableGaze == true && Physics.Raycast(ray, out var hit, GameplayReferences.HMDCameraComponent.farClipPlane, Cognitive3D_Preferences.Instance.GazeLayerMask, Cognitive3D_Preferences.Instance.TriggerInteraction))
                {
                    Vector3 pos = GameplayReferences.HMD.position;
                    Vector3 gazepoint = hit.point;
                    Quaternion rot = GameplayReferences.HMD.rotation;

                    //hit world
                    GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), gazepoint, pos, rot);
                    
                    //debugging
                    //Debug.DrawLine(pos, gazepoint, Color.red, Cognitive3D_Preferences.SnapshotInterval);
                    //Debug.DrawRay(gazepoint, Vector3.right, Color.red, 10);
                    //Debug.DrawRay(gazepoint, Vector3.forward, Color.blue, 10);
                    //Debug.DrawRay(gazepoint, Vector3.up, Color.green, 10);
                    
                    //active session view
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
                    GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos, rot);

                    //debugging
                    //Debug.DrawRay(pos, displayPosition, Color.cyan, Cognitive3D_Preferences.Instance.SnapshotInterval);
                    
                    //active session view
                    if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                        DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();
                    DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = displayPosition;
                    DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = Vector3.zero;
                    DisplayGazePoints[DisplayGazePoints.Count].Transform = null;
                    DisplayGazePoints[DisplayGazePoints.Count].IsLocal = false;
                    DisplayGazePoints.Update();
                }
            }
        }

        private void OnEndSessionEvent()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= OnEndSessionEvent;
            Destroy(this);
        }
    }
}