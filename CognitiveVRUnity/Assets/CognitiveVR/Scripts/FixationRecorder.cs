using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    //TODO try removing noisy outliers when creating new fixation points
    //https://stackoverflow.com/questions/3779763/fast-algorithm-for-computing-percentiles-to-remove-outliers
    //https://www.codeproject.com/Tips/602081/%2FTips%2F602081%2FStandard-Deviation-Extension-for-Enumerable

    public class FixationRecorder : MonoBehaviour
    {
        #region EyeTracker

#if CVR_FOVE
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
            return (long)(Time.realtimeSinceStartup * 1000;)
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
#elif CVR_TOBII

        Tobii.Research.Unity.VREyeTracker EyeTracker;
        Tobii.Research.Unity.IVRGazeData currentData;
        public Ray CombinedWorldGazeRay() { return currentData.CombinedGazeRayWorld; }

        public bool LeftEyeOpen() { return currentData.Left.PupilDiameterValid && currentData.Left.PupilPosiitionInTrackingAreaValid; }
        public bool RightEyeOpen() { return currentData.Right.PupilDiameterValid && currentData.Right.PupilPosiitionInTrackingAreaValid; }
        
        public long EyeCaptureTimestamp()
        {
            return currentData.TimeStamp / 1000;
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
#elif CVR_ADHAWK
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

        List<Vector3> WorldCapturePoints = new List<Vector3>();

        Vector3 Vector3_zero = new Vector3(0, 0, 0);

        //should be samples per second
        const int CachedEyeCaptures = 120; //TOBII
        //const int CachedEyeCaptures = 70; //FOVE

        int index = 0;
        int GetIndex(int offset)
        {
            if (index + offset < 0)
                return (CachedEyeCaptures + index + offset) % CachedEyeCaptures;
            return (index + offset) % CachedEyeCaptures;
        }

        public EyeCapture[] EyeCaptures = new EyeCapture[CachedEyeCaptures];
        public List<Fixation> Fixations = new List<Fixation>();

        public bool IsFixating { get; set; }
        //public bool IsLocalFixation { get; set; }
        public Fixation ActiveFixation;
        //the transform used for a local fixation
        public Transform FixationTransform { get; set; }

        [Header("blink")]
        public int MaxBlinkMs = 400;
        public int PreBlinkDiscardMs = 20;
        //how many samples to skip after blink has ended
        public int BlinkEndWarmupMs = 100;
        //the most recent time user has stopped blinking
        long EyeUnblinkTime;
        bool eyesClosed;

        [Header("fixation")]
        public int MinFixationMs = 60;
        public int MaxFixationConsecutiveNoiseMs = 10;
        public int PostSaccadeOscillationMs = 20;
        //the angle that must be achieved to begin/maintain a fixation
        public float MaxFixationAngle = 1;
        //float FixationDotSize = 0.99f;
        //amount of 'noise' allowed when following a dynamic object in a smooth pursuit
        public int MaxFixationConsecutiveNoiseDynamicMs = 20;
        public float DynamicFixationSizeMultiplier = 1.25f;
        public int DynamicRollingAverageMS = 100;
        public AnimationCurve FocusSizeFromCenter;
        DynamicObject LastHitDynamic; //could be the transform for a new moving fixation

        //default setting - reevaluate average fixation position
        bool useAverageFixationPosition = true;

        [Header("saccade")]
        //amount of saccades that must be consecutive before a fixation ends
        public int FixationEndSaccadeConfirmMS = 10;

        Camera HMDCam;
        
        public Dictionary<string, List<Fixation>> VISFixationEnds = new Dictionary<string, List<Fixation>>();
        public List<Vector3> VISGazepoints = new List<Vector3>(4096);

        //cognitive3d stuff
        //CognitiveVR.CommandBufferHelper commandBufferHelper;

        private void Start()
        {
            VISFixationEnds.Add("discard", new List<Fixation>());
            VISFixationEnds.Add("out of range", new List<Fixation>());
            VISFixationEnds.Add("microsleep", new List<Fixation>());
            VISFixationEnds.Add("off transform", new List<Fixation>());

            ActiveFixation = new Fixation();
            
            HMDCam = Camera.main;
            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                EyeCaptures[i] = new EyeCapture() { Discard = true };
            }
#if CVR_FOVE
            fovebase = FindObjectOfType<FoveInterfaceBase>();
#elif CVR_TOBII
            EyeTracker = FindObjectOfType<Tobii.Research.Unity.VREyeTracker>();
#endif

            //StartCoroutine(DelayFindHelper());
        }

        //IEnumerator DelayFindHelper()
        //{
        //    yield return new WaitForSeconds(4);
        //    commandBufferHelper = FindObjectOfType<CognitiveVR.CommandBufferHelper>();
        //    if (commandBufferHelper)
        //    {
        //        commandBufferHelper.ConnectFixation(PostGazeCallback);
        //    }
        //}

        private void Update()
        {
            PostGazeCallback();
        }

        //assuming command buffer will send a callback, use this when the command buffer is ready to read
        void PostGazeCallback()
        {
            //Debug.Log("fixation post gaze callbacl");
            //go through eye capture queue
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
                        FixationTransform = null;
                        IsFixating = true;
                    }
                }
            }
            else
            {
                //center is about 0.01
                //off screen is ~0.3

                EyeCaptures[index].OutOfRange = IsGazeOutOfRange(EyeCaptures[index]);
                EyeCaptures[index].OffTransform = IsGazeOffTransform(EyeCaptures[index]);
                ActiveFixation.AddEyeCapture(EyeCaptures[index]);

                ActiveFixation.DurationMs = EyeCaptures[index].Time - ActiveFixation.StartMs;

                if (CheckEndFixation(ActiveFixation))
                {
                    FixationCore.RecordFixation(ActiveFixation);

                    IsFixating = false;

                    if (ActiveFixation.IsLocal)
                    {
                        FixationTransform = null;
                    }
                    WorldCapturePoints.Clear();
                }
            }

            //reset all values
            EyeCaptures[index].Discard = false;
            EyeCaptures[index].SkipPositionForFixationAverage = false;
            EyeCaptures[index].OffTransform = false;
            EyeCaptures[index].OutOfRange = false;

            //set new current values
            EyeCaptures[index].EyesClosed = AreEyesClosed();
            EyeCaptures[index].HmdPosition = HMDCam.transform.position;
            EyeCaptures[index].Time = EyeCaptureTimestamp();

            Vector3 world;
            //CognitiveVR.DynamicObject hitDynamic;

            if (GazeRaycast(out world, out LastHitDynamic))
            {
                //hit something as expected
                EyeCaptures[index].WorldPosition = world;

                if (LastHitDynamic != null)
                    EyeCaptures[index].HitDynamicTransform = LastHitDynamic.transform;
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

            VISGazepoints.Add(EyeCaptures[index].WorldPosition);
            index = (index + 1) % CachedEyeCaptures;
        }

        //the position in the world/local hit. returns true if valid
        bool GazeRaycast(out Vector3 world, out CognitiveVR.DynamicObject hitDynamic)
        {
            world = Vector3_zero;
            hitDynamic = null;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(CombinedWorldGazeRay(), out hit))
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
            //discard if one eye isn't tracked
            //if (!LeftEyeOpen() && RightEyeOpen() ||
            //    LeftEyeOpen() && !RightEyeOpen())
            //{
            //    EyeCaptures[index].Discard = true;
            //}

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
                //EyeCaptures[index].EyesClosed = true; //return value is set to .EyesClosed
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

            if (capture.HitDynamicTransform != FixationTransform) { return true; }

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
                //want to check if eye capture is within fixation radius, not specifically if eye capture hit the same transform

                List<Vector3> dynamicAveragePoints = new List<Vector3>(); //constantly review eye captures for local fixations
                for (int i = 0; i < CachedEyeCaptures; i++)
                {
                    //go through all eye captures. use timestamps for pick out recent eye captures used in this local fixation
                    if (EyeCaptures[GetIndex(i)].Time < capture.Time + DynamicRollingAverageMS && EyeCaptures[GetIndex(i)].Time > capture.Time) //this is the old timestamp
                    {
                        dynamicAveragePoints.Add(EyeCaptures[GetIndex(i)].WorldPosition);
                    }
                    if (EyeCaptures[GetIndex(-i)].Time > capture.Time + DynamicRollingAverageMS)
                    {
                        //does this make sense?
                        break;
                    }
                    //TODO go backward through eye captures, break when reaching a sample outside of DynamicRollingAverageMS
                }

                Vector3 average = Vector3_zero;
                for (int i = 0; i < dynamicAveragePoints.Count; i++)
                {
                    average += dynamicAveragePoints[i];
                }
                average /= dynamicAveragePoints.Count;
                ActiveFixation.WorldPosition = average;

                //compare 
                var screenpos = HMDCam.WorldToViewportPoint(capture.WorldPosition);
                var screendist = Vector2.Distance(screenpos, Vector3.one * 0.5f);
                var rescale = FocusSizeFromCenter.Evaluate(screendist);
                var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);

                //compare local gaze with things?
                Vector3 lookDir = (capture.WorldPosition - capture.HmdPosition).normalized;
                Vector3 fixationDir = (ActiveFixation.WorldPosition - capture.HmdPosition).normalized;

                if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                {
                    return true;
                }
                else
                {
                    float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - capture.HmdPosition);
                    float currentRadius = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;
                    ActiveFixation.MinRadius = Mathf.Min(ActiveFixation.MinRadius, currentRadius);
                }
            }
            else
            {
                if (capture.SkipPositionForFixationAverage) //eye capture is invalid (probably from looking at skybox)
                {
                    //compare and return true/false
                    var screenpos = HMDCam.WorldToViewportPoint(capture.WorldPosition);
                    var screendist = Vector2.Distance(screenpos, Vector3.one * 0.5f);
                    var rescale = FocusSizeFromCenter.Evaluate(screendist);
                    var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * Mathf.Deg2Rad);

                    Vector3 lookDir = (capture.WorldPosition - capture.HmdPosition).normalized;
                    Vector3 fixationDir = (ActiveFixation.WorldPosition - capture.HmdPosition).normalized;

                    if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                    {
                        return true;
                    }
                    else
                    {
                        //looking at skybox will not update fixation radius

                        //float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - capture.HmdPosition);
                        //float currentRadius = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;
                        //ActiveFixation.MinRadius = Mathf.Min(ActiveFixation.MinRadius, currentRadius);
                    }
                }
                else if (useAverageFixationPosition)
                {
                    var screenpos = HMDCam.WorldToViewportPoint(capture.WorldPosition);
                    var screendist = Vector2.Distance(screenpos, Vector3.one * 0.5f);
                    var rescale = FocusSizeFromCenter.Evaluate(screendist);
                    var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * Mathf.Deg2Rad);

                    //if in range, recalculate average world position

                    Vector3 averageworldpos = Vector3_zero;
                    foreach (var v in WorldCapturePoints)
                    {
                        averageworldpos += v;
                    }
                    averageworldpos += capture.WorldPosition;
                    averageworldpos /= (WorldCapturePoints.Count + 1);


                    Vector3 lookDir = (capture.WorldPosition - capture.HmdPosition).normalized;
                    Vector3 fixationDir = (averageworldpos - capture.HmdPosition).normalized;

                    if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                    {
                        return true;
                    }

                    float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - capture.HmdPosition);
                    float currentRadius = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;
                    ActiveFixation.MinRadius = Mathf.Min(ActiveFixation.MinRadius, currentRadius);

                    WorldCapturePoints.Add(capture.WorldPosition);
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
                VISFixationEnds["microsleep"].Add(new Fixation(testFixation));
                return true;
            }

            //check for general discarding
            if (EyeCaptures[index].Time > testFixation.LastNonDiscardedTime + MaxFixationConsecutiveNoiseMs)
            {
                VISFixationEnds["discard"].Add(new Fixation(testFixation));
                //HMD issue, just a bunch of null data or some other issue
                return true;
            }

            //check for out of fixation point range
            if (EyeCaptures[index].Time > testFixation.LastInRange + FixationEndSaccadeConfirmMS)
            {
                VISFixationEnds["out of range"].Add(new Fixation(testFixation));
                return true;
            }

            if (ActiveFixation.IsLocal)
            {
                //if not looking at transform for a while, end fixation
                if (EyeCaptures[index].Time > testFixation.LastOnTransform + MaxFixationConsecutiveNoiseDynamicMs)
                {
                    VISFixationEnds["off transform"].Add(new Fixation(testFixation));
                    return true;
                }

                //check that the transform still exists
                if (FixationTransform == null) return true;
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

            int[] transformUseCount = new int[samples];

            for (int i = 0; i < hitTransforms.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (hitTransforms[i] != null && hitTransforms[i] == hitTransforms[j])
                    {
                        transformUseCount[i]++;
                        continue;
                    }
                }
            }

            int usecount = 0;
            for (int i = 0; i < transformUseCount.Length; i++)
            {
                if (transformUseCount[i] > usecount)
                {
                    usecount = transformUseCount[i];
                    mostUsed = hitTransforms[i];
                }
            }

            Vector3 averageWorldPosition = Vector3_zero;
            for (int i = 0; i < samples; i++)
            {
                if (EyeCaptures[GetIndex(i)].HitDynamicTransform == mostUsed)
                {
                    averageWorldPosition += EyeCaptures[GetIndex(i)].WorldPosition;
                }
            }

            averageWorldPosition /= usecount;
            
            //should this be in the loop? probably
            var screenpos = HMDCam.WorldToViewportPoint(EyeCaptures[index].WorldPosition);
            var screendist = Vector2.Distance(screenpos, Vector3.one * 0.5f);
            var rescale = FocusSizeFromCenter.Evaluate(screendist);
            var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);

            //use captures that hit the most common dynamic to figure out fixation start point
            //then use all captures world position to check if within fixation radius
            bool withinRadius = true;
            for (int i = 0; i < samples; i++)
            {
                Vector3 lookDir = EyeCaptures[GetIndex(i)].HmdPosition - EyeCaptures[GetIndex(i)].WorldPosition;
                Vector3 fixationDir = EyeCaptures[GetIndex(i)].HmdPosition - averageWorldPosition;

                if (Vector3.Dot(lookDir, fixationDir) < adjusteddotangle)
                {
                    withinRadius = false;
                    break;
                }
            }

            if (withinRadius)
            {
                //all eye captures within fixation radius. save transform, set ActiveFixation start time and world position
                ActiveFixation.WorldPosition = averageWorldPosition;
                FixationTransform = mostUsed;
                ActiveFixation.DynamicObjectId = LastHitDynamic.Id;

                float distance = Vector3.Magnitude(ActiveFixation.WorldPosition - EyeCaptures[index].HmdPosition);
                float opposite = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;

                ActiveFixation.StartDistance = distance;
                ActiveFixation.MinRadius = opposite;
                ActiveFixation.StartMs = EyeCaptures[index].Time;
                ActiveFixation.DebugScale = opposite;

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
            Vector3 averageWorldPos = Vector3_zero;
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
                ActiveFixation.MinRadius = opposite;

                ActiveFixation.DebugScale = opposite;

                for (int i = 0; i < sampleCount; i++)
                {
                    if (EyeCaptures[GetIndex(i)].SkipPositionForFixationAverage) { continue; }
                    WorldCapturePoints.Add(EyeCaptures[GetIndex(i)].WorldPosition);
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