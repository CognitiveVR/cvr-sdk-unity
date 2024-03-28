using UnityEngine;
using Cognitive3D.Components;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

#if C3D_DEFAULT
// using Unity.XR.CoreUtils;
// using UnityEngine.XR;
#endif

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationItems
    {
        private const ProjectValidation.ItemCategory CATEGORY = ProjectValidation.ItemCategory.All;
        private const float INITIAL_DELAY_IN_SECONDS = 0.2f;

        static ProjectValidationItems()
        {
            WaitBeforeProjectValidation();
        }

        // Adding a delay before adding and verifying items to ensure the scene is completely loaded in the editor
        internal static async void WaitBeforeProjectValidation()
        {
            await Task.Delay((int)(INITIAL_DELAY_IN_SECONDS * 1000));

            AddProjectValidationItems();
            UpdateProjectValidationItemStatus();
            ProjectValidationGUI.Reset();
        }

        private static void AddProjectValidationItems()
        {
            // Required Items
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
                    ProjectSetupWindow.Init(ProjectSetupWindow.Page.SDKSelection);
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
                    SceneSetupWindow.Init(SceneSetupWindow.Page.PlayerSetup);
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
                    ProjectSetupWindow.Init(ProjectSetupWindow.Page.APIKeys);
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
                    GameObject c3dManagerPrefab = Resources.Load<GameObject>("Cognitive3D_Manager");
                    PrefabUtility.InstantiatePrefab(c3dManagerPrefab);
                }
                );
            
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "No Cognitive3D preferences file is found in project folder",
                fixmessage: "Cognitive3D preferences file created in project folder",
                checkAction: () =>
                {
                    return Cognitive3D_Preferences.GetPreferencesFile();
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
                    SceneSetupWindow.Init(SceneSetupWindow.Page.Welcome);
                }
                );

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
                    SceneSetupWindow.Init(SceneSetupWindow.Page.Welcome);
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

            ProjectValidation.FindComponentInActiveScene<OculusSocial>(out var oculusSocial);
            if (oculusSocial != null && oculusSocial.Count != 0)
            {
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                message: "Recording Oculus user data like username, id, and display name is disabled",
                fixmessage: "Recording Oculus user data like username, id, and display name is enabled",
                checkAction: () =>
                {
                    return oculusSocial[0].GetRecordOculusUserData();
                },
                fixAction: () =>
                {
                    oculusSocial[0].SetRecordOculusUserData(true);
                }
                );
            }
#endif
#if C3D_DEFAULT
            // ProjectValidation.FindComponentInActiveScene<XROrigin>(out var xrorigins);
            // ProjectValidation.AddItem(
            //     level: ProjectValidation.ItemLevel.Recommended, 
            //     category: CATEGORY,
            //     message: "Tracking origin is set to . This can lead in to miscalculation in participant and controllers height. Set tracking origin to Floor?",
            //     fixmessage: "Tracking origin is set to floor",
            //     checkAction: () =>
            //     {
            //         if (xrorigins != null && xrorigins.Count != 0)
            //         {
            //             Debug.Log(xrorigins[0].CurrentTrackingOriginMode);
            //             // return xrorigins[0].CurrentTrackingOriginMode
            //         }
            //         return true;
            //     },
            //     fixAction: () =>
            //     {
                    
            //     }
            //     );
#endif
            // ProjectValidation.AddItem(
            //     level: ProjectValidation.ItemLevel.Recommended, 
            //     category: CATEGORY,
            //     message: "No controllers/hands are added as dynamic objects",
            //     fixmessage: "Controllers/hands are added as dynamic objects",
            //     checkAction: () =>
            //     {
            //         return EditorCore.IsLeftControllerValid() && EditorCore.IsRightControllerValid();
            //     },
            //     fixAction: () =>
            //     {
            //         SceneSetupWindow.Init(SceneSetupWindow.Page.PlayerSetup);
            //     }
            //     );

            // ProjectValidation.AddItem(
            //     level: ProjectValidation.ItemLevel.Required, 
            //     category: CATEGORY,
            //     message: "No scene associated with this SceneID on the dashboard",
            //     fixmessage: "Scene associated with this SceneID exists on the dashboard",
            //     checkAction: () => 
            //     {
            //         callbackComplete = false;
            //         if (!string.IsNullOrEmpty(EditorCore.DeveloperKey))
            //         {
            //             string name = SceneManager.GetActiveScene().name;
            //             Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindScene(name);

            //             if (c3dScene != null)
            //             {
            //                 Dictionary<string, string> headers = new Dictionary<string, string>();
            //                 headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            //                 EditorNetwork.Get("https://" + EditorCore.DisplayValue(DisplayKey.GatewayURL) + "/v0/scenes/" + c3dScene.SceneId, GetResponse, headers, true);
            //             }
            //         }

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
            var items = ProjectValidation.registry.GetAllItems();
            foreach (var item in items)
            {
                item.isFixed = item.checkAction();
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
