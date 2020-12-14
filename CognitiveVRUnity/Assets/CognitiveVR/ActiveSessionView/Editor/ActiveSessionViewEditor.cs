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
            var ret = asv.GetComponentInChildren<RenderEyetracking>();
            var src = asv.GetComponentInChildren<SensorRenderCamera>();
            var sc = asv.GetComponentInChildren<SensorCanvas>();

            if (IsSceneObject(asv.gameObject))
            {

                string tooltip = "VR Camera should be Main Camera";

#if CVR_FOVE
                tooltip = "VR Camera should be 'Fove Interface'"
#elif CVR_STEAMVR
                tooltip = "VR Camera should be 'Camera (eye)'";
#elif CVR_STEAMVR2
                tooltip = "VR Camera should be 'Camera'";
#endif
                if (asv.VRSceneCamera == null)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(tooltip, MessageType.Error);
                    if (GUILayout.Button("Fix", GUILayout.MaxWidth(40), GUILayout.Height(38)))
                    {
                        SetCameraTarget(asv);
                    }
                    GUILayout.EndHorizontal();
                }

            }

            asv.VRSceneCamera = (Camera)EditorGUILayout.ObjectField("VR Scene Camera", asv.VRSceneCamera, typeof(Camera), true);

            //fixations
            ret.lineWidth = EditorGUILayout.Slider("Saccade Width", ret.lineWidth, 0.001f, 0.1f);
            ret.FixationMaterial.color = EditorGUILayout.ColorField("Fixation Colour", ret.FixationMaterial.color);
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
            asv.RenderEyetracking = (RenderEyetracking)EditorGUILayout.ObjectField("Render EyeTracking Camera", asv.RenderEyetracking, typeof(RenderEyetracking), true);
            asv.WarningText = (Text)EditorGUILayout.ObjectField("Warning Text", asv.WarningText, typeof(Text), true);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(ret);
                EditorUtility.SetDirty(sc);
                EditorUtility.SetDirty(asv);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(asv.gameObject.scene);
            }
        }

        static bool IsSceneObject(GameObject go)
        {
            return AssetDatabase.GetAssetPath(go) == string.Empty ? true : false;
        }

        public static void SetCameraTarget(ActiveSessionView activeSessionView)
        {
            if (activeSessionView == null) { return; }
#if CVR_TOBIIVR
            activeSessionView.VRSceneCamera = Camera.main;
#elif CVR_FOVE
            var fove = FindObjectOfType<Fove.Unity.FoveInterface>();
            if (fove != null)
            {
                activeSessionView.VRSceneCamera = fove.GetComponent<Camera>();
            }
#elif CVR_STEAMVR
            var cam = FindObjectOfType<SteamVR_Camera>();
            if (cam != null)
            {
                activeSessionView.VRSceneCamera = cam.GetComponent<Camera>();
            }
            else
            {
                Debug.LogError("Couldn't find Camera (eye)!");
            }
#elif CVR_STEAMVR2
            
            var playarea = FindObjectOfType<Valve.VR.SteamVR_PlayArea>();
            if (playarea != null)
            {
                var cam = playarea.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    activeSessionView.VRSceneCamera = cam;
                }
                else
                {
                    Debug.LogError("Couldn't find Camera!");
                }
            }
            else
            {
                Debug.LogError("Couldn't find SteamVR Play Area!");
            }
#else
            activeSessionView.VRSceneCamera = Camera.main;
#endif
        }
    }
}