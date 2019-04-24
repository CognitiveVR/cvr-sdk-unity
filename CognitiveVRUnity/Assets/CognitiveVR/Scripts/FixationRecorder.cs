using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

namespace CognitiveVR
{
    //TODO try removing noisy outliers when creating new fixation points
    //https://stackoverflow.com/questions/3779763/fast-algorithm-for-computing-percentiles-to-remove-outliers
    //https://www.codeproject.com/Tips/602081/%2FTips%2F602081%2FStandard-Deviation-Extension-for-Enumerable

    [HelpURL("https://docs.cognitive3d.com/fixations/")]
    [AddComponentMenu("Cognitive3D/Common/Fixation Recorder")]
    public class FixationRecorder : MonoBehaviour
    {
        #region EyeTracker

#if CVR_FOVE
        const int CachedEyeCaptures = 70; //FOVE
        FoveInterfaceBase fovebase;
        public Ray CombinedWorldGazeRay()
        {
            var r = fovebase.GetGazeRays();
            return new Ray((r.left.origin + r.right.origin) / 2, (r.left.direction + r.right.direction) / 2);
        }

        public bool LeftEyeOpen() { return fovebase.CheckEyesClosed() != Fove.Managed.EFVR_Eye.Left && fovebase.CheckEyesClosed() != Fove.Managed.EFVR_Eye.Both; }
        public bool RightEyeOpen() { return fovebase.CheckEyesClosed() != Fove.Managed.EFVR_Eye.Right && fovebase.CheckEyesClosed() != Fove.Managed.EFVR_Eye.Both; }

        public long EyeCaptureTimestamp()
        {
            return (long)(Time.realtimeSinceStartup * 1000);
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
        Tobii.Research.Unity.VREyeTracker EyeTracker;
        Tobii.Research.Unity.IVRGazeData currentData;
        public Ray CombinedWorldGazeRay() { return currentData.CombinedGazeRayWorld; }

        public bool LeftEyeOpen() { return currentData.Left.PupilDiameterValid && currentData.Left.PupilPosiitionInTrackingAreaValid; }
        public bool RightEyeOpen() { return currentData.Right.PupilDiameterValid && currentData.Right.PupilPosiitionInTrackingAreaValid; }

        static System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

        //raw time since computer restarted. in nanoseconds
        long tobiiStartTimestamp;
        
        //start time since epoch. in seconds
        double epochtobiistart;

        private IEnumerator Start()
        {
            if (EyeTracker == null)
                EyeTracker = FindObjectOfType<Tobii.Research.Unity.VREyeTracker>();

            while (true)
            {
                tobiiStartTimestamp = EyeTracker.LatestGazeData.TimeStamp;
                if (tobiiStartTimestamp > 0)
                {
                    System.TimeSpan span = System.DateTime.UtcNow - epoch;
                    epochtobiistart = span.TotalSeconds;
                    break;
                }
                yield return null;
            }
        }

        public long EyeCaptureTimestamp()
        {
            long sinceStart = currentData.TimeStamp - tobiiStartTimestamp;
            sinceStart = (sinceStart / 1000); //remove microseconds
            var final = epochtobiistart * 1000 + sinceStart;
            return (long)final;
        }

        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            if (EyeTracker.GazeDataCount > 0)
            {
                currentData = EyeTracker.NextData;
                return true;
            }
            return false;
        }
#elif CVR_NEURABLE
        const int CachedEyeCaptures = 120;
        public Ray CombinedWorldGazeRay() { return Neurable.Core.NeurableUser.Instance.NeurableCam.GazeRay(); }

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
        public Ray CombinedWorldGazeRay()
        {
            Vector3 r = ah_calibrator.GetGazeVector(filterType: AdhawkApi.Numerics.Filters.FilterType.ExponentialMovingAverage);
            Vector3 x = ah_calibrator.GetGazeOrigin();
            return new Ray(x, r);
        }

        public bool LeftEyeOpen() { return eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingLeft || eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingBoth; }
        public bool RightEyeOpen() { return eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingRight || eyetracker.CurrentTrackingState == AdhawkApi.EyeTracker.TrackingState.TrackingBoth; }

