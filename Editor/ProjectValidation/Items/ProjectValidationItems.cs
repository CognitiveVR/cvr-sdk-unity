using UnityEngine;
using Cognitive3D.Components;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Threading.Tasks;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationItems
    {
        private const ProjectValidation.ItemCategory CATEGORY = ProjectValidation.ItemCategory.All;
        private const int INITIAL_DELAY_IN_SECONDS = 1;

        static ProjectValidationItems()
        {
            WaitBeforeProjectValidation();
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        // Adding a delay before adding and verifying items to ensure the scene is completely loaded in the editor
        static async void WaitBeforeProjectValidation()
        {
            await Task.Delay(INITIAL_DELAY_IN_SECONDS * 1000);

            AddProjectValidationItems();
            UpdateProjectValidationItemStatus();
            ProjectValidationGUI.Reset();
        }

        // Update project validation items when a new scene opens
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ProjectValidation.Reset();
            WaitBeforeProjectValidation();
        }

        private static void AddProjectValidationItems()
        {
            // TODO: Is the fix action good?
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "No Cognitive3D player definition is found",
                fixmessage: "Cognitive3D player definition is added",
                checkAction: () =>
                {
                    var playerDefines = EditorCore.GetPlayerDefines();
                    if (playerDefines != null && playerDefines[0].Contains("C3D"))
                    {
                        return true;
                    }
                    return false;
                },
                fixAction: () =>
                {
                    EditorCore.AddDefine("C3D_DEFAULT");
                }
                );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "Tracking space is not configured",
                fixmessage: "Tracking space is configured",
                checkAction: () =>
                {
                    return ProjectValidation.FindComponentInActiveScene<RoomTrackingSpace>();
                },
                fixAction: () =>
                {
                    SceneSetupWindow.currentPage = SceneSetupWindow.Page.PlayerSetup;
                    SceneSetupWindow.Init();
                }
                );
            
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "Application key is not valid",
                fixmessage: "Valid application key is found",
                checkAction: () =>
                {
                    return Cognitive3D_Preferences.Instance.IsApplicationKeyValid;
                },
                fixAction: () =>
                {
                    ProjectSetupWindow.currentPage = ProjectSetupWindow.Page.APIKeys;
                    ProjectSetupWindow.Init();
                }
                );
            
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "No Cognitive3D manager is found in current scene",
                fixmessage: "Cognitive3D manager exists in current scene",
                checkAction: () =>
                {
                    return ProjectValidation.FindComponentInActiveScene<Cognitive3D_Manager>();
                },
                fixAction: () =>
                {
                    var instance = Cognitive3D_Manager.Instance;
                }
                );
            
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "No Cognitive3D preferences file is found in project folder",
                fixmessage: "Cognitive3D preferences file created in project folder",
                checkAction: () =>
                {
                    return Cognitive3D_Preferences.Instance ? true : false;
                },
                fixAction: () =>
                {
                    EditorCore.GetPreferences();
                }
                );

#if C3D_OCULUS
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                message: "Oculus social is not enabled",
                fixmessage: "Oculus social is enabled",
                checkAction: () =>
                {
                    return ProjectValidation.FindComponentInActiveScene<OculusSocial>();
                },
                fixAction: () =>
                {
                    Cognitive3D_Manager.Instance.gameObject.AddComponent<OculusSocial>();
                }
                );

            OVRProjectConfig projectConfig = OVRProjectConfig.GetProjectConfig();
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                message: "Missing some Oculus target devices. Enable all?",
                fixmessage: "Oculus target devices are enabled",
                checkAction: () =>
                {   return projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest2) &&
                           projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.QuestPro) &&
                           projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest3);
                },
                fixAction: () =>
                {
                    if (!projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest2))
                    {
                        projectConfig.targetDeviceTypes.Add(OVRProjectConfig.DeviceType.Quest2);
                    }

                    if (!projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.QuestPro))
                    {
                        projectConfig.targetDeviceTypes.Add(OVRProjectConfig.DeviceType.QuestPro);
                    }

                    if (!projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest3))
                    {
                        projectConfig.targetDeviceTypes.Add(OVRProjectConfig.DeviceType.Quest3);
                    }

                    OVRProjectConfig.CommitProjectConfig(projectConfig);
                }
            );
#endif
        }

        public static void UpdateProjectValidationItemStatus()
        {
            var items = ProjectValidation.registry.GetAllItems();
            foreach (var item in items)
            {
                bool isFixed = item.checkAction();
                if (!isFixed)
                {
                    item.isFixed = false;
                }
            }
        }
    }
}
