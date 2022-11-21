using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// base for Cognitive3D analytics components
/// handles delay if components are enabled before session start, enabled late and sequential session in one game instance
/// </summary>

namespace Cognitive3D.Components
{
    public abstract class AnalyticsComponentBase : MonoBehaviour
    {
        /// <summary>
        /// subscribes to Cognitive3D_Manager.OnSessionBegin and calls OnSessionBegin() if a session is currently active
        /// </summary>
        protected virtual void OnEnable()
        {
            Cognitive3D_Manager.OnSessionBegin += OnSessionBegin;

            //if this component is enabled late, run startup as if session just began
            if (Cognitive3D_Manager.IsInitialized)
                OnSessionBegin();
        }

        /// <summary>
        /// use this to initialize values, start functions, etc
        /// add any needed callbacks here
        /// </summary>
        protected virtual void OnSessionBegin()
        {
            
        }

        protected virtual void OnDisable()
        {
            Cognitive3D_Manager.OnSessionBegin -= OnSessionBegin;
        }

        //Cognitive3D Component Setup uses reflection to find these Methods. These help display each component, but are not required
        public virtual bool GetWarning() { return false; }
        public virtual bool GetError() { return false; }
        public virtual string GetDescription() { return "description"; }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AnalyticsComponentBase), true)]
    public class ComponentInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as AnalyticsComponentBase;

            MessageType messageType = MessageType.Info;

            if (component.GetWarning()) { messageType = MessageType.Warning; }
            if (component.GetError()) { messageType = MessageType.Error; }


            EditorGUILayout.HelpBox(component.GetDescription(), messageType);
        }
    }

#endif
}