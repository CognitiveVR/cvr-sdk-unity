using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

namespace CognitiveVR
{
    public class ThreadGazePoint
    {
        public Vector3 WorldPoint;
        public bool IsLocal;
        public Vector3 LocalPoint;
        public Transform Transform; //ignored in thread
        public Matrix4x4 TransformMatrix; //set in update. used in thread
    }

    //IMPROVEMENT? try removing noisy outliers when creating new fixation points
    //https://stackoverflow.com/questions/3779763/fast-algorithm-for-computing-percentiles-to-remove-outliers
    //https://www.codeproject.com/Tips/602081/%2FTips%2F602081%2FStandard-Deviation-Extension-for-Enumerable

    [HelpURL("https://docs.cognitive3d.com/fixations/")]
    [AddComponentMenu("Cognitive3D/Common/Fixation Recorder")]
    public class FixationRecorder : MonoBehaviour
    {
        public static FixationRecorder Instance;
        private enum GazeRaycastResult
        {
            Invalid,
            HitNothing,
            HitWorld
        }

        #region EyeTracker

#if CVR_FOVE
        const int CachedEyeCaptures = 70; //FOVE
        Fove.Unity.FoveInterface fovebase;
        public bool CombinedWorldGazeRay(out Ray ray)
        {
            var r = fovebase.GetGazeRays();
            ray = new Ray((r.left.origin + r.right.origin) / 2, (r.left.direction + r.right.direction) / 2);
            return true;
        }

        public bool LeftEyeOpen() { return Fove.Unity.FoveManager.CheckEyesClosed() != Fove.Eye.Both && Fove.Unity.FoveManager.CheckEyesClosed() != Fove.Eye.Left; }
        public bool RightEyeOpen() { return Fove.Unity.FoveManager.CheckEyesClosed() != Fove.Eye.Both && Fove.Unity.FoveManager.CheckEyesClosed() != Fove.Eye.Right; }

        public long EyeCaptureTimestamp()
        {
            return (long)(Util.Timestamp() * 1000);
        }

        int lastProcessedFrame;
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (lastProcessedFrame != Time.frameCount)
            {
                lastProcessedFrame = Time.frameCount;
                return true;
            }
            return false;
        }
#elif CVR_TOBIIVR
        const int CachedEyeCaptures = 120; //TOBII
        Tobii.XR.IEyeTrackingProvider EyeTracker;
        Tobii.XR.TobiiXR_EyeTrackingData currentData;
        public bool CombinedWorldGazeRay(out Ray ray)
        {
            ray = new Ray();
            ray.origin = GameplayReferences.HMD.TransformPoint(currentData.GazeRay.Origin);
            ray.direction = GameplayReferences.HMD.TransformDirection(currentData.GazeRay.Direction);

            if (currentData.GazeRay.IsValid)
                Debug.DrawRay(ray.origin, ray.direction * 1000, Color.magenta, 5);

            return currentData.GazeRay.IsValid;
        }

        public bool LeftEyeOpen() { return !currentData.IsLeftEyeBlinking; }
        public bool RightEyeOpen() { return !currentData.IsRightEyeBlinking; }

        //unix timestamp of application start in milliseconds
        long tobiiStartTimestamp;
        
        private IEnumerator Start()
        {            
            while (EyeTracker == null)
            {
                yield return null;
                EyeTracker = Tobii.XR.TobiiXR.Internal.Provider;
                //is this fixation recorder component added immediately? or after cognitive session start?
            }
            tobiiStartTimestamp = CognitiveVR_Manager.Instance.StartupTimestampMilliseconds;
        }

        public long EyeCaptureTimestamp()
        {
            return tobiiStartTimestamp + (long)(currentData.Timestamp * 1000);
        }

        int lastProcessedFrame;
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (lastProcessedFrame != Time.frameCount)
            {
                currentData = Tobii.XR.TobiiXR.Internal.Provider.EyeTrackingDataLocal;
                lastProcessedFrame = Time.frameCount;
                return true;
            }
            return false;
        }
#elif CVR_PICONEO2EYE
        const int CachedEyeCaptures = 120; //PICO
        Pvr_UnitySDKAPI.EyeTrackingData data = new Pvr_UnitySDKAPI.EyeTrackingData();
        public bool CombinedWorldGazeRay(out Ray ray)
        {
            ray = new Ray();
            Pvr_UnitySDKAPI.EyeTrackingGazeRay gazeRay = new Pvr_UnitySDKAPI.EyeTrackingGazeRay();
            var t = Pvr_UnitySDKManager.SDK.HeadPose.Matrix;
            if (Pvr_UnitySDKAPI.System.UPvr_getEyeTrackingGazeRayWorld(ref gazeRay))
            {
                if (gazeRay.IsValid && gazeRay.Direction.sqrMagnitude > 0.1f)
                {
                    ray.direction = gazeRay.Direction;
                    ray.origin = gazeRay.Origin;
                    return true;
                }
            }
            return false;
        }

        public bool LeftEyeOpen()
        {
            Pvr_UnitySDKAPI.System.UPvr_getEyeTrackingData(ref data);
            return data.leftEyeOpenness > 0.5f;
        }
        public bool RightEyeOpen()
        {
            Pvr_UnitySDKAPI.System.UPvr_getEyeTrackingData(ref data);
            return data.rightEyeOpenness > 0.5f;
        }

        public long EyeCaptureTimestamp()
        {
            return (long)(CognitiveVR.Util.Timestamp() * 1000);
        }

        int lastProcessedFrame;
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (lastProcessedFrame != Time.frameCount)
            {
                lastProcessedFrame = Time.frameCount;
                return true;
            }
            return false;
        }
