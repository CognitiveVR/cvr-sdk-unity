using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//deals with most generic integration stuff
//hmd removed - send data

//must implement
//1. dynamics raycast
//2. call dynamic.OnGaze(interval)
//3. fove/pupil gaze direction
//4. media

namespace CognitiveVR
{
    public class GazeBase : MonoBehaviour
    {
        protected Camera cam;
        protected Camera CameraComponent
        {
            get
            {
                if (cam == null)
                    cam = Camera.main;
                return cam;
            }
        }
        protected Transform camtransform;
        protected Transform CameraTransform
        {
            get
            {
                if (camtransform == null)
                    camtransform = CameraComponent.transform;
                return camtransform;
            }
        }

        //called immediately after construction
        public virtual void Initialize()
        {
#if CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_OnPoseEvent; //1.2
#endif
#if CVR_OCULUS
            OVRManager.HMDMounted += OVRManager_HMDMounted;
            OVRManager.HMDUnmounted += OVRManager_HMDUnmounted;
#endif
            string hmdname = "none";
#if CVR_FOVE
                    hmdname = "fove";
#elif CVR_ARKIT
                    hmdname = "arkit";
#elif CVR_ARCORE
                    hmdname = "arcore";
#elif CVR_META
                    hmdname = "meta";
#else
#if UNITY_2017_2_OR_NEWER
            string rawHMDName = UnityEngine.XR.XRDevice.model.ToLower();
#else
            string rawHMDName = UnityEngine.VR.VRDevice.model.ToLower();
#endif
            hmdname = CognitiveVR.Util.GetSimpleHMDName(rawHMDName);
#endif

            GazeCore.SetHMDType(hmdname);
        }


#if CVR_STEAMVR
        void CognitiveVR_Manager_OnPoseEvent(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                headsetPresent = true;
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                headsetPresent = false;
                if (CognitiveVR_Preferences.Instance.SendDataOnHMDRemove)
                {
                    Core.SendDataEvent();
                }
            }
        }

        void CognitiveVR_Manager_OnPoseEventOLD(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                headsetPresent = true;
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                headsetPresent = false;
                if (CognitiveVR_Preferences.Instance.SendDataOnHMDRemove)
                {
                    Core.SendDataEvent();
                }
            }
        }
#endif

#if CVR_OCULUS
        private void OVRManager_HMDMounted()
        {
            headsetPresent = true;
        }

        private void OVRManager_HMDUnmounted()
        {
            headsetPresent = false;
            if (CognitiveVR_Preferences.Instance.SendDataOnHMDRemove)
            {
                Core.SendDataEvent();
            }
        }
#endif

        /// <summary>
        /// raycasts then spherecasts in a direction to find dynamic object being gazed at. returns true if hits dynamic
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public virtual bool DynamicRaycast(Vector3 pos, Vector3 direction, float distance, float radius, out float hitDistance, out DynamicObject hitDynamic, out Vector3 worldHitPoint)
        {
            //TODO raycast to dynamic. if failed, spherecast with radius
            //if hit dynamic, return info

            RaycastHit hit = new RaycastHit();
            bool didhitdynamic = false;
            hitDynamic = null;
            hitDistance = 0;
            worldHitPoint = Vector3.zero;

            if (Physics.Raycast(pos, direction, out hit, distance))
            {
                if (CognitiveVR_Preferences.S_DynamicObjectSearchInParent)
                {
                    hitDynamic = hit.collider.GetComponentInParent<DynamicObject>();
                }
                else
                {
                    hitDynamic = hit.collider.GetComponent<DynamicObject>();
                }

                if (hitDynamic != null)
                {
                    didhitdynamic = true;
                    worldHitPoint = hit.point;
                    hitDistance = hit.distance;
                }
            }
            if (!didhitdynamic && Physics.SphereCast(pos, radius, direction, out hit, distance))
            {
                if (CognitiveVR_Preferences.Instance.DynamicObjectSearchInParent)
                {
                    hitDynamic = hit.collider.GetComponentInParent<DynamicObject>();
                }
                else
                {
                    hitDynamic = hit.collider.GetComponent<DynamicObject>();
                }

                if (hitDynamic != null)
                {
                    didhitdynamic = true;
                    worldHitPoint = hit.point;
                    hitDistance = hit.distance;
                }
            }

            return didhitdynamic;
        }

        /// <summary>
        /// get the raw gaze direction in world space. includes fove/pupil labs eye tracking
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldGazeDirection()
        {
            Vector3 gazeDirection = CameraTransform.forward;
#if CVR_FOVE //direction
            var eyeRays = FoveInstance.GetGazeRays();
            var ray = eyeRays.left;
            gazeDirection = new Vector3(ray.direction.x, ray.direction.y, ray.direction.z);
            gazeDirection.Normalize();
#elif CVR_PUPIL
            //var v2 = PupilGazeTracker.Instance.GetEyeGaze(PupilGazeTracker.GazeSource.BothEyes); //0-1 screen pos
            var v2 = PupilData._2D.GetEyeGaze(Pupil.GazeSource.BothEyes);

            //if it doesn't find the eyes, skip this snapshot
            if (PupilTools.Confidence(PupilData.rightEyeID) > 0.1f)
            {
                var ray = instance.cam.ViewportPointToRay(v2);
                gazeDirection = ray.direction.normalized;
            } //else uses HMD forward
#endif //pupil direction
            return gazeDirection;
        }

        public Vector3 GetScreenGazePoint()
        {
            Vector2 screenGazePoint = Vector2.one * 0.5f;

#if CVR_FOVE //screenpoint

            //var normalizedPoint = FoveInterface.GetNormalizedViewportPosition(ray.GetPoint(1000), Fove.EFVR_Eye.Left); //Unity Plugin Version 1.3.1
            var normalizedPoint = cam.WorldToViewportPoint(ray.GetPoint(1000));

            //Vector2 gazePoint = hmd.GetGazePoint();
            if (float.IsNaN(normalizedPoint.x))
            {
                return;
            }

            screenGazePoint = new Vector2(normalizedPoint.x, normalizedPoint.y);
#elif CVR_PUPIL//screenpoint
            screenGazePoint = PupilData._2D.GetEyeGaze(Pupil.GazeSource.BothEyes);
#endif //pupil screenpoint
            return screenGazePoint;
        }
    }
}

public static class UnscaledTransformPoints
{
    public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var localToWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        return localToWorldMatrix.MultiplyPoint3x4(position);
    }

    public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
        return worldToLocalMatrix.MultiplyPoint3x4(position);
    }
}