using System.Collections;
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
