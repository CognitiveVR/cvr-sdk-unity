using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;

//deals with most generic hmd position and facing direction functions

namespace Cognitive3D
{
    [AddComponentMenu("")]
    public class GazeBase : MonoBehaviour
    {
        internal CircularBuffer<ThreadGazePoint> DisplayGazePoints = new CircularBuffer<ThreadGazePoint>(256);
        internal static Vector3 LastGazePoint;

        protected bool headsetPresent;
        protected Transform cameraRoot;

        //called immediately after construction
        internal virtual void Initialize()
        {
#if C3D_OCULUS
            OVRManager.HMDMounted += OVRManager_HMDMounted;
            OVRManager.HMDUnmounted += OVRManager_HMDUnmounted;
#elif C3D_OMNICEPT
            if (gb == null)
            {
                gb = GameplayReferences.GliaBehaviour;
                if (gb != null)
                    gb.OnEyeTracking.AddListener(DoEyeTracking);
            }
#endif
            cameraRoot = GameplayReferences.HMD.root;
        }

#if C3D_OCULUS
        private void OVRManager_HMDMounted()
        {
            headsetPresent = true;
        }

        private void OVRManager_HMDUnmounted()
        {
            headsetPresent = false;
            Cognitive3D_Manager.FlushData();
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
        protected virtual bool DynamicRaycast(Vector3 pos, Vector3 direction, float distance, float radius, out float hitDistance, out DynamicObject hitDynamic, out Vector3 worldHitPoint, out Vector3 localHitPoint, out Vector2 hitTextureCoord)
        {
            //raycast to dynamic. if failed, spherecast with radius
            //if hit dynamic, return info

            RaycastHit hit = new RaycastHit();
            bool didhitdynamic = false;
            hitDynamic = null;
            hitDistance = 0;
            worldHitPoint = Vector3.zero;
            localHitPoint = Vector3.zero;
            hitTextureCoord = Vector2.zero;

            if (Physics.Raycast(pos, direction, out hit, distance, Cognitive3D_Preferences.Instance.DynamicLayerMask, Cognitive3D_Preferences.Instance.TriggerInteraction))
            {
                if (Cognitive3D_Preferences.S_DynamicObjectSearchInParent)
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

                    localHitPoint = hitDynamic.transform.InverseTransformPoint(worldHitPoint);
                    hitDistance = hit.distance;
                    hitTextureCoord = hit.textureCoord;
                }
            }
            if (!didhitdynamic && Physics.SphereCast(pos, radius, direction, out hit, distance, Cognitive3D_Preferences.Instance.DynamicLayerMask, Cognitive3D_Preferences.Instance.TriggerInteraction))
            {
                if (Cognitive3D_Preferences.Instance.DynamicObjectSearchInParent)
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

                    localHitPoint = hitDynamic.transform.InverseTransformPoint(worldHitPoint);
                    hitDistance = hit.distance;
                    hitTextureCoord = hit.textureCoord;
                }
            }

            return didhitdynamic;
        }

#if C3D_OMNICEPT
        static Vector3 lastDirection = Vector3.forward;
        static HP.Omnicept.Unity.GliaBehaviour gb;

        static void DoEyeTracking(HP.Omnicept.Messaging.Messages.EyeTracking data)
        {
            if (data.CombinedGaze.Confidence > 0.5f)
            {
                lastDirection = GameplayReferences.HMD.TransformDirection(new Vector3(-data.CombinedGaze.X, data.CombinedGaze.Y, data.CombinedGaze.Z));
            }
        }
#endif
        Vector3 gazeDirection = Vector3.forward;

        /// <summary>
        /// get the raw gaze direction in world space. includes eye tracking. returns previous direction if currently invalid
        /// </summary>
        protected Vector3 GetWorldGazeDirection()
        {
#if C3D_SRANIPAL
            var ray = new Ray();
            //improvement? - if using callback, listen and use last valid data instead of calling SRanipal_Eye_API.GetEyeData
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE, out ray))
            {
                gazeDirection = GameplayReferences.HMD.TransformDirection(ray.direction);
            }
