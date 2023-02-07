using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine; //for fixation calculations - don't want to break this right now
using System.Threading; //for dynamic objects


//this is on the far side of the interface - what actually serializes and returns data
//might be written in c++ eventually. might be multithreaded

//should only include plain old data and callbacks. eventually no cognitive3d classes and no unity engine
namespace Cognitive3D.Serialization
{
    internal static class SharedCore
    {
        #region Delegates and Callbacks
        static System.Action<string> LogAction;
        static System.Action<string, string, bool> WebPost;
        static System.Action<Fixation> NewFixation;
        #endregion


        //some shared timer (10 seconds) + data thresholds?

        internal static void Flush(bool copyToCache)
        {
            if (!IsInitialized) { return; }

            SerializeEvents(copyToCache);
            SerializeGaze(copyToCache);
            SerializeSensors(copyToCache);
            SerializeFixations(copyToCache);

            InterruptThread = true;
            while (queuedSnapshots.Count > 0 || queuedManifest.Count > 0)
            {
                SerializeDynamicImmediate(copyToCache);
            }
            DynamicSnapshotsCount = 0;
        }

        #region Settings
        static string SessionId;
        static string DeviceId;
        static double SessionTimestamp;
        static string HMDName;
        static string ParticipantId;
        static int EventThreshold;
        static int GazeThreshold;
        static int DynamicThreshold;
        static int SensorThreshold;
        static int FixationThreshold;
        static bool IsInitialized = false;

        //TODO replace with a struct
        internal static void InitializeSettings(string sessionId, int eventThreshold, int gazeThreshold, int dynamicTreshold, int sensorThreshold, int fixationThreshold, double sessionTimestamp, string deviceId, System.Action<string, string, bool> webPost, System.Action<string> logAction, string hmdName)
        {
            DeviceId = deviceId;
            SessionTimestamp = sessionTimestamp;
            HMDName = hmdName;
            SessionId = sessionId;

            EventThreshold = eventThreshold;
            GazeThreshold = gazeThreshold;
            DynamicThreshold = dynamicTreshold;
            SensorThreshold = sensorThreshold;
            FixationThreshold = fixationThreshold;

            InitializeDynamicSnapshotPool();
            InitializeGaze();

            WebPost = webPost;
            LogAction = logAction;

            IsInitialized = true;
            
            //apply pre session properties
            foreach(var kvp in preSessionProperties)
            {
                SetSessionProperty(kvp.Key, kvp.Value);
            }
            preSessionProperties.Clear();
        }

        internal static void Reset()
        {
            IsInitialized = false;

            LogAction = null;
            WebPost = null;
            SessionId = string.Empty;
            DeviceId = string.Empty;
            ParticipantId = string.Empty;
            SessionTimestamp = 0;
            LobbyId = string.Empty;

            preSessionProperties.Clear();
            newSessionProperties.Clear();
            knownSessionProperties.Clear();

            ResetCustomEvents();
            ResetGaze();
            ResetDynamics();
            ResetSensors();
            ResetFixations();
        }

        //used internally for recording session properties set before the session has initialized
        static List<KeyValuePair<string, object>> preSessionProperties = new List<KeyValuePair<string, object>>(32);

        static string LobbyId;
        //any changed properties that have not been written to the session
        static List<KeyValuePair<string, object>> newSessionProperties = new List<KeyValuePair<string, object>>(32);

        //all session properties, including new properties not yet sent
        static List<KeyValuePair<string, object>> knownSessionProperties = new List<KeyValuePair<string, object>>(32);

        static void SetPreSessionProperty(string key, object value)
        {
            for (int i = 0; i < preSessionProperties.Count; i++)
            {
                if (preSessionProperties[i].Key == key)
                {
                    preSessionProperties[i] = new KeyValuePair<string, object>(key, value);
                    return;
                }
            }
            preSessionProperties.Add(new KeyValuePair<string, object>(key, value));
        }

        static void SetPreSessionPropertyIfEmpty(string key, object value)
        {
            for (int i = 0; i < preSessionProperties.Count; i++)
            {
                if (preSessionProperties[i].Key == key)
                {
                    return;
                }
            }
            preSessionProperties.Add(new KeyValuePair<string, object>(key, value));
        }

        internal static List<KeyValuePair<string, object>> GetNewSessionProperties(bool clearNewProperties)
        {
            if (!IsInitialized) { return null; }
            if (clearNewProperties)
            {
                if (newSessionProperties.Count > 0)
                {
                    List<KeyValuePair<string, object>> returndict = new List<KeyValuePair<string, object>>(newSessionProperties);
                    newSessionProperties.Clear();
                    return returndict;
                }
                else
                {
                    return newSessionProperties;
                }
            }
            return newSessionProperties;
        }

        internal static List<KeyValuePair<string, object>> GetAllSessionProperties(bool clearNewProperties)
        {
            if (!IsInitialized) { return null; }
            if (clearNewProperties)
            {
                newSessionProperties.Clear();
            }
            return knownSessionProperties;
        }

        internal static void SetSessionProperty(string key, object value)
        {
            if (!IsInitialized)
            {
                SetPreSessionProperty(key, value);
                return;
            }
            int foundIndex = 0;
            bool foundKey = false;
            for (int i = 0; i < knownSessionProperties.Count; i++)
            {
                if (knownSessionProperties[i].Key == key)
                {
                    foundKey = true;
                    foundIndex = i;
                    break;
                }
            }

            if (foundKey) //update value
            {
                if (knownSessionProperties[foundIndex].Value == value) //skip setting property if it hasn't actually changed
                {
                    return;
                }
                else
                {
                    knownSessionProperties[foundIndex] = new KeyValuePair<string, object>(key, value);

                    bool foundNewSessionPropKey = false;
                    int foundNewSessionPropIndex = 0;
                    for (int i = 0; i < newSessionProperties.Count; i++) //add/replace in 'newSessionProperty' (ie dirty value that will be sent with gaze)
                    {
                        if (newSessionProperties[i].Key == key)
                        {
                            foundNewSessionPropKey = true;
                            foundNewSessionPropIndex = i;
                            break;
                        }
                    }
                    if (foundNewSessionPropKey)
                    {
                        newSessionProperties[foundNewSessionPropIndex] = new KeyValuePair<string, object>(key, value);
                    }
                    else
                    {
                        newSessionProperties.Add(new KeyValuePair<string, object>(key, value));
                    }
                }
            }
            else
            {
                knownSessionProperties.Add(new KeyValuePair<string, object>(key, value));
                newSessionProperties.Add(new KeyValuePair<string, object>(key, value));
            }
        }

        internal static void SetSessionPropertyIfEmpty(string key, object value)
        {
            if (!IsInitialized)
            {
                SetPreSessionPropertyIfEmpty(key, value);
                return;
            }
            for (int i = 0; i < knownSessionProperties.Count; i++)
            {
                if (knownSessionProperties[i].Key == key)
                {
                    return;
                }
            }

            knownSessionProperties.Add(new KeyValuePair<string, object>(key, value));
            newSessionProperties.Add(new KeyValuePair<string, object>(key, value));
        }

        internal static void SetLobbyId(string lobbyid)
        {
            LobbyId = lobbyid;
            SetSessionProperty("c3d.lobbyId", lobbyid);
        }
        internal static void SetParticipantId(string participantId)
        {
            ParticipantId = participantId;
        }
        #endregion

        #region CustomEvent
        static int CachedEventCount;
        static int EventJsonPart = 1;
        //used to batch events together
        private static System.Text.StringBuilder eventRequestBuilder = new System.Text.StringBuilder(1024);
        //used to serialize each event
        private static System.Text.StringBuilder eventBuilder = new System.Text.StringBuilder(1024);

