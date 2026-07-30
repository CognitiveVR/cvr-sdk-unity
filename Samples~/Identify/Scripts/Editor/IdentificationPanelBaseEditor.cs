using UnityEditor;
using UnityEngine;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Shared inspector for IdentificationPanelBase-derived panels
    /// </summary>
    [CustomEditor(typeof(IdentificationPanelBase), true)]
    [CanEditMultipleObjects]
    public class IdentificationPanelBaseEditor : Editor
    {
        private const string PointerSettingsProp = "pointerSettings";
        private const string InteractionModeProp = "interactionMode";
        private const string ScriptProp = "m_Script";

        private const string AnchorModeProp = "anchorMode";
        private const string AnchorDistanceProp = "anchorDistance";
        private const string AnchorCameraOverrideProp = "anchorCameraOverride";
        private const string PlayerRelativeFollowProp = "playerRelativeFollow";
        private const string WorldSpaceSettingsProp = "worldSpaceSettings";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Snapshot the interaction mode before the user edits, so we can detect a change
            // and add/remove the matching canvas raycaster across the whole prefab.
            var modeProp = serializedObject.FindProperty(InteractionModeProp);
            int modeBefore = modeProp != null ? modeProp.enumValueIndex : -1;

            var iter = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iter.NextVisible(enterChildren))
            {
                enterChildren = false;

                // These are drawn manually (conditionally) right after the field they belong to.
                if (iter.propertyPath == PointerSettingsProp ||
                    iter.propertyPath == AnchorDistanceProp ||
                    iter.propertyPath == AnchorCameraOverrideProp ||
                    iter.propertyPath == PlayerRelativeFollowProp ||
                    iter.propertyPath == WorldSpaceSettingsProp)
                    continue;

                using (new EditorGUI.DisabledScope(iter.propertyPath == ScriptProp))
                {
                    EditorGUILayout.PropertyField(iter, true);
                }

                // Inject the pointer fields inline, immediately after interactionMode,
                // so they visually belong to the "VR Interaction" section.
                if (iter.propertyPath == InteractionModeProp &&
                    iter.enumValueIndex == (int)PanelInteractionMode.Cognitive3DPointer)
                {
                    DrawPointerSettingsInline();
                }

                // Show only the anchor fields relevant to the selected anchor mode.
                if (iter.propertyPath == AnchorModeProp)
                {
                    DrawAnchorFieldsInline(iter.enumValueIndex);
                }
            }

            serializedObject.ApplyModifiedProperties();

            // Interaction mode changed: reconfigure each Canvas's raycaster (XRIRayInteractor adds
            // the Tracked Device Graphic Raycaster, others remove it). Deferred so it isn't mid-layout.
            if (modeProp != null && !modeProp.hasMultipleDifferentValues &&
                modeProp.enumValueIndex != modeBefore)
            {
                var mode = (PanelInteractionMode)modeProp.enumValueIndex;

                var roots = new System.Collections.Generic.List<GameObject>();
                foreach (var t in targets)
                {
                    var comp = t as Component;
                    if (comp != null) roots.Add(comp.gameObject);
                }

                EditorApplication.delayCall += () =>
                {
                    foreach (var go in roots)
                    {
                        if (go == null) continue;
                        PanelInteractionSetup.ConfigureAllRaycasters(go, mode);
                        EditorUtility.SetDirty(go);
                    }
                };
            }
        }

        /// <summary>
        /// Draws only the anchor sub-fields that apply to the selected mode.
        /// </summary>
        private void DrawAnchorFieldsInline(int modeIndex)
        {
            var mode = (PanelAnchorMode)modeIndex;

            EditorGUI.indentLevel++;

            var camOverride = serializedObject.FindProperty(AnchorCameraOverrideProp);

            if (mode == PanelAnchorMode.FollowCamera)
            {
                // Follow Camera has no follow settings, so it carries its own distance.
                var distance = serializedObject.FindProperty(AnchorDistanceProp);
                if (distance != null) EditorGUILayout.PropertyField(distance);
                if (camOverride != null) EditorGUILayout.PropertyField(camOverride);
            }
            else if (mode == PanelAnchorMode.PlayerRelative)
            {
                // Distance comes from Display Distance below — no separate anchor distance field.
                if (camOverride != null) EditorGUILayout.PropertyField(camOverride);

                // Draw the follow settings flat (not a foldout) so they read as part of this section.
                var follow = serializedObject.FindProperty(PlayerRelativeFollowProp);
                if (follow != null)
                {
                    DrawRelative(follow, "stickWindow");
                    DrawRelative(follow, "lockYPosition");
                    DrawRelative(follow, "rotateToStayOnScreen");
                    DrawRelative(follow, "displayDistance");
                    DrawRelative(follow, "minimumDisplayDistance");
                }
            }
            else if (mode == PanelAnchorMode.WorldSpace)
            {
                // World Space needs no camera. Draw the placement toggles flat; each value field
                // appears only when its toggle is on.
                var ws = serializedObject.FindProperty(WorldSpaceSettingsProp);
                if (ws != null)
                {
                    DrawToggleThenValue(ws, "useOverridePosition", "overridePosition");
                    DrawToggleThenValue(ws, "useOverrideRotation", "overrideRotationEuler");
                    DrawToggleThenValue(ws, "useAttachTransform", "attachTransform");
                }
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawRelative(SerializedProperty parent, string relativeName)
        {
            var prop = parent.FindPropertyRelative(relativeName);
            if (prop != null) EditorGUILayout.PropertyField(prop);
        }

        // Draws a bool toggle, and its associated value field only when the toggle is on.
        private static void DrawToggleThenValue(SerializedProperty parent, string toggleName, string valueName)
        {
            var toggle = parent.FindPropertyRelative(toggleName);
            if (toggle == null) return;

            EditorGUILayout.PropertyField(toggle);
            if (toggle.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawRelative(parent, valueName);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPointerSettingsInline()
        {
            var settingsProp = serializedObject.FindProperty(PointerSettingsProp);
            if (settingsProp == null) return;

            EditorGUI.indentLevel++;

            var prefab = settingsProp.FindPropertyRelative("PointerPrefab");
            var activationButton = settingsProp.FindPropertyRelative("PointerActivationButton");
            var lineWidth = settingsProp.FindPropertyRelative("PointerLineWidth");
            var gradient = settingsProp.FindPropertyRelative("PointerGradient");
            var posOffset = settingsProp.FindPropertyRelative("PointerPositionOffset");
            var rotOffset = settingsProp.FindPropertyRelative("PointerRotationOffset");

            EditorGUILayout.PropertyField(prefab, new GUIContent(
                "Pointer Prefab",
                "Prefab that manages pointer input and visuals. Place it as a child of this panel prefab " +
                "to use it directly; otherwise it will be spawned at runtime as a fallback."));

            EditorGUILayout.PropertyField(activationButton, new GUIContent(
                "Pointer Input Button",
                "Controller button that activates the pointer (used by PointerInputHandler)."));

            EditorGUILayout.PropertyField(lineWidth, new GUIContent(
                "Pointer Line Width",
                "Scaling factor applied to the LineRenderer.widthCurve on the pointer's PointerVisualizer."));

            EditorGUILayout.PropertyField(gradient, new GUIContent(
                "Pointer Gradient",
                "Gradient color applied to the pointer's line renderer."));

            EditorGUILayout.PropertyField(posOffset, new GUIContent(
                "Position Offset",
                "Local position offset applied to the controller pointer."));

            EditorGUILayout.PropertyField(rotOffset, new GUIContent(
                "Rotation Offset",
                "Local rotation offset (in degrees) applied to the controller pointer."));

            EditorGUI.indentLevel--;
        }
    }
}