#elif CVR_VIVEPROEYE
        bool useDataQueue1;
        bool useDataQueue2;
        static Queue<ViveSR.anipal.Eye.EyeData> EyeDataQueue1;
        static Queue<ViveSR.anipal.Eye.EyeData_v2> EyeDataQueue2;
        const int CachedEyeCaptures = 120; //VIVEPROEYE
        ViveSR.anipal.Eye.EyeData currentData1;
        ViveSR.anipal.Eye.EyeData_v2 currentData2;

        static System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        double epochStart;

        //what exactly is this timestamp; since HMD enabled?
        long startTimestamp;

        void Start()
        {
            System.TimeSpan span = System.DateTime.UtcNow - epoch;
            epochStart = span.TotalSeconds;

            var framework = ViveSR.anipal.Eye.SRanipal_Eye_Framework.Instance;
            if (framework != null && framework.EnableEyeDataCallback)
            {
                if (framework.EnableEyeVersion == ViveSR.anipal.Eye.SRanipal_Eye_Framework.SupportedEyeVersion.version1)
                {
                    if (ViveSR.anipal.Eye.SRanipal_Eye_Framework.Status != ViveSR.anipal.Eye.SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                        ViveSR.anipal.Eye.SRanipal_Eye_Framework.Status != ViveSR.anipal.Eye.SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

                    if (ViveSR.anipal.Eye.SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true)
                    {
                        ViveSR.anipal.Eye.SRanipal_Eye.WrapperRegisterEyeDataCallback(System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((ViveSR.anipal.Eye.SRanipal_Eye.CallbackBasic)EyeCallback));
                        useDataQueue1 = true;
                        EyeDataQueue1 = new Queue<ViveSR.anipal.Eye.EyeData>(4);
                    }
                    else if (ViveSR.anipal.Eye.SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false)
                    {
                        ViveSR.anipal.Eye.SRanipal_Eye.WrapperUnRegisterEyeDataCallback(System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((ViveSR.anipal.Eye.SRanipal_Eye.CallbackBasic)EyeCallback));
                        useDataQueue1 = true;
                        EyeDataQueue1 = new Queue<ViveSR.anipal.Eye.EyeData>(4);
                    }
                }
                else
                {
                    if (ViveSR.anipal.Eye.SRanipal_Eye_Framework.Status != ViveSR.anipal.Eye.SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                        ViveSR.anipal.Eye.SRanipal_Eye_Framework.Status != ViveSR.anipal.Eye.SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

                    if (ViveSR.anipal.Eye.SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true)
                    {
                        ViveSR.anipal.Eye.SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((ViveSR.anipal.Eye.SRanipal_Eye_v2.CallbackBasic)EyeCallback2));
                        useDataQueue2 = true;
                        EyeDataQueue2 = new Queue<ViveSR.anipal.Eye.EyeData_v2>(4);
                    }
                    else if (ViveSR.anipal.Eye.SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false)
                    {
                        ViveSR.anipal.Eye.SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((ViveSR.anipal.Eye.SRanipal_Eye_v2.CallbackBasic)EyeCallback2));
                        useDataQueue2 = true;
                        EyeDataQueue2 = new Queue<ViveSR.anipal.Eye.EyeData_v2>(4);
                    }
                }
            }
        }

        private static void EyeCallback(ref ViveSR.anipal.Eye.EyeData eye_data)
        {
            EyeDataQueue1.Enqueue(eye_data);
        }

        private static void EyeCallback2(ref ViveSR.anipal.Eye.EyeData_v2 eye_data)
        {
            EyeDataQueue2.Enqueue(eye_data);
        }

        public bool CombinedWorldGazeRay(out Ray ray)
        {
            if (useDataQueue1)
            {
                var gazedir = currentData1.verbose_data.combined.eye_data.gaze_direction_normalized;
                gazedir.x *= -1;
                ray = new Ray(GameplayReferences.HMD.position, GameplayReferences.HMD.TransformDirection(gazedir));
                return currentData1.verbose_data.combined.eye_data.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);
            }
            if (useDataQueue2)
            {
                var gazedir = currentData2.verbose_data.combined.eye_data.gaze_direction_normalized;
                gazedir.x *= -1;
                ray = new Ray(GameplayReferences.HMD.position, GameplayReferences.HMD.TransformDirection(gazedir));
                return currentData2.verbose_data.combined.eye_data.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);
            }
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetGazeRay(ViveSR.anipal.Eye.GazeIndex.COMBINE,out ray))
            {
                ray.direction = GameplayReferences.HMD.TransformDirection(ray.direction);
                ray.origin = GameplayReferences.HMD.position;
                return true;
            }
            return false;
        }

        public bool LeftEyeOpen()
        {
            float openness = 0;
            if (useDataQueue1)
            {
                if (!currentData1.verbose_data.left.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY))
                    return false;
                return currentData1.verbose_data.left.eye_openness > 0.5f;
            }
            if (useDataQueue2)
            {
                if (!currentData2.verbose_data.left.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY))
                    return false;
                return currentData2.verbose_data.left.eye_openness > 0.5f;
            }
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetEyeOpenness(ViveSR.anipal.Eye.EyeIndex.LEFT, out openness))
            {
                return openness > 0.5f;
            }
            return false;
        }

        public bool RightEyeOpen()
        {
            float openness = 0;
            if (useDataQueue1)
            {
                if (!currentData1.verbose_data.right.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY))
                    return false;
                return currentData1.verbose_data.right.eye_openness > 0.5f;
            }
            if (useDataQueue2)
            {
                if (!currentData2.verbose_data.right.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY))
                    return false;
                return currentData2.verbose_data.right.eye_openness > 0.5f;
            }
            if (ViveSR.anipal.Eye.SRanipal_Eye.GetEyeOpenness(ViveSR.anipal.Eye.EyeIndex.RIGHT, out openness))
            {
                return openness > 0.5f;
            }
            return false;
        }

        public long EyeCaptureTimestamp()
        {
            if (useDataQueue1)
            {
                if (startTimestamp == 0)
                    startTimestamp = currentData1.timestamp;
                var MsSincestart = currentData1.timestamp - startTimestamp; //milliseconds since start
                var final = epochStart * 1000 + MsSincestart;
                return (long)final;
            }
            else if (useDataQueue2)
            {
                if (startTimestamp == 0)
                    startTimestamp = currentData1.timestamp;
                var MsSincestart = currentData1.timestamp - startTimestamp; //milliseconds since start
                var final = epochStart * 1000 + MsSincestart;
                return (long)final;
            }
            return (long)(Util.Timestamp() * 1000);
        }

        int lastProcessedFrame;
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (useDataQueue1)
            {
                if (EyeDataQueue1.Count > 0)
                {
                    currentData1 = EyeDataQueue1.Dequeue();
                    return true;
                }
                return false;
            }
            else if (useDataQueue2)
            {
                if (EyeDataQueue2.Count > 0)
                {
                    currentData2 = EyeDataQueue2.Dequeue();
                    return true;
                }
                return false;
            }

            if (lastProcessedFrame != Time.frameCount)
            {
                lastProcessedFrame = Time.frameCount;
                return true;
            }
            return false;
        }
