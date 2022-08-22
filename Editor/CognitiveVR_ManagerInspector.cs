using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;

namespace CognitiveVR
{
    [CustomEditor(typeof(CognitiveVR_Manager))]
    public class CognitiveVR_ManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            CognitiveVR_Manager m = (CognitiveVR_Manager)target;

            if (m.StartupDelayTime < 0)
            {
                m.StartupDelayTime = 0;
            }

            EditorGUILayout.HelpBox("Persists between scenes\nInitializes cognitiveVR Analytics\nGathers basic device info", MessageType.Info);
        }
    }
}