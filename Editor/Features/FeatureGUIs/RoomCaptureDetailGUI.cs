using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class RoomCaptureDetailGUI : IFeatureDetailGUI
    {
        private const string WAVE_SCENEPERCEPTION_DEFINE = "C3D_VIVEWAVE_SCENEPERCEPTION";

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Room Capture", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Audio Recording documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/room-capture/");
                }
                
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Captures spatial layout (walls, floors, furniture, etc.) for mixed-reality experiences. " +
                "Records anchor poses and supports gaze tracking on room surfaces. " +
                "Supports Meta MRUK, HTC Vive Wave Scene Perception, and Unity AR Foundation.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            GUILayout.Label("Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the Room Capture component to the Cognitive3D_Manager prefab.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);

            var roomLayoutLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.RoomCapture>()
                ? "Remove Room Capture"
                : "Add Room Capture";
            if (GUILayout.Button(roomLayoutLabel, GUILayout.Height(30)))
            {
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.RoomCapture>();
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
                metaMrukDetected ? MessageType.Info : MessageType.Warning);

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
                    ? "Disable Cognitive3D Wave Scene Perception Support"
                    : "Enable Cognitive3D Wave Scene Perception Support");

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

            EditorGUILayout.Space(15);

            GUILayout.Label("Unity AR Foundation", EditorCore.styles.FeatureTitle);

            bool arFoundationDetected = false;
#if COGNITIVE3D_AR_FOUNDATION_6_0_OR_NEWER
            arFoundationDetected = true;
#endif
            EditorGUILayout.HelpBox(
                arFoundationDetected
                    ? "Unity AR Foundation detected. The AR Foundation provider is used automatically on AR Foundation platforms. " +
                      "Requires an AR Session with an ARPlaneManager and/or ARBoundingBoxManager for capture, and an ARRaycastManager for gaze on surfaces."
                    : "AR Foundation not detected. Install Unity AR Foundation (com.unity.xr.arfoundation) 6.0.7 or newer to enable AR Foundation room layout support.",
                arFoundationDetected ? MessageType.Info : MessageType.Warning);
        }
    }
}
