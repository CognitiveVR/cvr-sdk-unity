using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR.ActiveSession
{
    public class FixationRenderCamera : MonoBehaviour
    {
        Transform TargetCameraTransform;
        Camera TargetCamera;
        Camera ThisCamera;

        public float LerpPositionSpeed = 0.1f;
        public float LerpRotationSpeed = 0.1f;

        public void Initialize(Camera sceneCamera)
        {
            TargetCameraTransform = sceneCamera.transform;
            TargetCamera = sceneCamera;
            ThisCamera = GetComponent<Camera>();
#if CVR_FOVE
            //just fully render the camera to be drawn on canvas
            ThisCamera.clearFlags = CameraClearFlags.Skybox;
            ThisCamera.cullingMask = -1;
#endif
        }

        void MatchTargetCamera()
        {
#if CVR_STEAMVR || CVR_STEAMVR2
            var vm = Valve.VR.OpenVR.System.GetProjectionMatrix(Valve.VR.EVREye.Eye_Left, ThisCamera.nearClipPlane, ThisCamera.farClipPlane);
            Matrix4x4 m = new Matrix4x4();
            m.m00 = vm.m0;
            m.m01 = vm.m1;
            m.m02 = vm.m2;
            m.m03 = vm.m3;
            m.m10 = vm.m4;
            m.m11 = vm.m5;
            m.m12 = vm.m6;
            m.m13 = vm.m7;
            m.m20 = vm.m8;
            m.m21 = vm.m9;
            m.m22 = vm.m10;
            m.m23 = vm.m11;
            m.m30 = vm.m12;
            m.m31 = vm.m13;
            m.m32 = vm.m14;
            m.m33 = vm.m15;

            ThisCamera.projectionMatrix = m;
#else
            ThisCamera.projectionMatrix = TargetCamera.projectionMatrix;
#endif
        }

        void LateUpdate()
        {
            if (TargetCameraTransform == null) { return; }
            MatchTargetCamera();
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, TargetCameraTransform.position, LerpPositionSpeed), Quaternion.Lerp(transform.rotation, TargetCameraTransform.rotation, LerpRotationSpeed));
        }
    }
}