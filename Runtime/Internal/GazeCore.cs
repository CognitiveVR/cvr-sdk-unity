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
        Physics = 0, //raycast
        Command = 1, //command buffer
        SkySphere = 2 //for 360 media rendered on the sky
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
        internal static void DynamicGazeRecordEvent(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnDynamicGazeRecord != null)
                OnDynamicGazeRecord.Invoke(timestamp,objectid,localgazepoint,hmdpoint,hmdrotation);
        }
        public static event onGazeRecord OnWorldGazeRecord;
        internal static void WorldGazeRecordEvent(double timestamp, string ignored, Vector3 worldgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnWorldGazeRecord != null)
                OnWorldGazeRecord.Invoke(timestamp, ignored, worldgazepoint, hmdpoint, hmdrotation);
        }
        public static event onGazeRecord OnSkyGazeRecord;
        internal static void SkyGazeRecordEvent(double timestamp, string ignored, Vector3 ignored2, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            if (OnSkyGazeRecord != null)
                OnSkyGazeRecord.Invoke(timestamp, ignored, ignored2, hmdpoint, hmdrotation);
        }

        static bool hasDisplayedSceneIdWarning = false;
        static Transform cameraRoot;
        internal static bool GetFloorPosition(ref Vector3 floorPos)
        {
            if (Cognitive3D_Preferences.Instance.RecordFloorPosition)
            {
                if (cameraRoot == null)
                {
                    cameraRoot = GameplayReferences.HMD.root;
                }
                RaycastHit floorhit = new RaycastHit();
                if (Physics.Raycast(GameplayReferences.HMD.position, -cameraRoot.up, out floorhit))
                {
                    if (!floorhit.collider.gameObject.GetComponent<DynamicObject>())
                    {
                        floorPos = floorhit.point;
                        return true;
                    }
                }
            }
            return false;
        }

        ///sky position
        internal static void RecordGazePoint(double timestamp, Vector3 hmdpoint, Quaternion hmdrotation) //looking at the camera far plane
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                if (!hasDisplayedSceneIdWarning)
                {
                    hasDisplayedSceneIdWarning = true;
                    Cognitive3D.Util.logWarning("GazeCore RecordGazePoint invalid SceneId");
                }
                return;
            }

            Vector3 floorPos = new Vector3();
            //if floor position is enabled and if the hmd is over a surface
            bool validFloor = GetFloorPosition(ref floorPos);

            Vector4 geo = new Vector4();
            bool useGeo = false;
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                if (GameplayReferences.TryGetGPSLocation(ref geo))
                {
                    useGeo = true;
                }
            }

            CoreInterface.RecordSkyGaze(hmdpoint, hmdrotation, timestamp, floorPos, validFloor, geo, useGeo);

            SkyGazeRecordEvent(timestamp, string.Empty, Vector3.zero, hmdpoint, hmdrotation);
        }

        //gaze on dynamic object
        internal static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation) //looking at a dynamic object
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                if (!hasDisplayedSceneIdWarning)
                {
                    hasDisplayedSceneIdWarning = true;
                    Cognitive3D.Util.logWarning("GazeCore RecordGazePoint invalid SceneId");
                }
                return;
            }

            Vector3 floorPos = new Vector3();
            //if floor position is enabled and if the hmd is over a surface
            bool validFloor = GetFloorPosition(ref floorPos);

            Vector4 geo = new Vector4();
            bool useGeo = false;
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                if (GameplayReferences.TryGetGPSLocation(ref geo))
                {
                    useGeo = true;
                }
            }

            CoreInterface.RecordDynamicGaze(hmdpoint, hmdrotation, localgazepoint, objectid, timestamp, floorPos, validFloor, geo, useGeo);

            DynamicGazeRecordEvent(timestamp, objectid, localgazepoint, hmdpoint, hmdrotation);
        }

        //world position
        internal static void RecordGazePoint(double timestamp, Vector3 gazepoint, Vector3 hmdpoint, Quaternion hmdrotation) //looking at world
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                if (!hasDisplayedSceneIdWarning)
                {
                    hasDisplayedSceneIdWarning = true;
                    Cognitive3D.Util.logWarning("GazeCore RecordGazePoint invalid SceneId");
                }
                return;
            }

            Vector3 floorPos = new Vector3();
            //if floor position is enabled and if the hmd is over a surface
            bool validFloor = GetFloorPosition(ref floorPos);

            Vector4 geo = new Vector4();
            bool useGeo = false;
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                if (GameplayReferences.TryGetGPSLocation(ref geo))
                {
                    useGeo = true;
                }
            }

            CoreInterface.RecordWorldGaze(hmdpoint, hmdrotation, gazepoint, timestamp, floorPos, validFloor, geo, useGeo);

            WorldGazeRecordEvent(timestamp, string.Empty, gazepoint, hmdpoint, hmdrotation);
        }

        //looking at a media dynamic object
        //mediatime is milliseconds since the start of the video
        internal static void RecordGazePoint(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation, string mediasource, int mediatimeMs, Vector2 uvs)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Gaze cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                if (!hasDisplayedSceneIdWarning)
                {
                    hasDisplayedSceneIdWarning = true;
                    Cognitive3D.Util.logWarning("GazeCore RecordGazePoint invalid SceneId");
                }
                return;
            }

            Vector3 floorPos = new Vector3();
            //if floor position is enabled and if the hmd is over a surface
            bool validFloor = GetFloorPosition(ref floorPos);

            Vector4 geo = new Vector4();
            bool useGeo = false;
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                if (GameplayReferences.TryGetGPSLocation(ref geo))
                {
                    useGeo = true;
                }
            }

            CoreInterface.RecordMediaGaze(hmdpoint, hmdrotation, localgazepoint, objectid, mediasource, timestamp, mediatimeMs, uvs, floorPos, validFloor, geo, useGeo);

            DynamicGazeRecordEvent(timestamp, objectid, localgazepoint, hmdpoint, hmdrotation);
        }
    }
}