using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    public class MenuItems
    {
        [MenuItem("cognitive3D/Add Cognitive Manager", priority = 0)]
        static void Cognitive3DManager()
        {
            var found = Object.FindObjectOfType<CognitiveVR_Manager>();
            if (found != null)
            {
                Selection.activeGameObject = found.gameObject;
                return;
            }
            else
            {
                //spawn prefab
                GameObject newManager = new GameObject("CognitiveVR_Manager");
                Selection.activeGameObject = newManager;
                Undo.RegisterCreatedObjectUndo(newManager, "Create CognitiveVR Manager");
                newManager.AddComponent<CognitiveVR_Manager>();
            }
        }

        [MenuItem("cognitive3D/Open Web Dashboard...", priority = 5)]
        static void Cognitive3DDashboard()
        {
            Application.OpenURL("http://dashboard.cognitivevr.io");
        }

        [MenuItem("cognitive3D/Check for Updates...", priority = 10)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
            //Application.OpenURL("http://dashboard.cognitivevr.io");
        }

        [MenuItem("cognitive3D/Scene Setup", priority = 55)]
        static void Cognitive3DSceneSetup()
        {
            //open window
            InitWizard.Init();
        }

        [MenuItem("cognitive3D/Manage Dynamic Objects", priority = 60)]
        static void Cognitive3DManageDynamicObjects()
        {
            //open window
            ManageDynamicObjects.Init();
            //CognitiveVR_ObjectManifestWindow.Init();
        }

        [MenuItem("cognitive3D/Advanced Options", priority = 65)]
        static void Cognitive3DOptions()
        {
            //open window
            Selection.activeObject = EditorCore.GetPreferences();

            //CognitiveVR_ComponentSetup.Init();
            //CognitiveVR_Settings.Init();
        }
    }
}