#elif CVR_VARJO
        const int CachedEyeCaptures = 100; //VARJO

        Varjo.VarjoPlugin.GazeData currentData;
        static System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

        //raw time since computer restarted. in nanoseconds
        long startTimestamp;

        //start time since epoch. in seconds
        double epochStart;

        IEnumerator Start()
        {
            while (true)
            {
                if (Varjo.VarjoPlugin.InitGaze())
                {
                    startTimestamp = Varjo.VarjoPlugin.GetGaze().captureTime;
                    if (startTimestamp > 0)
                    {
                        System.TimeSpan span = System.DateTime.UtcNow - epoch;
                        epochStart = span.TotalSeconds;
                        break;
                    }
                }
                yield return null;
            }
        }

        public bool CombinedWorldGazeRay(out Ray ray)
        {
            ray = new Ray();
            if (Varjo.VarjoPlugin.InitGaze())
            {
                // Check if gaze data is valid and calibrated
                if (currentData.status != Varjo.VarjoPlugin.GazeStatus.INVALID)
                {
                    ray.direction = GameplayReferences.HMD.TransformDirection(new Vector3((float)currentData.gaze.forward[0], (float)currentData.gaze.forward[1], (float)currentData.gaze.forward[2]));
                    ray.origin = GameplayReferences.HMD.TransformPoint(new Vector3((float)currentData.gaze.position[0], (float)currentData.gaze.position[1], (float)currentData.gaze.position[2]));
                    return true;
                }
            }
            return false;
        }

        public bool LeftEyeOpen()
        {
            if (Varjo.VarjoPlugin.InitGaze())
                return currentData.leftStatus != Varjo.VarjoPlugin.GazeEyeStatus.EYE_INVALID;
            return false;
        }
        public bool RightEyeOpen()
        {
            if (Varjo.VarjoPlugin.InitGaze())
                return currentData.rightStatus != Varjo.VarjoPlugin.GazeEyeStatus.EYE_INVALID;
            return false;
        }

        public long EyeCaptureTimestamp()
        {
            //currentData.captureTime //nanoseconds. steady clock
            long sinceStart = currentData.captureTime - startTimestamp;
            sinceStart = (sinceStart / 1000000); //remove NANOSECONDS
            var final = epochStart * 1000 + sinceStart;
            return (long)final;
        }
        
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (Varjo.VarjoPlugin.InitGaze())
            {
                if (Varjo.VarjoPlugin.GetOldestGazeIfAvailable(ref currentData))
                {
                    return true;
                }
            }

            return false;
        }
#elif CVR_NEURABLE
        const int CachedEyeCaptures = 120;
        //public Ray CombinedWorldGazeRay() { return Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay(); }
        public bool CombinedWorldGazeRay(out Ray ray)
        {
            ray = Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay();
            return true;
        }

        //TODO neurable check eye state
        public bool LeftEyeOpen() { return true; }
        public bool RightEyeOpen() { return true; }

        static System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        public long EyeCaptureTimestamp()
        {
            //TODO return correct timestamp - might need to use Tobii implementation
            System.TimeSpan span = System.DateTime.UtcNow - epoch;
            return (long)(span.TotalSeconds * 1000);
        }

        int lastProcessedFrame;
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (lastProcessedFrame != Time.frameCount)
            {
                lastProcessedFrame = Time.frameCount;
                return true;
            }
            return false;
        }
#elif CVR_AH
        const int CachedEyeCaptures = 120; //ADHAWK
        private static Calibrator ah_calibrator;
        AdhawkApi.EyeTracker eyetracker;
        public bool CombinedWorldGazeRay(out Ray ray)
        {
            Vector3 r = ah_calibrator.GetGazeVector(filterType: AdhawkApi.Numerics.Filters.FilterType.ExponentialMovingAverage);
            Vector3 x = ah_calibrator.GetGazeOrigin();
            ray = new Ray(x, r);
            return true;
        }

        public bool LeftEyeOpen() { return eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingLeft || eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingBoth; }
        public bool RightEyeOpen() { return eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingRight || eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingBoth; }

        public long EyeCaptureTimestamp()
        {
            return (long)(Util.Timestamp() * 1000);
        }

        int lastProcessedFrame;
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (lastProcessedFrame != Time.frameCount)
            {
                lastProcessedFrame = Time.frameCount;
                return true;
            }
            return false;
        }
#elif CVR_PUPIL
        const int CachedEyeCaptures = 120; //PUPIL LABS
        public bool CombinedWorldGazeRay(out Ray ray)
        {
            //world gaze direction
            ray = currentData.GazeRay;
            return true;
        }

        public bool LeftEyeOpen() { return currentData.LeftEyeOpen; }
        public bool RightEyeOpen() { return currentData.RightEyeOpen; }

        public long EyeCaptureTimestamp()
        {
            return currentData.Timestamp;
        }

        PupilGazeData currentData;
        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (GazeDataQueue.Count > 0)
            {
                currentData = GazeDataQueue.Dequeue();
                return true;
            }
            return false;
        }
#else
        const int CachedEyeCaptures = 120; //UNKNOWN
        //public Ray CombinedWorldGazeRay() { return new Ray(); }
        public bool CombinedWorldGazeRay(out Ray ray){ray = new Ray(GameplayReferences.HMD.position,GameplayReferences.HMD.forward); return true;}

        public bool LeftEyeOpen() { return true; }
        public bool RightEyeOpen() { return true; }

        public long EyeCaptureTimestamp()
        {
            return (long)(Util.Timestamp() * 1000);
        }

        //returns true if there is another data point to work on
        int lastProcessedFrame;
        public bool GetNextData()
        {
            if (lastProcessedFrame != Time.frameCount)
            {
                lastProcessedFrame = Time.frameCount;
                return true;
            }
            return false;
        }
#endif

        #endregion

        //if active fixation is world space, in world space, this indicates the last several positions for the average fixation position
        //if active fixation is local space, these are in local space
        List<Vector3> CachedEyeCapturePositions = new List<Vector3>();

        int index = 0;
        int GetIndex(int offset)
        {
            if (index + offset < 0)
                return (CachedEyeCaptures + index + offset) % CachedEyeCaptures;
            return (index + offset) % CachedEyeCaptures;
        }

        //DEBUG ONLY
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public EyeCapture GetLastEyeCapture()
        {
            return EyeCaptures[index];
        }
