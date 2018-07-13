using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace CognitiveVR
{
    [CustomEditor(typeof(MediaComponent))]
    public class MediaComponentInspector : Editor
    {
        int _choiceIndex = 0;

        public override void OnInspectorGUI()
        {
            MediaComponent m = (MediaComponent)target;

            var meshrenderer = m.GetComponent<MeshRenderer>();

            if (m.VideoPlayer == null && meshrenderer != null)
            {
                //image
            }
            else if (m.VideoPlayer != null)
            {
                //video
            }
            else
            {
                EditorGUILayout.HelpBox("If media is a video, Video Player must be set\nIf media is an image, must have mesh render on this gameobject", MessageType.Error);
                //not set up
            }

            //display script field
            var script = serializedObject.FindProperty("m_Script");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            //video player component
            m.VideoPlayer = (UnityEngine.Video.VideoPlayer)EditorGUILayout.ObjectField("Video Player", m.VideoPlayer, typeof(UnityEngine.Video.VideoPlayer), true);

            //media source
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Media Source", m.MediaSource);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            _choiceIndex = EditorGUILayout.Popup("Select Media Source",_choiceIndex, EditorCore.MediaSources);
            if (GUILayout.Button("Save",GUILayout.Width(40)))
            {
                m.MediaSource = EditorCore.MediaSources[_choiceIndex];
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUI.changed)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}