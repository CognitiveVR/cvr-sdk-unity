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
            // Required Items
            // TODO: Is the fix action good?
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "No Cognitive3D player definition is found",
                fixmessage: "Cognitive3D player definition is added",
                checkAction: () =>
                {
                    return EditorCore.HasC3DDefine();
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

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "Current scene is not found in Cognitive3D preferences",
                fixmessage: "Current scene is found in Cognitive3D preferences",
                checkAction: () =>
                {
                    Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindScene(SceneManager.GetActiveScene().name);
                    return c3dScene != null ? true : false;
                },
                fixAction: () =>
                {
                    SceneSetupWindow.currentPage = SceneSetupWindow.Page.Welcome;
                    SceneSetupWindow.Init();
                }
                );

            // Fix action?
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "Current scene has no scene ID",
                fixmessage: "Current scene has scene ID",
                checkAction: () =>
                {
                    Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindScene(SceneManager.GetActiveScene().name);
                    return (c3dScene != null  && !string.IsNullOrEmpty(c3dScene.SceneId)) ? true : false;
                },
                fixAction: () =>
                {
                    SceneSetupWindow.currentPage = SceneSetupWindow.Page.Welcome;
                    SceneSetupWindow.Init();
                }
                );

            // Recommended Items
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
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                message: "No controllers/hands are added as dynamic objects",
                fixmessage: "Controllers/hands are added as dynamic objects",
                checkAction: () =>
                {
                    Transform tempTransform;
                    // Not working?
                    return GameplayReferences.GetControllerTransform(false,out tempTransform) && GameplayReferences.GetControllerTransform(false,out tempTransform);
                },
                fixAction: () =>
                {
                    SceneSetupWindow.currentPage = SceneSetupWindow.Page.PlayerSetup;
                    SceneSetupWindow.Init();
                }
                );

            // Items that should be added with delay
            // await CheckItemsWithDelay();

            // ProjectValidation.AddItem(
            //     level: ProjectValidation.ItemLevel.Required, 
            //     category: CATEGORY,
            //     message: "No scene associated with this SceneID on the dashboard",
            //     fixmessage: "Scene associated with this SceneID exists on the dashboard",
            //     checkAction: () => 
            //     {
            //         Debug.Log("@@@ 2: " + successful);
            //         return successful;
            //     },
            //     fixAction: () =>
            //     {
            //         EditorCore.GetPreferences();
            //     }
            //     );
        }

        public static void UpdateProjectValidationItemStatus()
        {
            // await CheckItemsWithDelay();

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

        // private static bool successful;

        // private static void GetResponse(int responseCode, string error, string text)
        // {
        //     if (responseCode == 200)
        //     {
        //         successful = true;
        //         Debug.Log("@@@ 1: " + successful);
        //         return;
        //     }

        //     successful = false;
        // }

        // private static async Task CheckItemsWithDelay()
        // {
        //     if (!string.IsNullOrEmpty(EditorCore.DeveloperKey))
        //     {
        //         string name = SceneManager.GetActiveScene().name;
        //         Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindScene(name);

        //         if (c3dScene != null)
        //         {
        //             Dictionary<string, string> headers = new Dictionary<string, string>();
        //             headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
        //             EditorNetwork.Get("https://" + EditorCore.DisplayValue(DisplayKey.GatewayURL) + "/v0/scenes/" + c3dScene.SceneId, GetResponse, headers, true);
        //         }
                
        //     }
        //     await Task.Delay(500);
        // }
    }
}
