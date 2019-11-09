using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace CognitiveVR.ActiveSession
{
    [CustomEditor(typeof(ActiveSessionView))]
    public class ActiveSessionViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var script = serializedObject.FindProperty("m_Script");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            //base.OnInspectorGUI();
            ActiveSessionView asv = target as ActiveSessionView;
            var mc = asv.GetComponentInChildren<MetaCanvas>();
            var ret = asv.GetComponentInChildren<RenderEyetracking>();
            var follower = asv.GetComponentInChildren<FixationRenderCamera>();
            var src = asv.GetComponentInChildren<SensorRenderCamera>();
            var sc = asv.GetComponentInChildren<SensorCanvas>();


#if CVR_TOBIIVR
            if (asv.VRSceneCamera == null)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("VR Camera should be 'Camera (eye)'", MessageType.Error);
                if (GUILayout.Button("Fix", GUILayout.MaxWidth(40),GUILayout.Height(38)))
                {
                    var eye = GameObject.Find("Camera (eye)");
                    if (eye != null)
                        asv.VRSceneCamera = eye.GetComponent<Camera>();
                }
                GUILayout.EndHorizontal();
            }
#elif CVR_FOVE
            if (asv.VRSceneCamera == null)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("VR Camera should be 'Fove Interface'", MessageType.Error);
                if (GUILayout.Button("Fix", GUILayout.MaxWidth(40),GUILayout.Height(38)))
                {
                    var fove = FindObjectOfType<FoveInterfaceBase>();
                    if (fove != null)
                    {
                        asv.VRSceneCamera = fove.GetComponent<Camera>();
                    }
                }
                GUILayout.EndHorizontal();
            }
#else //pupil, varjo, others
            if (asv.VRSceneCamera == null)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("VR Camera should be 'MainCamera'", MessageType.Error);
                if (GUILayout.Button("Fix", GUILayout.MaxWidth(40), GUILayout.Height(38)))
                {
                    asv.VRSceneCamera = Camera.main;
                }
                GUILayout.EndHorizontal();
            }
#endif

            asv.VRSceneCamera = (Camera)EditorGUILayout.ObjectField("VR Scene Camera", asv.VRSceneCamera, typeof(Camera), true);

            //fixations
            ret.Width = EditorGUILayout.Slider("Saccade Width", ret.Width, 0.001f, 0.1f);
            ret.FixationMaterial.color = EditorGUILayout.ColorField("Fixation Colour", ret.FixationMaterial.color);
            ret.GetComponent<Camera>().backgroundColor = ret.FixationMaterial.color;
            ret.FixationColor = ret.FixationMaterial.color;
            if (ret.FixationMaterial.HasProperty("_EmissionColor"))
            {
                ret.FixationMaterial.SetColor("_EmissionColor", ret.FixationColor / 2);
            }
            ret.FixationScale = EditorGUILayout.Slider("Fixation Display Size", ret.FixationScale, 0, 1);

            //sensors
            src.LineWidth = EditorGUILayout.Slider("Sensor Line Width", src.LineWidth, 0.001f, 0.01f);
            sc.MaxSensorTimeSpan = EditorGUILayout.Slider("Sensor Timespan", sc.MaxSensorTimeSpan, 10, 120);

            //sensor colors

            GUILayout.Space(20);
            asv.MainCameraRenderImage = (RawImage)EditorGUILayout.ObjectField("Main Camera Render Image", asv.MainCameraRenderImage, typeof(RawImage), true);
            asv.FixationRenderCamera = (FixationRenderCamera)EditorGUILayout.ObjectField("Fixation Render Camera", asv.FixationRenderCamera, typeof(FixationRenderCamera), true);
            asv.WarningText = (Text)EditorGUILayout.ObjectField("Warning Text", asv.WarningText, typeof(Text), true);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(follower);
                EditorUtility.SetDirty(ret);
                EditorUtility.SetDirty(sc);
                EditorUtility.SetDirty(asv);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(asv.gameObject.scene);
            }
        }
    }
}