        public long EyeCaptureTimestamp()
        {
            return (long)(Time.realtimeSinceStartup * 1000);
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
#else
        const int CachedEyeCaptures = 120; //UNKNOWN
        public Ray CombinedWorldGazeRay() { return new Ray(); }

        public bool LeftEyeOpen() { return false; }
        public bool RightEyeOpen() { return false; }

        static System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

        public long EyeCaptureTimestamp()
        {
            System.TimeSpan span = System.DateTime.UtcNow - epoch;
            return (long)(span.TotalSeconds * 1000);
        }

        //returns true if there is another data point to work on
        public bool GetNextData()
        {
            return false;
        }
#endif

        #endregion

        //used as world or local positions, depending whether fixation is world or local, for updating fixation position
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

        public EyeCapture[] EyeCaptures = new EyeCapture[CachedEyeCaptures];
        public List<Fixation> Fixations = new List<Fixation>();

        public bool IsFixating { get; set; }
        public Fixation ActiveFixation;

        [Header("Blink")]
        [Tooltip("the maximum amount of time that can be assigned as a single 'blink'. if eyes are closed for longer than this, assume that the user is conciously closing their eyes")]
        public int MaxBlinkMs = 400;
        [Tooltip("when a blink occurs, ignore gaze preceding the blink up to this far back in time")]
        public int PreBlinkDiscardMs = 20;
        [Tooltip("after a blink has ended, ignore gaze up to this long afterwards")]
        public int BlinkEndWarmupMs = 100;
        //the most recent time user has stopped blinking
        long EyeUnblinkTime;
        bool eyesClosed;

        [Header("Fixation")]
        [Tooltip("the time that gaze must be within the max fixation angle before a fixation occurs")]
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

        Camera HMDCam;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        [Header("Debug (Editor Only)")]
        public List<Vector3> VISGazepoints = new List<Vector3>(4096);
        public Dictionary<string, List<Fixation>> VISFixationEnds = new Dictionary<string, List<Fixation>>();

        //visualization
        //shoudl use gaze reticle or something??
        GameObject lastEyeTrackingPointer;
        public Material DebugMaterial;
#endif

        //cognitive3d stuff
        //CognitiveVR.CommandBufferHelper commandBufferHelper;

        void Reset()
        {
            FocusSizeFromCenter = new AnimationCurve();
            FocusSizeFromCenter.AddKey(new Keyframe(0.01f, 1, 0, 0));
            FocusSizeFromCenter.AddKey(new Keyframe(0.5f, 2, 5, 0));
        }

        public void Initialize()
        {
            if (FocusSizeFromCenter == null) { Reset(); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            VISFixationEnds.Add("discard", new List<Fixation>());
            VISFixationEnds.Add("out of range", new List<Fixation>());
            VISFixationEnds.Add("microsleep", new List<Fixation>());
            VISFixationEnds.Add("off transform", new List<Fixation>());

            var viewer = FindObjectOfType<FixationVisualizer>();
            if (viewer != null)
                viewer.SetTarget(this);
            var saccade = FindObjectOfType<SaccadeDrawer>();
            if (saccade != null)
                saccade.SetTarget(this);
            //gameObject.AddComponent<FixationVisualizer>().SetTarget(this);
            lastEyeTrackingPointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lastEyeTrackingPointer.transform.localScale = Vector3.one * 0.2f;
            lastEyeTrackingPointer.GetComponent<MeshRenderer>().material = DebugMaterial;
            Destroy(lastEyeTrackingPointer.GetComponent<SphereCollider>());
#endif

            ActiveFixation = new Fixation();

            HMDCam = Camera.main;
            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                EyeCaptures[i] = new EyeCapture() { Discard = true };
            }
#if CVR_FOVE
            fovebase = FindObjectOfType<FoveInterfaceBase>();
#elif CVR_TOBIIVR
            if (EyeTracker == null)
                EyeTracker = FindObjectOfType<Tobii.Research.Unity.VREyeTracker>();
#elif CVR_AH
            ah_calibrator = Calibrator.Instance;
            eyetracker = EyeTracker.Instance;
#endif
        }

        private void Update()
        {
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
            //check for ending fixation
            //update eyecapture state

            if (!IsFixating)
            {
                //check if this is the start of a new fixation. set this and all next captures to this
                //the 'current' fixation we're checking is 1 second behind recoding eye captures

                if (TryBeginLocalFixation(index))
                {
                    ActiveFixation.IsLocal = true;
                    //FixationTransform set in TryBeginLocalFixation
                    IsFixating = true;
                }
                else
                {
                    if (TryBeginFixation())
                    {
                        ActiveFixation.IsLocal = false;
                        //FixationTransform = null;
                        IsFixating = true;
                    }
                }
            }
            else
            {
                //center is about 0.01
                //off screen is ~0.3

                EyeCaptures[index].OffTransform = IsGazeOffTransform(EyeCaptures[index]);
                EyeCaptures[index].OutOfRange = IsGazeOutOfRange(EyeCaptures[index]);
                ActiveFixation.AddEyeCapture(EyeCaptures[index]);

                ActiveFixation.DurationMs = EyeCaptures[index].Time - ActiveFixation.StartMs;

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
            }

            //reset all values
            EyeCaptures[index].Discard = false;
            EyeCaptures[index].SkipPositionForFixationAverage = false;
            EyeCaptures[index].OffTransform = false;
            EyeCaptures[index].OutOfRange = false;

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
            EyeCaptures[index].HmdPosition = HMDCam.transform.position;
            EyeCaptures[index].Time = EyeCaptureTimestamp();

            Vector3 world;

            DynamicObject hitDynamic = null;

            if (GazeRaycast(out world, out hitDynamic))
            {
                //hit something as expected
                EyeCaptures[index].WorldPosition = world;

                if (hitDynamic != null)
                {
                    EyeCaptures[index].HitDynamicTransform = hitDynamic.transform;
                    EyeCaptures[index].LocalPosition = hitDynamic.transform.InverseTransformPoint(world);
                }
                else
                    EyeCaptures[index].HitDynamicTransform = null;
            }
            else
            {
                //eye capture world point could be used for getting the direction, but position is invalid (on skybox)
                EyeCaptures[index].SkipPositionForFixationAverage = true;
                EyeCaptures[index].HitDynamicTransform = null;
                EyeCaptures[index].WorldPosition = world;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (float.IsNaN(world.x) || float.IsNaN(world.y) || float.IsNaN(world.z)) { }
            else{ lastEyeTrackingPointer.transform.position = world; } //turned invalid somewhere

            VISGazepoints.Add(EyeCaptures[index].WorldPosition);
#endif
            index = (index + 1) % CachedEyeCaptures;
        }

        //the position in the world/local hit. returns true if valid
        bool GazeRaycast(out Vector3 world, out CognitiveVR.DynamicObject hitDynamic)
        {
            world = Vector3.zero;
            hitDynamic = null;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(CombinedWorldGazeRay(), out hit, CognitiveVR_Preferences.Instance.GazeLayerMask))
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
                return true;
            }
            else
            {
                world = CombinedWorldGazeRay().GetPoint(Mathf.Min(100, HMDCam.farClipPlane));
                return false;
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
        /// for a smooth pursuit fixation, is transform
        /// </summary>
        /// <param name="capture"></param>
        /// <returns></returns>
        bool IsGazeOffTransform(EyeCapture capture)
        {
            if (!IsFixating) { return true; }

            if (capture.HitDynamicTransform != ActiveFixation.LocalTransform) { return true; }

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

            if (ActiveFixation.IsLocal)
            {
                if (ActiveFixation.LocalTransform == null) { return true; }
                var screenpos = HMDCam.WorldToViewportPoint(capture.WorldPosition);
                if (capture.SkipPositionForFixationAverage || capture.OffTransform)
                {
                    var _fixationWorldPosition = ActiveFixation.LocalTransform.TransformPoint(ActiveFixation.LocalPosition);
                    var _fixationDirection = (_fixationWorldPosition - capture.HmdPosition).normalized;
                    
                    var _eyeCaptureWorldPos = ActiveFixation.LocalTransform.TransformPoint(capture.LocalPosition);
                    var _eyeCaptureDirection = (_eyeCaptureWorldPos - capture.HmdPosition).normalized;
                    var _eyeCaptureScreenPos = HMDCam.WorldToViewportPoint(_eyeCaptureWorldPos);

                    var _screendist = Vector2.Distance(_eyeCaptureScreenPos, Vector3.one * 0.5f);
                    var _rescale = FocusSizeFromCenter.Evaluate(_screendist);
                    var _adjusteddotangle = Mathf.Cos(MaxFixationAngle * _rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);
                    if (Vector3.Dot(_eyeCaptureDirection, _fixationDirection) < _adjusteddotangle)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    //try to move fixation to include this capture too
                    Vector3 averagelocalpos = Vector3.zero;
                    foreach (var v in CachedEyeCapturePositions)
                    {
                        averagelocalpos += v;
                    }
                    averagelocalpos += capture.LocalPosition;
                    averagelocalpos /= (CachedEyeCapturePositions.Count + 1);

                    var _fixationWorldPosition = ActiveFixation.LocalTransform.TransformPoint(averagelocalpos);
                    var _fixationDirection = (_fixationWorldPosition - capture.HmdPosition).normalized;

                    var _eyeCaptureWorldPos = ActiveFixation.LocalTransform.TransformPoint(capture.LocalPosition);
                    var _eyeCaptureDirection = (_eyeCaptureWorldPos - capture.HmdPosition).normalized;
                    var _eyeCaptureScreenPos = HMDCam.WorldToViewportPoint(_eyeCaptureWorldPos);

                    var _screendist = Vector2.Distance(_eyeCaptureScreenPos, Vector3.one * 0.5f);
                    var _rescale = FocusSizeFromCenter.Evaluate(_screendist);
                    var _adjusteddotangle = Mathf.Cos(MaxFixationAngle * _rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);
                    if (Vector3.Dot(_eyeCaptureDirection, _fixationDirection) < _adjusteddotangle)
                    {
                        return true;
                    }

                    float distance = Vector3.Magnitude(_fixationWorldPosition - capture.HmdPosition);
                    float currentRadius = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;
                    ActiveFixation.MaxRadius = Mathf.Max(ActiveFixation.MaxRadius, currentRadius);

                    CachedEyeCapturePositions.Add(capture.LocalPosition);
                    ActiveFixation.LocalPosition = averagelocalpos;

                    return false;
                }
            }
            else
            {
                var screenpos = HMDCam.WorldToViewportPoint(capture.WorldPosition);
                var screendist = Vector2.Distance(screenpos, Vector3.one * 0.5f);
                var rescale = FocusSizeFromCenter.Evaluate(screendist);
                var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * Mathf.Deg2Rad);
                if (capture.SkipPositionForFixationAverage) //eye capture is invalid (probably from looking at skybox)
                {
                    Vector3 lookDir = (capture.WorldPosition - capture.HmdPosition).normalized;
                    Vector3 fixationDir = (ActiveFixation.WorldPosition - capture.HmdPosition).normalized;

                    if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                    {
                        return true;
                    }
                    else
                    {
                        //looking at skybox. will not update fixation radius
                    }
                }
                else
                {
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                VISFixationEnds["microsleep"].Add(new Fixation(testFixation));
#endif
                return true;
            }

            //check for general discarding
            if (EyeCaptures[index].Time > testFixation.LastNonDiscardedTime + MaxConsecutiveDiscardMs)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                VISFixationEnds["discard"].Add(new Fixation(testFixation));
#endif
                //HMD issue, just a bunch of null data or some other issue
                return true;
            }

            //check for out of fixation point range
            if (EyeCaptures[index].Time > testFixation.LastInRange + SaccadeFixationEndMs)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                VISFixationEnds["out of range"].Add(new Fixation(testFixation));
#endif
                return true;
            }

            if (ActiveFixation.IsLocal)
            {
                //if not looking at transform for a while, end fixation
                if (EyeCaptures[index].Time > testFixation.LastOnTransform + MaxConsecutiveOffDynamicMs)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    VISFixationEnds["off transform"].Add(new Fixation(testFixation));
#endif
                    return true;
                }

                //check that the transform still exists
                if (ActiveFixation.LocalTransform == null) return true;
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
            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time) { break; }
                if (EyeCaptures[GetIndex(i)].Discard || EyeCaptures[GetIndex(i)].EyesClosed) { return false; }
                samples++;
            }