#endif

        private EyeCapture[] EyeCaptures = new EyeCapture[CachedEyeCaptures];
        private List<Fixation> Fixations = new List<Fixation>();

        public bool IsFixating { get; set; }
        public Fixation ActiveFixation;

        [Header("Blink")]
        [Tooltip("the maximum amount of time that can be assigned as a single 'blink'. if eyes are closed for longer than this, assume that the user is consciously closing their eyes")]
        public int MaxBlinkMs = 400;
        [Tooltip("when a blink occurs, ignore gaze preceding the blink up to this far back in time")]
        public int PreBlinkDiscardMs = 20;
        [Tooltip("after a blink has ended, ignore gaze up to this long afterwards")]
        public int BlinkEndWarmupMs = 100;
        //the most recent time user has stopped blinking
        long EyeUnblinkTime;
        bool eyesClosed;

        [Header("Fixation")]
        [Tooltip("the time that gaze must be within the max fixation angle before a fixation begins")]
        public int MinFixationMs = 60;
        [Tooltip("the amount of time gaze can be discarded before a fixation is ended. gaze can be discarded if eye tracking values are outside of expected ranges")]
        public int MaxConsecutiveDiscardMs = 10;
        [Tooltip("the angle that a number of gaze samples must fall within to start a fixation event")]
        public float MaxFixationAngle = 1;
        [Tooltip("amount of time gaze can be off the transform before fixation ends. mostly useful when fixation is right on the edge of a dynamic object")]
        public int MaxConsecutiveOffDynamicMs = 500;
        [Tooltip("multiplier for SMOOTH PURSUIT fixation angle size on dynamic objects. helps reduce incorrect fixation ending")]
        public float DynamicFixationSizeMultiplier = 1.25f;
        [Tooltip("increases the size of the fixation angle as gaze gets toward the edge of the viewport. this is used to reduce the number of incorrectly ended fixations because of hardware limits at the edge of the eye tracking field of view")]
        public AnimationCurve FocusSizeFromCenter;

        [Header("Saccade")]
        [Tooltip("amount of consecutive eye samples before a fixation ends as the eye fixates elsewhere")]
        public int SaccadeFixationEndMs = 10;

        //[Header("Visualization")]
        public CircularBuffer<ThreadGazePoint> DisplayGazePoints = new CircularBuffer<ThreadGazePoint>(4096);
        
        GameObject lastEyeTrackingPointer;

        bool WasCaptureDiscardedLastFrame = false; //ensures at least 1 frame is discarded before ending fixations
        bool WasOutOfDispersionLastFrame = false; //ensures at least 1 frame is out of fixation dispersion cone before ending fixation


        void Reset()
        {
            FocusSizeFromCenter = new AnimationCurve();
            FocusSizeFromCenter.AddKey(new Keyframe(0.01f, 1, 0, 0));
            FocusSizeFromCenter.AddKey(new Keyframe(0.5f, 2, 5, 0));
        }

        void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Util.logWarning("Multiple Fixation Recorders found!");
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            if (FocusSizeFromCenter == null) { Reset(); }

            IsFixating = false;
            ActiveFixation = new Fixation();

            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                EyeCaptures[i] = new EyeCapture() { Discard = true };
            }
#if CVR_FOVE
            fovebase = GameplayReferences.FoveInstance;
#elif CVR_AH
            ah_calibrator = Calibrator.Instance;
            eyetracker = EyeTracker.Instance;
#elif CVR_PUPIL
            gazeController = FindObjectOfType<PupilLabs.GazeController>();
            if (gazeController != null)
                gazeController.OnReceive3dGaze += ReceiveEyeData;
            else
                Debug.LogError("Pupil Labs GazeController is null!");
#endif
        }

#if CVR_PUPIL

        PupilLabs.GazeController gazeController;

        void ReceiveEyeData(PupilLabs.GazeData data)
        {
            if (data.Confidence < 0.6f)
            {
                return;
            }
            PupilGazeData pgd = new PupilGazeData();

            pgd.Timestamp = (long)(Util.Timestamp() * 1000);
            pgd.LeftEyeOpen = data.IsEyeDataAvailable(1);
            pgd.RightEyeOpen = data.IsEyeDataAvailable(0);
            pgd.GazeRay = new Ray(GameplayReferences.HMD.position, GameplayReferences.HMD.TransformDirection(data.GazeDirection));
            GazeDataQueue.Enqueue(pgd);
        }

        Queue<PupilGazeData> GazeDataQueue = new Queue<PupilGazeData>(8);
        class PupilGazeData
        {
            public long Timestamp;
            public Ray GazeRay;
            public bool LeftEyeOpen;
            public bool RightEyeOpen;
        }

        private void OnDisable()
        {
            gazeController.OnReceive3dGaze -= ReceiveEyeData;
        }
