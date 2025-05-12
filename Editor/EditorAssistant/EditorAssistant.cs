using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cognitive3D
{
    public class EditorAssistant : EditorWindow
    {
        private string currentInput = "";
        private Vector2 scrollPos;
        private bool scrollToBottom = false;
        private static List<(string sender, string content)> messages = new List<(string, string)>();

        [MenuItem("Cognitive3D/Chat")]
        public static void ShowWindow()
        {
            EditorAssistant window = GetWindow<EditorAssistant>("Cognitive3D Chat");
            window.minSize = new Vector2(500, 550);
            window.Show();

            var firstMsg = ("<b>C3D AI:</b>", "Hi there! I'm C3D AI, here to help you with Cognitive3D setup and best practices. This chat box is currently experimental, so results may vary. For the best experience, please be as specific as possible with your questions. Let’s build something awesome together!");
            messages.Add(firstMsg);
        }

        async void SendMessage()
        {
            string input = currentInput;
            if (string.IsNullOrWhiteSpace(input)) return;

            messages.Add(("<b>User:</b>", input));
            currentInput = "";
            scrollToBottom = true;
            Repaint();

            // Add typing indicator
            var typingMsg = ("<b>C3D AI:</b>", "<i>Typing...</i>");
            messages.Add(typingMsg);
            int typingIndex = messages.Count - 1;
            Repaint();

            string response = await ChatGPTClient.SendMessageToChatGPT(input);

            if (!string.IsNullOrEmpty(response))
            {
                messages[typingIndex] = ("<b>C3D AI:</b>", response); // Replace typing message
                scrollToBottom = true;
                Repaint();
            }
            else
            {
                messages[typingIndex] = ("<b>C3D AI:</b>", "<i>Something went wrong.</i>");
            }
        }

        void ClearMessages()
        {
            ChatGPTClient.Clear();
            messages.Clear();
        }

        private Vector2 inputScrollPos;

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(EditorCore.LogoTexture, GUILayout.Width(200), GUILayout.Height(60)); // Adjust size as needed
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            GUIStyle userStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            userStyle.richText = true;

            GUIStyle aiStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            aiStyle.richText = true;
            aiStyle.normal.textColor = new Color(0.3f, 0.6f, 1f); // Light blue

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
            foreach (var msg in messages)
            {
                var style = msg.sender.Contains("User") ? userStyle : aiStyle;
                GUILayout.Label($"{msg.sender}\n{msg.content}", style);
            }
            GUILayout.EndScrollView();

            if (scrollToBottom)
            {
                scrollPos.y = float.MaxValue;
                scrollToBottom = false;
            }

            GUILayout.Space(10);
            // GUI.SetNextControlName("UserInputField");
            inputScrollPos = EditorGUILayout.BeginScrollView(inputScrollPos, GUILayout.Height(100));
            currentInput = GUILayout.TextArea(currentInput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            // EditorGUI.FocusTextInControl("UserInputField");

            if (!string.IsNullOrWhiteSpace(currentInput) && Event.current.keyCode == KeyCode.Return)
            {
                // Send message
                SendMessage();
                // Event.current.Use(); // Consume the event so Unity doesn't add newline
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send"))
            {
                SendMessage();
            }
            if (GUILayout.Button("Clear"))
            {
                ClearMessages();
            }
            GUILayout.EndHorizontal();
        }
    }
}