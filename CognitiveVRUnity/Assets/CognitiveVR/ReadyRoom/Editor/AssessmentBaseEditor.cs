using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AssessmentBase),true)]
public class AssessmentBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var ab = target as AssessmentBase;

        bool skipAssessment = false;
        if (ReadyRoomSetupWindow.UseEyeTracking != 1 && ab.RequiresEyeTracking)
        {
            EditorGUILayout.HelpBox("This assessment requires Eye Tracking.\n\nReady Room is not configured to use Eye Tracking or the selected VR SDK does not support Eye Tracking\n\nThis assessment will be skipped", MessageType.Warning, true);
            GUILayout.Space(15);
            skipAssessment = true;
        }
        else if (ReadyRoomSetupWindow.UseGrabbableObjects != 1 && ab.RequiresGrabbing)
        {
            EditorGUILayout.HelpBox("This assessment requires grabbing objects.\n\nReady Room is not configured to use grabbing objects or the selected VR SDK does not support grabbing objects\n\nThis assessment will be skipped", MessageType.Warning, true);
            GUILayout.Space(15);
            skipAssessment = true;
        }
        else if (ReadyRoomSetupWindow.UseRoomScale != 1 && ab.RequiresRoomScale)
        {
            EditorGUILayout.HelpBox("This assessment requires Room Scale.\n\nReady Room is not configured to use Room Scale or the selected VR SDK does not support Room Scale\n\nThis assessment will be skipped", MessageType.Warning, true);
            GUILayout.Space(15);
            skipAssessment = true;
        }
        if (!skipAssessment)
        {
            var textComponent = ab.GetComponentInChildren<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                EditorGUILayout.LabelField("Text Display:",textComponent.text, EditorStyles.boldLabel);
                GUILayout.Space(15);
            }
        }

        base.OnInspectorGUI();
    }
}
