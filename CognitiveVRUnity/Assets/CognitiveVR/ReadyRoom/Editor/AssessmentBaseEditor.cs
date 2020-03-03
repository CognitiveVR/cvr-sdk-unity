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
        if (ab.Active == false)
        {
            EditorGUILayout.HelpBox("This assessment is disabled because due to how Ready Room is configured", MessageType.Warning);
        }
        base.OnInspectorGUI();
    }
}
