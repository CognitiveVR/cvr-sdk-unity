using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    [InitializeOnLoad]
    public static class SetupNotificationInitializer
    {
        private const string PENDING_NOTIFICATION_KEY = "Cognitive3D_PendingNotification";

        static SetupNotificationInitializer()
        {
            // Wait a moment after Unity loads/recompiles before showing notification
            EditorApplication.delayCall += () =>
            {
                CheckAndShowNotification();
            };
        }

        private static void CheckAndShowNotification()
        {
            // Don't show during play mode or compilation
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                return;

            // Check if there's a pending notification (set before compilation)
            bool pendingNotification = EditorPrefs.GetBool(PENDING_NOTIFICATION_KEY, false);

            if (pendingNotification)
            {
                // Clear the pending flag
                EditorPrefs.DeleteKey(PENDING_NOTIFICATION_KEY);

                // Show the notification
                PostSetupDialog.MarkSetupComplete();
            }
        }
    }

    public class PostSetupDialog : EditorWindow
    {
        private const string DO_NOT_SHOW_AGAIN_KEY = "Cognitive3D_DoNotShowSetupNotification";

        private static PostSetupDialog window;
        private bool doNotShowAgain;

        public static void MarkSetupComplete()
        {
            // Check if user has disabled the notification
            bool doNotShowAgain = EditorPrefs.GetBool(DO_NOT_SHOW_AGAIN_KEY, false);
            if (!doNotShowAgain)
            {
                ShowNotification();
            }
        }

        public static void ShowNotification()
        {
            if (window != null)
            {
                window.Close();
            }

            window = GetWindow<PostSetupDialog>(true, "Cognitive3D Setup Complete", true);
            window.minSize = new Vector2(450, 350);
            window.maxSize = new Vector2(450, 350);

            // Center the window
            var main = EditorGUIUtility.GetMainWindowPosition();
            var pos = window.position;
            float centerX = main.x + (main.width - pos.width) * 0.5f;
            float centerY = main.y + (main.height - pos.height) * 0.5f;
            window.position = new Rect(centerX, centerY, pos.width, pos.height);

            window.ShowUtility();
        }

        private void OnDestroy()
        {
            // Called when window is closed (including X button)
            if (doNotShowAgain)
            {
                EditorPrefs.SetBool(DO_NOT_SHOW_AGAIN_KEY, true);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(15);
            // Logo at the top
            if (EditorCore.LogoCheckmark != null)
            {
                float logoScale = 2f; // Adjust this to change logo size
                float logoWidth = EditorCore.LogoCheckmark.width / logoScale;
                float logoHeight = EditorCore.LogoCheckmark.height / logoScale;
                float logoX = (position.width - logoWidth) / 2f;

                Rect logoRect = GUILayoutUtility.GetRect(position.width, logoHeight);
                GUI.DrawTexture(new Rect(logoX, logoRect.y, logoWidth, logoHeight), EditorCore.LogoCheckmark, ScaleMode.ScaleToFit);
            }
            GUILayout.Space(10);

            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("SDK Setup Complete!", headerStyle);

            GUILayout.Space(5);

            // Message
            GUIStyle messageStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            EditorGUILayout.LabelField(
                "Your project is set up. Here are some optional next steps:",
                messageStyle
            );

            GUILayout.Space(10);

            // Option 1: Scene Manager
            DrawOptionBox(
                EditorCore.SceneGeometryIcon,
                "Upload Scene Geometry",
                "Visualize user sessions in your exact 3D environment on the dashboard.",
                "Open Scene Manager",
                () => {
                    SceneManagerWindow.Init();
                    Close();
                }
            );

            GUILayout.Space(10);

            // Option 2: Feature Builder
            DrawOptionBox(
                EditorCore.ExploreFeaturesIcon,
                "Explore Features",
                "Check out advanced tracking features, dynamic objects, and more.",
                "Open Feature Builder",
                () => {
                    FeaturesWindow.Init();
                    Close();
                }
            );

            GUILayout.Space(5);

            // Don't show again toggle
            GUILayout.BeginHorizontal();
            doNotShowAgain = EditorGUILayout.ToggleLeft("Skip setup tips in the future", doNotShowAgain);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawOptionBox(Texture2D icon, string title, string description, string buttonText, System.Action onButtonClick)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);

            // Title with icon
            GUILayout.BeginHorizontal();

            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(25), GUILayout.Height(25));
            }

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                padding = new RectOffset(0, 0, 5, 0)
            };
            GUILayout.Label(title, titleStyle);

            GUILayout.EndHorizontal();

            // Description
            GUIStyle descStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 10,
                padding = new RectOffset(0, 0, 3, 3)
            };
            EditorGUILayout.LabelField(description, descStyle);

            GUILayout.Space(3);

            if (GUILayout.Button(buttonText, GUILayout.Height(25)))
            {
                onButtonClick?.Invoke();
            }

            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Clears the "do not show again" preference so the setup notification will appear again.
        /// Intended for testing only — do not call in normal workflows.
        /// </summary>
        internal static void ResetForTesting()
        {
            EditorPrefs.DeleteKey(DO_NOT_SHOW_AGAIN_KEY);
        }
    }
}
