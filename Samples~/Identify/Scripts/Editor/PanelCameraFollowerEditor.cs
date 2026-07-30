using UnityEditor;
using UnityEngine;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Inspector for PanelCameraFollower. When a parent IdentificationPanelBase governs it,
    /// the anchor fields are hidden (driven by that panel)
    /// </summary>
    [CustomEditor(typeof(PanelCameraFollower))]
    [CanEditMultipleObjects]
    public class PanelCameraFollowerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var follower = target as PanelCameraFollower;
            IdentificationPanelBase governing = follower != null
                ? follower.GetComponentInParent<IdentificationPanelBase>(true)
                : null;

            if (governing != null)
            {
                EditorGUILayout.HelpBox(
                    "Anchoring is driven by the \"" + governing.GetType().Name + "\" on \"" +
                    governing.name + "\" (its Panel Anchoring section). These settings are set " +
                    "there and overridden at runtime.",
                    MessageType.Info);

                return;
            }

            // Standalone use — no governing panel, so let the fields be edited directly.
            DrawDefaultInspector();
        }
    }
}
