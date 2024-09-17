using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Cognitive3D
{
    internal static class ProjectValidation
    {
        /// <summary>
        /// Represents the priority level of an item that needs to be addressed
        /// </summary>
        internal enum ItemLevel
        {
            Required = 0,
            Recommended = 1,
            Optional = 2            
        }

        /// <summary>
        /// This can serve to categorize items into groups such as performance, participants, exit polls, and etc.
        /// </summary>
        internal enum ItemCategory
        {
            All = 0,
        }

        /// <summary>
        /// Represents possible actions that can be taken on project validation items (Used for button text).
        /// </summary>
        internal enum ItemAction
        {
            None = 0,
            // Related fix action is performed automatically once the "Fix" button is pressed, with no developer or user involvement required.
            Fix = 1,
            // Requires updates and adjustments in the project or scene by developers or users once the "Edit" button is pressed
            Edit = 2,
            // Necessary action is performed automatically once the "Apply" button is pressed, with no developer or user involvement required (used for recommonded items).
            Apply = 3
        }

        internal static readonly ProjectValidationItemRegistry registry = new ProjectValidationItemRegistry();

        /// <summary>
        /// Add an <see cref="ProjectValidationItem"/> to project validation checklist items
        /// </summary>
        /// <param name="item">The item that will be added to project validation checklist</param>
        internal static void AddItem(ProjectValidationItem item)
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
        internal static void AddItem(ItemLevel level, ItemCategory category, ItemAction actionType, string message, string fixmessage, Func<bool> checkAction, Action fixAction = null, bool isIgnored = false)
        {
            var newItem = new ProjectValidationItem(level, category, actionType, message, fixmessage, checkAction, fixAction, isIgnored);
            AddItem(newItem);
        }

        /// <summary>
        /// Gets all existed <see cref="ProjectValidationItem"/>s
        /// </summary>
        internal static IEnumerable<ProjectValidationItem> GetAllItems()
        {
            return registry.GetAllItems();
        }

        // <summary>
        /// Gets all <see cref="ProjectValidationItem"/>s are ignored or not
        /// </summary>
        internal static IEnumerable<ProjectValidationItem> GetIgnoredItems(bool isIgnored)
        {
            return registry.GetIgnoredItems(isIgnored);
        }

        // <summary>
        /// Gets <see cref="ProjectValidationItem"/>s are ignored or not related to a level
        /// </summary>
        internal static IEnumerable<ProjectValidationItem> GetIgnoredItems(bool isIgnored, ItemLevel level)
        {
            return registry.GetIgnoredItems(isIgnored, level);
        }

        /// <summary>
        /// Gets all <see cref="ProjectValidationItem"/>s with a specific level
        /// </summary>
        /// <param name="level"></param>
        internal static IEnumerable<ProjectValidationItem> GetItems(ItemLevel level)
        {
            return registry.GetItems(level);
        }

        /// <summary>
        /// Gets all <see cref="ProjectValidationItem"/>s with a specific category
        /// </summary>
        /// <param name="category"></param>
        internal static IEnumerable<ProjectValidationItem> GetItems(ItemCategory category)
        {
            return registry.GetItems(category);
        }

        /// <summary>
        /// Gets all <see cref="ProjectValidationItem"/>s are fixed
        /// </summary>
        internal static IEnumerable<ProjectValidationItem> GetFixedItems()
        {
            return registry.GetFixedItems();
        }

        internal static IEnumerable<ItemLevel> GetLevelsOfItemsNotFixed()
        {
            return registry.GetLevelsOfItemsNotFixed();
        }

        /// <summary>
        /// Checks if current scene has <see cref="ProjectValidationItem"/>s to be fixed
        /// </summary>
        /// <returns></returns>
        internal static bool hasNotFixedItems()
        {
            return registry.hasNotFixedItems();
        }

        /// <summary>
        /// Fixes <see cref="ProjectValidationItem"/> item
        /// </summary>
        /// <param name="item"></param>
        internal static void FixItem(ProjectValidationItem item)
        {
            item.fixAction();
            item.isFixed = true;

            // Saving changes made in the current scene to reflect fixes
            Scene currentScene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(currentScene);

            ProjectValidation.RegenerateItems();
        }

        /// <summary>
        /// Ignores <see cref="ProjectValidationItem"/> item
        /// </summary>
        /// <param name="item"></param>
        internal static void IgnoreItem(ProjectValidationItem item, bool ignoreStatus)
        {
            item.isIgnored = ignoreStatus;
            if (ignoreStatus == true)
            {
                ProjectValidationLog.AddIgnoreItem(item.message);
            }
            else
            {
                ProjectValidationLog.RemoveIgnoreItem(item.message);
            }
            
            ProjectValidation.RegenerateItems();
        }

        /// <summary>
        /// Sets ignored <see cref="ProjectValidationItem"/> items from log
        /// </summary>
        internal static void SetIgnoredItemsFromLog()
        {
            registry.SetIgnoredItemsFromLog(ProjectValidationLog.GetLogIgnoreItems());
        }

        /// <summary>
        /// Removes a <see cref="ProjectValidationItem"/> item from registry
        /// </summary>
        internal static void RemoveItem(Hash128 id)
        {
            registry.RemoveItem(id);
        }

        /// <summary>
        /// Updates the message of a validation item that contain the specified message
        /// </summary>
        /// <param name="oldmessage">The message to search for in the validation items</param>
        /// <param name="newmessage">The new message to replace the old message with</param>
        public static void UpdateItemMessage(string oldmessage, string newmessage)
        {
            var items = registry.GetAllItems();
            foreach (var item in items)
            {
                if (item.message.Contains(oldmessage))
                {
                    item.message = newmessage;
                }
            }
        }

        /// <summary>
        /// Reevaluates all validation items and updates their fixed status
        /// </summary>
        /// Save this function for later
        public static void UpdateItemFixedStatus()
        {
            var items = registry.GetAllItems();
            foreach (var item in items)
            {
                item.isFixed = item.checkAction();
            }
        }

        /// <summary>
        /// Regenerates the validation item list by clearing the existing items and rebuilding the list
        /// </summary>
        public static void RegenerateItems()
        {
            Reset();
            ProjectValidationItems.DelayAndInitializeProjectValidation();
        }

        /// <summary>
        /// Resets project validation window GUI
        /// </summary>
        internal static void ResetGUI()
        {
            ProjectValidationGUI.Reset();
        }

        /// <summary>
        /// Resets ignored <see cref="ProjectValidationItem"/> items
        /// </summary>
        internal static void ResetIgnoredItems()
        {
            var ignoredItems =  registry.GetIgnoredItems(true);

            foreach (var item in ignoredItems)
            {
                item.isIgnored = false;
            }
        }

        /// <summary>
        /// Clears <see cref="ProjectValidationItem"/> list
        /// </summary>
        internal static void Reset()
        {
            registry.Clear();
        }

        /// <summary>
        /// Attempts to retrieve a list of controller names from the active scene.
        /// If no controllers are found, the function returns false
        /// </summary>
        /// <param name="controllerNamesList">An output list of controller names if found</param>
        /// <returns>Returns true if controllers are found, otherwise false</returns>
        internal static bool TryGetControllers(out List<String> controllerNamesList)
        {
            controllerNamesList = new List<string>();
            ProjectValidation.FindComponentInActiveScene<DynamicObject>(out var controllers);
            if (controllers == null)
            {
                return false;
            }

            foreach (var controller in controllers)
            {
                if (controller.IsController)
                {
                    controllerNamesList.Add(controller.name);
                }
            }
            return true;
        }

        /// <summary>
        /// Searches through game objects in active scene to find a component
        /// </summary>
        /// <typeparam name="T">Type of target component</typeparam>
        /// <returns></returns>
        internal static bool FindComponentInActiveScene<T>() where T : Component
        {
            var activeScene = SceneManager.GetActiveScene();
            var foundComponents = new List<T>();

            var rootObjects = activeScene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var components = rootObject.GetComponentsInChildren<T>(true);
                foundComponents.AddRange(components);
            }

            if (foundComponents != null && foundComponents.Count != 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Searches through game objects in active scene to find a component
        /// </summary>
        /// <typeparam name="T">Type of target component</typeparam>
        /// <param name="foundComponents">Founded components in active scene</param>
        internal static bool FindComponentInActiveScene<T>(out List<T> foundComponents) where T : Component
        {
            var activeScene = SceneManager.GetActiveScene();
            foundComponents = new List<T>();

            var rootObjects = activeScene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var components = rootObject.GetComponentsInChildren<T>(true);
                foundComponents.AddRange(components);
            }

            if (foundComponents != null && foundComponents.Count != 0)
            {
                return true;
            }

            return false;
        }
    }
}
