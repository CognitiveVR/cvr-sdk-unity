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
        enum FeatureVisibility
        {
            DetailViewAndFullscreen,
            DetailView,
            Fullscreen,
            Off
        }

        UnityEngine.EventSystems.EventSystem eventSystem;
        bool foldout;

        public override void OnInspectorGUI()
        {
            var script = serializedObject.FindProperty("m_Script");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            //base.OnInspectorGUI();
            ActiveSessionView asv = target as ActiveSessionView;
            var ret = asv.GetComponentInChildren<RenderEyetracking>();
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
                if (eventSystem == null)
                    eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (eventSystem == null)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("Cannot find Event System", MessageType.Error);
                    if (GUILayout.Button("Fix", GUILayout.MaxWidth(40), GUILayout.Height(38)))
                    {
                        GameObject go = new GameObject("Event System");
                        eventSystem = go.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }
                    GUILayout.EndHorizontal();
                }

            }

            asv.VRSceneCamera = (Camera)EditorGUILayout.ObjectField("HMD Camera", asv.VRSceneCamera, typeof(Camera), true);

            #region get feature visibility

            FeatureVisibility reticleVisibility = FeatureVisibility.Off;
            if (asv.FullscreenDisplay.showReticle && ret.showReticle)
            {
                reticleVisibility = FeatureVisibility.DetailViewAndFullscreen;
            }
            else if (!asv.FullscreenDisplay.showReticle && ret.showReticle)
            {
                reticleVisibility = FeatureVisibility.DetailView;
            }
            else if (asv.FullscreenDisplay.showReticle && !ret.showReticle)
            {
                reticleVisibility = FeatureVisibility.Fullscreen;
            }
            else if (!asv.FullscreenDisplay.showReticle && !ret.showReticle)
            {
                reticleVisibility = FeatureVisibility.Off;
            }

            FeatureVisibility fixationVisibility = FeatureVisibility.Off;
            if (asv.FullscreenDisplay.showFixations && ret.shouldDisplayFixations)
            {
                fixationVisibility = FeatureVisibility.DetailViewAndFullscreen;
            }
            else if (!asv.FullscreenDisplay.showFixations && ret.shouldDisplayFixations)
            {
                fixationVisibility = FeatureVisibility.DetailView;
            }
            else if (asv.FullscreenDisplay.showFixations && !ret.shouldDisplayFixations)
            {
                fixationVisibility = FeatureVisibility.Fullscreen;
            }
            else if (!asv.FullscreenDisplay.showFixations && !ret.shouldDisplayFixations)
            {
                fixationVisibility = FeatureVisibility.Off;
            }

            FeatureVisibility saccadeVisibility = FeatureVisibility.Off;
            if (asv.FullscreenDisplay.showSaccades && ret.shouldDisplaySaccades)
            {
                saccadeVisibility = FeatureVisibility.DetailViewAndFullscreen;
            }
            else if (!asv.FullscreenDisplay.showSaccades && ret.shouldDisplaySaccades)
            {
                saccadeVisibility = FeatureVisibility.DetailView;
            }
            else if (asv.FullscreenDisplay.showSaccades && !ret.shouldDisplaySaccades)
            {
                saccadeVisibility = FeatureVisibility.Fullscreen;
            }
            else if (!asv.FullscreenDisplay.showSaccades && !ret.shouldDisplaySaccades)
            {
                saccadeVisibility = FeatureVisibility.Off;
            }

            #endregion


            //reticle
            EditorGUILayout.LabelField("Reticle", EditorStyles.boldLabel);
            reticleVisibility = (FeatureVisibility)EditorGUILayout.EnumPopup("Show Reticle", reticleVisibility);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Reticle Texture");
            asv.FullscreenDisplay.ReticleTexture = (Texture)EditorGUILayout.ObjectField(asv.FullscreenDisplay.ReticleTexture, typeof(Texture), false);
            GUILayout.EndHorizontal();
            asv.FullscreenDisplay.ReticleSize = EditorGUILayout.Slider("Reticle Size (pixels)", asv.FullscreenDisplay.ReticleSize, 10, 120);
            asv.FullscreenDisplay.ReticleColor = EditorGUILayout.ColorField("Reticle Color", asv.FullscreenDisplay.ReticleColor);
            ret.ReticleColor = asv.FullscreenDisplay.ReticleColor;
            ret.ReticleSize = asv.FullscreenDisplay.ReticleSize;
            ret.ReticleTexture = asv.FullscreenDisplay.ReticleTexture;


            //fixations
            EditorGUILayout.LabelField("Fixations", EditorStyles.boldLabel);
            fixationVisibility = (FeatureVisibility)EditorGUILayout.EnumPopup("Show Fixations", fixationVisibility);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fixation Texture");
            asv.FullscreenDisplay.fixationTexture = (Texture)EditorGUILayout.ObjectField(asv.FullscreenDisplay.fixationTexture, typeof(Texture), false);
            GUILayout.EndHorizontal();
            asv.FullscreenDisplay.fixationSize = EditorGUILayout.Slider("Fixation Size (pixels)", asv.FullscreenDisplay.fixationSize, 10, 120);
            asv.FullscreenDisplay.FixationColor = EditorGUILayout.ColorField("Fixation Color", asv.FullscreenDisplay.FixationColor);
            asv.FullscreenDisplay.NumberOfFixationsToDisplay = EditorGUILayout.IntSlider("Number of Fixations to Display", asv.FullscreenDisplay.NumberOfFixationsToDisplay, 0, 64);
            ret.FixationColor = asv.FullscreenDisplay.FixationColor;
            ret.NumberOfFixationsToDisplay = asv.FullscreenDisplay.NumberOfFixationsToDisplay;
            //TEST number of fixations to display fullscreen
            //TODO number of fixations to display render view


            //saccades
            EditorGUILayout.LabelField("Saccades", EditorStyles.boldLabel);
            saccadeVisibility = (FeatureVisibility)EditorGUILayout.EnumPopup("Show Saccades", saccadeVisibility);
            if (saccadeVisibility == FeatureVisibility.DetailViewAndFullscreen || saccadeVisibility == FeatureVisibility.Fullscreen)
            {
                EditorGUILayout.HelpBox("Saccades not currently supported on Fullscreen view", MessageType.Warning);
            }
            asv.FullscreenDisplay.SaccadeColor = EditorGUILayout.ColorField("Saccade Color", asv.FullscreenDisplay.SaccadeColor);
            asv.FullscreenDisplay.SaccadeWidth = EditorGUILayout.Slider("Saccade Width", asv.FullscreenDisplay.SaccadeWidth, 0.001f, 0.1f);
            asv.FullscreenDisplay.SaccadeTimespan = EditorGUILayout.Slider("Saccade Recent Time (seconds)", asv.FullscreenDisplay.SaccadeTimespan, 0, 10);
            ret.SaccadesFromLastSeconds = asv.FullscreenDisplay.SaccadeTimespan;
            ret.SaccadeColor = asv.FullscreenDisplay.SaccadeColor;
            ret.SaccadeWidth = asv.FullscreenDisplay.SaccadeWidth;
            //TODO saccade clip to time
            //TODO saccades in fullscreen view


            //sensors
            EditorGUILayout.LabelField("Sensors", EditorStyles.boldLabel);
            sc.LineWidth = EditorGUILayout.Slider("Sensor Line Width", sc.LineWidth, 0.001f, 0.03f);
            sc.MaxSensorTimeSpan = EditorGUILayout.Slider("Sensor Timespan", sc.MaxSensorTimeSpan, 10, 120);



            //internal
            foldout = EditorGUILayout.Foldout(foldout, "Internal");
            if (foldout)
            {
                asv.FullscreenDisplay = (FullscreenDisplay)EditorGUILayout.ObjectField("Fullscreen Display", asv.FullscreenDisplay, typeof(FullscreenDisplay), true);
                asv.MainCameraRenderImage = (RawImage)EditorGUILayout.ObjectField("Main Camera Render Image", asv.MainCameraRenderImage, typeof(RawImage), true);
                asv.RenderEyetracking = (RenderEyetracking)EditorGUILayout.ObjectField("Render EyeTracking Camera", asv.RenderEyetracking, typeof(RenderEyetracking), true);
                asv.WarningText = (Text)EditorGUILayout.ObjectField("Warning Text", asv.WarningText, typeof(Text), true);
            }

            if (GUI.changed)
            {
                sc.SetTimespan(sc.MaxSensorTimeSpan);

                #region set feature visibility

                ret.showReticle = reticleVisibility == FeatureVisibility.DetailView || reticleVisibility == FeatureVisibility.DetailViewAndFullscreen;
                asv.FullscreenDisplay.showReticle = reticleVisibility == FeatureVisibility.Fullscreen || reticleVisibility == FeatureVisibility.DetailViewAndFullscreen;

                ret.shouldDisplayFixations = fixationVisibility == FeatureVisibility.DetailView || fixationVisibility == FeatureVisibility.DetailViewAndFullscreen;
                asv.FullscreenDisplay.showFixations = fixationVisibility == FeatureVisibility.Fullscreen || fixationVisibility == FeatureVisibility.DetailViewAndFullscreen;

                ret.shouldDisplaySaccades = saccadeVisibility == FeatureVisibility.DetailView || saccadeVisibility == FeatureVisibility.DetailViewAndFullscreen;
                asv.FullscreenDisplay.showSaccades = saccadeVisibility == FeatureVisibility.Fullscreen || saccadeVisibility == FeatureVisibility.DetailViewAndFullscreen;

                #endregion

                EditorUtility.SetDirty(ret);
                EditorUtility.SetDirty(sc);
                EditorUtility.SetDirty(asv);
                EditorUtility.SetDirty(asv.FullscreenDisplay);
                if (!Application.isPlaying)
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
                activeSessionView.VRSceneCamera = Camera.main;
            }
#else
            activeSessionView.VRSceneCamera = Camera.main;
#endif
        }
    }
}