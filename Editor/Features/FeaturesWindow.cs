using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cognitive3D
{
    internal class FeaturesWindow : EditorWindow
    {
        private bool devKeyValid = true;

        private float slideProgress;
        private readonly float slideSpeed = 4f;
        private bool slidingForward;
        private bool slidingBackward;

        private Vector2 mainScroll;

        internal static void Init()
        {
            SegmentAnalytics.TrackEvent("FeatureBuilderWindow_Opened", "FeatureBuilderWindow", "new");
            FeaturesWindow window = (FeaturesWindow)EditorWindow.GetWindow(typeof(FeaturesWindow), true, "Feature Builder (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(600, 800);
            window.Show();
            FeatureLibrary.RefreshFeatureStates += window.Repaint;
        }

        private List<FeatureData> features;
        private int currentFeatureIndex = -1;

        private void OnEnable()
        {
            if (EditorCore.IsUsingOldManagerPrefab())
            {
                bool updatePrefab = EditorUtility.DisplayDialog(
                    "Update Prefab Source",
                    "It looks like you're still using the Manager prefab from the package resources.\n\n" +
                    "We highly recommend moving it to Assets/Resources for all tracked scenes. " +
                    "Otherwise, any added components may be overridden by future updates since the prefab in the package is immutable.\n\n" +
                    "Would you like to update the prefab source now?",
                    "Yes, Update",
                    "No, Keep As Is"
                );

                if (updatePrefab)
                {
                    EditorCore.PrefabUpdater();
                }
            }

            if (Cognitive3D_Preferences.FindCurrentScene() != null && !EditorCore.IsManagerPrefabInScene())
            {
                bool addManager = EditorUtility.DisplayDialog(
                    "Cognitive3D Manager Not Found",
                    "This scene does not currently contain a Cognitive3D Manager.\n\n" +
                    "Would you like to add a Cognitive3D Manager to this scene?",
                    "Yes, Add Manager",
                    "No, Skip"
                );

                if (addManager)
                {
                    GameObject managerPrefab = EditorCore.GetCognitive3DManagerPrefab();
                    if (managerPrefab != null)
                    {
                        PrefabUtility.InstantiatePrefab(managerPrefab);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
                }
            }

            features = FeatureLibrary.CreateFeatures((index) =>
            {
                currentFeatureIndex = index;
                slidingForward = true;
            });

            EditorCore.RefreshMediaSources();
        }

        private void OnDisable()
        {
            FeatureLibrary.RefreshFeatureStates -= Repaint;
        }

        private void OnGUI()
        {
            // Slide transition handler
            if (slidingForward || slidingBackward)
            {
                slideProgress += Time.deltaTime * slideSpeed * (slidingForward ? 1 : -1);
                slideProgress = Mathf.Clamp01(slideProgress);

                float epsilon = 0.0001f;

                if (Mathf.Abs(slideProgress - 1f) < epsilon)
                {
                    slidingForward = false;
                }
                else if (Mathf.Abs(slideProgress - 0f) < epsilon)
                {
                    slidingBackward = false;
                }

                Repaint();
            }

            float width = position.width;

            Rect mainRect = new Rect(-width * slideProgress, 0, width, position.height);
            Rect detailRect = new Rect(width - width * slideProgress, 0, width, position.height);

            GUILayout.BeginArea(mainRect);
            mainScroll = GUILayout.BeginScrollView(mainScroll);
            DrawMainPage();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(detailRect);
            DrawDetailPage();
            GUILayout.EndArea();
        }

#region Main Page
        private void DrawMainPage()
        {
            // Header background and logo
            if (EditorCore.LogoTexture != null)
            {
                float bgHeight = 100f;

                Rect bgRect = new Rect(0, 0, position.width, bgHeight);
                GUI.DrawTexture(bgRect, EditorCore.BackgroundTexture, ScaleMode.ScaleAndCrop);

                float logoWidth = EditorCore.LogoTexture.width / 3f;
                float logoHeight = EditorCore.LogoTexture.height / 3f;
                float logoX = (position.width - logoWidth) / 2f;
                float logoY = (bgHeight - logoHeight) / 2f;

                GUI.DrawTexture(new Rect(logoX, logoY, logoWidth, logoHeight), EditorCore.LogoTexture, ScaleMode.ScaleToFit);

                GUILayout.Space(bgHeight);
            }

            using (new EditorGUILayout.VerticalScope(EditorCore.styles.ContextPadding))
            {
                GUILayout.Space(5); // spacing between logo and text
                GUILayout.Label("Welcome to the Feature Builder", EditorCore.styles.FeatureTitle);
                GUILayout.Label(
                    "Explore the features of our platform. Each feature unlocks powerful capabilities you can use in your experience, from analytics to live control and more.",
                    EditorStyles.wordWrappedLabel
                );
            }

            GUILayout.Space(10);

            if (!devKeyValid)
            {
                EditorGUILayout.HelpBox(
                    "Developer Key is either missing or invalid. Please set a valid key in the Project Setup window.",
                    MessageType.Error
                );
            }

            foreach (var feature in features)
            {
                EditorGUI.BeginDisabledGroup(!feature.isEnabled);
                DrawFeatureButton(feature);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawFeatureButton(FeatureData featureData)
        {
            if (featureData == null || featureData.Icon == null) return;

            GUILayout.BeginHorizontal();

            // Reserve the button area
            Rect buttonRect = GUILayoutUtility.GetRect(
                new GUIContent(featureData.Title, featureData.Icon),
                EditorCore.styles.FeatureButton,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(100)
            );

            // Define sub-rects
            Rect iconRect = new Rect(buttonRect.x, buttonRect.y + 1, 98, 98);
            Rect titleRect = new Rect(iconRect.xMax + 15, buttonRect.y + 30, buttonRect.width - 180, 20); // Title area
            Rect descriptionRect = new Rect(iconRect.xMax + 15, titleRect.yMax, buttonRect.width - 180, 25); // Description below title

            // Draw background of main button
            GUI.Box(buttonRect, GUIContent.none, EditorCore.styles.FeatureButton);

            // Draw icon and text
            GUI.DrawTexture(iconRect, featureData.Icon);
            GUI.Label(titleRect, featureData.Title, EditorCore.styles.FeatureButtonTitle);
            GUI.Label(descriptionRect, featureData.Description, EditorCore.styles.FeatureButtonDescription);

            // Draw action buttons (Apply, Upload, LinkTo, etc.)
            if (featureData.Actions != null && featureData.Actions.Count > 0)
            {
                float buttonWidth = 40;
                float spacing = 10;
                float yOffset = 30;

                for (int i = 0; i < featureData.Actions.Count; i++)
                {
                    var action = featureData.Actions[i];
                    float x = buttonRect.xMax - ((featureData.Actions.Count - i) * (buttonWidth + spacing));
                    Rect actionRect = new Rect(x, buttonRect.y + yOffset, buttonWidth, buttonRect.height - 60);

                    // Draw the button
                    if (GUI.Button(actionRect, new GUIContent(EditorCore.ExternalIcon, action.Tooltip), EditorCore.styles.FeatureSmallButton))
                    {
                        action.OnClick?.Invoke();
                        Event.current.Use();
                    }
                }
            }

            // Handle main button click (make sure it's not overlapping an action button)
            if (buttonRect.Contains(Event.current.mousePosition) &&
                Event.current.type == EventType.MouseDown &&
                Event.current.button == 0)
            {
                // Don't fire if clicking any action button area
                bool clickedAction = false;

                if (featureData.Actions != null)
                {
                    float buttonWidth = 40;
                    float spacing = 5;
                    float yOffset = 30;

                    for (int i = 0; i < featureData.Actions.Count; i++)
                    {
                        float x = buttonRect.xMax - ((featureData.Actions.Count - i) * (buttonWidth + spacing));
                        Rect actionRect = new Rect(x, buttonRect.y + yOffset, buttonWidth, buttonRect.height - 60);

                        if (actionRect.Contains(Event.current.mousePosition))
                        {
                            clickedAction = true;
                            break;
                        }
                    }
                }

                if (!clickedAction)
                {
                    featureData.OnClick?.Invoke();
                    Event.current.Use();
                }
            }

            GUILayout.EndHorizontal();
        }
#endregion

#region Detail Page
        private void DrawDetailPage()
        {
            if (currentFeatureIndex < 0 || currentFeatureIndex >= features.Count)
            {
                GUILayout.Label("Invalid feature selected.");
                return;
            }

            var feature = features[currentFeatureIndex];

            // Custom GUI
            GUILayout.BeginVertical(EditorCore.styles.DetailContainer);
            feature.DetailGUI?.OnGUI();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Back", GUILayout.Height(40)))
            {
                slidingBackward = true;
            }
        }
#endregion
    }
}
