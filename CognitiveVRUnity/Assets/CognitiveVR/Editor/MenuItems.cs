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
    }
}