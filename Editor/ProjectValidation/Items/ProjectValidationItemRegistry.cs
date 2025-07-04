using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cognitive3D
{
    internal class ProjectValidationItemRegistry
    {
        private readonly Dictionary<Hash128, ProjectValidationItem> itemsPerID = new Dictionary<Hash128, ProjectValidationItem>();

        private readonly List<ProjectValidationItem> items = new List<ProjectValidationItem>();

        public void AddItem(ProjectValidationItem item)
        {
            var id = item.id;
            if (itemsPerID.ContainsKey(id))
            {
                // This item is already registered
                return;
            }

            items.Add(item);
            itemsPerID.Add(id, item);
        }

        public void RemoveItem(Hash128 id)
        {
            var item = GetItem(id);
            RemoveItem(item);
        }

        public void RemoveItem(ProjectValidationItem item)
        {
            items.Remove(item);
            itemsPerID.Remove(item.id);
        }

        public ProjectValidationItem GetItem(Hash128 id)
        {
            itemsPerID.TryGetValue(id, out var item);
            return item;
        }

        public IEnumerable<ProjectValidationItem> GetItems(ProjectValidation.ItemLevel level)
        {
            return items.Where(item => item.level == level);
        }

        public IEnumerable<ProjectValidationItem> GetItems(ProjectValidation.ItemCategory category)
        {
            return items.Where(item => item.category == category);
        }

        public IEnumerable<ProjectValidationItem> GetFixedItems()
        {
            return items.Where(item => item.isFixed == true);
        }

        public IEnumerable<ProjectValidation.ItemLevel> GetLevelsOfItemsNotFixed()
        {
            return items.Where(item => !item.isFixed && !item.isIgnored).Select(item => item.level).Distinct();
        }

        public bool hasNotFixedItems()
        {
            return items.Where(item => !item.isFixed && !item.isIgnored).Count() != 0 ? true : false;
        }

        public IEnumerable<ProjectValidationItem> GetIgnoredItems(bool isIgnored)
        {
            return items.Where(item => item.isIgnored == isIgnored);
        }

        public IEnumerable<ProjectValidationItem> GetIgnoredItems(bool isIgnored, ProjectValidation.ItemLevel level)
        {
            return items.Where(item => item.isIgnored == isIgnored && item.level == level);
        }

        public void SetIgnoredItemsFromLog(List<string> ignoredMessages)
        {
            if (ignoredMessages != null && ignoredMessages.Count != 0)
            {
                items.ForEach(item =>
                {
                    if (!item.isFixed)
                    {
                        item.isIgnored = ignoredMessages.Contains(item.message);
                    }
                });
            }
        }

        public List<ProjectValidationItem> GetAllItems()
        {
            return items;
        }

        public void Clear()
        {
            itemsPerID.Clear();
            items.Clear();
        }
    }
}