        //records object of custom event data to be serialized later
        internal static void RecordCustomEvent(string category, double timestamp, List<KeyValuePair<string, object>> properties, float[] position, string dynamicObjectId = "")
        {
            if (!IsInitialized) { return; }
            eventBuilder.Append("{");
            JsonUtil.SetString("name", category, eventBuilder);
            eventBuilder.Append(",");
            JsonUtil.SetDouble("time", timestamp, eventBuilder);
            if (!string.IsNullOrEmpty(dynamicObjectId))
            {
                eventBuilder.Append(',');
                JsonUtil.SetString("dynamicId", dynamicObjectId, eventBuilder);
            }
            eventBuilder.Append(",");
            JsonUtil.SetVector("point", position, eventBuilder);

            if (properties != null && properties.Count > 0)
            {
                eventBuilder.Append(",");
                eventBuilder.Append("\"properties\":{");
                for (int i = 0; i < properties.Count; i++)
                {
                    if (i != 0) { eventBuilder.Append(","); }
                    if (properties[i].Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(properties[i].Key, (string)properties[i].Value, eventBuilder);
                    }
                    else if (properties[i].Value.GetType() == typeof(float))
                    {
                        JsonUtil.SetFloat(properties[i].Key, (float)properties[i].Value, eventBuilder);
                    }
                    else if (properties[i].Value.GetType() == typeof(double))
                    {
                        JsonUtil.SetDouble(properties[i].Key, (double)properties[i].Value, eventBuilder);
                    }
                    else if (properties[i].Value.GetType() == typeof(int))
                    {
                        JsonUtil.SetInt(properties[i].Key, (int)properties[i].Value, eventBuilder);
                    }
                    else if (properties[i].Value.GetType() == typeof(long))
                    {
                        JsonUtil.SetLong(properties[i].Key, (long)properties[i].Value, eventBuilder);
                    }
                    else if (properties[i].Value.GetType() == null)
                    {
                        JsonUtil.SetNull(properties[i].Key, eventBuilder);
                    }
                    else
                    {
                        JsonUtil.SetString(properties[i].Key, properties[i].Value.ToString(), eventBuilder);
                    }
                }
                eventBuilder.Append("}"); //close properties object
            }

            eventBuilder.Append("}"); //close transaction object
            eventBuilder.Append(",");

            //moved to core interface
            //CustomEventRecordedEvent(category, new Vector3(position[0], position[1], position[2]), properties, dynamicObjectId, Util.Timestamp(Time.frameCount));
            CachedEventCount++;
            if (CachedEventCount >= EventThreshold)
            {
                SerializeEvents(false);
                //activate network post
                //SerializeEvents();
            }
        }

        static void SerializeEvents(bool writeToCache)
        {
            if (CachedEventCount == 0) { return; }

            CachedEventCount = 0;
            //bundle up header stuff and transaction data

            //clear the transaction builder
            eventRequestBuilder.Length = 0;

            //Cognitive3D.Util.logDebug("package transaction event data " + partCount);
            //when thresholds are reached, etc

            eventRequestBuilder.Append("{");

            //header
            JsonUtil.SetString("userid", DeviceId, eventRequestBuilder);
            eventRequestBuilder.Append(",");

            if (!string.IsNullOrEmpty(LobbyId))
            {
                JsonUtil.SetString("lobbyId", LobbyId, eventRequestBuilder);
                eventRequestBuilder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", SessionTimestamp, eventRequestBuilder);
            eventRequestBuilder.Append(",");
            JsonUtil.SetString("sessionid", SessionId, eventRequestBuilder);
            eventRequestBuilder.Append(",");
            JsonUtil.SetInt("part", EventJsonPart, eventRequestBuilder);
            EventJsonPart++;
            eventRequestBuilder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", eventRequestBuilder);
            eventRequestBuilder.Append(",");

            //events
            eventRequestBuilder.Append("\"data\":[");

            eventRequestBuilder.Append(eventBuilder.ToString());

            if (eventRequestBuilder.Length > 0)
                eventRequestBuilder.Remove(eventRequestBuilder.Length - 1, 1); //remove the last comma
            eventRequestBuilder.Append("]");

            eventRequestBuilder.Append("}");

            eventBuilder.Length = 0;

            //send transaction contents to scene explorer

            string packagedEvents = eventRequestBuilder.ToString();
            WebPost("event", packagedEvents, writeToCache);
        }

        static void ResetCustomEvents()
        {
            eventRequestBuilder.Length = 0;
            eventBuilder.Length = 0;
            CachedEventCount = 0;
            EventJsonPart = 1;
        }

        #endregion

        #region Fixation

        static void ResetFixations()
        {
            fixationJsonPart = 1;
            Fixations.Clear();
            index = 0;
            IsFixating = false;
            WasCaptureDiscardedLastFrame = false;
            WasOutOfDispersionLastFrame = false;
            CachedEyeCapturePositions.Clear();
            worldFixationSamples.Clear();
            ActiveFixation = null;
            EyeCaptures = null; //<------------------
            fixationsInitialized = false;
            fixationHitDynamicIds.Clear();
            localFixationUsedCaptures.Clear();
        }

        private static int fixationJsonPart = 1;
        static List<Fixation> Fixations = new List<Fixation>();
        //static int CachedFixations { get { return Fixations.Count; } }
        static int index;
        static bool IsFixating;
        static bool WasCaptureDiscardedLastFrame = false; //ensures at least 1 frame is discarded before ending fixations
        static bool WasOutOfDispersionLastFrame = false; //ensures at least 1 frame is out of fixation dispersion cone before ending fixation

        static int MaxBlinkMs;
        static int PreBlinkDiscardMs;
        static int BlinkEndWarmupMs;
        static int MinFixationMs;
        static int MaxConsecutiveDiscardMs;
        static float MaxFixationAngle;
        static int MaxConsecutiveOffDynamicMs;
        static float DynamicFixationSizeMultiplier;
        static AnimationCurve FocusSizeFromCenter;
        static int SaccadeFixationEndMs;

        static List<Vector3> CachedEyeCapturePositions = new List<Vector3>();

        static List<EyeCapture> worldFixationSamples = new List<EyeCapture>(CachedEyeCaptures);
        const int CachedEyeCaptures = 120;
        static Fixation ActiveFixation;
        static EyeCapture[] EyeCaptures;

        //static int index = 0;
        static int GetIndex(int offset)
        {
            if (index + offset < 0)
                return (CachedEyeCaptures + index + offset) % CachedEyeCaptures;
            return (index + offset) % CachedEyeCaptures;
        }

        static List<string> fixationHitDynamicIds = new List<string>(CachedEyeCaptures);
        static List<EyeCapture> localFixationUsedCaptures = new List<EyeCapture>(CachedEyeCaptures);


        static bool fixationsInitialized;
        internal static void FixationInitialize(int maxBlinkMS, int preBlinkDiscardMS, int blinkEndWarmupMS, int minFixationMS, int maxConsecutiveDiscardMS, float maxfixationAngle, int maxConsecutiveOffDynamic, float dynamicFixationSizeMultiplier, AnimationCurve focusSizeFromCenter, int saccadefixationEndMS, System.Action<Fixation> newFixation)
        {
            if (!IsInitialized) { return; }

            MaxBlinkMs = maxBlinkMS;
            PreBlinkDiscardMs = preBlinkDiscardMS;
            BlinkEndWarmupMs = blinkEndWarmupMS;
            MinFixationMs = minFixationMS;
            MaxConsecutiveDiscardMs = maxConsecutiveDiscardMS;
            MaxFixationAngle = maxfixationAngle;
            MaxConsecutiveOffDynamicMs = maxConsecutiveOffDynamic;
            DynamicFixationSizeMultiplier = dynamicFixationSizeMultiplier;
            FocusSizeFromCenter = focusSizeFromCenter;
            SaccadeFixationEndMs = saccadefixationEndMS;

            EyeCaptures = new EyeCapture[CachedEyeCaptures];

            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                EyeCaptures[i] = new EyeCapture() { Discard = true };
            }
            ActiveFixation = new Fixation();
            NewFixation = newFixation;
            fixationsInitialized = true;
        }


