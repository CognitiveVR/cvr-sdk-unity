using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D.Serialization;
using System;

//this is on the engine side and communicates/registers delegates/handles interop with Serialization class
//eventually this will use loaddll and getdelegate functions and convert data into nice interop formats
//for now, this is just the points where all data passes through to the serializer

//TODO CONSIDER holding onto all data here until the end of the frame instead of passing data points through one at a time

namespace Cognitive3D
{
    internal static class CoreInterface
    {
        //logs a message in unity
        static System.Action<string> logCall;

        //returns the type of data (event, gaze, dynamic, sensor, fixation) and the body of the web request. also if the data should be cached immediately (flush called on session end)
        static System.Action<string,string, bool> webPost;

        internal static void Initialize(string sessionId, double sessionTimestamp, string deviceId, string hmdName)
        {
            logCall += LogInfo;
            webPost += WebPost;
            SharedCore.InitializeSettings(sessionId,
                Cognitive3D_Preferences.Instance.EventDataThreshold,
                Cognitive3D_Preferences.Instance.GazeSnapshotCount,
                Cognitive3D_Preferences.Instance.DynamicSnapshotCount,
                Cognitive3D_Preferences.Instance.SensorSnapshotCount,
                Cognitive3D_Preferences.Instance.FixationSnapshotCount,
                sessionTimestamp,
                deviceId,
                webPost,
                logCall,
                hmdName
                );
        }

        #region Session

        internal static void SetSessionProperty(string propertyName, object propertyValue)
        {
            SharedCore.SetSessionProperty(propertyName, propertyValue);
        }
        internal static void SetSessionPropertyIfEmpty(string propertyName, object propertyValue)
        {
            SharedCore.SetSessionPropertyIfEmpty(propertyName, propertyValue);
        }
        internal static void SetLobbyId(string lobbyid)
        {
            SharedCore.SetLobbyId(lobbyid);
        }

        #endregion

        #region CustomEvent

        internal static void RecordCustomEvent(string category, string dynamicObjectId = "")
        {
            SharedCore.RecordCustomEvent(category, Util.Timestamp(Time.frameCount), null, new float[] { GameplayReferences.HMD.position.x, GameplayReferences.HMD.position.y, GameplayReferences.HMD.position.z }, dynamicObjectId);
            CustomEvent.CustomEventRecordedEvent(category, GameplayReferences.HMD.position, null, dynamicObjectId, Util.Timestamp(Time.frameCount));
        }

        internal static void RecordCustomEvent(string category, Vector3 position, string dynamicObjectId = "")
        {
            SharedCore.RecordCustomEvent(category, Util.Timestamp(Time.frameCount), null, new float[] { position.x, position.y, position.z }, dynamicObjectId);
            CustomEvent.CustomEventRecordedEvent(category, position, null, dynamicObjectId, Util.Timestamp(Time.frameCount));
        }

        internal static void RecordCustomEvent(string category, List<KeyValuePair<string, object>> properties, Vector3 position, string dynamicObjectId = "")
        {
            SharedCore.RecordCustomEvent(category, Util.Timestamp(Time.frameCount), properties, new float[] { position.x, position.y, position.z }, dynamicObjectId);
            CustomEvent.CustomEventRecordedEvent(category, position, properties, dynamicObjectId, Util.Timestamp(Time.frameCount));
        }

        #endregion

        #region DynamicObject
        //CONSIDER moving dynamic data array into shared core - engine just provides settings and transform data + dynamics enabling/disabling. initialize handles sending new manifest data?
            //NO engines can be optimized to iterate through dynamic array, so this should be on the engine side
        //should the engine side keep a list of dynamic object data or just pass everything through this interface?
        //should checking for changes happen on engine side?

        internal static void WriteControllerManifestEntry(DynamicData dynamicData)
        {
            SharedCore.WriteControllerManifestEntry(dynamicData);
        }

        internal static void WriteDynamicManifestEntry(DynamicData dynamicData)
        {
            SharedCore.WriteDynamicManifestEntry(dynamicData);
        }

        internal static void WriteDynamic(DynamicData dynamicData, string props, bool writeScale, double time)
        {
            SharedCore.WriteDynamic(dynamicData, props, writeScale, time);
        }

        internal static void WriteDynamicController(DynamicData dynamicData, string props, bool writeScale, string buttonStates, double time)
        {
            SharedCore.WriteDynamicController(dynamicData, props, writeScale, buttonStates, time);
        }
        #endregion

        #region Gaze

