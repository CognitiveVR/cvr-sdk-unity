using UnityEngine;
using Cognitive3D.Components;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

#if COGNITIVE3D_INCLUDE_COREUTILITIES
using Unity.XR.CoreUtils;
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
using UnityEditor.XR.LegacyInputHelpers;
#endif

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal static class ProjectValidationItems
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
                message: "No Cognitive3D player definition is found. Select an SDK in the Project Setup window to support specific features.",
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
                message: "Tracking space is not configured. Select the tracking space of the player prefab in the Scene Setup window",
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
                message: "Application key is not valid. Re-enter application key in the Project Setup window.",
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
                message: "No Cognitive3D manager is found. Add Cognitive3D_Manager prefab to current scene?",
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
                message: "No Cognitive3D preferences file is found in project folder. Create an instance in Assets/Resources?",
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
                message: "Current scene is not found in Cognitive3D preferences. Please upload the current scene from the Scene Setup window.",
                fixmessage: "Current scene is found in Cognitive3D preferences",
                checkAction: () =>
                {
                    Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindSceneByPath(SceneManager.GetActiveScene().path);
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
                message: "Current scene has no SceneId. Please upload the current scene from the Scene Setup window.",
                fixmessage: "Current scene has SceneId",
                checkAction: () =>
                {
                    Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindSceneByPath(SceneManager.GetActiveScene().path);
                    return (c3dScene != null  && !string.IsNullOrEmpty(c3dScene.SceneId)) ? true : false;
                },
                fixAction: () =>
                {
                    SceneSetupWindow.Init(SceneSetupWindow.Page.Welcome);
                }
                );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "Current scene path is invalid. Please verify the path in Cognitive3D's preference scene settings",
                fixmessage: "Current scene path is valid",
                checkAction: () =>
                {
                    Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindCurrentScene();
                    if (c3dScene != null)
                    {
                        // Load the asset at the C3D scene path
                        UnityEngine.Object scene = AssetDatabase.LoadAssetAtPath(c3dScene.ScenePath, typeof(SceneAsset));
                        return scene != null;
                    }
                    
                    return false;
                },
                fixAction: () =>
                {
                    Selection.activeObject = EditorCore.GetPreferences();
                }
            );

            // Recommended Items
#if C3D_OCULUS
            OVRProjectConfig projectConfig = OVRProjectConfig.GetProjectConfig();
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                message: "Missing some Oculus target devices. Enable all?",
                fixmessage: "All Oculus target devices are enabled",
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

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                message: "Recording Oculus user data like username, id, and display name is disabled. Enable recording Oculus User Data?",
                fixmessage: "Recording Oculus user data like username, id, and display name is enabled",
                checkAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<OculusSocial>(out var oculusSocial);
                    if (oculusSocial != null && oculusSocial.Count != 0)
                    {
                        return oculusSocial[0].GetRecordOculusUserData();
                    }
                    return false;
                },
                fixAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<OculusSocial>(out var oculusSocial);
                    if (oculusSocial != null && oculusSocial.Count != 0)
                    {
                        oculusSocial[0].SetRecordOculusUserData(true);
                        return;
                    }

                    Cognitive3D_Manager.Instance.gameObject.AddComponent<OculusSocial>();
                    ProjectValidation.FindComponentInActiveScene<OculusSocial>(out oculusSocial);
                    if (oculusSocial != null && oculusSocial.Count != 0)
                    {
                        oculusSocial[0].SetRecordOculusUserData(true);
                    }
                }
            );
#endif
#if C3D_DEFAULT

#if COGNITIVE3D_INCLUDE_COREUTILITIES
            ProjectValidation.FindComponentInActiveScene<XROrigin>(out var xrorigins);

            if (xrorigins != null && xrorigins.Count != 0)
            {
                ProjectValidation.AddItem(
                    level: ProjectValidation.ItemLevel.Recommended, 
                    category: CATEGORY,
                    message: "Tracking origin is not set to floor. This can lead in to miscalculation in participant and controllers height. Set tracking origin to Floor?",
                    fixmessage: "Tracking origin is set to floor",
                    checkAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<XROrigin>(out var _xrorigins);
                        if (_xrorigins != null && _xrorigins.Count != 0)
                        {
                            if (_xrorigins[0].RequestedTrackingOriginMode != XROrigin.TrackingOriginMode.Floor)
                            {
                                return false;
                            }
                        }
                        return true;
                    },
                    fixAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<XROrigin>(out var _xrorigins);
                        if (_xrorigins != null && _xrorigins.Count != 0)
                        {
                            _xrorigins[0].RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                        }
                    }
                );
            } 
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
            ProjectValidation.FindComponentInActiveScene<CameraOffset>(out var cameraOffset);

            if (cameraOffset != null && cameraOffset.Count != 0)
            {
                ProjectValidation.AddItem(
                    level: ProjectValidation.ItemLevel.Recommended, 
                    category: CATEGORY,
                    message: "Tracking origin is set to floor. This can lead in to miscalculation in participant and controllers height. Set tracking origin to Floor?",
                    fixmessage: "Tracking origin is set to floor",
                    checkAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<CameraOffset>(out var _cameraOffset);

                        if (_cameraOffset != null && _cameraOffset.Count != 0)
                        {
                            if (_cameraOffset[0].requestedTrackingMode != UnityEditor.XR.LegacyInputHelpers.UserRequestedTrackingMode.Floor)
                            {
                                return false;
                            }
                        }
                        return true;
                    },
                    fixAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<CameraOffset>(out var _cameraOffset);
                        if (_cameraOffset != null && _cameraOffset.Count != 0)
                        {
                            _cameraOffset[0].requestedTrackingMode = UnityEditor.XR.LegacyInputHelpers.UserRequestedTrackingMode.Floor;
                        }
                    }
                );
            }
#endif

#endif
        }

        public static void UpdateProjectValidationItemStatus()
        {
            var items = ProjectValidation.registry.GetAllItems();
            foreach (var item in items)
            {
                item.isFixed = item.checkAction();
            }
        }
    }
}
