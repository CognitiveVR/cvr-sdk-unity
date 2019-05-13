using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// base for CognitiveVR analytics components
/// </summary>

namespace CognitiveVR.Components
{
    public abstract class CognitiveVRAnalyticsComponent : MonoBehaviour
    {
        public virtual void CognitiveVR_Init(Error initError)
        {
            //called after cognitiveVR initializes
        }

        //CognitiveVR Component Setup uses reflection to find these Methods. These help display each component, but are not required
        public virtual bool GetWarning() { return false; }
        public virtual bool GetError() { return false; }
        public virtual string GetDescription() { return "description"; }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CognitiveVRAnalyticsComponent), true)]
    public class ComponentInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as CognitiveVRAnalyticsComponent;

            MessageType messageType = MessageType.Info;

            if (component.GetWarning()) { messageType = MessageType.Warning; }
            if (component.GetError()) { messageType = MessageType.Error; }


            EditorGUILayout.HelpBox(component.GetDescription(), messageType);
        }
    }

#endif
}