#elif C3D_VARJOXR
            if (Varjo.XR.VarjoEyeTracking.IsGazeAllowed() && Varjo.XR.VarjoEyeTracking.IsGazeCalibrated())
            {
                var data = Varjo.XR.VarjoEyeTracking.GetGaze();
                if (data.status != Varjo.XR.VarjoEyeTracking.GazeStatus.Invalid)
                {
                    var ray = data.gaze;
                    gazeDirection = GameplayReferences.HMD.TransformDirection(new Vector3((float)ray.forward[0], (float)ray.forward[1], (float)ray.forward[2]));
                }
            }
#elif C3D_VARJOVR
            if (Varjo.XR.VarjoEyeTracking.IsGazeAllowed() && Varjo.XR.VarjoEyeTracking.IsGazeCalibrated())
            {
                var data = Varjo.XR.VarjoEyeTracking.GetGaze();
                if (data.status != Varjo.XR.VarjoEyeTracking.GazeStatus.Invalid)
                {
                    var ray = data.gaze;
                    gazeDirection = GameplayReferences.HMD.TransformDirection(new Vector3((float)ray.forward[0], (float)ray.forward[1], (float)ray.forward[2]));
                }
            }
#elif C3D_OMNICEPT
            gazeDirection = lastDirection;
#else
            UnityEngine.XR.Eyes eyes;
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye).TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
            {
                //first arg probably to mark which feature the value should return. type alone isn't enough to indicate the property
                Vector3 convergancePoint;
                if (eyes.TryGetFixationPoint(out convergancePoint))
                {
                    Vector3 leftPos = Vector3.zero;
                    eyes.TryGetLeftEyePosition(out leftPos);
                    Vector3 rightPos = Vector3.zero;
                    eyes.TryGetRightEyePosition(out rightPos);

                    Vector3 centerPos = (rightPos + leftPos) / 2f;

                    gazeDirection = (convergancePoint - centerPos).normalized;
                    //openxr implementation returns a direction adjusted by the HMD's transform, but not by the parent transformations
                    if (GameplayReferences.HMD.parent != null)
                        gazeDirection = GameplayReferences.HMD.parent.TransformDirection(gazeDirection);
                }
            }
#endif
            return gazeDirection;
        }

        /// <summary>
        /// get the position of the eye gaze in normalized viewport space
        /// </summary>
        protected Vector3 GetViewportGazePoint()
        {
            Vector2 screenGazePoint = new Vector2(0.5f, 0.5f);

#if C3D_SRANIPAL
            var leftv2 = Vector2.zero;
            var rightv2 = Vector2.zero;

            bool leftSet = ViveSR.anipal.Eye.SRanipal_Eye.GetPupilPosition(ViveSR.anipal.Eye.EyeIndex.LEFT, out leftv2);
            bool rightSet = ViveSR.anipal.Eye.SRanipal_Eye.GetPupilPosition(ViveSR.anipal.Eye.EyeIndex.RIGHT, out rightv2);
            if (leftSet && !rightSet)
                screenGazePoint = leftv2;
            else if (!leftSet && rightSet)
                screenGazePoint = rightv2;
            else if (leftSet && rightSet)
                screenGazePoint = (leftv2 + rightv2) / 2;
#else
            UnityEngine.XR.Eyes eyes;
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye).TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
            {
                //first arg probably to mark which feature the value should return. type alone isn't enough to indicate the property
                Vector3 convergancePoint;
                if (eyes.TryGetFixationPoint(out convergancePoint))
                {
                    Vector3 leftPos = Vector3.zero;
                    eyes.TryGetLeftEyePosition(out leftPos);
                    Vector3 rightPos = Vector3.zero;
                    eyes.TryGetRightEyePosition(out rightPos);

                    Vector3 centerPos = (rightPos + leftPos) / 2f;

                    var worldGazeDirection = (convergancePoint - centerPos).normalized;
                    screenGazePoint = GameplayReferences.HMDCameraComponent.WorldToScreenPoint(GameplayReferences.HMD.position + 10 * worldGazeDirection);
                }
            }
#endif
            return screenGazePoint;
        }
    }
} //namespace

