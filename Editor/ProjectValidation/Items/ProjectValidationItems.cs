using UnityEngine;
using Cognitive3D.Components;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;

#if COGNITIVE3D_INCLUDE_COREUTILITIES
using Unity.XR.CoreUtils;
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
using UnityEditor.XR.LegacyInputHelpers;
#endif

#if COGNITIVE3D_INCLUDE_OPENXR_1_9_0_OR_NEWER || COGNITIVE3D_INCLUDE_OPENXR_1_8_1_OR_1_8_2
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
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
            DelayAndInitializeProjectValidation();
        }

        // Adding a delay before adding and verifying items to ensure the scene is completely loaded in the editor
        internal static async void DelayAndInitializeProjectValidation()
        {
            await Task.Delay((int)(INITIAL_DELAY_IN_SECONDS * 1000));

            AddProjectValidationItems();
            ProjectValidation.SetIgnoredItemsFromLog();
            ProjectValidation.ResetGUI();
        }

        /// <summary>
        /// Adds project validation items to the registry
        /// </summary>
        private static void AddProjectValidationItems()
        {            
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Edit,
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

            string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScenePath);
            if (currentSettings != null)
            {
                string url = CognitiveStatics.GetSceneVersions(currentSettings.SceneId);
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey }
                };

                EditorNetwork.Get(url, (responsecode, error, text) => 
                {
                    if (responsecode == 200)
                    {
                        var collection = JsonUtility.FromJson<SceneVersionCollection>(text);
                        if (collection != null)
                        {
                            // Required item for non-exist scene on dashboard
                            if (collection.versions.Find(version => version.versionNumber == currentSettings.VersionNumber) != null)
                            {
                                ProjectValidation.AddItem(
                                    level: ProjectValidation.ItemLevel.Required, 
                                    category: CATEGORY,
                                    actionType: ProjectValidation.ItemAction.Fix,
                                    message: "Current scene version not found on dashboard. Set to the latest version?",
                                    fixmessage: "Current scene version found on dashboard.",
                                    checkAction: () =>
                                    {
                                        return true;
                                    },
                                    fixAction: () =>
                                    {
                                        EditorCore.RefreshSceneVersion(null);
                                    }
                                );
                            }
                            else
                            {
                                ProjectValidation.AddItem(
                                    level: ProjectValidation.ItemLevel.Required, 
                                    category: CATEGORY,
                                    actionType: ProjectValidation.ItemAction.Fix,
                                    message: "Current scene version not found on dashboard. Set to the latest version?",
                                    fixmessage: "Current scene version found on dashboard.",
                                    checkAction: () =>
                                    {
                                        return false;
                                    },
                                    fixAction: () =>
                                    {
                                        EditorCore.RefreshSceneVersion(null);
                                    }
                                );
                            }

                            // Recommended item for latest version
                            if (collection.GetLatestVersion().versionNumber > currentSettings.VersionNumber)
                            {
                                ProjectValidation.AddItem(
                                    level: ProjectValidation.ItemLevel.Recommended, 
                                    category: CATEGORY,
                                    actionType: ProjectValidation.ItemAction.Apply,
                                    message: "No latest scene version is used. Set to the latest version?",
                                    fixmessage: "Latest scene version is used.",
                                    checkAction: () =>
                                    {
                                        return false;
                                    },
                                    fixAction: () =>
                                    {
                                        EditorCore.RefreshSceneVersion(null);
                                    }
                                );
                            }
                            else
                            {
                                ProjectValidation.AddItem(
                                    level: ProjectValidation.ItemLevel.Recommended, 
                                    category: CATEGORY,
                                    actionType: ProjectValidation.ItemAction.Apply,
                                    message: "No latest scene version is used. Set to the latest version?",
                                    fixmessage: "Latest scene version is used.",
                                    checkAction: () =>
                                    {
                                        return true;
                                    },
                                    fixAction: () =>
                                    {
                                        EditorCore.RefreshSceneVersion(null);
                                    }
                                );
                            }
                        }
                    }
                }, headers, true, "Get Scene Version");
            }

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Edit,
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
                actionType: ProjectValidation.ItemAction.Edit,
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
                actionType: ProjectValidation.ItemAction.Fix,
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
                actionType: ProjectValidation.ItemAction.Fix,
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
                actionType: ProjectValidation.ItemAction.Edit,
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
                actionType: ProjectValidation.ItemAction.Edit,
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
                actionType: ProjectValidation.ItemAction.Edit,
                message: "Current scene path is invalid. Please verify the path in Cognitive3D's preference scene settings",
                fixmessage: "Current scene path is valid",
                checkAction: () =>
                {
                    Cognitive3D_Preferences.SceneSettings c3dScene = Cognitive3D_Preferences.FindCurrentScene();
                    if (c3dScene != null)
                    {
                        // Load the asset at the C3D scene path
                        Object scene = AssetDatabase.LoadAssetAtPath(c3dScene.ScenePath, typeof(SceneAsset));
                        return scene != null;
                    }
                    
                    return false;
                },
                fixAction: () =>
                {
                    Selection.activeObject = EditorCore.GetPreferences();
                }
            );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.None,
                message : "The maximum limit of controllers in the scene has been exceeded. Please remove any extra controller dynamic objects.",
                fixmessage: "The maximum limit of controllers in the scene has not been exceeded.",
                checkAction: () =>
                {
                    string newMessage;
                    string oldMessage = "The maximum limit of controllers in the scene has been exceeded. Please remove any extra controller dynamic objects.";
                    if (ProjectValidation.TryGetControllers(out var _controllerNamesList))
                    {
                        if (_controllerNamesList.Count > 2)
                        {
                            newMessage = oldMessage + $" The detected controller objects are: {string.Join(", ", _controllerNamesList)}";
                            ProjectValidation.UpdateItemMessage(oldMessage, newMessage);
                        }

                        return _controllerNamesList.Count <= 2;
                    }
                    
                    return true;
                },
                fixAction: () =>
                {
                    
                }
            );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Edit,
                message : "Controllers are not correctly set up. Less than 2 controllers are detected in the scene. You can configure the controllers in Cognitive3D > Scene Setup",
                fixmessage: "Controllers are correctly set up in current scene",
                checkAction: () =>
                {
                    if (ProjectValidation.TryGetControllers(out var _controllerNamesList))
                    {
                        return _controllerNamesList.Count >= 2;
                    }
                    
                    return false;
                },
                fixAction: () =>
                {
                    SceneSetupWindow.Init(SceneSetupWindow.Page.PlayerSetup);
                }
            );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Edit,
                message: "A camera with the 'MainCamera' tag should be included in the scene. If the player rig is spawned or persist from another scene, no action is needed. Edit the player rig?",
                fixmessage: "Main Camera found in the scene",
                checkAction: () =>
                {
                    return Camera.main != null;
                },
                fixAction: () =>
                {
                    SceneSetupWindow.Init(SceneSetupWindow.Page.PlayerSetup);
                }
            );

