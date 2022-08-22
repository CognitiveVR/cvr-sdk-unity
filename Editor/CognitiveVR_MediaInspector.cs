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

            //media source / media id
            EditorGUILayout.LabelField("Media Id", m.MediaSource);

            EditorGUILayout.BeginHorizontal();
            string[] displayOptions = new string[EditorCore.MediaSources.Length];
            if (displayOptions.Length == 0)
            {
                if (GUILayout.Button("Refresh Media"))
                {
                    EditorCore.RefreshMediaSources();
                }
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (!string.IsNullOrEmpty(m.MediaSource) && _choiceIndex == 0 && displayOptions.Length > 0)
            {
                //try once to select the correct media
                for (int i = 0; i < EditorCore.MediaSources.Length; i++)
                {
                    if (EditorCore.MediaSources[i].uploadId == m.MediaSource)
                    {
                        _choiceIndex = i;
                    }
                }
            }

            for (int i = 0; i < EditorCore.MediaSources.Length; i++)
            {
                displayOptions[i] = EditorCore.MediaSources[i].name;
            }
            _choiceIndex = EditorGUILayout.Popup("Select Media Source", _choiceIndex, displayOptions);


            if (m.MediaSource != EditorCore.MediaSources[_choiceIndex].uploadId)
            {
                GUI.color = Color.green;
            }

            if (GUILayout.Button("Save", GUILayout.Width(40)))
            {
                m.MediaSource = EditorCore.MediaSources[_choiceIndex].uploadId;
            }

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Description", EditorCore.MediaSources[_choiceIndex].description);

            if (GUI.changed)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}