using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

//draws saccade lines and fixation spheres for target camera

namespace CognitiveVR.ActiveSession
{
    public class RenderEyetracking : MonoBehaviour
    {
        public int FixationRenderLayer = 3; //unnamed internal layer
        public int Mask = 8;

        public float FixationScale = 0.2f;
        public Mesh FixationMesh;
        public Material FixationMaterial;
        public Color FixationColor;

        public Material SaccadeMaterial;

        CognitiveVR.FixationRecorder fixationRecorder;
        CognitiveVR.GazeBase gazeBase;
        Camera FixationCamera;
        Transform TargetCameraTransform;
        Camera FollowCamera;

        bool displayFixations = false;
        bool displayGaze = false;

        Queue<Fixation> fixationQueue = new Queue<Fixation>(50);



        Vector3 hmdforward;
        Matrix4x4 m4proj;
        Matrix4x4 m4world;
        int pixelwidth;
        int pixelheight;
        public float lineWidth = 0.2f;
        System.Threading.Thread VectorMathThread;

        public void Initialize(Camera followCamera)
        {
            FollowCamera = followCamera;
            TargetCameraTransform = followCamera.transform;
            FixationCamera = GetComponent<Camera>();
            fixationRecorder = FixationRecorder.Instance;
            gazeBase = FindObjectOfType<GazeBase>();
            FixationMaterial.color = FixationColor;

            if (Core.IsInitialized)
            {
                if (fixationRecorder != null)
                {
                    quadPositions = new Vector3[FixationRecorder.DisplayGazePointCount * 4];
                    displayFixations = true;
                    FixationCore.OnFixationRecord += FixationCore_OnFixationRecord;
                    if (threaded)
                    {
                        VectorMathThread = new System.Threading.Thread(CalculateVectors);
                        VectorMathThread.Start();
                    }
                }
                else
                {
                    displayGaze = true;
                }
            }
            else
            {
                Core.InitEvent += Core_InitEvent;
            }
            FixationCamera.cullingMask = Mask;
#if CVR_FOVE
            //just fully render the camera to be drawn on canvas
            FixationCamera.clearFlags = CameraClearFlags.Skybox;
            FixationCamera.cullingMask = -1;
#endif
        }

        private void Core_InitEvent(Error initError)
        {
            Core.InitEvent -= Core_InitEvent;

            fixationRecorder = FixationRecorder.Instance;

            if (initError == Error.None && fixationRecorder != null)
            {
                quadPositions = new Vector3[FixationRecorder.DisplayGazePointCount * 4];
                displayFixations = true;
                FixationCore.OnFixationRecord += FixationCore_OnFixationRecord;
                if (threaded)
                {
                    VectorMathThread = new System.Threading.Thread(CalculateVectors);
                    VectorMathThread.Start();
                }
            }
            else
            {
                if (gazeBase == null)
                    gazeBase = FindObjectOfType<GazeBase>();
                displayGaze = true;
            }
        }

        private void FixationCore_OnFixationRecord(Fixation fixation)
        {
            if (fixationQueue.Count > 49)
                fixationQueue.Dequeue();
            fixationQueue.Enqueue(new Fixation(fixation));
        }

        void Update()
        {
            if (!displayFixations && !displayGaze) { return; }

            hmdforward = CognitiveVR.GameplayReferences.HMD.forward;

            m4proj = FixationCamera.projectionMatrix;
            m4world = FixationCamera.worldToCameraMatrix;
            pixelwidth = FixationCamera.pixelWidth;
            pixelheight = FixationCamera.pixelHeight;

            if (displayFixations)
            {
                Vector3 scale = Vector3.one * FixationScale;
                //on new fixation
                foreach (Fixation f in fixationQueue)
                {
                    if (f.IsLocal && f.DynamicTransform != null)
                    {
                        Matrix4x4 m = Matrix4x4.TRS(f.DynamicTransform.TransformPoint(f.LocalPosition), Quaternion.identity, scale * f.MaxRadius);
                        Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, FixationCamera);
                    }
                    else
                    {
                        Matrix4x4 m = Matrix4x4.TRS(f.WorldPosition, Quaternion.identity, scale * f.MaxRadius);
                        Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, FixationCamera);
                    }
                }