#if UNITY_ANDROID
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Apply,
                message: "The minimum Android API level is below 30. The minimum recommended level is 30 to ensure full support for Cognitive3D android plugin features and functionality.",
                fixmessage: "The minimum Android API level is 30 or higher.",
                checkAction: () =>
                {
                    return PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel30;
                },
                fixAction: () =>
                {
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel30;
                }
            );
#endif

#if C3D_OCULUS
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Fix,
                message: "No OVR_Manager found in current scene.",
                fixmessage: "OVR_Manager found in current scene.",
                checkAction: () =>
                {   
                    return ProjectValidation.FindComponentInActiveScene<OVRManager>();
                },
                fixAction: () =>
                {
                    var ovrmngr = new GameObject();
                    ovrmngr.name = "OVR_Manager";
                    ovrmngr.AddComponent<OVRManager>();
                }
            );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Apply,
                message: "Dynamic Object component detected on Camera rig. Camera rig should have no Dynamic Object component. Remove the component?",
                fixmessage: "No Dynamic Object component detected on Camera rig.",
                checkAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<OVRCameraRig>(out var _ovrCameraRigs);

                    if (_ovrCameraRigs != null && _ovrCameraRigs.Count > 0)
                    {
                        return !_ovrCameraRigs[0].GetComponent<DynamicObject>();
                    }
                    return true;
                },
                fixAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<OVRCameraRig>(out var _ovrCameraRigs);

                    if (_ovrCameraRigs != null && _ovrCameraRigs.Count > 0)
                    {
                        if (_ovrCameraRigs[0].GetComponent<DynamicObject>())
                        {
                            Object.DestroyImmediate(_ovrCameraRigs[0].GetComponent<DynamicObject>() as Object, true);
                        }
                    }
                }
            );

            OVRProjectConfig projectConfig = OVRProjectConfig.CachedProjectConfig;
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Apply,
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
                actionType: ProjectValidation.ItemAction.Apply,
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
#elif C3D_PICOXR
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Fix,
                message: "No PXR_Manager found in current scene.",
                fixmessage: "PXR_Manager found in current scene.",
                checkAction: () =>
                {   
                    return ProjectValidation.FindComponentInActiveScene<Unity.XR.PXR.PXR_Manager>();
                },
                fixAction: () =>
                {
                    var pxrmngr = new GameObject();
                    pxrmngr.name = "PXR_Manager";
                    pxrmngr.AddComponent<Unity.XR.PXR.PXR_Manager>();
                }
            );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Apply,
                message: "Dynamic Object component detected on XR rig. XR rig should have no Dynamic Object component. Remove the component?",
                fixmessage: "No Dynamic Object component detected on XR rig.",
                checkAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<Unity.XR.PXR.PXR_Manager>(out var _pxrRigs);

                    if (_pxrRigs != null && _pxrRigs.Count > 0)
                    {
                        return !_pxrRigs[0].GetComponent<DynamicObject>();
                    }
                    return true;
                },
                fixAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<Unity.XR.PXR.PXR_Manager>(out var _pxrRigs);

                    if (_pxrRigs != null && _pxrRigs.Count > 0)
                    {
                        if (_pxrRigs[0].GetComponent<DynamicObject>())
                        {
                            Object.DestroyImmediate(_pxrRigs[0].GetComponent<DynamicObject>() as Object, true);
                        }
                    }
                }
            );
