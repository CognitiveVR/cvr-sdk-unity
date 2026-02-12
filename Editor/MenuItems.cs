using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class MenuItems
    {
        [MenuItem("Cognitive3D/Project Setup", priority = 5)]
        static void Cognitive3DProjectSetup()
        {
            ProjectSetupWindow.Init();
        }
        [MenuItem("Cognitive3D/Feature Builder", priority = 10)]
        static void Cognitive3DFeatureBuilder()
        {
            FeaturesWindow.Init();
        }
        [MenuItem("Cognitive3D/Project Validation", priority = 15)]
        static void Cognitive3DProjectValidation()
        {
            ProjectValidationSettingsProvider.OpenSettingsWindow();
        }
        [MenuItem("Cognitive3D/Scene Manager", priority = 20)]
        static void Cognitive3DSceneManagerSetup()
        {
            SceneManagerWindow.Init();
        }
        [MenuItem("Cognitive3D/Preferences", priority = 25)]
        static void Cognitive3DPreferences()
        {
            PreferencesSettingsProvider.OpenSettingsWindow();
        }
        [MenuItem("Cognitive3D/Data Uploader", priority = 30)]
        static void Cognitive3DDataUploader()
        {
            DataUploaderSettingsProvider.OpenSettingsWindow();
        }



        [MenuItem("Cognitive3D/Legacy/Dynamic Objects", priority = 55)]
        static void Cognitive3DLegacyDynamicObjects()
        {
            LegacyDynamicObjectsWindow.Init();
        }
        [MenuItem("Cognitive3D/Legacy/Project Setup", priority = 60)]
        static void Cognitive3DLegacyProjectSetup()
        {
            LegacyProjectSetupWindow.Init();
        }
        [MenuItem("Cognitive3D/Legacy/Scene Setup", priority = 65)]
        static void Cognitive3DLegacySceneSetup()
        {
            LegacySceneSetupWindow.Init();
        }
        [MenuItem("Cognitive3D/Legacy/Scene Management", priority = 70)]
        static void Cognitive3DLegacySceneManagement()
        {
            LegacySceneManagementWindow.Init();
        }
        [MenuItem("Cognitive3D/Legacy/360 Setup", priority = 75)]
        static void Cognitive3DLegacy360Setup()
        {
            LegacySetup360Window.Init();
        }
        [MenuItem("Cognitive3D/Legacy/Help", priority = 80)]
        static void Cognitive3DLegacyHelp()
        {
            LegacyHelpWindow.Init();
        }


        [MenuItem("Cognitive3D/Open Web Dashboard...", priority = 105)]
        static void Cognitive3DDashboard()
        {
            Application.OpenURL(Cognitive3D_Preferences.Instance.Protocol + "://app." + CognitiveStatics.GetDomain());
        }
        [MenuItem("Cognitive3D/Documentation...", priority = 110)]
        static void Cognitive3DDocumentation()
        {
            Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Documentation);
        }
        [MenuItem("Cognitive3D/Check for Updates...", priority = 115)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
        }
        [MenuItem("Cognitive3D/Open Discord Server...", priority = 120)]
        static void CognitiveDiscordServer()
        {
            Application.OpenURL("https://discord.gg/x38sNUdDRH");
        }
        [MenuItem("Cognitive3D/Setup Notification/Show Setup Notification", false, 125)]
        private static void ShowNotificationMenuItem()
        {
            PostSetupDialog.ShowNotification();
        }
        [MenuItem("Cognitive3D/Setup Notification/Reset Setup Notification", false, 130)]
        private static void ResetNotificationMenuItem()
        {
            PostSetupDialog.ResetForTesting();
            Debug.Log("Setup notification preferences have been reset. The notification will appear on the next project setup.");
        }
    }
}
