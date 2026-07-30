using UnityEngine;
using UnityEditor;

namespace Cognitive3D.Auth
{
    [CustomEditor(typeof(PanelColorScheme))]
    public class PanelColorSchemeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                PanelColorScheme scheme = (PanelColorScheme)target;

                // Refresh every panel in the scene that uses this scheme (QR, PIN, Verbal, Confirmation)
                foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
                {
                    if (!(mb is IPanelColorScheme applier)) continue;

                    SerializedObject so = new SerializedObject(mb);
                    var schemeProp = so.FindProperty("colorScheme");
                    if (schemeProp != null && schemeProp.objectReferenceValue == scheme)
                    {
                        applier.ApplyColorScheme(scheme);
                        EditorUtility.SetDirty(mb);
                    }
                }

                SceneView.RepaintAll();
            }
        }
    }
}