        //TODO replace matrix4x4 with something else not tied to unity
        internal static void RecordEyeData(long time, Vector3 worldPosition, Vector3 localPosition, Vector3 hmdposition, Vector2 screenposition, bool blinking, string dynamicId, Matrix4x4 dynamicMatrix, int hitType)
        {
            if (!IsInitialized) { return; }
            if (!fixationsInitialized) { return; }
            //check for new fixation
            //else check for ending fixation
            //update eyecapture state

            if (!IsFixating)
            {
                //check if this is the start of a new fixation. set this and all next captures to this
                //the 'current' fixation we're checking is 1 second behind recording eye captures
                if (TryBeginLocalFixation(index))
                {
                    IsFixating = true;
                }
                else
                {
                    if (TryBeginFixation(index))
                    {
                        IsFixating = true;
                    }
                }
            }
            else
            {
                //check if eye capture is valid to append to fixation
                //check if fixation 

                if (ActiveFixation.IsLocal)
                {
                    if (ActiveFixation.DynamicObjectId != EyeCaptures[index].HitDynamicId)
                    {
                        EyeCaptures[index].SkipPositionForFixationAverage = true;
                    }
                }
                else
                {
                    if (EyeCaptures[index].UseCaptureMatrix)
                        EyeCaptures[index].SkipPositionForFixationAverage = true;
                }

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

                if (CheckEndFixation(ActiveFixation))
                {
                    if (ActiveFixation.DurationMs > MinFixationMs)
                    {
                        RecordFixation(ActiveFixation);
                    }

                    IsFixating = false;

                    if (ActiveFixation.IsLocal)
                    {
                        //ActiveFixation.LocalTransform = null;
                    }
                    CachedEyeCapturePositions.Clear();
                }
                WasOutOfDispersionLastFrame = IsOutOfRange;
            }


            EyeCaptures[index].Discard = false;
            EyeCaptures[index].SkipPositionForFixationAverage = false;
            EyeCaptures[index].OffTransform = true;
            EyeCaptures[index].OutOfRange = false;
            EyeCaptures[index].HitDynamicId = string.Empty;
            EyeCaptures[index].EyesClosed = blinking;
            EyeCaptures[index].HmdPosition = hmdposition;
            EyeCaptures[index].Time = time;
            if (EyeCaptures[index].EyesClosed) { EyeCaptures[index].SkipPositionForFixationAverage = true; }

            if (hitType == 0) //world
            {
                EyeCaptures[index].WorldPosition = worldPosition;
                EyeCaptures[index].ScreenPos = screenposition;
                EyeCaptures[index].UseCaptureMatrix = false;
                EyeCaptures[index].OffTransform = false;
                EyeCaptures[index].HitDynamicId = string.Empty;
            }
            else if (hitType == 1) //dynamic
            {
                EyeCaptures[index].WorldPosition = worldPosition;
                EyeCaptures[index].ScreenPos = screenposition;
                EyeCaptures[index].UseCaptureMatrix = true;
                EyeCaptures[index].CaptureMatrix = dynamicMatrix;
                EyeCaptures[index].HitDynamicId = dynamicId;
                EyeCaptures[index].LocalPosition = localPosition;
                EyeCaptures[index].OffTransform = false;
            }
            else if (hitType == 2) //sky
            {
                EyeCaptures[index].WorldPosition = worldPosition;
                EyeCaptures[index].ScreenPos = screenposition;
                EyeCaptures[index].UseCaptureMatrix = false;
                EyeCaptures[index].OffTransform = true;
                EyeCaptures[index].SkipPositionForFixationAverage = true;
            }
            else if (hitType == 3) //invalid
            {
                EyeCaptures[index].Discard = true;
                EyeCaptures[index].SkipPositionForFixationAverage = true;
                EyeCaptures[index].UseCaptureMatrix = false;
                EyeCaptures[index].OffTransform = true;
            }

            index = (index + 1) % CachedEyeCaptures;
        }


        internal static void RecordFixation(Fixation newFixation)
        {
            if (!IsInitialized) { return; }
            Fixations.Add(new Fixation(newFixation));
            if (Fixations.Count > FixationThreshold)
            {
                SerializeFixations(false);
            }
        }

        static bool IsGazeOutOfRange(EyeCapture capture)
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
                    //IMPROVEMENT multiplyPoint3x4 without decomposing + rebuilding matrix
                    Vector3 position = ActiveFixation.DynamicMatrix.GetColumn(3);
                    Quaternion rotation = Quaternion.LookRotation(
                        ActiveFixation.DynamicMatrix.GetColumn(2),
                        ActiveFixation.DynamicMatrix.GetColumn(1)
                    );
                    var unscaledMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);

                    var captureWorldPos = unscaledMatrix.MultiplyPoint3x4(capture.LocalPosition);
                    var activeFixationWorldPos = unscaledMatrix.MultiplyPoint3x4(ActiveFixation.LocalPosition);

                    var _fixationDirection = (activeFixationWorldPos - capture.HmdPosition).normalized;
                    var _eyeCaptureDirection = (captureWorldPos - capture.HmdPosition).normalized;
                    var _screendist = Vector2.Distance(capture.ScreenPos, new Vector2(0.5f, 0.5f));
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

                    //IMPROVEMENT multiplyPoint3x4 without decomposing + rebuilding matrix
                    Vector3 position = ActiveFixation.DynamicMatrix.GetColumn(3);
                    Quaternion rotation = Quaternion.LookRotation(
                        ActiveFixation.DynamicMatrix.GetColumn(2),
                        ActiveFixation.DynamicMatrix.GetColumn(1)
                    );
                    var unscaledMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
                    var captureWorldPos = unscaledMatrix.MultiplyPoint3x4(capture.LocalPosition);

                    var activeFixationWorldPos = unscaledMatrix.MultiplyPoint3x4(ActiveFixation.LocalPosition);

                    //if in range, we will add captureWorldPos to CachedEyeCapturePositions and update activefixation.localposition
                    //then update average position then check angle
                    Vector3 averagelocalpos = Vector3.zero;
                    foreach (var v in CachedEyeCapturePositions)
                    {
                        averagelocalpos += v;
                    }
                    averagelocalpos += capture.LocalPosition;
                    averagelocalpos /= (CachedEyeCapturePositions.Count + 1);

                    var _fixationDirection = (activeFixationWorldPos - capture.HmdPosition).normalized;
                    var _eyeCaptureDirection = (captureWorldPos - capture.HmdPosition).normalized;

                    var _screendist = Vector2.Distance(capture.ScreenPos, new Vector2(0.5f, 0.5f));
                    var _rescale = FocusSizeFromCenter.Evaluate(_screendist);
                    var _adjusteddotangle = Mathf.Cos(MaxFixationAngle * _rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);
                    float dot = Vector3.Dot(_eyeCaptureDirection, _fixationDirection);

                    if (dot < _adjusteddotangle)
                    {
                        return true;
                    }

                    float distance = Vector3.Magnitude(activeFixationWorldPos - capture.HmdPosition);
                    float currentRadius = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;
                    ActiveFixation.MaxRadius = Mathf.Max(ActiveFixation.MaxRadius, currentRadius);

                    CachedEyeCapturePositions.Add(capture.LocalPosition);