#elif C3D_VIVEWAVE
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Fix,
                message: "No WaveRig found in current scene.",
                fixmessage: "WaveRig found in current scene.",
                checkAction: () =>
                {   
                    return ProjectValidation.FindComponentInActiveScene<Wave.Essence.WaveRig>();
                },
                fixAction: () =>
                {
                    var waverig = new GameObject();
                    waverig.name = "Wave Rig";
                    waverig.AddComponent<Wave.Essence.WaveRig>();
                }
            );

            // Keep as recommended. For required, should not set as controller.
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Apply,
                message: "Tracking origin in Wave Rig is not set to floor. This can lead in to miscalculation in participant and controllers height. Set tracking origin to Floor?",
                fixmessage: "Tracking origin in Wave Rig is set to floor",
                checkAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<Wave.Essence.WaveRig>(out var _waveRigs);
                    if (_waveRigs != null && _waveRigs.Count != 0)
                    {
                        if (_waveRigs[0].TrackingOrigin != UnityEngine.XR.TrackingOriginModeFlags.Floor)
                        {
                            return false;
                        }
                    }
                    return true;
                },
                fixAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<Wave.Essence.WaveRig>(out var _waveRigs);
                    if (_waveRigs != null && _waveRigs.Count != 0)
                    {
                        _waveRigs[0].TrackingOrigin = UnityEngine.XR.TrackingOriginModeFlags.Floor;
                    }
                }
            );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Apply,
                message: "Dynamic Object component detected on Wave rig. Wave rig should have no Dynamic Object component. Remove the component?",
                fixmessage: "No Dynamic Object component detected on Wave rig.",
                checkAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<Wave.Essence.WaveRig>(out var _waveRigs);
                    if (_waveRigs != null && _waveRigs.Count != 0)
                    {
                        return !_waveRigs[0].GetComponent<DynamicObject>();
                    }
                    return true;
                },
                fixAction: () =>
                {
                    ProjectValidation.FindComponentInActiveScene<Wave.Essence.WaveRig>(out var _waveRigs);
                    if (_waveRigs != null && _waveRigs.Count != 0)
                    {
                        if (_waveRigs[0].GetComponent<DynamicObject>())
                        {
                            Object.DestroyImmediate(_waveRigs[0].GetComponent<DynamicObject>() as Object, true);
                        }
                    }
                }
            );