            Transform mostUsed = null;

            Transform[] hitTransforms = new Transform[samples];

            for (int i = 0; i < samples; i++)
            {
                if (EyeCaptures[GetIndex(i)].HitDynamicTransform != null)
                {
                    hitTransforms[i] = EyeCaptures[GetIndex(i)].HitDynamicTransform;
                }
            }

            int hitTransformCount = 0;
            for (int i = 0; i < hitTransforms.Length; i++)
            {
                if (hitTransforms[i] != null)
                    hitTransformCount++;
            }

            if (hitTransformCount == 0) { return false; } //didn't hit any valid dynamic objects

            //TODO replace with 2 arrays or something
            Dictionary<Transform, int> hitCounts = new Dictionary<Transform, int>();

            for (int i = 0; i < hitTransforms.Length; i++)
            {
                if (EyeCaptures[GetIndex(i)].HitDynamicTransform != null)
                {
                    if (hitCounts.ContainsKey(EyeCaptures[GetIndex(i)].HitDynamicTransform))
                    {
                        hitCounts[EyeCaptures[GetIndex(i)].HitDynamicTransform]++;
                    }
                    else
                    {
                        hitCounts.Add(EyeCaptures[GetIndex(i)].HitDynamicTransform, 1);
                    }
                }
            }

