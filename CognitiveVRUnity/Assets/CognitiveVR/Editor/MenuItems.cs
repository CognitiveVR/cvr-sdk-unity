using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    public class MenuItems
    {
#if CVR_FOVE
        [MenuItem("cognitiveVR/Add Fove Prefab",priority=52)]
        static void MakeFovePrefab()
        {
            GameObject player = new GameObject("Player");
            GameObject foveInterface = new GameObject("Fove Interface");
            GameObject cameraboth = new GameObject("Fove Eye Camera both");
            GameObject cameraright = new GameObject("Fove Eye Camera right");
            GameObject cameraleft = new GameObject("Fove Eye Camera left");

            foveInterface.transform.SetParent(player.transform);

            cameraboth.transform.SetParent(foveInterface.transform);
            cameraright.transform.SetParent(foveInterface.transform);
            cameraleft.transform.SetParent(foveInterface.transform);

            var tempInterface = foveInterface.AddComponent<FoveInterface>();

            var rightcam = cameraright.AddComponent<FoveEyeCamera>();
            rightcam.whichEye = Fove.EFVR_Eye.Right;
            //cameraright.AddComponent<Camera>();

            var leftcam = cameraleft.AddComponent<FoveEyeCamera>();
            leftcam.whichEye = Fove.EFVR_Eye.Left;
            //cameraleft.AddComponent<Camera>();

            cameraboth.AddComponent<Camera>();
            cameraboth.tag = "MainCamera";
            Undo.RecordObjects(new Object[] { player, foveInterface, cameraboth, cameraright, cameraleft }, "Create Fove Prefab");
            Undo.FlushUndoRecordObjects();

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
#endif
        [MenuItem("cognitiveVR/Add cognitiveVR Manager", priority = 51)]
        static void AddCognitiveVRManager()
        {
            CognitiveVR_ComponentSetup.AddCognitiveVRManager();
        }

#if CVR_PUPIL
        [MenuItem("cognitiveVR/Add Pupil Labs Vive Prefab", priority = 53)]
        static void AddPupilLabsVivePrefab()
        {
            GameObject maincam = new GameObject("Main Camera");
            GameObject pupilgaze = new GameObject("PupilGaze");
            GameObject calibrationcam = new GameObject("Calibration Camera");

            GameObject canvas = new GameObject("Canvas");
            GameObject calib = new GameObject("Calibration Target");
            GameObject left = new GameObject("Left Eye");
            GameObject right = new GameObject("Right Eye");
            GameObject center = new GameObject("Center");


            //main camera
            var cam = maincam.AddComponent<Camera>();
            maincam.tag = "MainCamera";
            cam.depth = -5;
            cam.farClipPlane = 200;

            //pupil gaze tracker
            var gazetracker = pupilgaze.AddComponent<PupilGazeTracker>();
            gazetracker.ServerIP = "127.0.0.1";

            //calibration camera
            var calibcam = calibrationcam.AddComponent<Camera>();
            calibcam.clearFlags = CameraClearFlags.Depth;
            calibcam.cullingMask = 1 << 5; //UI layer
            calibcam.orthographic = true;
            calibcam.orthographicSize = 325.4f;
            calibcam.depth = -1;

            //calibration canvas
            var calibcanvas = canvas.AddComponent<Canvas>();
            calibcanvas.renderMode = RenderMode.ScreenSpaceCamera;
            calibcanvas.worldCamera = calibcam;
            calibcanvas.planeDistance = 569.4f;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            canvas.transform.SetParent(calibrationcam.transform);
            canvas.layer = LayerMask.NameToLayer("UI");

            calib.AddComponent<CanvasRenderer>();
            calib.AddComponent<UnityEngine.UI.Image>(); //this is just a square, not the bullseye
            calib.AddComponent<PupilCalibMarker>();
            calib.transform.SetParent(canvas.transform);
            calib.layer = LayerMask.NameToLayer("UI");

            left.AddComponent<CanvasRenderer>();
            left.AddComponent<UnityEngine.UI.Image>().color = Color.red;
            left.AddComponent<EyeGazeRenderer>().Gaze = PupilGazeTracker.GazeSource.LeftEye;
            left.transform.SetParent(canvas.transform);
            left.transform.localScale = Vector3.one * 0.1f;
            left.layer = LayerMask.NameToLayer("UI");

            right.AddComponent<CanvasRenderer>();
            right.AddComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0, 1);
            right.AddComponent<EyeGazeRenderer>().Gaze = PupilGazeTracker.GazeSource.RightEye;
            right.transform.SetParent(canvas.transform);
            right.transform.localScale = Vector3.one * 0.1f;
            right.layer = LayerMask.NameToLayer("UI");

            center.AddComponent<CanvasRenderer>();
            center.AddComponent<UnityEngine.UI.Image>().color = Color.green;
            center.AddComponent<EyeGazeRenderer>().Gaze = PupilGazeTracker.GazeSource.BothEyes;
            center.transform.SetParent(canvas.transform);
            center.transform.localScale = Vector3.one * 0.33f;
            center.layer = LayerMask.NameToLayer("UI");

            Undo.RecordObjects(new Object[] { maincam, pupilgaze, calibrationcam, canvas, calib, left, right, center }, "Create Pupil Labs Vive Prefab");
            Undo.FlushUndoRecordObjects();

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
#endif
    }
}