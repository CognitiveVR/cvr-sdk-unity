using UnityEngine;
using UnityEditor;

namespace Cognitive3D.Identify
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
#if UNITY_6000_6_OR_NEWER
                var allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
#else
                var allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
                foreach (var mb in allBehaviours)
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
