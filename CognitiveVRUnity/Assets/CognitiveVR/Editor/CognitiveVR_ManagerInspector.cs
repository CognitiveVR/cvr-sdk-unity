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

            EditorGUILayout.HelpBox("Initializes the CognitiveVR Analytics platform on Start\nAutomatically records device data", MessageType.Info);

            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR_ComponentSetup.Init();
            }
            //doesn't work in 5.4?
            /*if (GUILayout.Button("Open CognitiveVR Preferences"))
            {
                var asm = System.Reflection.Assembly.GetAssembly(typeof(EditorWindow));
                var T = asm.GetType("UnityEditor.PreferencesWindow");
                var M = T.GetMethod("ShowPreferencesWindow", BindingFlags.NonPublic | BindingFlags.Static);
                PropertyInfo selectedSection = T.GetProperty("selectedSectionIndex", BindingFlags.Instance | BindingFlags.NonPublic);

                //open window
                M.Invoke(null, null);
                var window = UnityEditor.EditorWindow.GetWindow(T);

                //repaint and select cognitive preferences
                T.GetMethod("RepaintImmediately", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, null);
                //TODO check which section cognitive
                selectedSection.SetValue(window, 7, null);
            }*/
        }
    }
}