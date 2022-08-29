using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cognitive3D;
using System.Text;
//TODO merge gazecore and gaze base.
namespace Cognitive3D
{
    public enum GazeType
    {
        //TODO add skysphere 360 video recorder
        Physics, //raycast
        Command //command buffer
    }

    public static class GazeCore
    {
        public static event Cognitive3D_Manager.onSendData OnGazeSend;
        internal static void GazeSendEvent()
        {
            if (OnGazeSend != null)
                OnGazeSend.Invoke(false);
        }

        public delegate void onGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation);
        public static event onGazeRecord OnDynamicGazeRecord;
        public static void DynamicGazeRecordEvent(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnDynamicGazeRecord != null)
                OnDynamicGazeRecord.Invoke(timestamp,objectid,localgazepoint,hmdpoint,hmdrotation);
        }
        public static event onGazeRecord OnWorldGazeRecord;
        public static void WorldGazeRecordEvent(double timestamp, string ignored, Vector3 worldgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnWorldGazeRecord != null)
                OnWorldGazeRecord.Invoke(timestamp, ignored, worldgazepoint, hmdpoint, hmdrotation);
        }
        public static event onGazeRecord OnSkyGazeRecord;
        public static void SkyGazeRecordEvent(double timestamp, string ignored, Vector3 ignored2, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnSkyGazeRecord != null)
                OnSkyGazeRecord.Invoke(timestamp, ignored, ignored2, hmdpoint, hmdrotation);
        }

        //public static void SetHMDType(string hmdname)
        //{
        //    HMDName = hmdname;
        //}

        ///sky position
        public static void RecordGazePoint(double timestamp, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos, bool validFloor) //looking at the camera far plane
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            CoreInterface.RecordSkyGaze(hmdpoint, hmdrotation, timestamp, floorPos,validFloor);

            SkyGazeRecordEvent(timestamp, string.Empty, Vector3.zero, hmdpoint, hmdrotation);
        }

        //gaze on dynamic object
        public static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos, bool validFloor) //looking at a dynamic object
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            CoreInterface.RecordDynamicGaze(hmdpoint, hmdrotation, localgazepoint, objectid, timestamp, floorPos, validFloor);
            
            DynamicGazeRecordEvent(timestamp, objectid, localgazepoint, hmdpoint, hmdrotation);
        }

        //world position
        public static void RecordGazePoint(double timestamp, Vector3 gazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, Vector3 floorPos, bool validFloor) //looking at world
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            CoreInterface.RecordWorldGaze(hmdpoint, hmdrotation, gazepoint, timestamp, floorPos, validFloor);
            
            WorldGazeRecordEvent(timestamp, string.Empty, gazepoint, hmdpoint, hmdrotation);
        }

        //looking at a media dynamic object
        //mediatime is milliseconds since the start of the video
        public static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation, Vector3 gpsloc, float compass, string mediasource, int mediatimeMs, Vector2 uvs, Vector3 floorPos, bool validFloor)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Gaze recorded without SceneId"); return; }

            CoreInterface.RecordMediaGaze(hmdpoint, hmdrotation, localgazepoint, objectid, mediasource, timestamp, mediatimeMs, uvs, floorPos, validFloor);
            
            DynamicGazeRecordEvent(timestamp, objectid, localgazepoint, hmdpoint, hmdrotation);
        }
    }
}