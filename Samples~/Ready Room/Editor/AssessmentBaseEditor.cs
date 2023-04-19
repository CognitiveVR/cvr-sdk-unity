using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D.ReadyRoom
{
    [CustomEditor(typeof(AssessmentBase), true)]
    public class AssessmentBaseEditor : Editor
    {
        GUIStyle helpboxStyle;
        GUIStyle boldWrap;
        public void InitGUIStyle()
        {
            if (helpboxStyle != null) { return; }
            helpboxStyle = new GUIStyle(EditorStyles.helpBox);
            helpboxStyle.richText = true;

            boldWrap = new GUIStyle(EditorStyles.boldLabel);
            boldWrap.wordWrap = true;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var ab = target as AssessmentBase;

            //display warnings from setup
            bool skipAssessment = false;
            if (EditorPrefs.GetInt("useEyeTracking", -1) != 1 && ab.RequiresEyeTracking)
            {
                EditorGUILayout.HelpBox("This assessment requires Eye Tracking.\n\nReady Room is not configured to use Eye Tracking or the selected VR SDK does not support Eye Tracking\n\nThis assessment will be skipped", MessageType.Warning, true);
                GUILayout.Space(15);
                skipAssessment = true;
            }
            else if (EditorPrefs.GetInt("useGrabbable", -1) != 1 && ab.RequiresGrabbing)
            {
                EditorGUILayout.HelpBox("This assessment requires grabbing objects.\n\nReady Room is not configured to use grabbing objects or the selected VR SDK does not support grabbing objects\n\nThis assessment will be skipped", MessageType.Warning, true);
                GUILayout.Space(15);
                skipAssessment = true;
            }
            else if (EditorPrefs.GetInt("useRoomScale", -1) != 1 && ab.RequiresRoomScale)
            {
                EditorGUILayout.HelpBox("This assessment requires Room Scale.\n\nReady Room is not configured to use Room Scale or the selected VR SDK does not support Room Scale\n\nThis assessment will be skipped", MessageType.Warning, true);
                GUILayout.Space(15);
                skipAssessment = true;
            }
            if (!skipAssessment)
            {
                //whatever setup stuff you need


                ////display list of enable/disabled objects
                //string controlledComponentList = "These components are enabled only while this Assessment is active:\n";
                //int childCount = ab.transform.childCount;
                //if (childCount > 0)
                //    controlledComponentList += "\n<b>Child Transforms</b>";
                //
                //InitGUIStyle();
                //EditorGUILayout.LabelField(new GUIContent(controlledComponentList, "These GameObjects are:\n- Disabled on OnEnable\n- Enabled on BeginAssessment\n- Disabled on CompleteAssessment"), helpboxStyle);
                //
                //EditorGUI.BeginDisabledGroup(true);
                //var textComponent = ab.GetComponentInChildren<UnityEngine.UI.Text>();
                //if (textComponent != null)
                //{
                //    EditorGUILayout.LabelField("Text Display:", textComponent.text, boldWrap);
                //}
                ////EditorGUILayout.IntField(new GUIContent("Order", "The order this assessment will be displayed to the participant. Can be set in the Ready Room Setup window"), ab.Order);
                //EditorGUI.EndDisabledGroup();
            }
        }
    }
}