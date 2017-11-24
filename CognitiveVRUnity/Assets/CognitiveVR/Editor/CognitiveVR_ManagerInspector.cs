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
            if (m.EnableLogging)
            {
                EditorGUILayout.HelpBox("Enable Logging is helpful for setting up Cognitive Analytics but Debug.Log will affect performance in the Editor and development builds!", MessageType.Warning);
            }


            EditorGUILayout.HelpBox("Persists between scenes\nInitializes cognitiveVR Analytics\nGathers basic device info", MessageType.Info);

            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR_ComponentSetup.Init();
            }
        }
    }
}