                    if (CachedEyeCapturePositions.Count > 120) //IMPROVEMENT cache eye captures based on time, not on count
                        CachedEyeCapturePositions.RemoveAt(0);
                    ActiveFixation.LocalPosition = averagelocalpos;
                }
                ActiveFixation.LastInRange = capture.Time;
                return false;
            }
            else //world
            {
                if (capture.UseCaptureMatrix == true)
                {
                    capture.SkipPositionForFixationAverage = true;
                }

                var screendist = Vector2.Distance(capture.ScreenPos, new Vector2(0.5f, 0.5f));
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
                ActiveFixation.LastInRange = capture.Time;
            }
            return false;
        }

        static bool CheckEndFixation(Fixation testFixation)
        {
            //check for blinking too long
            if (EyeCaptures[index].Time > testFixation.LastEyesOpen + MaxBlinkMs)
            {
                NewFixation(testFixation);
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
                    NewFixation(testFixation);
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
                    //Debug.LogError("END FIXATION RANGE duration" + testFixation.DurationMs);
                    NewFixation(testFixation);
                    return true;
                }
            }

            //if not looking at transform for a while, end fixation
            if (EyeCaptures[index].Time > testFixation.LastOnTransform + MaxConsecutiveOffDynamicMs)
            {
                NewFixation(testFixation);
                //Debug.LogError("END FIXATION TRANSFORM " + EyeCaptures[index].Time);
                return true;
            }

            return false;
        }

        static bool IsFixatingOffTransform(EyeCapture capture)
        {
            if (ActiveFixation.IsLocal)
            {
                if (!capture.UseCaptureMatrix)
                {
                    return true;
                }
                if (capture.HitDynamicId != ActiveFixation.DynamicObjectId)
                {
                    return true;
                }
            }
            if (capture.OffTransform)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// returns true if 'active fixation' is actually active again
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        static bool TryBeginLocalFixation(int index)
        {
            int samples = 0;
            fixationHitDynamicIds.Clear();
            localFixationUsedCaptures.Clear();
            long firstOnTransformTime = 0;            

            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                if (EyeCaptures[GetIndex(i)].Discard || EyeCaptures[GetIndex(i)].EyesClosed) { return false; }
                if (!EyeCaptures[GetIndex(i)].UseCaptureMatrix) { return false; }
                if (EyeCaptures[GetIndex(i)].SkipPositionForFixationAverage)
                {
                    //if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time) { break; }
                    //continue;
                    return false;
                }
                samples++;
                localFixationUsedCaptures.Add(EyeCaptures[GetIndex(i)]);
                if (firstOnTransformTime < 1)
                    firstOnTransformTime = EyeCaptures[GetIndex(i)].Time;
                if (EyeCaptures[GetIndex(i)].UseCaptureMatrix)
                {
                    fixationHitDynamicIds.Add(EyeCaptures[GetIndex(i)].HitDynamicId);
                }
                if (EyeCaptures[index].Time + MinFixationMs < EyeCaptures[GetIndex(i)].Time) { break; }
            }

            //check that there are any valid eye captures
            if (samples < 2)
            {
                return false;
            }
            if (localFixationUsedCaptures.Count > 2)
            {
                //fail if the time between samples is < minFixationMs
                if ((localFixationUsedCaptures[localFixationUsedCaptures.Count - 1].Time - localFixationUsedCaptures[0].Time) < MinFixationMs) { return false; }
            }
            //TODO find source of rare bug with fixation duration < MinFixationMs when fixating on fast moving dynamic object

            if (EyeCaptures[index].Time - firstOnTransformTime > MaxConsecutiveOffDynamicMs)
            {
                //fail now. off transform time will fail this before getting to first on transform time
                //otherwise, fixation steady enough to be called a fixation and surface point will 'eventually' be valid
                return false;
            }
            //================================= CALCULATE HIT DYNAMIC IDS
            int hitTransformCount = 0;
            Dictionary<string, int> hitCounts = new Dictionary<string, int>();

            Vector3 averageLocalPosition = Vector3.zero;
            Vector3 averageWorldPosition = Vector3.zero;
            foreach (var v in localFixationUsedCaptures)
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

            //======== average positions and check if fixations are within radius. using only the first eye capture matrix as a reference

            int hitSampleCount = 0;
            Vector3 position = localFixationUsedCaptures[0].CaptureMatrix.GetColumn(3);
            Quaternion rotation = Quaternion.LookRotation(
                localFixationUsedCaptures[0].CaptureMatrix.GetColumn(2),
                localFixationUsedCaptures[0].CaptureMatrix.GetColumn(1)
            );
            var unscaledCaptureMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);

            foreach (var v in localFixationUsedCaptures)
            {
                if (v.HitDynamicId != mostUsedId) { continue; }
                if (!v.UseCaptureMatrix) { continue; }
                hitSampleCount++;
                averageLocalPosition += v.LocalPosition;
                averageWorldPosition += unscaledCaptureMatrix.MultiplyPoint3x4(v.LocalPosition);
            }

            averageLocalPosition /= hitSampleCount;
            averageWorldPosition /= hitSampleCount;


            //use captures that hit the most common dynamic to figure out fixation start point
            //then use all captures world position to check if within fixation radius

            bool withinRadius = true;
            foreach (var v in localFixationUsedCaptures)
            {
                if (v.HitDynamicId != mostUsedId) { continue; }
                if (!v.UseCaptureMatrix) { continue; }

                var screendist = Vector2.Distance(v.ScreenPos, new Vector2(0.5f, 0.5f));
                var rescale = FocusSizeFromCenter.Evaluate(screendist);
                var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * DynamicFixationSizeMultiplier * Mathf.Deg2Rad);

                Vector3 p = v.CaptureMatrix.GetColumn(3);
                Quaternion r = Quaternion.LookRotation(
                    v.CaptureMatrix.GetColumn(2),
                    v.CaptureMatrix.GetColumn(1)
                );
                var m = Matrix4x4.TRS(p, r, Vector3.one);

                Vector3 lookDir = (m.MultiplyPoint3x4(v.LocalPosition) - v.HmdPosition).normalized;
                Vector3 fixationDir = (averageWorldPosition - v.HmdPosition).normalized;
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
                ActiveFixation.WorldPosition = averageWorldPosition; //average world position is already matrix unscaled above
                ActiveFixation.DynamicObjectId = mostUsedId;
                ActiveFixation.DynamicMatrix = localFixationUsedCaptures[0].CaptureMatrix;

                float distance = Vector3.Magnitude(averageWorldPosition - localFixationUsedCaptures[0].HmdPosition);
                float opposite = Mathf.Atan(MaxFixationAngle * Mathf.Deg2Rad) * distance;

                ActiveFixation.StartDistance = distance;
                ActiveFixation.MaxRadius = opposite;
                ActiveFixation.StartMs = localFixationUsedCaptures[0].Time;
                ActiveFixation.LastOnTransform = localFixationUsedCaptures[0].Time;
                ActiveFixation.LastEyesOpen = localFixationUsedCaptures[0].Time;
                ActiveFixation.LastNonDiscardedTime = localFixationUsedCaptures[0].Time;
                ActiveFixation.LastInRange = localFixationUsedCaptures[0].Time;
                ActiveFixation.IsLocal = true;
                ActiveFixation.DynamicTransform = localFixationUsedCaptures[0].HitDynamicTransform;
                foreach (var c in localFixationUsedCaptures)
                {
                    if (c.SkipPositionForFixationAverage) { continue; }
                    if (c.UseCaptureMatrix && c.HitDynamicId == ActiveFixation.DynamicObjectId)
                    {
                        //added to cachedEyeCapturePositions here - should skip when checking isGazeInRange
                        c.SkipPositionForFixationAverage = true;
                        CachedEyeCapturePositions.Add(c.LocalPosition);
                    }
                }
                foreach (var c in localFixationUsedCaptures)
                {
                    //add first used sample as start
                    if (c.UseCaptureMatrix && c.HitDynamicId == ActiveFixation.DynamicObjectId)
                    {
                        ActiveFixation.AddEyeCapture(c);
                        break;
                    }
                }
                WasOutOfDispersionLastFrame = false;
                return true;
            }
            else
            {
                //something out of range from average world pos
                return false;
            }
        }

        //checks the NEXT eyecaptures to see if we should start a fixation
        static bool TryBeginFixation(int index)
        {
            Vector3 averageWorldPos = Vector3.zero;
            //number of eye captures on a surface
            int sampleCount = 0;
            worldFixationSamples.Clear();

            long firstOnTransformTime = 0;
            long firstSampleTime = long.MaxValue;
            long lastSampleTime = 0;

            //take all the eye captures within the minimum fixation duration
            //escape if any are eyes closed or discarded captures
            for (int i = 0; i < CachedEyeCaptures; i++)
            {
                var sample = EyeCaptures[GetIndex(i)];
                if (sample.Discard || sample.EyesClosed) { return false; }
                if (sample.SkipPositionForFixationAverage)
                {
                    //eye capture should be skipped (look at sky, discarded). also check if out of min fixation time
                    //if (EyeCaptures[index].Time + MinFixationMs < sample.Time){break;}
                    //continue;
                    return false;
                }

                if (sample.UseCaptureMatrix)
                {
                    //CONSIDER would this be more accurate to return false if a threshold of eye captures are on dynamics? any dynamics? one dynamic?
                    return false;
                }

                firstSampleTime = System.Math.Min(firstSampleTime, sample.Time);
                lastSampleTime = System.Math.Max(lastSampleTime, sample.Time);

                sampleCount++;
                worldFixationSamples.Add(EyeCaptures[GetIndex(i)]);
                //lastSampleTime = EyeCaptures[GetIndex(i)].Time;
                if (firstOnTransformTime < 1)
                    firstOnTransformTime = sample.Time;
                //TODO should use EyeCaptures.LocalPosition * EyeCaptures.Matrix. world position will be offset if object is moving
                averageWorldPos += sample.WorldPosition;
                if (EyeCaptures[index].Time + MinFixationMs < sample.Time)
                {
                    break;
                }
            }

            if (sampleCount < 2)
            {
                //need at least 2 samples for a time span
                return false;
            }

            //duration of first sample to last sample
            long duration = lastSampleTime - firstSampleTime;
            if (duration < MinFixationMs)
            {
                return false;
            }

            averageWorldPos /= sampleCount;

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

            //check that each sample is within the fixation radius
            //for (int i = 0; i < sampleCount; i++)
            foreach (var v in worldFixationSamples)
            {
                //get starting screen position to compare other eye capture points against
                //var screenpos = GameplayReferences.HMDCameraComponent.WorldToViewportPoint(EyeCaptures[index].WorldPosition);
                var screendist = Vector2.Distance(v.ScreenPos, new Vector2(0.5f, 0.5f));
                var rescale = FocusSizeFromCenter.Evaluate(screendist);
                var adjusteddotangle = Mathf.Cos(MaxFixationAngle * rescale * Mathf.Deg2Rad);

                //var sample = EyeCaptures[GetIndex(i)];
                Vector3 lookDir = (v.WorldPosition - v.HmdPosition).normalized;
                Vector3 fixationDir = (averageWorldPos - v.HmdPosition).normalized;

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
                    var sample = EyeCaptures[GetIndex(i)];
                    if (sample.SkipPositionForFixationAverage) { continue; }

                    //added to cachedEyeCapturePositions here - should skip when checking isGazeInRange
                    sample.SkipPositionForFixationAverage = true;
                    CachedEyeCapturePositions.Add(sample.WorldPosition);
                }
                return true;
            }
            else
            {
                //something out of range from average world pos
                return false;
            }
        }

        private static void SerializeFixations(bool copyDataToCache)
        {
            if (Fixations.Count <= 0) { return; }

            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{");
            JsonUtil.SetString("userid", DeviceId, sb);
            sb.Append(",");
            JsonUtil.SetString("sessionid", SessionId, sb);
            sb.Append(",");
            JsonUtil.SetInt("timestamp", (int)SessionTimestamp, sb);
            sb.Append(",");
            JsonUtil.SetInt("part", fixationJsonPart, sb);
            sb.Append(",");
            fixationJsonPart++;

            sb.Append("\"data\":[");
            for (int i = 0; i < Fixations.Count; i++)
            {
                sb.Append("{");
                JsonUtil.SetDouble("time", System.Convert.ToDouble((double)Fixations[i].StartMs / 1000.0), sb);
                sb.Append(",");
                JsonUtil.SetLong("duration", Fixations[i].DurationMs, sb);
                sb.Append(",");
                JsonUtil.SetFloat("maxradius", Fixations[i].MaxRadius, sb);
                sb.Append(",");

                if (Fixations[i].IsLocal)
                {
                    JsonUtil.SetString("objectid", Fixations[i].DynamicObjectId, sb);
                    sb.Append(",");
                    JsonUtil.SetVector("p", new float[] { Fixations[i].LocalPosition.x, Fixations[i].LocalPosition.y, Fixations[i].LocalPosition.z }, sb);
                }
                else
                {
                    JsonUtil.SetVector("p", new float[]{ Fixations[i].WorldPosition.x,Fixations[i].WorldPosition.y,Fixations[i].WorldPosition.z}, sb);
                }
                sb.Append("},");
            }
            if (Fixations.Count > 0)
            {
                sb.Remove(sb.Length - 1, 1); //remove last comma from fixation object
            }

            sb.Append("]}");

            Fixations.Clear();

            WebPost("fixation", sb.ToString(), copyDataToCache);
        }

        #endregion

        #region Gaze
        static StringBuilder gazebuilder;
        static int gazeCount;
        static int gazeJsonPart;

        static void InitializeGaze()
        {
            gazebuilder = new StringBuilder(70 * Cognitive3D_Preferences.Instance.GazeSnapshotCount + 1200);
            gazebuilder.Append("{\"data\":[");
        }

        static void ResetGaze()
        {
            gazebuilder = null;
            gazeJsonPart = 1;
            gazeCount = 0;
        }

        internal static void RecordGazeSky(float[] hmdposition, float[] hmdrotation, double timestamp, float[] floorPos, bool includeFloor, float[] geo, bool useGeo)
        {
            if (!IsInitialized) { return; }
            gazebuilder.Append("{");

            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdposition, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            if (useGeo)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", geo, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", geo[3], gazebuilder);
            }
            if (includeFloor)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }

            gazebuilder.Append("}");
            gazeCount++;
            if (gazeCount >= GazeThreshold)
            {
                SerializeGaze(false);
                //SendGazeData(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
        }


        internal static void RecordGazeMedia(float[] hmdpoint, float[] hmdrotation, float[] localgazepoint, string objectid, string mediaId, double timestamp, int mediaTimeMs, float[] uvs, float[] floorPos, bool includeFloor, float[] geo, bool useGeo)
        {
            if (!IsInitialized) { return; }
            gazebuilder.Append("{");
            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("o", objectid, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", localgazepoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("mediaId", mediaId, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetInt("mediatime", mediaTimeMs, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector2("uvs", uvs, gazebuilder);
            if (useGeo)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", geo, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", geo[3], gazebuilder);
            }
            if (includeFloor)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }

            gazebuilder.Append("}");
            gazeCount++;
            if (gazeCount >= GazeThreshold)
            {
                SerializeGaze(false);
                //SendGazeData(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
        }

        internal static void RecordGazeWorld(float[] hmdpoint, float[] hmdrotation, float[] gazepoint, double timestamp, float[] floorPos, bool includeFloor, float[] geo, bool useGeo)
        {
            if (!IsInitialized) { return; }
            gazebuilder.Append("{");
            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", gazepoint, gazebuilder);
            if (useGeo)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", geo, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", geo[3], gazebuilder);
            }
            if (includeFloor)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }
            gazebuilder.Append("}");

            gazeCount++;
            if (gazeCount >= GazeThreshold)
            {
                SerializeGaze(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
        }

        internal static void RecordGazeDynamic(float[] hmdpoint, float[] hmdrotation, float[] localgazepoint, string objectid, double timestamp, float[] floorPos, bool includeFloor, float[] geo, bool useGeo)
        {
            if (!IsInitialized) { return; }
            gazebuilder.Append("{");
            JsonUtil.SetDouble("time", timestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("o", objectid, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("p", hmdpoint, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetQuat("r", hmdrotation, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetVector("g", localgazepoint, gazebuilder);
            if (useGeo)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("gpsloc", geo, gazebuilder);
                gazebuilder.Append(",");
                JsonUtil.SetFloat("compass", geo[3], gazebuilder);
            }
            if (includeFloor)
            {
                gazebuilder.Append(",");
                JsonUtil.SetVector("f", floorPos, gazebuilder);
            }
            gazebuilder.Append("}");

            gazeCount++;
            if (gazeCount >= GazeThreshold)
            {
                SerializeGaze(false);
            }
            else
            {
                gazebuilder.Append(",");
            }
        }

        static void SerializeGaze(bool writeToCache)
        {
            if (gazeCount == 0 && newSessionProperties.Count == 0) { return; }

            //TODO allow option to send session properties but not gaze. Look at this for XRPF implementation

            if (gazebuilder[gazebuilder.Length - 1] == ',')
            {
                gazebuilder = gazebuilder.Remove(gazebuilder.Length - 1, 1);
            }

            gazebuilder.Append("],");

            gazeCount = 0;

            //header
            JsonUtil.SetString("userid", DeviceId, gazebuilder);
            gazebuilder.Append(",");

            if (!string.IsNullOrEmpty(LobbyId))
            {
                JsonUtil.SetString("lobbyId", LobbyId, gazebuilder);
                gazebuilder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)SessionTimestamp, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetString("sessionid", SessionId, gazebuilder);
            gazebuilder.Append(",");
            JsonUtil.SetInt("part", gazeJsonPart, gazebuilder);
            gazeJsonPart++;
            gazebuilder.Append(",");

            //TODO check if HMDName is used anywhere
            JsonUtil.SetString("hmdtype", HMDName, gazebuilder);
            gazebuilder.Append(",");

            JsonUtil.SetFloat("interval", 0.1f, gazebuilder);
            gazebuilder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", gazebuilder);

            //TODO remove this reference to cognitive manager - this should be true when scene has changed - add a callback
            if (Cognitive3D_Manager.ForceWriteSessionMetadata) //if scene changed and haven't sent metadata recently
            {
                Cognitive3D_Manager.ForceWriteSessionMetadata = false;
                gazebuilder.Append(",");
                gazebuilder.Append("\"properties\":{");
                foreach (var kvp in GetAllSessionProperties(true))
                {
                    if (kvp.Value == null) { Util.logDevelopment("Session Property " + kvp.Key + " is NULL "); continue; }
                    if (kvp.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(kvp.Key, (string)kvp.Value, gazebuilder);
                    }
                    else if (kvp.Value.GetType() == typeof(float))
                    {
                        JsonUtil.SetFloat(kvp.Key, (float)kvp.Value, gazebuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(kvp.Key, kvp.Value, gazebuilder);
                    }
                    gazebuilder.Append(",");
                }
                gazebuilder.Remove(gazebuilder.Length - 1, 1); //remove comma
                gazebuilder.Append("}");
            }
            else if (GetNewSessionProperties(false).Count > 0) //if a session property has changed
            {
                gazebuilder.Append(",");
                gazebuilder.Append("\"properties\":{");
                foreach (var kvp in GetNewSessionProperties(true))
                {
                    if (kvp.Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(kvp.Key, (string)kvp.Value, gazebuilder);
                    }
                    else if (kvp.Value.GetType() == typeof(float))
                    {
                        JsonUtil.SetFloat(kvp.Key, (float)kvp.Value, gazebuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(kvp.Key, kvp.Value, gazebuilder);
                    }
                    gazebuilder.Append(",");
                }
                gazebuilder.Remove(gazebuilder.Length - 1, 1); //remove comma
                gazebuilder.Append("}");
            }

            gazebuilder.Append("}");
            WebPost("gaze", gazebuilder.ToString(), writeToCache);
            gazebuilder.Length = 9;
        }

        #endregion

        #region Sensor
        internal class SensorData
        {
            internal string Name;
            internal string Rate;
            internal float NextRecordTime;
            internal float UpdateInterval;

            internal SensorData(string name, float rate)
            {
                Name = name;
                Rate = string.Format("{0:0.00}", rate);
                if (rate == 0)
                {
                    UpdateInterval = 1 / 10;
                    //Util.logWarning("Initializing Sensor " + name + " at 0 hz! Defaulting to 10hz");
                }
                else
                {
                    UpdateInterval = 1 / rate;
                }
            }
        }

        static Dictionary<string, SensorData> sensorData = new Dictionary<string, SensorData>();
        private static int sensorJsonPart = 1;
        private static Dictionary<string, List<string>> CachedSnapshots = new Dictionary<string, List<string>>();
        private static int currentSensorSnapshots = 0;
        static StringBuilder sbdatapoint = new StringBuilder(256);

        internal static void RecordSensor(string category, float value, double unixTimestamp)
        {
            if (!IsInitialized) { return; }
            if (!CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots.Add(category, new List<string>());
            }
            CachedSnapshots[category].Add(GetSensorDataToString(unixTimestamp, value));
            currentSensorSnapshots++;
            if (currentSensorSnapshots >= SensorThreshold)
            {
                SerializeSensors(false);
            }
        }

        private static void SerializeSensors(bool writeToCache)
        {
            if (currentSensorSnapshots == 0) { return; }

            //write metadata for serialized object
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{");
            JsonUtil.SetString("name", DeviceId, sb);
            sb.Append(",");
            if (!string.IsNullOrEmpty(LobbyId))
            {
                JsonUtil.SetString("lobbyId", LobbyId, sb);
                sb.Append(",");
            }
            JsonUtil.SetString("sessionid", SessionId, sb);
            sb.Append(",");
            JsonUtil.SetInt("timestamp", (int)SessionTimestamp, sb);
            sb.Append(",");
            JsonUtil.SetInt("part", sensorJsonPart, sb);
            sb.Append(",");
            sensorJsonPart++;
            JsonUtil.SetString("formatversion", "2.0", sb);
            sb.Append(",");
            sb.Append("\"data\":[");

            //serialize each sensor into separate objects
            foreach (var k in CachedSnapshots.Keys)
            {
                sb.Append("{");
                JsonUtil.SetString("name", k, sb);
                sb.Append(",");
                if (sensorData.ContainsKey(k))
                {
                    JsonUtil.SetString("sensorHzLimitType", sensorData[k].Rate, sb);
                    sb.Append(",");
                    if (sensorData[k].UpdateInterval >= 0.1f)
                    {
                        JsonUtil.SetString("sensorHzLimited", "true", sb);
                        sb.Append(",");
                    }
                }

                //put each data point (already a timestamp/value pair) into an array
                sb.Append("\"data\":[");
                foreach (var v in CachedSnapshots[k])
                {
                    sb.Append(v);
                    sb.Append(",");
                }

                //remove last comma from data array
                if (CachedSnapshots[k].Count > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                }

                //end object
                sb.Append("]");
                sb.Append("}");
                sb.Append(",");
            }

            //remove last comma from the array of sensor objects
            if (CachedSnapshots.Keys.Count > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            //close serialized object
            sb.Append("]}");

            //cleanup and send web request
            foreach (var k in CachedSnapshots)
            {
                k.Value.Clear();
            }
            currentSensorSnapshots = 0;
            WebPost("sensor", sb.ToString(), writeToCache);
        }

        
        //put this into the list of saved sensor data based on the name of the sensor
        private static string GetSensorDataToString(double timestamp, double sensorvalue)
        {
            //TODO test if string concatenation is just faster/less garbage

            sbdatapoint.Length = 0;

            sbdatapoint.Append("[");
            sbdatapoint.ConcatDouble(timestamp);
            //sbdatapoint.Append(timestamp);
            sbdatapoint.Append(",");
            sbdatapoint.ConcatDouble(sensorvalue);
            //sbdatapoint.Append(sensorvalue);
            sbdatapoint.Append("]");

            return sbdatapoint.ToString();
        }

        static void ResetSensors()
        {
            sensorData.Clear();
            sensorJsonPart = 1;
            CachedSnapshots.Clear();
            currentSensorSnapshots = 0;
            sbdatapoint.Length = 0;
        }

        #endregion

        #region Dynamic

        static int DynamicJsonPart;
        //marks the looping coroutine as 'ready' to pull data from the queue on a separate thread
        static bool ReadyToWriteJson;
        static bool InterruptThread;

        private static Queue<DynamicObjectSnapshot> queuedSnapshots = new Queue<DynamicObjectSnapshot>();
        private static Queue<DynamicObjectManifestEntry> queuedManifest = new Queue<DynamicObjectManifestEntry>();

        internal static int DynamicSnapshotsCount = 0;

        static void InitializeDynamicSnapshotPool()
        {
            //TODO EVENTUALLY replace coroutine with a thread
            Cognitive3D_Manager.Instance.StartCoroutine(CheckWriteJson());
            for (int i = 0; i < DynamicThreshold; i++)
            {
                DynamicObjectSnapshot.SnapshotPool.Enqueue(new DynamicObjectSnapshot());
            }
        }

        static void ResetDynamics()
        {
            DynamicJsonPart = 1;
            ReadyToWriteJson = false;
            InterruptThread = false;
            queuedSnapshots.Clear();
            queuedManifest.Clear();
            DynamicSnapshotsCount = 0;
        }

        internal static void WriteControllerManifestEntry(DynamicData data)
        {
            if (!IsInitialized) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            dome.controllerType = data.ControllerType;
            dome.isController = true;
            if (data.IsRightHand)
            {
                dome.Properties = "{\"controller\": \"right\"}";
            }
            else
            {
                dome.Properties = "{\"controller\": \"left\"}";
            }
            dome.HasProperties = true;

            queuedManifest.Enqueue(dome);
            DynamicSnapshotsCount++;
            if (DynamicSnapshotsCount > DynamicThreshold)
            {
                DynamicSnapshotsCount = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        internal static void WriteDynamicMediaManifestEntry(DynamicData data, string videourl)
        {
            if (!IsInitialized) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);
            dome.videoURL = videourl;

            queuedManifest.Enqueue(dome);
            DynamicSnapshotsCount++;
            if (DynamicSnapshotsCount > DynamicThreshold)
            {
                DynamicSnapshotsCount = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        internal static void WriteDynamicManifestEntry(DynamicData data, string formattedProperties)
        {
            if (!IsInitialized) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            dome.HasProperties = true;
            dome.Properties = formattedProperties;

            queuedManifest.Enqueue(dome);
            DynamicSnapshotsCount++;
            if (DynamicSnapshotsCount > DynamicThreshold)
            {
                DynamicSnapshotsCount = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        /// <summary>
        /// put data into dynamic manifest
        /// </summary>
        /// <param name="data"></param>
        internal static void WriteDynamicManifestEntry(DynamicData data)
        {
            if (!IsInitialized) { return; }
            DynamicObjectManifestEntry dome = new DynamicObjectManifestEntry(data.Id, data.Name, data.MeshName);

            queuedManifest.Enqueue(dome);
            DynamicSnapshotsCount++;
            if (DynamicSnapshotsCount > DynamicThreshold)
            {
                DynamicSnapshotsCount = 0;
                ReadyToWriteJson = true;
            }
        }

        internal static void WriteDynamic(DynamicData data, string props, bool writeScale, double time)
        {
            if (!IsInitialized) { return; }
            var s = DynamicObjectSnapshot.GetSnapshot();
            s.Id = data.Id;
            s.posX = data.LastPosition.x;
            s.posY = data.LastPosition.y;
            s.posZ = data.LastPosition.z;
            s.rotX = data.LastRotation.x;
            s.rotY = data.LastRotation.y;
            s.rotZ = data.LastRotation.z;
            s.rotW = data.LastRotation.w;

            if (writeScale)
            {
                s.DirtyScale = true;
                s.scaleX = data.LastScale.x;
                s.scaleY = data.LastScale.y;
                s.scaleZ = data.LastScale.z;
            }
            s.Properties = props;
            s.Timestamp = time;

            queuedSnapshots.Enqueue(s);
            DynamicSnapshotsCount++;
            if (DynamicSnapshotsCount > DynamicThreshold)
            {
                DynamicSnapshotsCount = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        //button properties are formated as   ,"buttons":{"input":value,"input":value}
        internal static void WriteDynamicController(DynamicData data, string props, bool writeScale, string jbuttonstates, double time)
        {
            if (!IsInitialized) { return; }
            var s = DynamicObjectSnapshot.GetSnapshot();
            s.Id = data.Id;
            s.posX = data.LastPosition.x;
            s.posY = data.LastPosition.y;
            s.posZ = data.LastPosition.z;
            s.rotX = data.LastRotation.x;
            s.rotY = data.LastRotation.y;
            s.rotZ = data.LastRotation.z;
            s.rotW = data.LastRotation.w;

            if (writeScale)
            {
                s.DirtyScale = true;
                s.scaleX = data.LastScale.x;
                s.scaleY = data.LastScale.y;
                s.scaleZ = data.LastScale.z;
            }
            s.Properties = props;
            props = null;
            s.Buttons = jbuttonstates;

            s.Timestamp = time;

            queuedSnapshots.Enqueue(s);
            DynamicSnapshotsCount++;
            if (DynamicSnapshotsCount > DynamicThreshold)
            {
                DynamicSnapshotsCount = 0;
                ReadyToWriteJson = true; //mark the coroutine as ready to pull from the queue
            }
        }

        //TODO eventually this should just be a loop on a thread, not a coroutine
        static IEnumerator CheckWriteJson()
        {
            while (true)
            {
                if (ReadyToWriteJson) //threading and waiting
                {
                    InterruptThread = false;
                    int totalDataToWrite = queuedManifest.Count + queuedSnapshots.Count;
                    totalDataToWrite = Mathf.Min(totalDataToWrite, DynamicThreshold);

                    var builder = new System.Text.StringBuilder(200 + 128 * totalDataToWrite);
                    int manifestCount = Mathf.Min(queuedManifest.Count, totalDataToWrite);
                    int count = Mathf.Min(queuedSnapshots.Count, totalDataToWrite - manifestCount);

                    bool threadDone = true;
                    bool encounteredError = false;

                    builder.Append("{");

                    //header
                    JsonUtil.SetString("userid", DeviceId, builder);
                    builder.Append(",");

                    if (!string.IsNullOrEmpty(LobbyId))
                    {
                        JsonUtil.SetString("lobbyId", LobbyId, builder);
                        builder.Append(",");
                    }

                    JsonUtil.SetDouble("timestamp", (int)SessionTimestamp, builder);
                    builder.Append(",");
                    JsonUtil.SetString("sessionid", SessionId, builder);
                    builder.Append(",");
                    JsonUtil.SetInt("part", DynamicJsonPart, builder);
                    builder.Append(",");
                    DynamicJsonPart++;
                    JsonUtil.SetString("formatversion", "1.0", builder);

                    //manifest entries
                    if (manifestCount > 0)
                    {
                        builder.Append(",\"manifest\":{");
                        threadDone = false;
                        Queue<DynamicObjectManifestEntry> copyQueue = new Queue<DynamicObjectManifestEntry>(queuedManifest);

                        new Thread(() =>
                        {
                            try
                            {
                                for (int i = 0; i < manifestCount; i++)
                                {
                                    if (i != 0)
                                        builder.Append(',');
                                    //var manifestentry = queuedManifest.Dequeue();
                                    var manifestentry = copyQueue.Dequeue();
                                    SetManifestEntry(manifestentry, builder);
                                    //numberOfEntriesCopied++;
                                }
                            }
                            catch
                            {
                                encounteredError = true;
                            }
                            threadDone = true;
                        }).Start();

                        while (!threadDone && !encounteredError)
                        {
                            yield return null;
                        }

                        //compare 
                        builder.Append("}");
                    }

                    //check if this logic can be skipped because it will be invalidated
                    if (!InterruptThread && !encounteredError)
                    {
                        //snapshots
                        if (count > 0)
                        {
                            builder.Append(",\"data\":[");
                            threadDone = false;

                            Queue<DynamicObjectSnapshot> copyQueue = new Queue<DynamicObjectSnapshot>(queuedSnapshots);
                            new Thread(() =>
                            {
                                try
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        if (i != 0)
                                            builder.Append(',');
                                        var snap = copyQueue.Dequeue();
                                        SetSnapshot(snap, builder);
                                        //snap.ReturnToPool();
                                    }
                                }
                                catch
                                {
                                    encounteredError = true;
                                }
                                threadDone = true;
                            }).Start();

                            while (!threadDone && !encounteredError)
                            {
                                yield return null;
                            }
                            builder.Append("]");
                        }
                        builder.Append("}");
                    }

                    if (!InterruptThread && !encounteredError)
                    {
                        //if this coroutine reached here and the thread hasn't been interrupted (from flushdata) and encounter no errors
                        //then remove entries and snapshots from real queues
                        try
                        {
                            for (int i = 0; i < manifestCount; i++)
                            {
                                queuedManifest.Dequeue();
                            }
                            for (int i = 0; i < count; i++)
                            {
                                var snap = queuedSnapshots.Dequeue();
                                snap.ReturnToPool();
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }


                        if (queuedSnapshots.Count == 0 && queuedManifest.Count == 0)
                        {
                            ReadyToWriteJson = false;
                        }

                        string s = builder.ToString();
                        WebPost("dynamic", s, false);
                    }
                }
                else //wait to write data
                {
                    yield return null;
                }
            }
        }

        //writes a batch of data on main thread
        //called on flush
        static void SerializeDynamicImmediate(bool copyDataToCache)
        {
            int totalDataToWrite = queuedManifest.Count + queuedSnapshots.Count;
            totalDataToWrite = Mathf.Min(totalDataToWrite, DynamicThreshold);

            var builder = new StringBuilder(200 + 128 * totalDataToWrite);
            int manifestCount = Mathf.Min(queuedManifest.Count, totalDataToWrite);
            int count = Mathf.Min(queuedSnapshots.Count, totalDataToWrite - manifestCount);

            builder.Append("{");

            //header
            JsonUtil.SetString("userid", DeviceId, builder);
            builder.Append(",");

            if (!string.IsNullOrEmpty(LobbyId))
            {
                JsonUtil.SetString("lobbyId", LobbyId, builder);
                builder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", (int)SessionTimestamp, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionid", SessionId, builder);
            builder.Append(",");
            JsonUtil.SetInt("part", DynamicJsonPart, builder);
            builder.Append(",");
            DynamicJsonPart++;
            JsonUtil.SetString("formatversion", "1.0", builder);

            //manifest entries
            if (manifestCount > 0)
            {
                builder.Append(",\"manifest\":{");
                for (int i = 0; i < manifestCount; i++)
                {
                    if (i != 0)
                        builder.Append(',');
                    var manifestentry = queuedManifest.Dequeue();
                    SetManifestEntry(manifestentry, builder);
                }
                builder.Append("}");
            }

            //snapshots
            if (count > 0)
            {
                builder.Append(",\"data\":[");
                for (int i = 0; i < count; i++)
                {
                    if (i != 0)
                        builder.Append(',');
                    var snap = queuedSnapshots.Dequeue();
                    SetSnapshot(snap, builder);
                    snap.ReturnToPool();
                }
                builder.Append("]");
            }
            builder.Append("}");

            string s = builder.ToString();
            WebPost("dynamic", s, copyDataToCache);
        }

        static void SetManifestEntry(DynamicObjectManifestEntry entry, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(entry.Id);
            builder.Append("\":{");


            if (!string.IsNullOrEmpty(entry.Name))
            {
                JsonUtil.SetString("name", entry.Name, builder);
                builder.Append(",");
            }
            JsonUtil.SetString("mesh", entry.MeshName, builder);
            builder.Append(",");
            JsonUtil.SetString("fileType", "gltf", builder);
            //JsonUtil.SetString("fileType", DynamicObjectManifestEntry.FileType, builder);

            if (entry.isVideo)
            {
                JsonUtil.SetString("externalVideoSource", entry.videoURL, builder);
            }

            if (entry.isController)
            {
                builder.Append(",");
                JsonUtil.SetString("controllerType", entry.controllerType, builder);
            }

            //properties should already be formatted, just need to append them here
            if (!string.IsNullOrEmpty(entry.Properties))
            {
                builder.Append(",\"properties\":[");
                builder.Append(entry.Properties);
                builder.Append("]");
            }

            builder.Append("}"); //close manifest entry
        }

        static void SetSnapshot(DynamicObjectSnapshot snap, StringBuilder builder)
        {
            builder.Append('{');

            JsonUtil.SetString("id", snap.Id, builder);
            builder.Append(',');
            JsonUtil.SetDouble("time", snap.Timestamp, builder);
            builder.Append(',');
            JsonUtil.SetVectorRaw("p", snap.posX, snap.posY, snap.posZ, builder);
            builder.Append(',');
            JsonUtil.SetQuatRaw("r", snap.rotX, snap.rotY, snap.rotZ, snap.rotW, builder);
            if (snap.DirtyScale)
            {
                builder.Append(',');
                JsonUtil.SetVectorRaw("s", snap.scaleX, snap.scaleY, snap.scaleZ, builder);
            }

            //properties should already be formatted, just need to append them here
            if (!string.IsNullOrEmpty(snap.Properties))
            {
                builder.Append(",\"properties\":[");
                builder.Append(snap.Properties);
                builder.Append("]");
            }

            if (!string.IsNullOrEmpty(snap.Buttons))
            {
                builder.Append(",\"buttons\":{");
                builder.Append(snap.Buttons);
                builder.Append("}");
            }

            builder.Append("}"); //close object snapshot
        }

        #endregion

        #region Exitpoll
        //TODO list of some generic class that can be serialized instead of Cognitive3D.ExitPollSet.ResponseContext
        internal static string FormatExitpoll(List<Cognitive3D.ExitPollSet.ResponseContext> responseProperties, string QuestionSetId, string hook, string sceneId, int versionNumber, int versionId)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            JsonUtil.SetString("userId", DeviceId, builder);
            builder.Append(",");
            if (!string.IsNullOrEmpty(ParticipantId))
            {
                JsonUtil.SetString("participantId", ParticipantId, builder);
                builder.Append(",");
            }
            if (!string.IsNullOrEmpty(LobbyId))
            {
                JsonUtil.SetString("lobbyId", LobbyId, builder);
                builder.Append(",");
            }
            JsonUtil.SetString("questionSetId", QuestionSetId, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionId", SessionId, builder);
            builder.Append(",");
            JsonUtil.SetString("hook", hook, builder);
            builder.Append(",");

            if (!string.IsNullOrEmpty(sceneId))
            {
                JsonUtil.SetString("sceneId", sceneId, builder);
                builder.Append(",");
                JsonUtil.SetInt("versionNumber", versionNumber, builder);
                builder.Append(",");
                JsonUtil.SetInt("versionId", versionId, builder);
                builder.Append(",");
            }

            builder.Append("\"answers\":[");

            for (int i = 0; i < responseProperties.Count; i++)
            {
                var valueString = responseProperties[i].ResponseValue as string;
                if (!string.IsNullOrEmpty(valueString) && valueString == "skip")
                {
                    builder.Append("null,");
                }
                else
                {
                    builder.Append("{");
                    JsonUtil.SetString("type", responseProperties[i].QuestionType, builder);
                    builder.Append(",\"value\":");

                    if (!string.IsNullOrEmpty(valueString))
                    {
                        builder.Append("\"");
                        builder.Append(valueString);
                        builder.Append("\"");
                    }
                    else if (responseProperties[i].ResponseValue is bool)
                    {
                        builder.Append(((bool)responseProperties[i].ResponseValue).ToString().ToLower());
                    }
                    else if (responseProperties[i].ResponseValue is int)
                    {
                        builder.Append((int)responseProperties[i].ResponseValue);
                    }
                    else
                    {
                        builder.Append("\"\"");
                    }

                    builder.Append("},");
                }
            }
            builder.Remove(builder.Length - 1, 1); //remove comma
            builder.Append("]");
            builder.Append("}");

            return builder.ToString();
        }
        #endregion
    }
}