#elif C3D_DEFAULT

    #if COGNITIVE3D_INCLUDE_COREUTILITIES
            ProjectValidation.FindComponentInActiveScene<XROrigin>(out var xrorigins);

            if (xrorigins != null && xrorigins.Count != 0)
            {
                ProjectValidation.AddItem(
                    level: ProjectValidation.ItemLevel.Recommended, 
                    category: CATEGORY,
                    actionType: ProjectValidation.ItemAction.Apply,
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

                ProjectValidation.AddItem(
                    level: ProjectValidation.ItemLevel.Recommended, 
                    category: CATEGORY,
                    actionType: ProjectValidation.ItemAction.Apply,
                    message: "Dynamic Object component detected on XR rig. XR rig should have no Dynamic Object component. Remove the component?",
                    fixmessage: "No Dynamic Object component detected on XR rig.",
                    checkAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<XROrigin>(out var _xrorigins);
                        if (_xrorigins != null && _xrorigins.Count != 0)
                        {
                            return !_xrorigins[0].GetComponent<DynamicObject>();
                        }
                        return true;
                    },
                    fixAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<XROrigin>(out var _xrorigins);
                        if (_xrorigins != null && _xrorigins.Count != 0)
                        {
                            if (_xrorigins[0].GetComponent<DynamicObject>())
                            {
                                Object.DestroyImmediate(_xrorigins[0].GetComponent<DynamicObject>() as Object, true);
                            }
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
                    actionType: ProjectValidation.ItemAction.Apply,
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

                ProjectValidation.AddItem(
                    level: ProjectValidation.ItemLevel.Recommended, 
                    category: CATEGORY,
                    actionType: ProjectValidation.ItemAction.Apply,
                    message: "Dynamic Object component detected on XR rig. XR rig should have no Dynamic Object component. Remove the component?",
                    fixmessage: "No Dynamic Object component detected on XR rig.",
                    checkAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<CameraOffset>(out var _cameraOffset);

                        if (_cameraOffset != null && _cameraOffset.Count != 0)
                        {
                            return !_cameraOffset[0].GetComponent<DynamicObject>();
                        }
                        return true;
                    },
                    fixAction: () =>
                    {
                        ProjectValidation.FindComponentInActiveScene<CameraOffset>(out var _cameraOffset);

                        if (_cameraOffset != null && _cameraOffset.Count != 0)
                        {
                            if (_cameraOffset[0].GetComponent<DynamicObject>())
                            {
                                Object.DestroyImmediate(_cameraOffset[0].GetComponent<DynamicObject>() as Object, true);
                            }
                        }
                    }
                );
            }
    #endif

    #if COGNITIVE3D_INCLUDE_OPENXR_1_9_0_OR_NEWER
            var androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var questFeature = androidOpenXRSettings.GetFeature<MetaQuestFeature>();

            // Check if Meta Quest Support exists and enabled in OpenXR Android settings
            if (questFeature != null && questFeature.enabled)
            {
                ProjectValidation.AddItem(
                    level: ProjectValidation.ItemLevel.Required, 
                    category: CATEGORY,
                    actionType: ProjectValidation.ItemAction.Fix,
                    message: "\"Force Remove Internet Permission\" is enabled in OpenXR Meta Quest Support > Manifest settings (Android Build Target). This could potentially disrupt network connectivity when sending data. Disable \"Force Remove Internet Permission\"?",
                    fixmessage: "\"Force Remove Internet Permission\" is disabled in OpenXR Meta Quest Support > Manifest settings (Android Build Target).",
                    checkAction: () =>
                    {
                        var _androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
                        var _questFeature = androidOpenXRSettings.GetFeature<MetaQuestFeature>();

                        return !_questFeature.ForceRemoveInternetPermission;
                    },
                    fixAction: () =>
                    {
                        var _androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
                        var _questFeature = androidOpenXRSettings.GetFeature<MetaQuestFeature>();

                        _questFeature.ForceRemoveInternetPermission = false;
                    }
                );
            }
    #endif

    #if COGNITIVE3D_INCLUDE_OPENXR_1_8_1_OR_1_8_2
            var androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var questFeature = androidOpenXRSettings.GetFeature<MetaQuestFeature>();

            // Check if Meta Quest Support exists and enabled in OpenXR Android settings
            if (questFeature != null && questFeature.enabled)
            {
                ProjectValidation.AddItem(
                    level: ProjectValidation.ItemLevel.Required, 
                    category: CATEGORY,
                    actionType: ProjectValidation.ItemAction.Fix,
                    message: "\"Force Remove Internet Permission\" is enabled in OpenXR Meta Quest Support > Manifest settings (Android Build Target). This could potentially disrupt network connectivity when sending data. Disable \"Force Remove Internet Permission\"?",
                    fixmessage: "\"Force Remove Internet Permission\" is disabled in OpenXR Meta Quest Support > Manifest settings (Android Build Target).",
                    checkAction: () =>
                    {
                        var _androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
                        var _questFeature = _androidOpenXRSettings.GetFeature<MetaQuestFeature>();
                        var _questFeatureType = _questFeature.GetType();

                        var _forceRemoveInternetPermission = _questFeatureType.GetField("forceRemoveInternetPermission", BindingFlags.NonPublic | BindingFlags.Instance);

                        if(_forceRemoveInternetPermission != null)
                        {
                            object _permission = _forceRemoveInternetPermission.GetValue(questFeature);

                            return !(bool)_permission;
                        }
                        
                        return true;
                    },
                    fixAction: () =>
                    {
                        var _androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
                        var _questFeature = _androidOpenXRSettings.GetFeature<MetaQuestFeature>();
                        var _questFeatureType = _questFeature.GetType();

                        var _forceRemoveInternetPermission = _questFeatureType.GetField("forceRemoveInternetPermission", BindingFlags.NonPublic | BindingFlags.Instance);

                        if(_forceRemoveInternetPermission != null)
                        {
                            _forceRemoveInternetPermission.SetValue(questFeature, false);
                        }
                    }
                );
            }
    #endif

#endif

#region Multiplayer Support
#if C3D_PHOTON
        #if !PHOTON_UNITY_NETWORKING
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.None,
                message: "Photon plugin is missing or not installed in this project. Please install it via the Package Manager or Asset Store.",
                fixmessage: "Photon plugin is installed in this project.",
                checkAction: () =>
                {
                    return false;
                },
                fixAction: () =>
                {
                    
                }
            );
        #else
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.None,
                message: "Photon plugin is missing or not installed in this project. Please install it via the Package Manager or Asset Store.",
                fixmessage: "Photon plugin is installed in this project.",
                checkAction: () =>
                {
                    return true;
                },
                fixAction: () =>
                {

                }
            );
        #endif