                //update list of points
                Color magenta = Color.magenta;
                int count = fixationRecorder.DisplayGazePoints.Count;
                for (int i = 0; i < count; i++)
                {
                    ThreadGazePoint temp = fixationRecorder.DisplayGazePoints[i];

                    //if (!threaded && fixationRecorder.DisplayGazePoints[i - 1] != null)
                        //Debug.DrawLine(fixationRecorder.DisplayGazePoints[i - 1].WorldPoint, temp.WorldPoint, magenta);
                    if (!temp.IsLocal) { continue; }
                    if (temp.Transform == null) { temp.IsLocal = false; continue; }
                    temp.TransformMatrix = temp.Transform.localToWorldMatrix;
                }
            }
            else if (displayGaze)
            {
                //update list of points
                Color magenta = Color.magenta;
                int count = gazeBase.DisplayGazePoints.Count;
                for (int i = 0; i < count; i++)
                {
                    ThreadGazePoint temp = gazeBase.DisplayGazePoints[i];

                    //if (!threaded && gazeBase.DisplayGazePoints[i - 1] != null)
                        //Debug.DrawLine(gazeBase.DisplayGazePoints[i - 1].WorldPoint, temp.WorldPoint, magenta);
                    if (!temp.IsLocal) { continue; }
                    if (temp.Transform == null) { temp.IsLocal = false; continue; }
                    temp.TransformMatrix = temp.Transform.localToWorldMatrix;
                }
            }
        }

        bool threaded = true;
        void CalculateVectors()
        {
            Vector3 zero = Vector3.zero;
            while (true)
            {
                int j = 0;

                Vector3 b1 = zero; //inner corner
                Vector3 b2 = zero; //outer corner

                if (displayFixations)
                {
                    for (int i = 0; i < fixationRecorder.DisplayGazePoints.Count; i++)
                    {
                        int prevIndex = i - 1;
                        int currentIndex = i;
                        int nextIndex = i + 1;

                        //draw everything
                        Vector3 previousPoint = zero;
                        if (fixationRecorder.DisplayGazePoints[prevIndex] != null)
                        {

                            if (fixationRecorder.DisplayGazePoints[prevIndex].IsLocal)
                            {
                                //do math from transform matrix
                                previousPoint = fixationRecorder.DisplayGazePoints[prevIndex].TransformMatrix.MultiplyPoint(fixationRecorder.DisplayGazePoints[prevIndex].LocalPoint);
                            }
                            else
                            {
                                previousPoint = fixationRecorder.DisplayGazePoints[prevIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            previousPoint = fixationRecorder.DisplayGazePoints[currentIndex].WorldPoint + Vector3.down * 0.01f;
                        }

                        Vector3 currentPoint = zero;
                        if (fixationRecorder.DisplayGazePoints[currentIndex].IsLocal)
                        {
                            //do math from transform matrix
                            currentPoint = fixationRecorder.DisplayGazePoints[currentIndex].TransformMatrix.MultiplyPoint(fixationRecorder.DisplayGazePoints[currentIndex].LocalPoint);
                        }
                        else
                        {
                            currentPoint = fixationRecorder.DisplayGazePoints[currentIndex].WorldPoint;
                        }

                        Vector3 nextPoint = zero;
                        if (fixationRecorder.DisplayGazePoints[nextIndex] != null)
                        {
                            if (fixationRecorder.DisplayGazePoints[nextIndex].IsLocal)
                            {
                                //do math from transform matrix
                                nextPoint = fixationRecorder.DisplayGazePoints[nextIndex].TransformMatrix.MultiplyPoint(fixationRecorder.DisplayGazePoints[nextIndex].LocalPoint);
                            }
                            else
                            {
                                nextPoint = fixationRecorder.DisplayGazePoints[nextIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            nextPoint = fixationRecorder.DisplayGazePoints[currentIndex].WorldPoint + Vector3.up * 0.01f;
                        }

                        if (i == 0)
                        {
                            b1 = currentPoint;
                            b2 = currentPoint;
                        }
                        CalculateQuadPoints(previousPoint, currentPoint, nextPoint, ref b1, ref b2, ref j);
                    }
                }
                else if (displayGaze)
                {
                    for (int i = 0; i < gazeBase.DisplayGazePoints.Count; i++)
                    {
                        int prevIndex = i - 1;
                        int currentIndex = i;
                        int nextIndex = i + 1;

                        //draw everything
                        Vector3 previousPoint = zero;
                        if (gazeBase.DisplayGazePoints[prevIndex] != null)
                        {

                            if (gazeBase.DisplayGazePoints[prevIndex].IsLocal)
                            {
                                //do math from transform matrix
                                previousPoint = gazeBase.DisplayGazePoints[prevIndex].TransformMatrix.MultiplyPoint(gazeBase.DisplayGazePoints[prevIndex].LocalPoint);
                            }
                            else
                            {
                                previousPoint = gazeBase.DisplayGazePoints[prevIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            previousPoint = gazeBase.DisplayGazePoints[currentIndex].WorldPoint + Vector3.down * 0.01f;
                        }

                        Vector3 currentPoint = zero;
                        if (gazeBase.DisplayGazePoints[currentIndex].IsLocal)
                        {
                            //do math from transform matrix
                            currentPoint = gazeBase.DisplayGazePoints[currentIndex].TransformMatrix.MultiplyPoint(gazeBase.DisplayGazePoints[currentIndex].LocalPoint);
                        }
                        else
                        {
                            currentPoint = gazeBase.DisplayGazePoints[currentIndex].WorldPoint;
                        }

                        Vector3 nextPoint = zero;
                        if (gazeBase.DisplayGazePoints[nextIndex] != null)
                        {
                            if (gazeBase.DisplayGazePoints[nextIndex].IsLocal)
                            {
                                //do math from transform matrix
                                nextPoint = gazeBase.DisplayGazePoints[nextIndex].TransformMatrix.MultiplyPoint(gazeBase.DisplayGazePoints[nextIndex].LocalPoint);
                            }
                            else
                            {
                                nextPoint = gazeBase.DisplayGazePoints[nextIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            nextPoint = gazeBase.DisplayGazePoints[currentIndex].WorldPoint + Vector3.up * 0.01f;
                        }

                        if (i == 0)
                        {
                            b1 = currentPoint;
                            b2 = currentPoint;
                        }
                        CalculateQuadPoints(previousPoint, currentPoint, nextPoint, ref b1, ref b2, ref j);
                    }
                }
                quadPositionCount = j;
                if (threaded)
                {
                    System.Threading.Thread.Sleep(10);
                }
                else
                {
                    break;
                }
            }
        }

        private void CalculateQuadPoints(Vector3 previousPoint, Vector3 currentPoint, Vector3 nextPoint, ref Vector3 b1, ref Vector3 b2, ref int j)
        {

            //screenspace stuff
            Vector3 mathscreenprevious = manualWorldToScreenPoint(previousPoint, m4proj, m4world, pixelwidth, pixelheight);
            Vector3 mathscreencurrent = manualWorldToScreenPoint(currentPoint, m4proj, m4world, pixelwidth, pixelheight);
            Vector3 mathscreennext = manualWorldToScreenPoint(nextPoint, m4proj, m4world, pixelwidth, pixelheight);
            var screenDirection = (mathscreencurrent - mathscreenprevious).normalized;
            Vector3 screencross = Vector3.Cross(screenDirection, hmdforward); //used to fallback with very acute corners

            Vector3 firstDir = (mathscreenprevious - mathscreencurrent).normalized;
            Vector3 secondDir = (mathscreennext - mathscreencurrent).normalized;

            //line segment AB
            //var firstDirectionPerp = Vector3.Cross(firstDir, hmdforward).normalized * lineWidth / 2f; //hmd forward

            //line segment BC
            //var secondDirectionPerp = Vector3.Cross(secondDir, hmdforward).normalized * lineWidth / 2f; //hmd forward

            //line angle ABC
            float lineAngleABC = SignedAngle(firstDir, secondDir, hmdforward);
            float complementaryAngleABC = 90 - lineAngleABC;


            //outer corner
            float hypotenuse = (lineWidth / 2f) / Mathf.Cos(complementaryAngleABC * Mathf.Deg2Rad);
            Vector3 lowerCorner = currentPoint + -secondDir.normalized * hypotenuse - firstDir * hypotenuse;

            //inner corner
            Vector3 upperCorner = currentPoint + secondDir.normalized * hypotenuse + firstDir * hypotenuse;

            //quad corner points from previous line segment
            quadPositions[j] = b1;
            j++;
            quadPositions[j] = b2;
            j++;

            if ((lineAngleABC < 40 && lineAngleABC > -40) || lineAngleABC > 160 || lineAngleABC < -160)
            {
                quadPositions[j] = currentPoint - screencross * 0.5f * lineWidth;
                j++;
                quadPositions[j] = currentPoint + screencross * 0.5f * lineWidth;
                j++;

                b1 = currentPoint + screencross * 0.5f * lineWidth;
                b2 = currentPoint - screencross * 0.5f * lineWidth;
                //Debug.DrawLine(b2, b1, Color.red);
            }
            else
            {
                b1 = upperCorner;
                b2 = lowerCorner;
                quadPositions[j] = b2;
                j++;
                quadPositions[j] = b1;
                j++;
                //Debug.DrawLine(b2, b1, Color.green);
            }
        }

        //this doesn't exist in unity 5.6
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsignedAngle = Vector3.Angle(from, to);

            float cross_x = from.y * to.z - from.z * to.y;
            float cross_y = from.z * to.x - from.x * to.z;
            float cross_z = from.x * to.y - from.y * to.x;
            float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return unsignedAngle * sign;
        }
        
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct HmdMatrix44_t
        {
            public float m0; //float[4][4]
            public float m1;
            public float m2;
            public float m3;
            public float m4;
            public float m5;
            public float m6;
            public float m7;
            public float m8;
            public float m9;
            public float m10;
            public float m11;
            public float m12;
            public float m13;
            public float m14;
            public float m15;
        }

        void MatchTargetCamera()
        {
            //uses projection matrix from openvr if developer is using a openvr-based sdk
#if CVR_VIVEPROEYE || CVR_STEAMVR || CVR_STEAMVR2 || CVR_TOBIIVR
            var vm = VRSystem().GetProjectionMatrix(EVREye.Eye_Left, FixationCamera.nearClipPlane, FixationCamera.farClipPlane);
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

            FixationCamera.projectionMatrix = m;
#else
            FixationCamera.projectionMatrix = FollowCamera.projectionMatrix;
#endif
        }

        void LateUpdate()
        {
            if (!displayFixations) { return; }
            if (TargetCameraTransform == null) { return; }
            MatchTargetCamera();
            transform.SetPositionAndRotation(TargetCameraTransform.position, TargetCameraTransform.rotation);
        }

        //IMPROVEMENT allow duration of previous saccades to be configurable. this can currently only be changed by fixationrecorder.displaygazepoints count
        //the output of the thread
        Vector3[] quadPositions;
        int quadPositionCount = 0;


        Vector3 manualWorldToScreenPoint(Vector3 wp, Matrix4x4 camproj, Matrix4x4 worldtocam, int width, int height)
        {
            // calculate view-projection matrix
            Matrix4x4 mat = camproj * worldtocam;

            // multiply world point by VP matrix
            Vector4 temp = mat * new Vector4(wp.x, wp.y, wp.z, 1f);

            if (temp.w == 0f)
            {
                // point is exactly on camera focus point, screen point is undefined
                // unity handles this by returning 0,0,0
                return Vector3.zero;
            }
            else
            {
                // convert x and y from clip space to window coordinates
                temp.x = (temp.x / temp.w + 1f) * .5f * width;
                temp.y = (temp.y / temp.w + 1f) * .5f * height;
                return new Vector3(temp.x, temp.y, wp.z);
            }
        }

        private void OnPostRender()
        {
            if (!displayFixations && !displayGaze) { return; }

            SaccadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(FixationCamera.projectionMatrix);
            GL.modelview = FixationCamera.worldToCameraMatrix;

            GL.Begin(GL.QUADS);

            for (int i = 0; i < quadPositionCount; i++)
            {
                GL.Vertex3(quadPositions[i].x, quadPositions[i].y, quadPositions[i].z);
            }

            GL.End();
            GL.PopMatrix();
        }

        void OnDestroy()
        {
            FixationCore.OnFixationRecord -= FixationCore_OnFixationRecord;
        }

        #region OpenVR Functions

        //this section gets the projection matrix from openvr to correctly offset drawing the gaze render point on screen
        //from https://github.com/ValveSoftware/openvr/blob/master/headers/openvr_api.cs

        static uint VRToken { get; set; }
        const string FnTable_Prefix = "FnTable:";
        public const string IVRSystem_Version = "IVRSystem_016";

        public class CVRSystem
        {
            IVRSystem FnTable;
            internal CVRSystem(IntPtr pInterface)
            {
                FnTable = (IVRSystem)Marshal.PtrToStructure(pInterface, typeof(IVRSystem));
            }
            public HmdMatrix44_t GetProjectionMatrix(EVREye eEye, float fNearZ, float fFarZ)
            {
                HmdMatrix44_t result = FnTable.GetProjectionMatrix(eEye, fNearZ, fFarZ);
                return result;
            }
        }
        private CVRSystem m_pVRSystem;
        public CVRSystem VRSystem()
        {
            if (VRToken != OpenVRInterop.GetInitToken())
            {
                m_pVRSystem = null;
                VRToken = OpenVRInterop.GetInitToken();
            }

            if (m_pVRSystem == null)
            {
                var eError = EVRInitError.None;
                var pInterface = OpenVRInterop.GetGenericInterface(FnTable_Prefix + IVRSystem_Version, ref eError);
                if (pInterface != IntPtr.Zero && eError == EVRInitError.None)
                    m_pVRSystem = new CVRSystem(pInterface);
            }
            return m_pVRSystem;
        }

        public class OpenVRInterop
        {
            [DllImportAttribute("openvr_api", EntryPoint = "VR_InitInternal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern uint InitInternal(ref EVRInitError peError, EVRApplicationType eApplicationType);
            [DllImportAttribute("openvr_api", EntryPoint = "VR_ShutdownInternal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void ShutdownInternal();
            [DllImportAttribute("openvr_api", EntryPoint = "VR_IsHmdPresent", CallingConvention = CallingConvention.Cdecl)]
            internal static extern bool IsHmdPresent();
            [DllImportAttribute("openvr_api", EntryPoint = "VR_IsRuntimeInstalled", CallingConvention = CallingConvention.Cdecl)]
            internal static extern bool IsRuntimeInstalled();
            [DllImportAttribute("openvr_api", EntryPoint = "VR_GetStringForHmdError", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr GetStringForHmdError(EVRInitError error);
            [DllImportAttribute("openvr_api", EntryPoint = "VR_GetGenericInterface", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr GetGenericInterface([In, MarshalAs(UnmanagedType.LPStr)] string pchInterfaceVersion, ref EVRInitError peError);
            [DllImportAttribute("openvr_api", EntryPoint = "VR_IsInterfaceVersionValid", CallingConvention = CallingConvention.Cdecl)]
            internal static extern bool IsInterfaceVersionValid([In, MarshalAs(UnmanagedType.LPStr)] string pchInterfaceVersion);
            [DllImportAttribute("openvr_api", EntryPoint = "VR_GetInitToken", CallingConvention = CallingConvention.Cdecl)]
            internal static extern uint GetInitToken();
        }

        #endregion

        #region Enums and Structs

        public enum EVRApplicationType
        {
            VRApplication_Other = 0,
            VRApplication_Scene = 1,
            VRApplication_Overlay = 2,
            VRApplication_Background = 3,
            VRApplication_Utility = 4,
            VRApplication_VRMonitor = 5,
            VRApplication_SteamWatchdog = 6,
            VRApplication_Bootstrapper = 7,
            VRApplication_Max = 8,
        }

        public enum EVRInitError
        {
            None = 0,
            Unknown = 1,
            Init_InstallationNotFound = 100,
            Init_InstallationCorrupt = 101,
            Init_VRClientDLLNotFound = 102,
            Init_FileNotFound = 103,
            Init_FactoryNotFound = 104,
            Init_InterfaceNotFound = 105,
            Init_InvalidInterface = 106,
            Init_UserConfigDirectoryInvalid = 107,
            Init_HmdNotFound = 108,
            Init_NotInitialized = 109,
            Init_PathRegistryNotFound = 110,
            Init_NoConfigPath = 111,
            Init_NoLogPath = 112,
            Init_PathRegistryNotWritable = 113,
            Init_AppInfoInitFailed = 114,
            Init_Retry = 115,
            Init_InitCanceledByUser = 116,
            Init_AnotherAppLaunching = 117,
            Init_SettingsInitFailed = 118,
            Init_ShuttingDown = 119,
            Init_TooManyObjects = 120,
            Init_NoServerForBackgroundApp = 121,
            Init_NotSupportedWithCompositor = 122,
            Init_NotAvailableToUtilityApps = 123,
            Init_Internal = 124,
            Init_HmdDriverIdIsNone = 125,
            Init_HmdNotFoundPresenceFailed = 126,
            Init_VRMonitorNotFound = 127,
            Init_VRMonitorStartupFailed = 128,
            Init_LowPowerWatchdogNotSupported = 129,
            Init_InvalidApplicationType = 130,
            Init_NotAvailableToWatchdogApps = 131,
            Init_WatchdogDisabledInSettings = 132,
            Init_VRDashboardNotFound = 133,
            Init_VRDashboardStartupFailed = 134,
            Init_VRHomeNotFound = 135,
            Init_VRHomeStartupFailed = 136,
            Driver_Failed = 200,
            Driver_Unknown = 201,
            Driver_HmdUnknown = 202,
            Driver_NotLoaded = 203,
            Driver_RuntimeOutOfDate = 204,
            Driver_HmdInUse = 205,
            Driver_NotCalibrated = 206,
            Driver_CalibrationInvalid = 207,
            Driver_HmdDisplayNotFound = 208,
            Driver_TrackedDeviceInterfaceUnknown = 209,
            Driver_HmdDriverIdOutOfBounds = 211,
            Driver_HmdDisplayMirrored = 212,
            IPC_ServerInitFailed = 300,
            IPC_ConnectFailed = 301,
            IPC_SharedStateInitFailed = 302,
            IPC_CompositorInitFailed = 303,
            IPC_MutexInitFailed = 304,
            IPC_Failed = 305,
            IPC_CompositorConnectFailed = 306,
            IPC_CompositorInvalidConnectResponse = 307,
            IPC_ConnectFailedAfterMultipleAttempts = 308,
            Compositor_Failed = 400,
            Compositor_D3D11HardwareRequired = 401,
            Compositor_FirmwareRequiresUpdate = 402,
            Compositor_OverlayInitFailed = 403,
            Compositor_ScreenshotsInitFailed = 404,
            Compositor_UnableToCreateDevice = 405,
            VendorSpecific_UnableToConnectToOculusRuntime = 1000,
            VendorSpecific_HmdFound_CantOpenDevice = 1101,
            VendorSpecific_HmdFound_UnableToRequestConfigStart = 1102,
            VendorSpecific_HmdFound_NoStoredConfig = 1103,
            VendorSpecific_HmdFound_ConfigTooBig = 1104,
            VendorSpecific_HmdFound_ConfigTooSmall = 1105,
            VendorSpecific_HmdFound_UnableToInitZLib = 1106,
            VendorSpecific_HmdFound_CantReadFirmwareVersion = 1107,
            VendorSpecific_HmdFound_UnableToSendUserDataStart = 1108,
            VendorSpecific_HmdFound_UnableToGetUserDataStart = 1109,
            VendorSpecific_HmdFound_UnableToGetUserDataNext = 1110,
            VendorSpecific_HmdFound_UserDataAddressRange = 1111,
            VendorSpecific_HmdFound_UserDataError = 1112,
            VendorSpecific_HmdFound_ConfigFailedSanityCheck = 1113,
            Steam_SteamInstallationNotFound = 2000,
        }

        public enum EDeviceActivityLevel
        {
            k_EDeviceActivityLevel_Unknown = -1,
            k_EDeviceActivityLevel_Idle = 0,
            k_EDeviceActivityLevel_UserInteraction = 1,
            k_EDeviceActivityLevel_UserInteraction_Timeout = 2,
            k_EDeviceActivityLevel_Standby = 3,
        }
        public enum EVREye
        {
            Eye_Left = 0,
            Eye_Right = 1,
        }
        public enum ETextureType
        {
            DirectX = 0,
            OpenGL = 1,
            Vulkan = 2,
            IOSurface = 3,
            DirectX12 = 4,
        }
        public enum EColorSpace
        {
            Auto = 0,
            Gamma = 1,
            Linear = 2,
        }
        public enum ETrackingResult
        {
            Uninitialized = 1,
            Calibrating_InProgress = 100,
            Calibrating_OutOfRange = 101,
            Running_OK = 200,
            Running_OutOfRange = 201,
        }
        public enum ETrackedDeviceClass
        {
            Invalid = 0,
            HMD = 1,
            Controller = 2,
            GenericTracker = 3,
            TrackingReference = 4,
            DisplayRedirect = 5,
        }
        public enum ETrackedControllerRole
        {
            Invalid = 0,
            LeftHand = 1,
            RightHand = 2,
        }
        public enum ETrackingUniverseOrigin
        {
            TrackingUniverseSeated = 0,
            TrackingUniverseStanding = 1,
            TrackingUniverseRawAndUncalibrated = 2,
        }

        public enum ETrackedDeviceProperty
        {
            Prop_Invalid = 0,
            Prop_TrackingSystemName_String = 1000,
            Prop_ModelNumber_String = 1001,
            Prop_SerialNumber_String = 1002,
            Prop_RenderModelName_String = 1003,
            Prop_WillDriftInYaw_Bool = 1004,
            Prop_ManufacturerName_String = 1005,
            Prop_TrackingFirmwareVersion_String = 1006,
            Prop_HardwareRevision_String = 1007,
            Prop_AllWirelessDongleDescriptions_String = 1008,
            Prop_ConnectedWirelessDongle_String = 1009,
            Prop_DeviceIsWireless_Bool = 1010,
            Prop_DeviceIsCharging_Bool = 1011,
            Prop_DeviceBatteryPercentage_Float = 1012,
            Prop_StatusDisplayTransform_Matrix34 = 1013,
            Prop_Firmware_UpdateAvailable_Bool = 1014,
            Prop_Firmware_ManualUpdate_Bool = 1015,
            Prop_Firmware_ManualUpdateURL_String = 1016,
            Prop_HardwareRevision_Uint64 = 1017,
            Prop_FirmwareVersion_Uint64 = 1018,
            Prop_FPGAVersion_Uint64 = 1019,
            Prop_VRCVersion_Uint64 = 1020,
            Prop_RadioVersion_Uint64 = 1021,
            Prop_DongleVersion_Uint64 = 1022,
            Prop_BlockServerShutdown_Bool = 1023,
            Prop_CanUnifyCoordinateSystemWithHmd_Bool = 1024,
            Prop_ContainsProximitySensor_Bool = 1025,
            Prop_DeviceProvidesBatteryStatus_Bool = 1026,
            Prop_DeviceCanPowerOff_Bool = 1027,
            Prop_Firmware_ProgrammingTarget_String = 1028,
            Prop_DeviceClass_Int32 = 1029,
            Prop_HasCamera_Bool = 1030,
            Prop_DriverVersion_String = 1031,
            Prop_Firmware_ForceUpdateRequired_Bool = 1032,
            Prop_ViveSystemButtonFixRequired_Bool = 1033,
            Prop_ParentDriver_Uint64 = 1034,
            Prop_ResourceRoot_String = 1035,
            Prop_ReportsTimeSinceVSync_Bool = 2000,
            Prop_SecondsFromVsyncToPhotons_Float = 2001,
            Prop_DisplayFrequency_Float = 2002,
            Prop_UserIpdMeters_Float = 2003,
            Prop_CurrentUniverseId_Uint64 = 2004,
            Prop_PreviousUniverseId_Uint64 = 2005,
            Prop_DisplayFirmwareVersion_Uint64 = 2006,
            Prop_IsOnDesktop_Bool = 2007,
            Prop_DisplayMCType_Int32 = 2008,
            Prop_DisplayMCOffset_Float = 2009,
            Prop_DisplayMCScale_Float = 2010,
            Prop_EdidVendorID_Int32 = 2011,
            Prop_DisplayMCImageLeft_String = 2012,
            Prop_DisplayMCImageRight_String = 2013,
            Prop_DisplayGCBlackClamp_Float = 2014,
            Prop_EdidProductID_Int32 = 2015,
            Prop_CameraToHeadTransform_Matrix34 = 2016,
            Prop_DisplayGCType_Int32 = 2017,
            Prop_DisplayGCOffset_Float = 2018,
            Prop_DisplayGCScale_Float = 2019,
            Prop_DisplayGCPrescale_Float = 2020,
            Prop_DisplayGCImage_String = 2021,
            Prop_LensCenterLeftU_Float = 2022,
            Prop_LensCenterLeftV_Float = 2023,
            Prop_LensCenterRightU_Float = 2024,
            Prop_LensCenterRightV_Float = 2025,
            Prop_UserHeadToEyeDepthMeters_Float = 2026,
            Prop_CameraFirmwareVersion_Uint64 = 2027,
            Prop_CameraFirmwareDescription_String = 2028,
            Prop_DisplayFPGAVersion_Uint64 = 2029,
            Prop_DisplayBootloaderVersion_Uint64 = 2030,
            Prop_DisplayHardwareVersion_Uint64 = 2031,
            Prop_AudioFirmwareVersion_Uint64 = 2032,
            Prop_CameraCompatibilityMode_Int32 = 2033,
            Prop_ScreenshotHorizontalFieldOfViewDegrees_Float = 2034,
            Prop_ScreenshotVerticalFieldOfViewDegrees_Float = 2035,
            Prop_DisplaySuppressed_Bool = 2036,
            Prop_DisplayAllowNightMode_Bool = 2037,
            Prop_DisplayMCImageWidth_Int32 = 2038,
            Prop_DisplayMCImageHeight_Int32 = 2039,
            Prop_DisplayMCImageNumChannels_Int32 = 2040,
            Prop_DisplayMCImageData_Binary = 2041,
            Prop_SecondsFromPhotonsToVblank_Float = 2042,
            Prop_DriverDirectModeSendsVsyncEvents_Bool = 2043,
            Prop_DisplayDebugMode_Bool = 2044,
            Prop_GraphicsAdapterLuid_Uint64 = 2045,
            Prop_AttachedDeviceId_String = 3000,
            Prop_SupportedButtons_Uint64 = 3001,
            Prop_Axis0Type_Int32 = 3002,
            Prop_Axis1Type_Int32 = 3003,
            Prop_Axis2Type_Int32 = 3004,
            Prop_Axis3Type_Int32 = 3005,
            Prop_Axis4Type_Int32 = 3006,
            Prop_ControllerRoleHint_Int32 = 3007,
            Prop_FieldOfViewLeftDegrees_Float = 4000,
            Prop_FieldOfViewRightDegrees_Float = 4001,
            Prop_FieldOfViewTopDegrees_Float = 4002,
            Prop_FieldOfViewBottomDegrees_Float = 4003,
            Prop_TrackingRangeMinimumMeters_Float = 4004,
            Prop_TrackingRangeMaximumMeters_Float = 4005,
            Prop_ModeLabel_String = 4006,
            Prop_IconPathName_String = 5000,
            Prop_NamedIconPathDeviceOff_String = 5001,
            Prop_NamedIconPathDeviceSearching_String = 5002,
            Prop_NamedIconPathDeviceSearchingAlert_String = 5003,
            Prop_NamedIconPathDeviceReady_String = 5004,
            Prop_NamedIconPathDeviceReadyAlert_String = 5005,
            Prop_NamedIconPathDeviceNotReady_String = 5006,
            Prop_NamedIconPathDeviceStandby_String = 5007,
            Prop_NamedIconPathDeviceAlertLow_String = 5008,
            Prop_DisplayHiddenArea_Binary_Start = 5100,
            Prop_DisplayHiddenArea_Binary_End = 5150,
            Prop_UserConfigPath_String = 6000,
            Prop_InstallPath_String = 6001,
            Prop_HasDisplayComponent_Bool = 6002,
            Prop_HasControllerComponent_Bool = 6003,
            Prop_HasCameraComponent_Bool = 6004,
            Prop_HasDriverDirectModeComponent_Bool = 6005,
            Prop_HasVirtualDisplayComponent_Bool = 6006,
            Prop_VendorSpecific_Reserved_Start = 10000,
            Prop_VendorSpecific_Reserved_End = 10999,
        }
        public enum ETrackedPropertyError
        {
            TrackedProp_Success = 0,
            TrackedProp_WrongDataType = 1,
            TrackedProp_WrongDeviceClass = 2,
            TrackedProp_BufferTooSmall = 3,
            TrackedProp_UnknownProperty = 4,
            TrackedProp_InvalidDevice = 5,
            TrackedProp_CouldNotContactServer = 6,
            TrackedProp_ValueNotProvidedByDevice = 7,
            TrackedProp_StringExceedsMaximumLength = 8,
            TrackedProp_NotYetAvailable = 9,
            TrackedProp_PermissionDenied = 10,
            TrackedProp_InvalidOperation = 11,
        }

        public enum EVRButtonId
        {
            k_EButton_System = 0,
            k_EButton_ApplicationMenu = 1,
            k_EButton_Grip = 2,
            k_EButton_DPad_Left = 3,
            k_EButton_DPad_Up = 4,
            k_EButton_DPad_Right = 5,
            k_EButton_DPad_Down = 6,
            k_EButton_A = 7,
            k_EButton_ProximitySensor = 31,
            k_EButton_Axis0 = 32,
            k_EButton_Axis1 = 33,
            k_EButton_Axis2 = 34,
            k_EButton_Axis3 = 35,
            k_EButton_Axis4 = 36,
            k_EButton_SteamVR_Touchpad = 32,
            k_EButton_SteamVR_Trigger = 33,
            k_EButton_Dashboard_Back = 2,
            k_EButton_Max = 64,
        }

        public enum EVRControllerAxisType
        {
            k_eControllerAxis_None = 0,
            k_eControllerAxis_TrackPad = 1,
            k_eControllerAxis_Joystick = 2,
            k_eControllerAxis_Trigger = 3,
        }

        public enum EVRFirmwareError
        {
            None = 0,
            Success = 1,
            Fail = 2,
        }

        public enum EVREventType
        {
            VREvent_None = 0,
            VREvent_TrackedDeviceActivated = 100,
            VREvent_TrackedDeviceDeactivated = 101,
            VREvent_TrackedDeviceUpdated = 102,
            VREvent_TrackedDeviceUserInteractionStarted = 103,
            VREvent_TrackedDeviceUserInteractionEnded = 104,
            VREvent_IpdChanged = 105,
            VREvent_EnterStandbyMode = 106,
            VREvent_LeaveStandbyMode = 107,
            VREvent_TrackedDeviceRoleChanged = 108,
            VREvent_WatchdogWakeUpRequested = 109,
            VREvent_LensDistortionChanged = 110,
            VREvent_PropertyChanged = 111,
            VREvent_ButtonPress = 200,
            VREvent_ButtonUnpress = 201,
            VREvent_ButtonTouch = 202,
            VREvent_ButtonUntouch = 203,
            VREvent_MouseMove = 300,
            VREvent_MouseButtonDown = 301,
            VREvent_MouseButtonUp = 302,
            VREvent_FocusEnter = 303,
            VREvent_FocusLeave = 304,
            VREvent_Scroll = 305,
            VREvent_TouchPadMove = 306,
            VREvent_OverlayFocusChanged = 307,
            VREvent_InputFocusCaptured = 400,
            VREvent_InputFocusReleased = 401,
            VREvent_SceneFocusLost = 402,
            VREvent_SceneFocusGained = 403,
            VREvent_SceneApplicationChanged = 404,
            VREvent_SceneFocusChanged = 405,
            VREvent_InputFocusChanged = 406,
            VREvent_SceneApplicationSecondaryRenderingStarted = 407,
            VREvent_HideRenderModels = 410,
            VREvent_ShowRenderModels = 411,
            VREvent_OverlayShown = 500,
            VREvent_OverlayHidden = 501,
            VREvent_DashboardActivated = 502,
            VREvent_DashboardDeactivated = 503,
            VREvent_DashboardThumbSelected = 504,
            VREvent_DashboardRequested = 505,
            VREvent_ResetDashboard = 506,
            VREvent_RenderToast = 507,
            VREvent_ImageLoaded = 508,
            VREvent_ShowKeyboard = 509,
            VREvent_HideKeyboard = 510,
            VREvent_OverlayGamepadFocusGained = 511,
            VREvent_OverlayGamepadFocusLost = 512,
            VREvent_OverlaySharedTextureChanged = 513,
            VREvent_DashboardGuideButtonDown = 514,
            VREvent_DashboardGuideButtonUp = 515,
            VREvent_ScreenshotTriggered = 516,
            VREvent_ImageFailed = 517,
            VREvent_DashboardOverlayCreated = 518,
            VREvent_RequestScreenshot = 520,
            VREvent_ScreenshotTaken = 521,
            VREvent_ScreenshotFailed = 522,
            VREvent_SubmitScreenshotToDashboard = 523,
            VREvent_ScreenshotProgressToDashboard = 524,
            VREvent_PrimaryDashboardDeviceChanged = 525,
            VREvent_Notification_Shown = 600,
            VREvent_Notification_Hidden = 601,
            VREvent_Notification_BeginInteraction = 602,
            VREvent_Notification_Destroyed = 603,
            VREvent_Quit = 700,
            VREvent_ProcessQuit = 701,
            VREvent_QuitAborted_UserPrompt = 702,
            VREvent_QuitAcknowledged = 703,
            VREvent_DriverRequestedQuit = 704,
            VREvent_ChaperoneDataHasChanged = 800,
            VREvent_ChaperoneUniverseHasChanged = 801,
            VREvent_ChaperoneTempDataHasChanged = 802,
            VREvent_ChaperoneSettingsHaveChanged = 803,
            VREvent_SeatedZeroPoseReset = 804,
            VREvent_AudioSettingsHaveChanged = 820,
            VREvent_BackgroundSettingHasChanged = 850,
            VREvent_CameraSettingsHaveChanged = 851,
            VREvent_ReprojectionSettingHasChanged = 852,
            VREvent_ModelSkinSettingsHaveChanged = 853,
            VREvent_EnvironmentSettingsHaveChanged = 854,
            VREvent_PowerSettingsHaveChanged = 855,
            VREvent_EnableHomeAppSettingsHaveChanged = 856,
            VREvent_StatusUpdate = 900,
            VREvent_MCImageUpdated = 1000,
            VREvent_FirmwareUpdateStarted = 1100,
            VREvent_FirmwareUpdateFinished = 1101,
            VREvent_KeyboardClosed = 1200,
            VREvent_KeyboardCharInput = 1201,
            VREvent_KeyboardDone = 1202,
            VREvent_ApplicationTransitionStarted = 1300,
            VREvent_ApplicationTransitionAborted = 1301,
            VREvent_ApplicationTransitionNewAppStarted = 1302,
            VREvent_ApplicationListUpdated = 1303,
            VREvent_ApplicationMimeTypeLoad = 1304,
            VREvent_ApplicationTransitionNewAppLaunchComplete = 1305,
            VREvent_ProcessConnected = 1306,
            VREvent_ProcessDisconnected = 1307,
            VREvent_Compositor_MirrorWindowShown = 1400,
            VREvent_Compositor_MirrorWindowHidden = 1401,
            VREvent_Compositor_ChaperoneBoundsShown = 1410,
            VREvent_Compositor_ChaperoneBoundsHidden = 1411,
            VREvent_TrackedCamera_StartVideoStream = 1500,
            VREvent_TrackedCamera_StopVideoStream = 1501,
            VREvent_TrackedCamera_PauseVideoStream = 1502,
            VREvent_TrackedCamera_ResumeVideoStream = 1503,
            VREvent_TrackedCamera_EditingSurface = 1550,
            VREvent_PerformanceTest_EnableCapture = 1600,
            VREvent_PerformanceTest_DisableCapture = 1601,
            VREvent_PerformanceTest_FidelityLevel = 1602,
            VREvent_MessageOverlay_Closed = 1650,
            VREvent_VendorSpecific_Reserved_Start = 10000,
            VREvent_VendorSpecific_Reserved_End = 19999,
        }

        public enum EHiddenAreaMeshType
        {
            k_eHiddenAreaMesh_Standard = 0,
            k_eHiddenAreaMesh_Inverse = 1,
            k_eHiddenAreaMesh_LineLoop = 2,
            k_eHiddenAreaMesh_Max = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IVRSystem
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _GetRecommendedRenderTargetSize(ref uint pnWidth, ref uint pnHeight);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetRecommendedRenderTargetSize GetRecommendedRenderTargetSize;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate HmdMatrix44_t _GetProjectionMatrix(EVREye eEye, float fNearZ, float fFarZ);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetProjectionMatrix GetProjectionMatrix;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _GetProjectionRaw(EVREye eEye, ref float pfLeft, ref float pfRight, ref float pfTop, ref float pfBottom);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetProjectionRaw GetProjectionRaw;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _ComputeDistortion(EVREye eEye, float fU, float fV, ref DistortionCoordinates_t pDistortionCoordinates);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _ComputeDistortion ComputeDistortion;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate HmdMatrix34_t _GetEyeToHeadTransform(EVREye eEye);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetEyeToHeadTransform GetEyeToHeadTransform;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _GetTimeSinceLastVsync(ref float pfSecondsSinceLastVsync, ref ulong pulFrameCounter);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetTimeSinceLastVsync GetTimeSinceLastVsync;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate int _GetD3D9AdapterIndex();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetD3D9AdapterIndex GetD3D9AdapterIndex;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _GetDXGIOutputInfo(ref int pnAdapterIndex);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetDXGIOutputInfo GetDXGIOutputInfo;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _GetOutputDevice(ref ulong pnDevice, ETextureType textureType);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetOutputDevice GetOutputDevice;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _IsDisplayOnDesktop();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _IsDisplayOnDesktop IsDisplayOnDesktop;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _SetDisplayVisibility(bool bIsVisibleOnDesktop);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _SetDisplayVisibility SetDisplayVisibility;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin eOrigin, float fPredictedSecondsToPhotonsFromNow, [In, Out] TrackedDevicePose_t[] pTrackedDevicePoseArray, uint unTrackedDevicePoseArrayCount);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetDeviceToAbsoluteTrackingPose GetDeviceToAbsoluteTrackingPose;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _ResetSeatedZeroPose();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _ResetSeatedZeroPose ResetSeatedZeroPose;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate HmdMatrix34_t _GetSeatedZeroPoseToStandingAbsoluteTrackingPose();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetSeatedZeroPoseToStandingAbsoluteTrackingPose GetSeatedZeroPoseToStandingAbsoluteTrackingPose;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate HmdMatrix34_t _GetRawZeroPoseToStandingAbsoluteTrackingPose();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetRawZeroPoseToStandingAbsoluteTrackingPose GetRawZeroPoseToStandingAbsoluteTrackingPose;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate uint _GetSortedTrackedDeviceIndicesOfClass(ETrackedDeviceClass eTrackedDeviceClass, [In, Out] uint[] punTrackedDeviceIndexArray, uint unTrackedDeviceIndexArrayCount, uint unRelativeToTrackedDeviceIndex);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetSortedTrackedDeviceIndicesOfClass GetSortedTrackedDeviceIndicesOfClass;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate EDeviceActivityLevel _GetTrackedDeviceActivityLevel(uint unDeviceId);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetTrackedDeviceActivityLevel GetTrackedDeviceActivityLevel;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _ApplyTransform(ref TrackedDevicePose_t pOutputPose, ref TrackedDevicePose_t pTrackedDevicePose, ref HmdMatrix34_t pTransform);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _ApplyTransform ApplyTransform;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate uint _GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole unDeviceType);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetTrackedDeviceIndexForControllerRole GetTrackedDeviceIndexForControllerRole;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate ETrackedControllerRole _GetControllerRoleForTrackedDeviceIndex(uint unDeviceIndex);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetControllerRoleForTrackedDeviceIndex GetControllerRoleForTrackedDeviceIndex;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate ETrackedDeviceClass _GetTrackedDeviceClass(uint unDeviceIndex);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetTrackedDeviceClass GetTrackedDeviceClass;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _IsTrackedDeviceConnected(uint unDeviceIndex);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _IsTrackedDeviceConnected IsTrackedDeviceConnected;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _GetBoolTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetBoolTrackedDeviceProperty GetBoolTrackedDeviceProperty;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate float _GetFloatTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetFloatTrackedDeviceProperty GetFloatTrackedDeviceProperty;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate int _GetInt32TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetInt32TrackedDeviceProperty GetInt32TrackedDeviceProperty;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate ulong _GetUint64TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetUint64TrackedDeviceProperty GetUint64TrackedDeviceProperty;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate HmdMatrix34_t _GetMatrix34TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetMatrix34TrackedDeviceProperty GetMatrix34TrackedDeviceProperty;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate uint _GetStringTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, System.Text.StringBuilder pchValue, uint unBufferSize, ref ETrackedPropertyError pError);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetStringTrackedDeviceProperty GetStringTrackedDeviceProperty;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate IntPtr _GetPropErrorNameFromEnum(ETrackedPropertyError error);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetPropErrorNameFromEnum GetPropErrorNameFromEnum;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _PollNextEvent(ref VREvent_t pEvent, uint uncbVREvent);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _PollNextEvent PollNextEvent;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _PollNextEventWithPose(ETrackingUniverseOrigin eOrigin, ref VREvent_t pEvent, uint uncbVREvent, ref TrackedDevicePose_t pTrackedDevicePose);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _PollNextEventWithPose PollNextEventWithPose;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate IntPtr _GetEventTypeNameFromEnum(EVREventType eType);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetEventTypeNameFromEnum GetEventTypeNameFromEnum;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate HiddenAreaMesh_t _GetHiddenAreaMesh(EVREye eEye, EHiddenAreaMeshType type);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetHiddenAreaMesh GetHiddenAreaMesh;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _GetControllerState(uint unControllerDeviceIndex, ref VRControllerState_t pControllerState, uint unControllerStateSize);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetControllerState GetControllerState;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _GetControllerStateWithPose(ETrackingUniverseOrigin eOrigin, uint unControllerDeviceIndex, ref VRControllerState_t pControllerState, uint unControllerStateSize, ref TrackedDevicePose_t pTrackedDevicePose);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetControllerStateWithPose GetControllerStateWithPose;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _TriggerHapticPulse(uint unControllerDeviceIndex, uint unAxisId, char usDurationMicroSec);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _TriggerHapticPulse TriggerHapticPulse;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate IntPtr _GetButtonIdNameFromEnum(EVRButtonId eButtonId);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetButtonIdNameFromEnum GetButtonIdNameFromEnum;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate IntPtr _GetControllerAxisTypeNameFromEnum(EVRControllerAxisType eAxisType);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _GetControllerAxisTypeNameFromEnum GetControllerAxisTypeNameFromEnum;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _CaptureInputFocus();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _CaptureInputFocus CaptureInputFocus;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _ReleaseInputFocus();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _ReleaseInputFocus ReleaseInputFocus;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate bool _IsInputFocusCapturedByAnotherProcess();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _IsInputFocusCapturedByAnotherProcess IsInputFocusCapturedByAnotherProcess;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate uint _DriverDebugRequest(uint unDeviceIndex, string pchRequest, string pchResponseBuffer, uint unResponseBufferSize);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _DriverDebugRequest DriverDebugRequest;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate EVRFirmwareError _PerformFirmwareUpdate(uint unDeviceIndex);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _PerformFirmwareUpdate PerformFirmwareUpdate;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _AcknowledgeQuit_Exiting();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _AcknowledgeQuit_Exiting AcknowledgeQuit_Exiting;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void _AcknowledgeQuit_UserPrompt();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            internal _AcknowledgeQuit_UserPrompt AcknowledgeQuit_UserPrompt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HmdMatrix34_t
        {
            public float m0; //float[3][4]
            public float m1;
            public float m2;
            public float m3;
            public float m4;
            public float m5;
            public float m6;
            public float m7;
            public float m8;
            public float m9;
            public float m10;
            public float m11;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct HmdVector3_t
        {
            public float v0; //float[3]
            public float v1;
            public float v2;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct TrackedDevicePose_t
        {
            public HmdMatrix34_t mDeviceToAbsoluteTracking;
            public HmdVector3_t vVelocity;
            public HmdVector3_t vAngularVelocity;
            public ETrackingResult eTrackingResult;
            [MarshalAs(UnmanagedType.I1)]
            public bool bPoseIsValid;
            [MarshalAs(UnmanagedType.I1)]
            public bool bDeviceIsConnected;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DistortionCoordinates_t
        {
            public float rfRed0; //float[2]
            public float rfRed1;
            public float rfGreen0; //float[2]
            public float rfGreen1;
            public float rfBlue0; //float[2]
            public float rfBlue1;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct VREvent_Data_t
        {
            [FieldOffset(0)]
            public VREvent_Reserved_t reserved;
            [FieldOffset(0)]
            public VREvent_Controller_t controller;
            [FieldOffset(0)]
            public VREvent_Mouse_t mouse;
            [FieldOffset(0)]
            public VREvent_Scroll_t scroll;
            [FieldOffset(0)]
            public VREvent_Process_t process;
            [FieldOffset(0)]
            public VREvent_Notification_t notification;
            [FieldOffset(0)]
            public VREvent_Overlay_t overlay;
            [FieldOffset(0)]
            public VREvent_Status_t status;
            [FieldOffset(0)]
            public VREvent_Ipd_t ipd;
            [FieldOffset(0)]
            public VREvent_Chaperone_t chaperone;
            [FieldOffset(0)]
            public VREvent_PerformanceTest_t performanceTest;
            [FieldOffset(0)]
            public VREvent_TouchPadMove_t touchPadMove;
            [FieldOffset(0)]
            public VREvent_SeatedZeroPoseReset_t seatedZeroPoseReset;
            [FieldOffset(0)]
            public VREvent_Screenshot_t screenshot;
            [FieldOffset(0)]
            public VREvent_ScreenshotProgress_t screenshotProgress;
            [FieldOffset(0)]
            public VREvent_ApplicationLaunch_t applicationLaunch;
            [FieldOffset(0)]
            public VREvent_EditingCameraSurface_t cameraSurface;
            [FieldOffset(0)]
            public VREvent_MessageOverlay_t messageOverlay;
            [FieldOffset(0)]
            public VREvent_Keyboard_t keyboard; // This has to be at the end due to a mono bug
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Controller_t
        {
            public uint button;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Mouse_t
        {
            public float x;
            public float y;
            public uint button;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Scroll_t
        {
            public float xdelta;
            public float ydelta;
            public uint repeatCount;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_TouchPadMove_t
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool bFingerDown;
            public float flSecondsFingerDown;
            public float fValueXFirst;
            public float fValueYFirst;
            public float fValueXRaw;
            public float fValueYRaw;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Notification_t
        {
            public ulong ulUserValue;
            public uint notificationId;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Process_t
        {
            public uint pid;
            public uint oldPid;
            [MarshalAs(UnmanagedType.I1)]
            public bool bForced;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Overlay_t
        {
            public ulong overlayHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Status_t
        {
            public uint statusState;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Keyboard_t
        {
            public byte cNewInput0, cNewInput1, cNewInput2, cNewInput3, cNewInput4, cNewInput5, cNewInput6, cNewInput7;
            public ulong uUserValue;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Ipd_t
        {
            public float ipdMeters;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Chaperone_t
        {
            public ulong m_nPreviousUniverse;
            public ulong m_nCurrentUniverse;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Reserved_t
        {
            public ulong reserved0;
            public ulong reserved1;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_PerformanceTest_t
        {
            public uint m_nFidelityLevel;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_SeatedZeroPoseReset_t
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool bResetBySystemMenu;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_Screenshot_t
        {
            public uint handle;
            public uint type;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_ScreenshotProgress_t
        {
            public float progress;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_ApplicationLaunch_t
        {
            public uint pid;
            public uint unArgsHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_EditingCameraSurface_t
        {
            public ulong overlayHandle;
            public uint nVisualMode;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_MessageOverlay_t
        {
            public uint unVRMessageOverlayResponse;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VREvent_t
        {
            public uint eventType;
            public uint trackedDeviceIndex;
            public float eventAgeSeconds;
            public VREvent_Data_t data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HiddenAreaMesh_t
        {
            public IntPtr pVertexData; // const struct vr::HmdVector2_t *
            public uint unTriangleCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VRControllerAxis_t
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VRControllerState_t
        {
            public uint unPacketNum;
            public ulong ulButtonPressed;
            public ulong ulButtonTouched;
            public VRControllerAxis_t rAxis0; //VRControllerAxis_t[5]
            public VRControllerAxis_t rAxis1;
            public VRControllerAxis_t rAxis2;
            public VRControllerAxis_t rAxis3;
            public VRControllerAxis_t rAxis4;
        }
        #endregion
    }
}