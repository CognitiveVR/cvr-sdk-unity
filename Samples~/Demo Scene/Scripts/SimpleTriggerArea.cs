using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//writes an event when a collider enters this trigger
//if that collider is a dynamic object, include a reference to that. otherwise, include the gameobject's name

namespace Cognitive3D.Demo
{
    public class SimpleTriggerArea : MonoBehaviour
    {
        public void OnTriggerEnter(Collider other)
        {
            Cognitive3D.CustomEvent customEvent = new Cognitive3D.CustomEvent("Trigger Enter");

            Cognitive3D.DynamicObject dynamicObject = other.GetComponent<Cognitive3D.DynamicObject>();
            if (dynamicObject != null)
            {
                customEvent.SetDynamicObject(dynamicObject.GetId());
            }
            else
            {
                customEvent.SetProperty("Object Name", other.gameObject.name);
            }
            customEvent.Send(other.transform.position);
        }
    }
}