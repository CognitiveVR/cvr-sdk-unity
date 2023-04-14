using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class MenuItems
    {
        [MenuItem("Cognitive3D/Preferences", priority = 5)]
        static void Cognitive3DOptions()
        {
            Selection.activeObject = EditorCore.GetPreferences();
        }
        [MenuItem("Cognitive3D/Help", priority = 10)]
        static void Cognitive3DHelp()
        {
            HelpWindow.Init();
        }
        [MenuItem("Cognitive3D/Project Setup", priority = 15)]
        static void Cognitive3DProjectSetup()
        {
            ProjectSetupWindow.Init();
        }
        [MenuItem("Cognitive3D/Scene Setup", priority = 20)]
        static void Cognitive3DSceneSetup()
        {
            SceneSetupWindow.Init();
        }
        [MenuItem("Cognitive3D/Dynamic Objects", priority = 25)]
        static void Cognitive3DManageDynamicObjects()
        {
            DynamicObjectsWindow.Init();
        }
        [MenuItem("Cognitive3D/360 Setup", priority = 30)]
        static void Cognitive3D360Setup()
        {
            Setup360Window.Init();
        }


        [MenuItem("Cognitive3D/Open Web Dashboard...", priority = 55)]
        static void Cognitive3DDashboard()
        {
            Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Dashboard);
        }
        [MenuItem("Cognitive3D/Documentation...", priority = 60)]
        static void Cognitive3DDocumentation()
        {
            Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Documentation);
        }
        [MenuItem("Cognitive3D/Check for Updates...", priority = 65)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
        }
    }
}