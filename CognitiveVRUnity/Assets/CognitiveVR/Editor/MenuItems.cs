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

        [MenuItem(Menu + "Select Cognitive3D Analytics Manager", priority = 0)]
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

        [MenuItem(Menu + "Select Active Session View Canvas", priority = 3)]
        static void SelectSessionView()
        {
            var found = Object.FindObjectOfType<ActiveSession.ActiveSessionView>();
            if (found != null)
            {
                Selection.activeGameObject = found.gameObject;
                return;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t: Prefab activesessionview");
            if (guids.Length > 0)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0])));
                Selection.activeGameObject = instance;
                var asv = instance.GetComponent<ActiveSession.ActiveSessionView>();
                ActiveSession.ActiveSessionViewEditor.SetCameraTarget(asv);
                Undo.RegisterCreatedObjectUndo(instance, "Added Active Session View");
            }

            //add the GazeReticle prefab too
            GazeReticle gazeReticle = Object.FindObjectOfType<GazeReticle>();
            if (gazeReticle == null)
            {
                var reticleAsset = Resources.Load<GameObject>("GazeReticle");
                if (reticleAsset != null)
                {
                    GameObject reticleInstance = (GameObject)PrefabUtility.InstantiatePrefab(reticleAsset);
                    Undo.RegisterCreatedObjectUndo(reticleInstance, "Added Gaze Reticle");
                }
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

        [MenuItem(Menu + "Ready Room Setup", priority = 55)]
        static void Cognitive3DAssessmentSetup()
        {
            //open window
            ReadyRoomSetupWindow.Init();
        }

        [MenuItem(Menu + "Debug Information", priority = 58)]
        static void CognitiveDebugWindow()
        {
            //open window
            DebugInformationWindow.Init();
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

        [MenuItem(Menu + "Fetch Media from Dashboard", priority = 110)]
        static void RefreshMediaSources()
        {
            EditorCore.RefreshMediaSources();
        }
    }
}