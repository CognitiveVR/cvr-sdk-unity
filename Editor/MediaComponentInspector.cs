using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Cognitive3D
{
    [CustomEditor(typeof(MediaComponent))]
    public class MediaComponentInspector : Editor
    {
        int _choiceIndex = 0;
        
        /// <summary>
        /// indicates that the developer has pressed the refresh button. used to display a warning if no media is returned
        /// </summary>
        private bool hasRefreshedMedia;
        private bool setupFoldout;
        public override void OnInspectorGUI()
        {
            MediaComponent m = (MediaComponent)target;

            //display script field
            var script = serializedObject.FindProperty("m_Script");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true);
            EditorGUI.EndDisabledGroup();
            
            
            //TODO display video player field in a way that suggests it is optional - a mesh renderer is used for image media instead
            m.VideoPlayer = (UnityEngine.Video.VideoPlayer)EditorGUILayout.ObjectField("Video Player", m.VideoPlayer, typeof(UnityEngine.Video.VideoPlayer), true);
            
            //media source / media id
            EditorGUILayout.LabelField("Media Name",m.MediaName);
            EditorGUILayout.LabelField("Media Id",m.MediaSource);

            if (string.IsNullOrEmpty(m.MediaSource))
            {
                setupFoldout = true;
            }

            setupFoldout = EditorGUILayout.Foldout(setupFoldout, "Setup");
            if (setupFoldout)
            {
                EditorGUI.indentLevel++;

                //drop down button
                EditorGUILayout.BeginHorizontal();
                string[] displayOptions = new string[EditorCore.MediaSources.Length];

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

                EditorGUI.BeginDisabledGroup(EditorCore.MediaSources.Length == 0);
                _choiceIndex = EditorGUILayout.Popup("Select Media Source", _choiceIndex, displayOptions);
                EditorGUI.EndDisabledGroup();

                //save button
                if (EditorCore.MediaSources.Length > 0)
                {
                    if (m.MediaSource != EditorCore.MediaSources[_choiceIndex].uploadId)
                    {
                        GUI.color = Color.green;
                    }

                    if (GUILayout.Button("Save", GUILayout.Width(40)))
                    {
                        m.MediaSource = EditorCore.MediaSources[_choiceIndex].uploadId;
                        m.MediaName = EditorCore.MediaSources[_choiceIndex].name;
                    }

                    GUI.color = Color.white;
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Button("Save", GUILayout.Width(40));
                    EditorGUI.EndDisabledGroup();
                }
                
                //gui style that has less border so the refresh icon is more clear
                int border = 2;
                GUIStyle minimalPaddingButton = new GUIStyle("button");
                minimalPaddingButton.padding = new RectOffset(border, border, border, border);

                //refresh button
                if (GUILayout.Button(new GUIContent(EditorCore.RefreshIcon, "Refresh Media"),minimalPaddingButton, GUILayout.Width(19),
                        GUILayout.Height(19)))
                {
                    EditorCore.RefreshMediaSources();
                    hasRefreshedMedia = true;
                }

                EditorGUILayout.EndHorizontal();

                //description
                if (EditorCore.MediaSources.Length > 0)
                {
                    EditorGUILayout.LabelField("Description", EditorCore.MediaSources[_choiceIndex].description);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("Description", "");
                    EditorGUI.EndDisabledGroup();
                }

                if (hasRefreshedMedia && EditorCore.MediaSources.Length == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No media was found for this project. Have you uploaded media to the dashboard?",
                        MessageType.Warning);
                }

                EditorGUI.indentLevel--;
            }
            
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

            if (GUI.changed)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}