            int usecount = 0;
            foreach (var v in hitCounts)
            {
                if (v.Value > usecount)
                {
                    mostUsed = v.Key;
                    usecount = v.Value;
                }
            }

            Vector3 averageLocalPosition = Vector3.zero;
            for (int i = 0; i < samples; i++)
            {
                averageLocalPosition += EyeCaptures[GetIndex(i)].LocalPosition;
            }

            averageLocalPosition /= samples;

            var screenpos = HMDCam.WorldToViewportPoint(EyeCaptures[index].WorldPosition);
            var screendist = Vector2.Distance(screenpos, Vector3.one * 0.5f);
            var rescale = FocusSizeFromCenter.Evaluate(screendist);
            var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);

            //use captures that hit the most common dynamic to figure out fixation start point
            //then use all captures world position to check if within fixation radius
            bool withinRadius = true;
            for (int i = 0; i < samples; i++)
            {
                Vector3 lookDir = EyeCaptures[GetIndex(i)].HmdPosition - EyeCaptures[GetIndex(i)].LocalPosition;
                Vector3 fixationDir = EyeCaptures[GetIndex(i)].HmdPosition - averageLocalPosition;

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
                ActiveFixation.WorldPosition = mostUsed.TransformPoint(averageLocalPosition);
                Debug.DrawRay(ActiveFixation.WorldPosition, Vector3.up * 0.5f, Color.red, 3);
                ActiveFixation.DynamicObjectId = mostUsed.GetComponent<DynamicObject>().Id;

                float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - EyeCaptures[index].HmdPosition);
                float opposite = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;

                ActiveFixation.StartDistance = distance;
                ActiveFixation.MaxRadius = opposite;
                ActiveFixation.StartMs = EyeCaptures[index].Time;
                ActiveFixation.DebugScale = opposite;
                ActiveFixation.LocalTransform = mostUsed;

                return true;
            }
            else
            {
                //something out of range from average world pos
                return false;
            }
        }

        //checks the NEXT eyecaptures to see if we should start a fixation
        bool TryBeginFixation()
        {
            Vector3 averageWorldPos = Vector3.zero;
            int averageWorldSamples = 0;
            int sampleCount = 0;

            //take all the eye captures within the minimum fixation duration
            //escape if any are eyes closed or discarded captures
            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time) { break; }

                if (EyeCaptures[GetIndex(i)].Discard || EyeCaptures[GetIndex(i)].EyesClosed) { return false; }
                sampleCount++;
                if (EyeCaptures[GetIndex(i)].SkipPositionForFixationAverage) { continue; }

                averageWorldPos += EyeCaptures[GetIndex(i)].WorldPosition;
                averageWorldSamples++;
            }
            if (averageWorldSamples == 0)
            {
                //TODO figure out how to support fixation on skybox
                //there could be a fixation somewhere on the skybox, but we can't really allow that
                return false;
            }
            averageWorldPos /= averageWorldSamples;

            //TODO allow some noise here
            bool withinRadius = true;

            //get starting screen position to compare other eye capture points against
            var screenpos = HMDCam.WorldToViewportPoint(EyeCaptures[index].WorldPosition);
            var screendist = Vector2.Distance(screenpos, Vector3.one * 0.5f);
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

                ActiveFixation.DebugScale = opposite;

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