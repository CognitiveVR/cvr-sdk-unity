using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D.Serialization;
using System;

//this is on the engine side and communicates/registers delegates/handles interop with Serialization class
//eventually this will use loaddll and getdelegate functions and convert data into nice interop formats
//for now, this is just the points where all data passes through to the serializer

namespace Cognitive3D
{
    public static class CoreInterface
    {
        //logs a message in unity
        static System.Action<string> logCall;

        //returns the type of data (event, gaze, dynamic, sensor, fixation) and the body of the web request. also if the data should be cached immediately (flush called on session end)
        static System.Action<string,string, bool> webPost;

        internal static void Initialize(string sessionId, double sessionTimestamp, string deviceId)
        {
            logCall += LogInfo;
            webPost += WebPost;

            SharedCore.SetLogDelegate(logCall);
            SharedCore.SetPostDelegate(webPost);
            SharedCore.InitializeSettings(sessionId, 16, 16, 16, 16, 16,sessionTimestamp,deviceId);
        }

        #region Session

        internal static void SetSessionProperty(string propertyName, object propertyValue)
        {
            SharedCore.SetSessionProperty(propertyName, propertyValue);
        }
        internal static void SetLobby(string lobbyid)
        {
            SharedCore.SetLobbyId(lobbyid);
        }

        #endregion

        #region CustomEvent

        //internal static void RecordCustomEvent(string category, List<KeyValuePair<string, object>> properties, float[] position, string dynamicObjectId = "")
        //{
        //    SharedCore.RecordCustomEvent(category, Util.Timestamp(Time.frameCount), properties, position, dynamicObjectId);
        //}

        internal static void RecordCustomEvent(string category, string dynamicObjectId = "")
        {
            SharedCore.RecordCustomEvent(category, Util.Timestamp(Time.frameCount), null, new float[] { GameplayReferences.HMD.position.x, GameplayReferences.HMD.position.y, GameplayReferences.HMD.position.z }, dynamicObjectId);
        }

        internal static void RecordCustomEvent(string category, Vector3 position, string dynamicObjectId = "")
        {
            SharedCore.RecordCustomEvent(category, Util.Timestamp(Time.frameCount), null, new float[] { position.x, position.y, position.z }, dynamicObjectId);
        }

        internal static void RecordCustomEvent(string category, List<KeyValuePair<string, object>> properties, Vector3 position, string dynamicObjectId = "")
        {
            SharedCore.RecordCustomEvent(category, Util.Timestamp(Time.frameCount), properties, new float[] { position.x, position.y, position.z }, dynamicObjectId);
        }

        #endregion

        #region DynamicObject
        internal static void RegisterDynamicObject(string id)
        {
            //Cognitive3D.Serialization.SharedCore.RegisterDynamicObject()
        }

        internal static void RecordDynamicObject()
        {

        }
        #endregion

        #region Gaze

        internal static void RecordWorldGaze(Vector3 position, Quaternion rotation, Vector3 gazePoint, double time)
        {
            SharedCore.RecordGazeWorld(
                new float[] { position.x, position.y, position.z },
                new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                new float[] { gazePoint.x, gazePoint.y, gazePoint.z },
                time);
        }
        internal static void RecordDynamicGaze(Vector3 position, Quaternion rotation, Vector3 gazePoint, string dynamicId, double time)
        {
            SharedCore.RecordGazeDynamic(
                new float[] { position.x, position.y, position.z },
                new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                new float[] { gazePoint.x, gazePoint.y, gazePoint.z },
                dynamicId,
                time);
        }
        internal static void RecordMediaGaze(Vector3 position, Quaternion rotation, Vector3 gazePoint, string dynamicId,string mediaId, double time, int mediatime, Vector2 uv)
        {
            SharedCore.RecordGazeMedia(
                new float[] { position.x, position.y, position.z },
                new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                new float[] { gazePoint.x, gazePoint.y, gazePoint.z },
                dynamicId,
                mediaId,
                time,
                mediatime,
                new float[] {uv.x,uv.y}
                );
        }
        internal static void RecordSkyGaze(Vector3 position, Quaternion rotation, double time)
        {
            SharedCore.RecordGazeSky(new float[] { position.x, position.y, position.z }, new float[] { rotation.x, rotation.y, rotation.z, rotation.w }, time);
        }
        #endregion

        #region Fixation

        internal static void FixationSettings(int maxBlinkMS, int preBlinkDiscardMS, int blinkEndWarmupMS, int minFixationMS, int maxConsecutiveDiscardMS, float maxfixationAngle, int maxConsecutiveOffDynamic, float dynamicFixationSizeMultiplier, AnimationCurve focusSizeFromCenter, int saccadefixationEndMS)
        {
            //also send a delegate to announce when a new fixation has begun/end
            SharedCore.FixationInitialize(maxBlinkMS, preBlinkDiscardMS, blinkEndWarmupMS, minFixationMS, maxConsecutiveDiscardMS, maxfixationAngle, maxConsecutiveOffDynamic, dynamicFixationSizeMultiplier, focusSizeFromCenter, saccadefixationEndMS);
        }

        internal static void RecordEyeData(EyeCapture data)
        {
            double time = data.Time;
            float[] worldPosition = new float[] { data.WorldPosition.x, data.WorldPosition.y, data.WorldPosition.z };
            float[] hmdposition = new float[] { data.HmdPosition.x, data.HmdPosition.y, data.HmdPosition.z };
            float[] screenposition = new float[] { data.ScreenPos.x, data.ScreenPos.y };
            bool blinking = data.EyesClosed;
            string dynamicId = data.HitDynamicId;
            Matrix4x4 dynamicMatrix = data.CaptureMatrix;

            SharedCore.RecordEyeData(time, worldPosition, hmdposition, screenposition, blinking, dynamicId, dynamicMatrix);
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
        public static string SerializeExitpollAnswers(List<ExitPollSet.ResponseContext> responses, string questionSetId,string hook)
        {
            return SharedCore.FormatExitpoll(responses,questionSetId,hook);
        }
        #endregion

        internal static void Flush()
        {

            //TODO interrupt dynamic object thread and just write everything immediately
        }

        /// <summary>
        /// clear all saved variables in shared core
        /// </summary>
        internal static void Reset()
        {
            
        }

        static void LogInfo(string info)
        {
            Debug.Log(info);
        }

        static void WebPost(string requestType, string body, bool cache)
        {
            //construct url from requesttype and cognitivestatics
            string url = string.Empty;
            switch (requestType)
            {
                case "event": url = CognitiveStatics.POSTEVENTDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);break;
            }


            Cognitive3D_Manager.NetworkManager.Post(url, body);
        }
    }
}