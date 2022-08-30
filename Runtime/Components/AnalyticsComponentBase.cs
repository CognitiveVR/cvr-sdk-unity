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
    public abstract class AnalyticsComponentBase : MonoBehaviour
    {
        protected void Start()
        {
            //if this component is enabled late, run startup as if session just began
            if (Cognitive3D_Manager.IsInitialized)
                Cognitive3D_Init();
        }

        /// <summary>
        /// called as the last step of the Cognitive3D session begin process OR on start if Cognitive3D Manager has already been initialized
        /// </summary>
        public virtual void Cognitive3D_Init()
        {
            
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