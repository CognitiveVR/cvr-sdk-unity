using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
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
            m_BodyStyle.richText = true;

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
            EditorGUILayout.LabelField("1. Add your interaction script(s) to this object, to make it grabbable.", BodyStyle);
#if CVR_OCULUS
        EditorGUILayout.HelpBox("Oculus: You likely need to add the \"Grabbable\" component", MessageType.Info);
#elif CVR_STEAMVR
        EditorGUILayout.HelpBox("SteamVR: You likely need to add the \"Interactable\" and \"Throwable\" components\n\nYou may need to set the \"Attachment Flags\" as well", MessageType.Info);
#elif CVR_STEAMVR2
        EditorGUILayout.HelpBox("SteamVR2: You likely need to add the \"Interactable\" and \"Throwable\" components\n\nYou may need to set the \"Attachment Flags\" as well", MessageType.Info);
#elif CVR_UNITYXR
        EditorGUILayout.HelpBox("UnityXR: You likely need to add the \"XR Grab Interactable\" component", MessageType.Info);
#endif
            EditorGUILayout.LabelField("2. Add a collider if necessary", BodyStyle);
            EditorGUILayout.LabelField("3. Remove this component", BodyStyle);
            EditorGUI.indentLevel--;
        }
    }
}