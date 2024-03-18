using UnityEngine;
using Cognitive3D.Components;
using UnityEditor;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationItems
    {
        private const ProjectValidation.ItemCategory CATEGORY = ProjectValidation.ItemCategory.All;

        static ProjectValidationItems()
        {
            AddProjectValidationItems();
        }

        private static void AddProjectValidationItems()
        {
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
