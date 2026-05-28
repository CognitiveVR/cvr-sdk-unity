using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class RoomLayoutDetailGUI : IFeatureDetailGUI
    {
        private const string WAVE_SCENEPERCEPTION_DEFINE = "COGNITIVE3D_VIVE_SCENEPERCEPTION";

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Room Layout", EditorCore.styles.FeatureTitle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Captures spatial layout (walls, floors, furniture, etc.) for mixed-reality experiences. " +
                "Records anchor poses and supports gaze tracking on room surfaces. " +
                "Supports Meta MRUK and HTC Vive Wave Scene Perception.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            GUILayout.Label("Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the Room Layout component to the Cognitive3D_Manager prefab.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);

            var roomLayoutLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.RoomLayout>()
                ? "Remove Room Layout"
                : "Add Room Layout";
            if (GUILayout.Button(roomLayoutLabel, GUILayout.Height(30)))
            {
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.RoomLayout>();
            }

            EditorGUILayout.Space(15);

            // Meta MRUK status (read-only)
            GUILayout.Label("Meta MRUK", EditorCore.styles.FeatureTitle);

            bool metaMrukDetected = false;
#if COGNITIVE3D_META_MRUK_68_OR_NEWER
            metaMrukDetected = true;
#endif
            EditorGUILayout.HelpBox(
                metaMrukDetected
                    ? "Meta MRUK 68 or newer detected. The Meta provider will be used automatically when running on Quest hardware."
                    : "Meta MRUK 68+ not detected. Install the Meta XR MR Utility Kit package (com.meta.xr.mrutilitykit) to enable Meta room layout support.",
                metaMrukDetected ? MessageType.Info : MessageType.None);

            EditorGUILayout.Space(15);

            // Vive Wave Scene Perception toggle
            GUILayout.Label("Vive Wave Scene Perception", EditorCore.styles.FeatureTitle);

            bool viveWaveDetected = false;
#if C3D_VIVEWAVE
            viveWaveDetected = true;
#endif

            EditorGUILayout.HelpBox(
                "Wave Scene Perception is a beta feature pack that must be imported manually via " +
                "Project Settings > Wave XR > Essence > Scene Perception > Enable Scene Perception. " +
                "After importing, enable support here to add the " + WAVE_SCENEPERCEPTION_DEFINE + " scripting define.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            bool defineSet = EditorCore.GetPlayerDefines().Contains(WAVE_SCENEPERCEPTION_DEFINE);

            GUI.enabled = viveWaveDetected;
            var waveLabel = !viveWaveDetected
                ? "Wave SDK Not Detected"
                : (defineSet
                    ? "Disable Wave Scene Perception Support"
                    : "Enable Wave Scene Perception Support");

            if (GUILayout.Button(waveLabel, GUILayout.Height(30)))
            {
                if (defineSet)
                {
                    EditorCore.RemoveDefine(WAVE_SCENEPERCEPTION_DEFINE);
                }
                else
                {
                    EditorCore.AddDefine(WAVE_SCENEPERCEPTION_DEFINE);
                }
            }
            GUI.enabled = true;
        }
    }
}
