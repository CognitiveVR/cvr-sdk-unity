using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrabComponentsRequired))]
[CanEditMultipleObjects]
public class GrabComponentsRequiredEditor : Editor
{
    bool m_Initialized;

    GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
    [SerializeField] GUIStyle m_HeadingStyle;

    GUIStyle BodyStyle { get { return m_BodyStyle; } }
    [SerializeField] GUIStyle m_BodyStyle;

    GUIStyle SmallBodyStyle { get { return m_SmallBodyStyle; } }
    [SerializeField] GUIStyle m_SmallBodyStyle;

    void InitGUIStyles()
    {
        if (m_Initialized)
            return;
        m_BodyStyle = new GUIStyle(EditorStyles.label);
        m_BodyStyle.wordWrap = true;
        m_BodyStyle.fontSize = 14;

        m_SmallBodyStyle = new GUIStyle(EditorStyles.label);
        m_SmallBodyStyle.wordWrap = true;
        m_SmallBodyStyle.fontSize = 11;

        m_HeadingStyle = new GUIStyle(m_BodyStyle);
        m_HeadingStyle.fontSize = 18;

        m_Initialized = true;
    }

    public override void OnInspectorGUI()
	{
        base.OnInspectorGUI();
        InitGUIStyles();

        GUILayout.Label("Setup Instructions", HeadingStyle);

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("1. Add a 'grabbable' component from your chosen SDK", BodyStyle);
        EditorGUILayout.LabelField("2. Add a collider if necessary", BodyStyle);
        EditorGUILayout.LabelField("3. Remove this component", BodyStyle);
        EditorGUI.indentLevel--;

        GUILayout.Label("Quick Setup", HeadingStyle);
        EditorGUILayout.LabelField("If you are using a standard SDK input system, you can select the SDK below to add the required components", SmallBodyStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Oculus"))
        {
            ReadyRoomSetupWindow.SetupOculus(serializedObject.targetObjects);
        }
        if (GUILayout.Button("SteamVR2 Interactions"))
        {
            ReadyRoomSetupWindow.SetupSteamVR2(serializedObject.targetObjects);
        }
        if (GUILayout.Button("Unity XR Interaction Toolkit"))
        {
            ReadyRoomSetupWindow.SetupXRInteractionToolkit(serializedObject.targetObjects);
        }
        GUILayout.EndHorizontal();
    }
}

