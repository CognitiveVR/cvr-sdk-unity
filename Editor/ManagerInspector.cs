using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;

namespace Cognitive3D
{
    [CustomEditor(typeof(Cognitive3D_Manager))]
    public class ManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Cognitive3D_Manager m = (Cognitive3D_Manager)target;

            if (m.StartupDelayTime < 0)
            {
                m.StartupDelayTime = 0;
            }

            EditorGUILayout.HelpBox("Persists between scenes\nInitializes Cognitive3D Analytics\nGathers basic device info", MessageType.Info);
        }
    }
}