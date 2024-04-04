using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Cognitive3D
{
    internal static class ProjectValidation
    {
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

        internal static ProjectValidationItemRegistry registry = new ProjectValidationItemRegistry();

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
        internal static void AddItem(ItemLevel level, ItemCategory category, string message, string fixmessage, Func<bool> checkAction, Action fixAction = null)
        {
            var newItem = new ProjectValidationItem(level, category, message, fixmessage, checkAction, fixAction);
            AddItem(newItem);
        }

        /// <summary>
        /// Gets all existed <see cref="ProjectValidationItem"/>s
        /// </summary>
        internal static IEnumerable<ProjectValidationItem> GetAllItems()
        {
            return registry.GetAllItems();
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

            ProjectValidationItems.UpdateProjectValidationItemStatus();
        }

        /// <summary>
        /// Clears <see cref="ProjectValidationItem"/> list
        /// </summary>
        internal static void Reset()
        {
            registry.Clear();
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
