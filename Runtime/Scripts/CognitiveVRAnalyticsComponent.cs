using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// base for Cognitive3D analytics components
/// </summary>

namespace Cognitive3D.Components
{
    public abstract class Cognitive3DAnalyticsComponent : MonoBehaviour
    {
        public virtual void Cognitive3D_Init(Error initError)
        {
            //called after Cognitive3D initializes
        }

        //Cognitive3D Component Setup uses reflection to find these Methods. These help display each component, but are not required
        public virtual bool GetWarning() { return false; }
        public virtual bool GetError() { return false; }
        public virtual string GetDescription() { return "description"; }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Cognitive3DAnalyticsComponent), true)]
    public class ComponentInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as Cognitive3DAnalyticsComponent;

            MessageType messageType = MessageType.Info;

            if (component.GetWarning()) { messageType = MessageType.Warning; }
            if (component.GetError()) { messageType = MessageType.Error; }


            EditorGUILayout.HelpBox(component.GetDescription(), messageType);
        }
    }

#endif
}