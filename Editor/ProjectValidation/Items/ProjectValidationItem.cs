using System;
using UnityEngine;

namespace Cognitive3D
{
    internal class ProjectValidationItem
    {
        public Hash128 id;
        public ProjectValidation.ItemLevel level { get; }
        public ProjectValidation.ItemCategory category { get; }
        public string message { get; }
        public string fixmessage { get; }
        public Func<bool> checkAction;
        public bool isFixed;
        public Action fixAction { get; }

        public ProjectValidationItem(ProjectValidation.ItemLevel level, ProjectValidation.ItemCategory category, string message, string fixmessage, Func<bool> checkAction, Action fixAction)
        {
            this.level = level;
            this.category = category;
            this.message = message;
            this.fixmessage = fixmessage;
            this.checkAction = checkAction;
            this.isFixed = checkAction.Invoke();
            this.fixAction = fixAction;

#if UNITY_2020_3_OR_NEWER
            var hash = new Hash128();
            hash.Append(this.message);
            id = hash;
#else
            var hash = Hash128.Compute(this.message);
            id = hash;
#endif
        }
    }
}
