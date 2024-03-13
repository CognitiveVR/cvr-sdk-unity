using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    internal class ProjectValidation
    {
        public enum ItemLevel
        {
            Optional = 0,
            Recommended = 1,
            Required = 2
        }

        internal static ProjectValidationItemRegistry registry;

        /// <summary>
        /// Add an <see cref="ProjectValidationItem"/> to project validation checklist items
        /// </summary>
        /// <param name="item">The item that will be added to project validation checklist</param>
        public void AddItem(ProjectValidationItem item)
        {
            registry.AddItem(item);
        }

        /// <summary>
        /// Add an <see cref="ProjectValidationItem"/> to project validation checklist items
        /// </summary>
        /// <param name="level">Severity of the item configuration</param>
        /// <param name="message">Description of the item</param>
        /// <param name="fixmessage">Description of the fix for the item</param>
        /// <param name="isFixed">Checks if item is fixed or not</param>
        /// <param name="fixAction">Delegate that validates the item</param>
        public void AddItem(ItemLevel level, string message, string fixmessage, bool isFixed, Action fixAction = null)
        {
            var newItem = new ProjectValidationItem(level, message, fixmessage, isFixed, fixAction);
            AddItem(newItem);
        }
    }
}
