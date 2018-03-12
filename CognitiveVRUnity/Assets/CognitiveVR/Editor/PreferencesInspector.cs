using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;

namespace CognitiveVR
{
    [CustomEditor(typeof(CognitiveVR_Preferences))]
    public class PreferencesInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Persists between scenes\nInitializes cognitiveVR Analytics\nGathers basic device info", MessageType.Info);

            


            base.OnInspectorGUI();

            //CognitiveVR_Manager m = (CognitiveVR_Manager)target;
        }
    }
}