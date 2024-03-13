using System;
using System.Collections.Generic;
using Cognitive3D.Components;
using UnityEditor;
using UnityEngine;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationItems
    {
        private const ProjectValidation.ItemCategory CATEGORY = ProjectValidation.ItemCategory.All;

        static ProjectValidationItems()
        {
            ProjectValidation.AddItem(
                ProjectValidation.ItemLevel.Required, 
                CATEGORY,
                "Tracking space is not configured",
                "Tracking space is configured",
                ProjectValidation.FindComponentInActiveScene<RoomTrackingSpace>()
                );

#if C3D_OCULUS
            ProjectValidation.AddItem(
                ProjectValidation.ItemLevel.Recommended, 
                CATEGORY,
                "Oculus social is not enabled",
                "Oculus social is enabled",
                ProjectValidation.FindComponentInActiveScene<OculusSocial>()
                );
#endif
        }
    }
}
