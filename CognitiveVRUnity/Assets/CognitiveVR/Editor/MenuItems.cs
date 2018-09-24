using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    public class MenuItems
    {
#if CVR_NEURABLE
        public const string Menu = "Neurable/Analytics Portal/";
#else
        public const string Menu = "cognitive3D/";
#endif

        [MenuItem(Menu + "Add Analytics Manager", priority = 0)]
        static void Cognitive3DManager()
        {
            var found = Object.FindObjectOfType<CognitiveVR_Manager>();
            if (found != null)
            {
#if CVR_NEURABLE
                Neurable.Analytics.Portal.NeurableCognitiveMenu.InstantiateAnalyticsManager();
#endif
                Selection.activeGameObject = found.gameObject;
                return;
            }
            else
            {
                EditorCore.SpawnManager(EditorCore.DisplayValue(DisplayKey.ManagerName));
            }
        }

        [MenuItem(Menu + "Open Web Dashboard...", priority = 5)]
        static void Cognitive3DDashboard()
        {
            Application.OpenURL("https://" + CognitiveVR_Preferences.Instance.Dashboard);
        }

        [MenuItem(Menu + "Check for Updates...", priority = 10)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
        }

        [MenuItem(Menu + "Scene Setup", priority = 55)]
        static void Cognitive3DSceneSetup()
        {
            //open window
            InitWizard.Init();
        }

        [MenuItem(Menu + "360 Setup", priority = 56)]
        static void Cognitive3D360Setup()
        {
            //open window
            Setup360Window.Init();
        }

        [MenuItem(Menu + "Manage Dynamic Objects", priority = 60)]
        static void Cognitive3DManageDynamicObjects()
        {
            //open window
            ManageDynamicObjects.Init();
        }

        [MenuItem(Menu + "Advanced Options", priority = 65)]
        static void Cognitive3DOptions()
        {
            //select asset
            Selection.activeObject = EditorCore.GetPreferences();
        }

        [MenuItem(Menu + "Refresh Media", priority = 110)]
        static void RefreshMediaSources()
        {
            EditorCore.RefreshMediaSources();
        }
    }
}