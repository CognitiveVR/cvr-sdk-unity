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
            Application.OpenURL("https://"+CognitiveVR_Preferences.Instance.Dashboard);
        }

        [MenuItem("cognitive3D/Check for Updates...", priority = 10)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
        }

        [MenuItem("cognitive3D/Scene Setup", priority = 55)]
        static void Cognitive3DSceneSetup()
        {
            //open window
            InitWizard.Init();
        }

        [MenuItem("cognitive3D/360 Setup", priority = 56)]
        static void Cognitive3D360Setup()
        {
            //open window
            Setup360Window.Init();
        }

        [MenuItem("cognitive3D/Manage Dynamic Objects", priority = 60)]
        static void Cognitive3DManageDynamicObjects()
        {
            //open window
            ManageDynamicObjects.Init();
        }

        [MenuItem("cognitive3D/Advanced Options", priority = 65)]
        static void Cognitive3DOptions()
        {
            //select asset
            Selection.activeObject = EditorCore.GetPreferences();
        }
    }
}