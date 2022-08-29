using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;

//deals with most generic integration stuff
//hmd removed - send data

//must implement
//1. dynamics raycast
//2. call dynamic.OnGaze(interval)
//3. fove/pupil/tobii gaze direction
//4. media
//5. gps + compass
//6. floor position

namespace Cognitive3D
{
    [AddComponentMenu("")]
    public class GazeBase : MonoBehaviour
    {
        public CircularBuffer<ThreadGazePoint> DisplayGazePoints = new CircularBuffer<ThreadGazePoint>(256);
        public static Vector3 LastGazePoint;

        protected bool headsetPresent;
        protected Transform cameraRoot;

        //called immediately after construction
        public virtual void Initialize()
        {
#if C3D_STEAMVR
            Cognitive3D_Manager.PoseEvent += Cognitive3D_Manager_OnPoseEvent; //1.2
#elif C3D_OCULUS
            OVRManager.HMDMounted += OVRManager_HMDMounted;
            OVRManager.HMDUnmounted += OVRManager_HMDUnmounted;
#elif C3D_PUPIL
            gazeController = GameplayReferences.GazeController;
            if (gazeController != null)
                gazeController.OnReceive3dGaze += ReceiveEyeData;
            else
                Debug.LogError("Pupil Labs GazeController is null!");
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

        //TODO support more sdks to send when HMD removed (wave, varjo, steamvr2)

#if C3D_STEAMVR
        void Cognitive3D_Manager_OnPoseEvent(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                headsetPresent = true;
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                headsetPresent = false;
                if (Cognitive3D_Preferences.Instance.SendDataOnHMDRemove)
                {
                    Core.InvokeSendDataEvent(false);
                }
            }
        }

        void Cognitive3D_Manager_OnPoseEventOLD(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                headsetPresent = true;
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                headsetPresent = false;
                if (Cognitive3D_Preferences.Instance.SendDataOnHMDRemove)
                {
                    Core.InvokeSendDataEvent(false);
                }
            }
        }
#endif

#if C3D_OCULUS
        private void OVRManager_HMDMounted()
        {
            headsetPresent = true;
        }

        private void OVRManager_HMDUnmounted()
        {
            headsetPresent = false;
            if (Cognitive3D_Preferences.Instance.SendDataOnHMDRemove)
            {
                Core.InvokeSendDataEvent(false);
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
        public virtual bool DynamicRaycast(Vector3 pos, Vector3 direction, float distance, float radius, out float hitDistance, out DynamicObject hitDynamic, out Vector3 worldHitPoint, out Vector3 localHitPoint, out Vector2 hitTextureCoord)
        {
            //TODO raycast to dynamic. if failed, spherecast with radius
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

        public void GetOptionalSnapshotData(ref Vector3 gpsloc, ref float compass, ref Vector3 floorPos)
        {
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                Cognitive3D_Manager.Instance.GetGPSLocation(ref gpsloc, ref compass);
            }
            if (Cognitive3D_Preferences.Instance.RecordFloorPosition)
            {
                if (cameraRoot == null)
                {
                    cameraRoot = GameplayReferences.HMD.root;
                }
                RaycastHit floorhit = new RaycastHit();
                if (Physics.Raycast(GameplayReferences.HMD.position, -cameraRoot.up, out floorhit))
                {
                    floorPos = floorhit.point;
                }
            }
        }

#if C3D_PUPIL
        PupilLabs.GazeController gazeController;
        Vector3 localGazeDirection;
        Vector3 pupilViewportPosition;

        void ReceiveEyeData(PupilLabs.GazeData data)
        {
            if (data.Confidence < 0.6f) { return; }
            localGazeDirection = data.GazeDirection;
            pupilViewportPosition = data.NormPos;
        }

        private void OnDisable()
        {
            gazeController.OnReceive3dGaze -= ReceiveEyeData;
        }
#endif
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
        public Vector3 GetWorldGazeDirection()
        {
#if C3D_PUPIL
            gazeDirection = GameplayReferences.HMD.TransformDirection(localGazeDirection);
#elif C3D_TOBIIVR
            if (Tobii.XR.TobiiXR.Internal.Provider.EyeTrackingDataLocal.GazeRay.IsValid)
            {
                gazeDirection = GameplayReferences.HMD.TransformDirection(Tobii.XR.TobiiXR.Internal.Provider.EyeTrackingDataLocal.GazeRay.Direction);
            }    
#elif C3D_SRANIPAL
            var ray = new Ray();
            //improvement? - if using callback, listen and use last valid data instead of calling SRanipal_Eye_API.GetEyeData
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE, out ray))
            {
                gazeDirection = GameplayReferences.HMD.TransformDirection(ray.direction);
            }
#elif C3D_VARJO
            if (Varjo.XR.VarjoEyeTracking.IsGazeAllowed() && Varjo.XR.VarjoEyeTracking.IsGazeCalibrated())
            {
                var data = Varjo.XR.VarjoEyeTracking.GetGaze();
                if (data.status != Varjo.XR.VarjoEyeTracking.GazeStatus.Invalid)
                {
                    var ray = data.gaze;
                    gazeDirection = GameplayReferences.HMD.TransformDirection(new Vector3((float)ray.forward[0], (float)ray.forward[1], (float)ray.forward[2]));
                }
            }
#elif C3D_NEURABLE
            gazeDirection = Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay().direction;
#elif C3D_SNAPDRAGON
            gazeDirection = SvrManager.Instance.leftCamera.transform.TransformDirection(SvrManager.Instance.EyeDirection);
#elif C3D_OMNICEPT
            gazeDirection = lastDirection;
#elif C3D_XR
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
        public Vector3 GetViewportGazePoint()
        {
            Vector2 screenGazePoint = new Vector2(0.5f,0.5f);

#if C3D_PUPIL//screenpoint
            screenGazePoint = pupilViewportPosition;
#elif C3D_TOBIIVR
            if (Tobii.XR.TobiiXR.Internal.Provider.EyeTrackingDataLocal.GazeRay.IsValid)
            {
                var gazeray = Tobii.XR.TobiiXR.Internal.Provider.EyeTrackingDataLocal.GazeRay;
                screenGazePoint = GameplayReferences.HMDCameraComponent.WorldToViewportPoint(gazeray.Origin + GameplayReferences.HMD.TransformDirection(gazeray.Direction) * 1000);                
            }
#elif C3D_SRANIPAL
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

#elif C3D_NEURABLE
            screenGazePoint = Neurable.Core.NeurableUser.Instance.NeurableCam.NormalizedFocalPoint;
#elif C3D_SNAPDRAGON
            var worldgazeDirection = SvrManager.Instance.leftCamera.transform.TransformDirection(SvrManager.Instance.EyeDirection);
            screenGazePoint = GameplayReferences.HMDCameraComponent.WorldToScreenPoint(GameplayReferences.HMD.position + 10 * worldgazeDirection);
#elif C3D_XR
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

