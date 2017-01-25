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

            EditorGUILayout.HelpBox("Persists between scenes\nInitializes cognitiveVR Analytics\nGathers basic device info", MessageType.Info);

            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR_ComponentSetup.Init();
            }
        }
    }
}