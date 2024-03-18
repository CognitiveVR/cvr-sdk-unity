using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cognitive3D.Components;
using UnityEditor;
using UnityEngine;


#if C3D_OCULUS
using Settings = UnityEngine.XR.XRSettings;
#endif

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationItems
    {
        private const ProjectValidation.ItemCategory CATEGORY = ProjectValidation.ItemCategory.All;

        static ProjectValidationItems()
        {
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Required, 
                category: CATEGORY,
                message: "Tracking space is not configured",
                fixmessage: "Tracking space is configured",
                isFixed: ProjectValidation.FindComponentInActiveScene<RoomTrackingSpace>(),
                fixAction: () =>
                {
                    
                }
                );

#if C3D_OCULUS
            ProjectValidation.AddItem(
                level: ProjectValidation.ItemLevel.Recommended, 
                category: CATEGORY,
                message: "Oculus social is not enabled",
                fixmessage: "Oculus social is enabled",
                isFixed: ProjectValidation.FindComponentInActiveScene<OculusSocial>(),
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
                isFixed: !projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest2) ||
                         !projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.QuestPro) ||
                         !projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest3),
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
                }
            );
#endif
        }
    }
}
