using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class MenuItems
    {
        [MenuItem("Cognitive3D/Select Cognitive3D Analytics Manager", priority = 50)]
        static void Cognitive3DManager()
        {
            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found != null)
            {
                Selection.activeGameObject = found.gameObject;
                return;
            }
            else
            {
                EditorCore.SpawnManager(EditorCore.DisplayValue(DisplayKey.ManagerName));
            }
        }

        [MenuItem("Cognitive3D/Open Web Dashboard...", priority = 10)]
        static void Cognitive3DDashboard()
        {
            Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Dashboard);
        }

        [MenuItem("Cognitive3D/Check for Updates...", priority = 5)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
        }

        [MenuItem("Cognitive3D/Scene Setup And Upload", priority = 60)]
        static void Cognitive3DSceneSetup()
        {
            //open window
            InitWizard.Init();
        }

        [MenuItem("Cognitive3D/360 Setup", priority = 65)]
        static void Cognitive3D360Setup()
        {
            //open window
            Setup360Window.Init();
        }

        [MenuItem("Cognitive3D/Manage Dynamic Objects", priority = 55)]
        static void Cognitive3DManageDynamicObjects()
        {
            //open window
            ManageDynamicObjects.Init();
        }

        [MenuItem("Cognitive3D/Preferences", priority = 0)]
        static void Cognitive3DOptions()
        {
            //select asset
            Selection.activeObject = EditorCore.GetPreferences();
        }
    }
}