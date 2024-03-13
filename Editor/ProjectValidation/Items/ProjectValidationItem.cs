using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    internal class ProjectValidationItem
    {
        public Hash128 id;
        public ProjectValidation.ItemLevel level { get; }
        public string message { get; }
        public string fixmessage { get; }
        public bool isFixed;
        public Action fixAction { get; }

        public ProjectValidationItem(ProjectValidation.ItemLevel level, string message, string fixmessage, bool isFixed, Action fixAction)
        {
            this.level = level;
            this.message = message;
            this.fixmessage = fixmessage;
            this.isFixed = isFixed;
            this.fixAction = fixAction;

            var hash = new Hash128();
            hash.Append(this.message);
            id = hash;

            Debug.Log("@@@ id is " + id);
        }
    }
}