#endif

#if C3D_NETCODE
        #if !COGNITIVE3D_INCLUDE_UNITY_NETCODE
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.None,
                message: "Unity Netcode for Gameobjects plugin is missing or not installed in this project. Please install it via the Package Manager or Asset Store.",
                fixmessage: "Unity Netcode for Gameobjects plugin is installed in this project.",
                checkAction: () =>
                {
                    return false;
                },
                fixAction: () =>
                {
                    
                }
            );
        #else
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.None,
                message: "Unity Netcode for Gameobjects plugin is missing or not installed in this project. Please install it via the Package Manager or Asset Store.",
                fixmessage: "Unity Netcode for Gameobjects plugin is installed in this project.",
                checkAction: () =>
                {
                    return true;
                },
                fixAction: () =>
                {

                }
            );

            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.Apply,
                message: "No Network Manager found in current scene. To enable Unity Netcode support, the Network Manager needs to be present in one of the project scenes. Add a Network Manager to current scene? If there's already a Network Manager in one of the project scenes, click \"Ignore\".",
                fixmessage: "Network Manager found in current scene.",
                checkAction: () =>
                {
                    if (ProjectValidation.FindComponentInActiveScene<Unity.Netcode.NetworkManager>())
                    {
                        return true;
                    }

                    return false;
                },
                fixAction: () =>
                {
                    GameObject networkManagerObject = new GameObject("NetworkManager");

                    Unity.Netcode.NetworkManager networkManager = networkManagerObject.AddComponent<Unity.Netcode.NetworkManager>();
                    Unity.Netcode.Transports.UTP.UnityTransport transport = networkManagerObject.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

                    networkManager.NetworkConfig.NetworkTransport = transport;
                }
            );
        #endif
#endif

#if C3D_NORMCORE
        #if !COGNITIVE3D_INCLUDE_NORMCORE
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.None,
                message: "Normcore plugin is missing or not installed in this project. Please install it via the Package Manager or Asset Store.",
                fixmessage: "Normcore plugin is installed in this project.",
                checkAction: () =>
                {
                    return false;
                },
                fixAction: () =>
                {
                    
                }
            );
        #else
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                actionType: ProjectValidation.ItemAction.None,
                message: "Normcore plugin is missing or not installed in this project. Please install it via the Package Manager or Asset Store.",
                fixmessage: "Normcore plugin is installed in this project.",
                checkAction: () =>
                {
                    return true;
                },
                fixAction: () =>
                {

                }
            );
        #endif
#endif

#endregion
        }
    }
}
