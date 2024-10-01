using System;
using UnityEngine;

namespace Cognitive3D
{
    internal class ProjectValidationItem
    {
        public Hash128 id;
        public ProjectValidation.ItemLevel level { get; }
        public ProjectValidation.ItemCategory category { get; }
        public ProjectValidation.ItemAction actionType {get;}
        public string message { get; set; }
        public string fixmessage { get; }
        public Func<bool> checkAction;
        public bool isFixed;
        public Action fixAction { get; }
        public bool isIgnored;

        public ProjectValidationItem(ProjectValidation.ItemLevel level, ProjectValidation.ItemCategory category, ProjectValidation.ItemAction actionType, string message, string fixmessage, Func<bool> checkAction, Action fixAction, bool isIgnored)
        {
            this.level = level;
            this.category = category;
            this.actionType = actionType;
            this.message = message;
            this.fixmessage = fixmessage;
            this.checkAction = checkAction;
            this.isFixed = checkAction.Invoke();
            this.fixAction = fixAction;
            this.isIgnored = isIgnored;

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