#endif

        private void Update()
        {
            if (!Core.IsInitialized) { return; }
            if (GameplayReferences.HMD == null) { CognitiveVR.Util.logWarning("HMD is null! Fixation will not function"); return; }

            PostGazeCallback();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (lastEyeTrackingPointer == null) { return; }
            if (IsFixating)
            {
                lastEyeTrackingPointer.GetComponent<MeshRenderer>().material.SetColor("g_vOutlineColor", ActiveFixation.IsLocal ? Color.red : Color.cyan);
            }
            else
            {
                lastEyeTrackingPointer.GetComponent<MeshRenderer>().material.SetColor("g_vOutlineColor", Color.white);
            }
#endif
        }

        //assuming command buffer will send a callback, use this when the command buffer is ready to read
        void PostGazeCallback()
        {
            while (GetNextData())
            {
                RecordEyeCapture();
            }
        }

        void RecordEyeCapture()
        {
            //check for new fixation
            //else check for ending fixation
            //update eyecapture state

            if (!IsFixating)
            {
                //check if this is the start of a new fixation. set this and all next captures to this
                //the 'current' fixation we're checking is 1 second behind recording eye captures

                if (TryBeginLocalFixation(index))
                {
                    //FixationTransform set in TryBeginLocalFixation
                    IsFixating = true;
                    //Debug.Log("BEGIN LOCAL FIXATION " + ActiveFixation.StartMs);
                }
                else
                {
                    if (TryBeginFixation(index))
                    {
                        //FixationTransform = null;
                        IsFixating = true;
                    }
                }
            }
            else
            {
                //check if eye capture is valid to append to fixation
                //check if fixation 

                //update if eye capture is out of range
                //compared here since we don't necessarily know how fixation has changed since last capture
                //this is relative to the fixation, not an absolute that could be changed since recording
                bool IsOutOfRange = IsGazeOutOfRange(EyeCaptures[index]);
                if (!IsOutOfRange)
                {
                    //ActiveFixation.DurationMs = EyeCaptures[index].Time - ActiveFixation.StartMs;
                }
                else
                {
                    EyeCaptures[index].OutOfRange = true;
                }

                //update if eye capture is freshly off a transform (eg, if eye capture not on active fixation's dynamic object)
                EyeCaptures[index].OffTransform = IsFixatingOffTransform(EyeCaptures[index]);

                ActiveFixation.AddEyeCapture(EyeCaptures[index]);

                if (ActiveFixation.IsLocal)
                {
                    Vector3 hitWorld = EyeCaptures[index].LocalPosition;
                    Debug.DrawRay(hitWorld, Vector3.right, Color.red, 1);
                    Debug.DrawRay(hitWorld, Vector3.forward, Color.blue, 1);
                    Debug.DrawRay(hitWorld, Vector3.up, Color.green, 1);
                }

                if (CheckEndFixation(ActiveFixation))
                {
                    FixationCore.RecordFixation(ActiveFixation);

                    IsFixating = false;

                    if (ActiveFixation.IsLocal)
                    {
                        ActiveFixation.LocalTransform = null;
                    }
                    CachedEyeCapturePositions.Clear();
                }
                WasOutOfDispersionLastFrame = IsOutOfRange;
            }

            WasCaptureDiscardedLastFrame = EyeCaptures[index].Discard;
            

            //reset all values
            EyeCaptures[index].Discard = false;
            EyeCaptures[index].SkipPositionForFixationAverage = false;
            EyeCaptures[index].OffTransform = true;
            EyeCaptures[index].OutOfRange = false;
            EyeCaptures[index].HitDynamicId = string.Empty;

            bool areEyesClosed = AreEyesClosed();

#if CVR_PUPIL
            // discard gaze point if confidence too low. WILL THIS CONFLICT WITH BLINKING?
            //EyeCaptures[index].Discard = PupilTools.FloatFromDictionary(PupilTools.gazeDictionary, "confidence") < 0.5f;
#endif
#if CVR_TOBIIVR
            // discard gaze point if direction from either eye is invalid. WILL THIS CONFLICT WITH BLINKING?
            //EyeCaptures[index].Discard = currentData.Right.GazeDirectionValid && currentData.Left.GazeDirectionValid ? false : true;
#endif
#if CVR_AH
            //EyeCaptures[index].Discard = eyetracker.CurrentTrackingState == EyeTracker.TrackingState.TrackingUnknown || (eyetracker.CurrentTrackingState == EyeTracker.TrackingState.TrackingLost && !areEyesClosed);
#endif

            //set new current values
            EyeCaptures[index].EyesClosed = areEyesClosed;
            EyeCaptures[index].HmdPosition = GameplayReferences.HMD.position;
            EyeCaptures[index].Time = EyeCaptureTimestamp();
            if (EyeCaptures[index].EyesClosed) { EyeCaptures[index].SkipPositionForFixationAverage = true; }

            Vector3 world;

            DynamicObject hitDynamic = null;

            var hitresult = GazeRaycast(out world, out hitDynamic);

            if (hitresult == GazeRaycastResult.HitWorld)
            {
                //hit something as expected
                EyeCaptures[index].WorldPosition = world;
                EyeCaptures[index].ScreenPos = GameplayReferences.HMDCameraComponent.WorldToScreenPoint(world);

                if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                    DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();

                if (hitDynamic != null)
                {
                    EyeCaptures[index].UseCaptureMatrix = true;
                    //TODO test that this matrix is correct if dynamic is parented to offset/rotated/scaled transform
                    EyeCaptures[index].CaptureMatrix = Matrix4x4.TRS(hitDynamic.transform.localPosition, hitDynamic.transform.localRotation, hitDynamic.transform.localScale);
                    EyeCaptures[index].HitDynamicId = hitDynamic.GetId();

                    EyeCaptures[index].LocalPosition = hitDynamic.transform.InverseTransformPoint(world);
                    DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = EyeCaptures[index].WorldPosition;
                    DisplayGazePoints[DisplayGazePoints.Count].IsLocal = true;
                    DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = EyeCaptures[index].LocalPosition;
                    DisplayGazePoints[DisplayGazePoints.Count].Transform = hitDynamic.transform;
                    EyeCaptures[index].OffTransform = false;
                }
                else
                {
                    EyeCaptures[index].UseCaptureMatrix = false;

                    DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = EyeCaptures[index].WorldPosition;
                    DisplayGazePoints[DisplayGazePoints.Count].IsLocal = false;
                    EyeCaptures[index].OffTransform = false;
                    EyeCaptures[index].HitDynamicId = string.Empty;
                }
            }
            else if (hitresult == GazeRaycastResult.HitNothing)
            {
                //eye capture world point could be used for getting the direction, but position is invalid (on skybox)
                EyeCaptures[index].SkipPositionForFixationAverage = true;
                EyeCaptures[index].UseCaptureMatrix = false;

                EyeCaptures[index].WorldPosition = world;

                if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                    DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();
                DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = world;
                DisplayGazePoints[DisplayGazePoints.Count].IsLocal = false;
                EyeCaptures[index].ScreenPos = GameplayReferences.HMDCameraComponent.WorldToScreenPoint(world);
                EyeCaptures[index].OffTransform = true;
            }
            else if (hitresult == GazeRaycastResult.Invalid)
            {
                EyeCaptures[index].SkipPositionForFixationAverage = true;
                EyeCaptures[index].UseCaptureMatrix = false;
                EyeCaptures[index].Discard = true;
                EyeCaptures[index].OffTransform = true;
            }

            if (float.IsNaN(world.x) || float.IsNaN(world.y) || float.IsNaN(world.z)) { }
            else if (lastEyeTrackingPointer != null){ lastEyeTrackingPointer.transform.position = world; } //turned invalid somewhere

            if (areEyesClosed || EyeCaptures[index].Discard) { }
            else
            {
                DisplayGazePoints.Update();
            }
            index = (index + 1) % CachedEyeCaptures;
        }

        //the position in the world/local hit. returns true if hit something
        GazeRaycastResult GazeRaycast(out Vector3 world, out CognitiveVR.DynamicObject hitDynamic)
        {
            RaycastHit hit = new RaycastHit();
            Ray combinedWorldGaze;
            bool validRay = CombinedWorldGazeRay(out combinedWorldGaze);
            if (!validRay) { hitDynamic = null; world = Vector3.zero; return GazeRaycastResult.Invalid; }
            if (Physics.Raycast(combinedWorldGaze, out hit, 1000f, CognitiveVR_Preferences.Instance.GazeLayerMask, QueryTriggerInteraction.Ignore))
            {
                world = hit.point;

                if (CognitiveVR_Preferences.S_DynamicObjectSearchInParent)
                {
                    hitDynamic = hit.collider.GetComponentInParent<DynamicObject>();
                }
                else
                {
                    hitDynamic = hit.collider.GetComponent<DynamicObject>();
                }
                return GazeRaycastResult.HitWorld;
            }
            else
            {
                world = combinedWorldGaze.GetPoint(Mathf.Min(100, GameplayReferences.HMDCameraComponent.farClipPlane));
                hitDynamic = null;
                return GazeRaycastResult.HitNothing;
            }
        }

        /// <summary>
        /// are eyes closed this capture?
        /// if they begin being closed, discard the last PreBlinkDiscard MS
        /// if they become opened, still discard next BlinkEndWarmup MS
        /// </summary>
        /// <returns></returns>
        bool AreEyesClosed()
        {
            if (!LeftEyeOpen() && !RightEyeOpen()) //eyes are closed / begin blink?
            {
                //when blinking started, discard previous eye capture
                if (!eyesClosed)
                {
                    //iterate through eye captures. if current time - preblinkms > capture time, discard
                    for (int i = 0; i < CachedEyeCaptures; i++)
                    {
                        if (EyeCaptures[index].Time - PreBlinkDiscardMs > EyeCaptures[GetIndex(-i)].Time)
                        {
                            EyeCaptures[GetIndex(-i)].EyesClosed = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                eyesClosed = true;
                return true;
            }

            if (eyesClosed && LeftEyeOpen() && RightEyeOpen()) //end blink
            {
                //when blinking ended, discard next couple eye captures
                eyesClosed = false;
                EyeUnblinkTime = EyeCaptures[index].Time;
            }

            if (EyeUnblinkTime + BlinkEndWarmupMs > EyeCaptures[index].Time)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// for local fixations, is this eye capture hitting the active fixation dynamic object
        /// for either fixation return true if looking at sky
        /// </summary>
        /// <param name="capture"></param>
        /// <returns></returns>
        bool IsFixatingOffTransform(EyeCapture capture)
        {
            if (ActiveFixation.IsLocal)
            {
                if (!capture.UseCaptureMatrix)
                {
                    //Debug.Log("OFF TRANSFORM don't use matrix");
                    return true;
                }
                if (capture.HitDynamicId != ActiveFixation.DynamicObjectId)
                {
                    //Debug.Log("OFF TRANSFORM ids don't match " + capture.HitDynamicId + "  active fixation id " +ActiveFixation.DynamicObjectId);
                    return true;
                }
            }
            if (capture.OffTransform)
            {
                //Debug.Log("OFF TRANSFORM already off transform from sky or invalid capture");
                return true;
            }
            return false;
        }

        /// <summary>
        /// returns true if gaze is NOT within active fixation
        /// updates average fixation position
        /// removes old smooth pursuit eye captures points (DynamicRollingAverageMS)
        /// </summary>
        /// <param name="capture"></param>
        /// <returns></returns>
        bool IsGazeOutOfRange(EyeCapture capture)
        {
            if (!IsFixating) { return true; }

            if (ActiveFixation.IsLocal) //local fixations need to update world position based on local eye captures
            {
                if (capture.UseCaptureMatrix == false)
                {
                    capture.SkipPositionForFixationAverage = true;
                }
                else if (capture.HitDynamicId != ActiveFixation.DynamicObjectId) //if hit a dynamic, BUT NOT THIS DYNAMIC, don't update position
                {
                    capture.SkipPositionForFixationAverage = true;
                }

                if (capture.SkipPositionForFixationAverage || capture.OffTransform)
                {
                    //TODO compare matrix * local position instead of world position
                    var _fixationWorldPosition = ActiveFixation.WorldPosition;
                    var _fixationDirection = (_fixationWorldPosition - capture.HmdPosition).normalized;
                    var _eyeCaptureWorldPos = capture.WorldPosition;
                    var _eyeCaptureDirection = (_eyeCaptureWorldPos - capture.HmdPosition).normalized;
                    var _screendist = Vector2.Distance(capture.ScreenPos, Vector3.one * 0.5f);
                    var _rescale = FocusSizeFromCenter.Evaluate(_screendist);
                    var _adjusteddotangle = Mathf.Cos(MaxFixationAngle * _rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);
                    if (Vector3.Dot(_eyeCaptureDirection, _fixationDirection) < _adjusteddotangle)
                    {
                        return true;
                    }
                }
                else
                {
                    //should use transform matrix from when eye capture was captured instead of world position
                    //using the capture's matrix against the active fixation matrix. this will be 1 frame behind?
                    //TEST HERE compare matrix * local position instead of world position
                    var captureWorldPos = capture.CaptureMatrix.MultiplyPoint(capture.LocalPosition);
                    var activeFixationWorldPos = capture.CaptureMatrix.MultiplyPoint(ActiveFixation.LocalPosition);
                    //Debug.DrawLine(ActiveFixation.LocalPosition, activeFixationWorldPos);
                    //Debug.Log("ActiveFixation.LocalPosition: " + ActiveFixation.LocalPosition + " activeFixationWorldPos: " + activeFixationWorldPos);

                    //if in range, we will add captureWorldPos to CachedEyeCapturePositions and update activefixation.localposition
                    //then update average position then check angle
                    Vector3 averagelocalpos = Vector3.zero;
                    foreach (var v in CachedEyeCapturePositions)
                    {
                        averagelocalpos += v;
                    }
                    averagelocalpos += capture.LocalPosition;
                    averagelocalpos /= (CachedEyeCapturePositions.Count + 1);                                        

                    var _fixationWorldPosition = activeFixationWorldPos;
                    var _fixationDirection = (_fixationWorldPosition - capture.HmdPosition).normalized;
                    var _eyeCaptureWorldPos = captureWorldPos;
                    var _eyeCaptureDirection = (_eyeCaptureWorldPos - capture.HmdPosition).normalized;
                    var _screendist = Vector2.Distance(capture.ScreenPos, Vector3.one * 0.5f);
                    var _rescale = FocusSizeFromCenter.Evaluate(_screendist);
                    var _adjusteddotangle = Mathf.Cos(MaxFixationAngle * _rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);
                    float dot = Vector3.Dot(_eyeCaptureDirection, _fixationDirection);
                    //Debug.DrawLine(_fixationWorldPosition,_eyeCaptureWorldPos,Color.black);
                    
                    if (dot < _adjusteddotangle)
                    {
                        return true;
                    }

                    float distance = Vector3.Magnitude(_fixationWorldPosition - capture.HmdPosition);
                    float currentRadius = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;
                    ActiveFixation.MaxRadius = Mathf.Max(ActiveFixation.MaxRadius, currentRadius);
                    //Debug.Log("hmd position: "+capture.HmdPosition + " fixation world position: "+ _fixationWorldPosition  + " distance: " + distance);

                    CachedEyeCapturePositions.Add(capture.LocalPosition);

                    if (CachedEyeCapturePositions.Count > 10) //IMPROVEMENT cache eye captures based on time, not on count
                        CachedEyeCapturePositions.RemoveAt(0);
                    ActiveFixation.LocalPosition = averagelocalpos;

                    //Debug.Log("UPDATE LOCAL POSITION by " + capture.LocalPosition + "        new fixation position " + averagelocalpos);
                }
                ActiveFixation.LastInRange = capture.Time;
                return false;
            }
            else
            {
                var screendist = Vector2.Distance(capture.ScreenPos, Vector3.one * 0.5f);
                var rescale = FocusSizeFromCenter.Evaluate(screendist);
                var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * Mathf.Deg2Rad);
                if (capture.SkipPositionForFixationAverage || capture.OffTransform) //eye capture is invalid (probably from looking at skybox)
                {
                    Vector3 lookDir = (capture.WorldPosition - capture.HmdPosition).normalized;
                    Vector3 fixationDir = (ActiveFixation.WorldPosition - capture.HmdPosition).normalized;

                    if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                    {
                        return true;
                    }
                    else
                    {
                        //look at skybox. not necessarily out of fixation range
                    }
                }
                else
                {
                    //TODO if hit dynamic, don't update position
                    Vector3 averageworldpos = Vector3.zero;
                    foreach (var v in CachedEyeCapturePositions)
                    {
                        averageworldpos += v;
                    }
                    averageworldpos += capture.WorldPosition;
                    averageworldpos /= (CachedEyeCapturePositions.Count + 1);


                    Vector3 lookDir = (capture.WorldPosition - capture.HmdPosition).normalized;
                    Vector3 fixationDir = (averageworldpos - capture.HmdPosition).normalized;

                    if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                    {
                        return true;
                    }

                    float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - capture.HmdPosition);
                    float currentRadius = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;
                    ActiveFixation.MaxRadius = Mathf.Max(ActiveFixation.MaxRadius, currentRadius);

                    CachedEyeCapturePositions.Add(capture.WorldPosition);
                    ActiveFixation.WorldPosition = averageworldpos;
                }
            }
            return false;
        }

        /// <summary>
        /// returns true if the fixation should be ended. copies fixation for visualization
        /// </summary>
        /// <param name="testFixation"></param>
        /// <returns></returns>
        bool CheckEndFixation(Fixation testFixation)
        {
            //check for blinking too long
            if (EyeCaptures[index].Time > testFixation.LastEyesOpen + MaxBlinkMs)
            {
                FixationCore.FixationRecordEvent(testFixation);
                //Debug.LogError("END FIXATION BLINK " + EyeCaptures[index].Time);
                return true;
            }

            //check for general discarding. maybe HMD issue, just a bunch of null data or some other issue
            if (EyeCaptures[index].Time > testFixation.LastNonDiscardedTime + MaxConsecutiveDiscardMs)
            {
                if (!WasCaptureDiscardedLastFrame)
                {
                }
                else
                {
                    FixationCore.FixationRecordEvent(testFixation);
                    //Debug.LogError("END FIXATION DISCARD " + EyeCaptures[index].Time);
                    return true;
                }
            }

            //check for out of fixation point range
            if (EyeCaptures[index].Time > testFixation.LastInRange + SaccadeFixationEndMs)
            {
                if (!WasOutOfDispersionLastFrame)
                {
                    //out of range dispersion threshold the previous frame
                }
                else
                {
                    //Debug.LogError("END FIXATION RANGE " + EyeCaptures[index].Time + " from position " + ActiveFixation.LocalPosition);
                    FixationCore.FixationRecordEvent(testFixation);
                    return true;
                }
            }

            //if not looking at transform for a while, end fixation
            if (EyeCaptures[index].Time > testFixation.LastOnTransform + MaxConsecutiveOffDynamicMs)
            {
                FixationCore.FixationRecordEvent(testFixation);
                //Debug.LogError("END FIXATION TRANSFORM " + EyeCaptures[index].Time);
                return true;
            }

            return false;
        }

        /// <summary>
        /// returns true if 'active fixation' is actually active again
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        bool TryBeginLocalFixation(int index)
        {
            int samples = 0;
            List<string> hitDynamicIds = new List<string>();
            long firstOnTransformTime = 0;

            List<EyeCapture> usedCaptures = new List<EyeCapture>();

            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                if (EyeCaptures[GetIndex(i)].Discard || EyeCaptures[GetIndex(i)].EyesClosed) { return false; }
                if (EyeCaptures[GetIndex(i)].SkipPositionForFixationAverage)
                {
                    if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time) { break; }
                    continue;
                }
                samples++;
                usedCaptures.Add(EyeCaptures[GetIndex(i)]);
                if (firstOnTransformTime < 1)
                    firstOnTransformTime = EyeCaptures[GetIndex(i)].Time;
                if (EyeCaptures[GetIndex(i)].UseCaptureMatrix)
                {
                    hitDynamicIds.Add(EyeCaptures[GetIndex(i)].HitDynamicId);
                }
                if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time) { break; }
            }

            //check that there are any valid eye captures
            if (samples == 0)
            {
                return false;
            }

            if (EyeCaptures[index].Time - firstOnTransformTime > MaxConsecutiveOffDynamicMs)
            {
                //SATURDAY is this correct? should this start a fixation technically off a transform, if it is relatively stable and will eventually hit a transform
                    //ANSWER IS NO!!! fixation will possibly fail again shortly after first fixation point hits transform
                    //THEN AGAIN MAYBE?? even if first fixation point hits transform, who knows if this fixation will be stable enough to 'survive' on the transform, even for the minimum time
                        //this could lead to incorrectly high number of fixations continually falling off the edge of an object
                //TODO SOLUTION must check that off transform times are valid all through starting

                //fail now! off transform time will fail this before getting to first on transform time
                //otherwise, fixation steady enough to be called a fixation and surface point will 'eventually' be valid
                return false;
            }


            //================================= CALCULATE HIT DYNAMIC IDS
            int hitTransformCount = 0;
            Dictionary<string, int> hitCounts = new Dictionary<string, int>();

            Vector3 averageLocalPosition = Vector3.zero;
            Vector3 averageWorldPosition = Vector3.zero;
            foreach (var v in usedCaptures)
            {
                if (v.UseCaptureMatrix)
                {
                    if (hitCounts.ContainsKey(v.HitDynamicId))
                    {
                        hitCounts[v.HitDynamicId]++;
                    }
                    else
                    {
                        hitCounts.Add(v.HitDynamicId, 1);
                    }
                    hitTransformCount++;
                    if (!v.SkipPositionForFixationAverage)
                    {
                        //figure out average fixation positions
                        averageWorldPosition += v.WorldPosition;
                        averageLocalPosition += v.LocalPosition;
                    }
                }
            }
            
            //escape if no eye captures are using dynamic object transform matrix (this is possibly redundant)
            if (hitTransformCount == 0)
            {
                return false;
            }
            
            //======= figure out most used DynamicObjectId
            int usecount = 0;
            string mostUsedId = null;
            foreach (var v in hitCounts)
            {
                if (v.Value > usecount)
                {
                    mostUsedId = v.Key;
                    usecount = v.Value;
                }
            }

            if (string.IsNullOrEmpty(mostUsedId))
            {
                //most used dynamic object id is none! something is wrong somehow
                return false;
            }

            //======== average positions and check if fixations are within radius
            averageLocalPosition /= usedCaptures.Count;
            averageWorldPosition /= usedCaptures.Count;
            
            var screendist = Vector2.Distance(EyeCaptures[index].ScreenPos, Vector3.one * 0.5f);
            var rescale = FocusSizeFromCenter.Evaluate(screendist);
            var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);

            //use captures that hit the most common dynamic to figure out fixation start point
            //then use all captures world position to check if within fixation radius
            bool withinRadius = true;
            for (int i = 0; i < samples; i++)
            {
                Vector3 lookDir = (EyeCaptures[GetIndex(i)].HmdPosition - EyeCaptures[GetIndex(i)].WorldPosition).normalized;
                Vector3 fixationDir = (EyeCaptures[GetIndex(i)].HmdPosition - averageWorldPosition).normalized;

                if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                {
                    withinRadius = false;
                    break;
                }
            }

            if (withinRadius)
            {
                //all eye captures within fixation radius. save transform, set ActiveFixation start time and world position
                ActiveFixation.LocalPosition = averageLocalPosition;
                ActiveFixation.WorldPosition = averageWorldPosition;
                ActiveFixation.DynamicObjectId = mostUsedId;

                float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - EyeCaptures[index].HmdPosition);
                float opposite = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;

                ActiveFixation.StartDistance = distance;
                ActiveFixation.MaxRadius = opposite;
                ActiveFixation.StartMs = EyeCaptures[index].Time;
                ActiveFixation.LastOnTransform = EyeCaptures[index].Time;
                ActiveFixation.LastEyesOpen = EyeCaptures[index].Time;
                ActiveFixation.LastNonDiscardedTime = EyeCaptures[index].Time;
                ActiveFixation.LastInRange = EyeCaptures[index].Time;
                ActiveFixation.IsLocal = true;
                foreach(var c in usedCaptures)
                {
                    if (c.UseCaptureMatrix && c.HitDynamicId == ActiveFixation.DynamicObjectId)
                    {
                        CachedEyeCapturePositions.Add(c.LocalPosition);
                    }
                }
                Debug.LogWarning("BEIGN LOCAL FIXATION on id "+ActiveFixation.DynamicObjectId+"      time:" + EyeCaptures[index].Time + "    local position:" + averageLocalPosition);
                return true;
            }
            else
            {
                //something out of range from average world pos
                return false;
            }
        }

        //checks the NEXT eyecaptures to see if we should start a fixation
        bool TryBeginFixation(int index)
        {
            Vector3 averageWorldPos = Vector3.zero;
            //number of eye captures on a surface
            int averageWorldSamples = 0;
            int sampleCount = 0;

            long firstOnTransformTime = 0;
            long lastSampleTime = 0;
            long startECTime = EyeCaptures[index].Time;

            //take all the eye captures within the minimum fixation duration
            //escape if any are eyes closed or discarded captures
            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                if (EyeCaptures[GetIndex(i)].Discard || EyeCaptures[GetIndex(i)].EyesClosed) { return false; }
                if (EyeCaptures[GetIndex(i)].SkipPositionForFixationAverage)
                {
                    //eye capture should be skipped (look at sky, discarded). also check if out of min fixation time
                    if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time){break;}
                    continue;
                }

                sampleCount++;
                lastSampleTime = EyeCaptures[GetIndex(i)].Time;
                if (firstOnTransformTime < 1)
                    firstOnTransformTime = EyeCaptures[GetIndex(i)].Time;
                //TODO should use EyeCaptures.LocalPosition * EyeCaptures.Matrix. world position will be offset if object is moving
                averageWorldPos += EyeCaptures[GetIndex(i)].WorldPosition;
                averageWorldSamples++;
                if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time)
                {
                    break;
                }
            }
            
            if (sampleCount == 0)
            {
                //no samples were within time
                return false;
            }
            
            if (averageWorldSamples == 0)
            {
                //no samples hit any objects, so can't calculate fixation position
                return false;
            }
            averageWorldPos /= averageWorldSamples;

            //TODO what is the time span between samples - how does 1 sample think it's enough time to make a world fixation??
            //a fixation MUST start on a transform. alternatively could mark transform as 'on transform' early
            //IMPROVEMENT set fixation as starting now if MaxOffTransformMS < time to first point with transform
            //if (EyeCaptures[index].OffTransform) { return false; }

            if (EyeCaptures[index].Time - firstOnTransformTime > MaxConsecutiveOffDynamicMs)
            {
                //fail now! off transform time will fail this before getting to first on transform time
                //otherwise, fixation steady enough to be called a fixation and surface point will 'eventually' be valid
                return false;
            }
            
            bool withinRadius = true;

            //get starting screen position to compare other eye capture points against
            //var screenpos = GameplayReferences.HMDCameraComponent.WorldToViewportPoint(EyeCaptures[index].WorldPosition);
            var screendist = Vector2.Distance(EyeCaptures[index].ScreenPos, Vector3.one * 0.5f);
            var rescale = FocusSizeFromCenter.Evaluate(screendist);
            var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * Mathf.Deg2Rad);

            //check that each sample is within the fixation radius
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 lookDir = (EyeCaptures[GetIndex(i)].HmdPosition - EyeCaptures[GetIndex(i)].WorldPosition).normalized;
                Vector3 fixationDir = (EyeCaptures[GetIndex(i)].HmdPosition - averageWorldPos).normalized;

                if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                {
                    withinRadius = false;
                    break;
                }
            }

            if (withinRadius)
            {
                //all points are within the fixation radius. set ActiveFixation start time and save the each world position for used eye capture points
                ActiveFixation.WorldPosition = averageWorldPos;

                float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - EyeCaptures[index].HmdPosition);
                float opposite = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;

                ActiveFixation.StartMs = EyeCaptures[index].Time;
                ActiveFixation.LastInRange = ActiveFixation.StartMs;
                ActiveFixation.StartDistance = distance;
                ActiveFixation.MaxRadius = opposite;
                ActiveFixation.LastEyesOpen = EyeCaptures[index].Time;
                ActiveFixation.LastNonDiscardedTime = EyeCaptures[index].Time;
                ActiveFixation.LastInRange = EyeCaptures[index].Time;
                ActiveFixation.LastOnTransform = firstOnTransformTime;
                ActiveFixation.IsLocal = false;
                for (int i = 0; i < sampleCount; i++)
                {
                    if (EyeCaptures[GetIndex(i)].SkipPositionForFixationAverage) { continue; }
                    CachedEyeCapturePositions.Add(EyeCaptures[GetIndex(i)].WorldPosition);
                }
                return true;
            }
            else
            {
                //something out of range from average world pos
                return false;
            }
        }
    }
}