        internal static void RecordWorldGaze(Vector3 position, Quaternion rotation, Vector3 gazePoint, double time, Vector3 floorPos, bool useFloor, Vector4 geolocation, bool useGeo)
        {
            SharedCore.RecordGazeWorld(
                new float[] { position.x, position.y, position.z },
                new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                new float[] { gazePoint.x, gazePoint.y, gazePoint.z },
                time,
                new float[] { floorPos.x, floorPos.y, floorPos.z },
                useFloor,
                new float[] { geolocation.x, geolocation.y, geolocation.z, geolocation.w },
                useGeo);
        }
        internal static void RecordDynamicGaze(Vector3 position, Quaternion rotation, Vector3 gazePoint, string dynamicId, double time, Vector3 floorPos, bool useFloor, Vector4 geolocation, bool useGeo)
        {
            SharedCore.RecordGazeDynamic(
                new float[] { position.x, position.y, position.z },
                new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                new float[] { gazePoint.x, gazePoint.y, gazePoint.z },
                dynamicId,
                time,
                new float[] { floorPos.x, floorPos.y, floorPos.z },
                useFloor,
                new float[] { geolocation.x, geolocation.y, geolocation.z, geolocation.w },
                useGeo);
        }
        internal static void RecordMediaGaze(Vector3 position, Quaternion rotation, Vector3 gazePoint, string dynamicId,string mediaId, double time, int mediatime, Vector2 uv, Vector3 floorPos, bool useFloor, Vector4 geolocation, bool useGeo)
        {
            SharedCore.RecordGazeMedia(
                new float[] { position.x, position.y, position.z },
                new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                new float[] { gazePoint.x, gazePoint.y, gazePoint.z },
                dynamicId,
                mediaId,
                time,
                mediatime,
                new float[] {uv.x,uv.y},
                new float[] {floorPos.x,floorPos.y,floorPos.z},
                useFloor,
                new float[] { geolocation.x, geolocation.y, geolocation.z, geolocation.w },
                useGeo
                );
        }
        internal static void RecordSkyGaze(Vector3 position, Quaternion rotation, double time, Vector3 floorPos, bool useFloor, Vector4 geolocation, bool useGeo)
        {
            SharedCore.RecordGazeSky(
                new float[] { position.x, position.y, position.z },
                new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                time,
                new float[] { floorPos.x, floorPos.y, floorPos.z },
                useFloor,
                new float[] { geolocation.x, geolocation.y, geolocation.z, geolocation.w },
                useGeo);
        }
        #endregion

        #region Fixation

        internal static void FixationSettings(int maxBlinkMS, int preBlinkDiscardMS, int blinkEndWarmupMS, int minFixationMS, int maxConsecutiveDiscardMS, float maxfixationAngle, int maxConsecutiveOffDynamic, float dynamicFixationSizeMultiplier, AnimationCurve focusSizeFromCenter, int saccadefixationEndMS)
        {
            //also send a delegate to announce when a new fixation has begun/end. connect that to FixationCore.FixationRecordEvent()
            SharedCore.FixationInitialize(maxBlinkMS, preBlinkDiscardMS, blinkEndWarmupMS, minFixationMS, maxConsecutiveDiscardMS, maxfixationAngle, maxConsecutiveOffDynamic, dynamicFixationSizeMultiplier, focusSizeFromCenter, saccadefixationEndMS, OnFixationRecorded);
        }

        private static void OnFixationRecorded(Fixation obj)
        {
            //pass to ASV or call some other action that things can subscribe to
            FixationRecorder.FixationRecordEvent(obj);
        }

        internal static void RecordEyeData(EyeCapture data, int hitType)
        {
            //double time = data.Time;
            //float[] worldPosition = new float[] { data.WorldPosition.x, data.WorldPosition.y, data.WorldPosition.z };
            //float[] hmdposition = new float[] { data.HmdPosition.x, data.HmdPosition.y, data.HmdPosition.z };
            //float[] screenposition = new float[] { data.ScreenPos.x, data.ScreenPos.y };
            bool blinking = data.EyesClosed;
            string dynamicId = data.HitDynamicId;
            Matrix4x4 dynamicMatrix = data.CaptureMatrix;

            //SharedCore.RecordEyeData(time, worldPosition, hmdposition, screenposition, blinking, dynamicId, dynamicMatrix);
            SharedCore.RecordEyeData(data.Time, data.WorldPosition, data.LocalPosition, data.HmdPosition, data.ScreenPos, blinking, dynamicId, dynamicMatrix, hitType);
        }

        #endregion

        

        #region Sensors
        //sensorrecorder still keeps a dictionary of sensor values and some utility functions. these calls are just for serialization
        internal static void RecordSensor(string name, float value, double time)
        {
            SharedCore.RecordSensor(name, value, time);
        }
        #endregion

        #region Exitpoll
        internal static string SerializeExitpollAnswers(List<ExitPollSet.ResponseContext> responses, string questionSetId,string hook)
        {
            return SharedCore.FormatExitpoll(responses,questionSetId,hook,Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber,Cognitive3D_Manager.TrackingSceneVersionId);
        }
        #endregion

        internal static void Flush(bool copyToCache)
        {
            SharedCore.Flush(copyToCache);
        }

        /// <summary>
        /// clear all saved variables in shared core
        /// </summary>
        internal static void Reset()
        {
            SharedCore.Reset();
            logCall -= LogInfo;
            webPost -= WebPost;
        }

        static void LogInfo(string info)
        {
            Debug.Log(info);
        }

        static void WebPost(string requestType, string body, bool cache)
        {
            //construct url from requesttype and cognitivestatics
            string url;
            switch (requestType)
            {
                case "event":
                    url = CognitiveStatics.POSTEVENTDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);
                    CustomEvent.CustomEventSendEvent();
                    break;
                case "sensor":
                    url = CognitiveStatics.POSTSENSORDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);
                    SensorRecorder.SensorSendEvent();
                    break;
                case "dynamic":
                    url = CognitiveStatics.POSTDYNAMICDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);
                    DynamicManager.DynamicObjectSendEvent();
                    break;
                case "gaze":
                    url = CognitiveStatics.POSTGAZEDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);
                    GazeCore.GazeSendEvent();
                    break;
                case "fixation":
                    url = CognitiveStatics.POSTFIXATIONDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);
                    FixationRecorder.FixationSendEvent();
                    break;
                default: Util.logDevelopment("Invalid Web Post type"); return;
            }

            //TODO CONSIDER shouldn't this be in NetworkManager?
            if (cache && Cognitive3D_Manager.NetworkManager.runtimeCache != null && Cognitive3D_Manager.NetworkManager.runtimeCache.CanWrite(url, body))
            {
                Cognitive3D_Manager.NetworkManager.runtimeCache.WriteContent(url, body);
            }

            Cognitive3D_Manager.NetworkManager.Post(url, body);
        }
    }
}