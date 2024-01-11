using UnityEngine;
using UnityEditor;
using UnityEngine.XR;
using System.Threading.Tasks;


namespace Cognitive3D
{
    public class EditorUtils : EditorWindow
    {
        const float initialDelay = 2;
        const float interval = 900;
        const float waitTime = 1200;

        static bool buttonPressed;
        static EditorUtils window;
        

        // 15 minute wait time starts when user becomes inactive (headset is unmounted)
        public static async void CheckUserActivity()
        {
            await Task.Delay((int)initialDelay * 1000);
            
            if (EditorApplication.isPlaying)
            {
                if (!IsUserPresent())
                {
                    await Task.Delay((int)interval * 1000);

                    // Check if still in play mode before initializing the window
                    if (EditorApplication.isPlaying)
                    {
                        Init();
                    }

                    await WaitingForUserResponse();
                }

                CheckUserActivity();
            }
            
        }

        internal static void Init()
        {
            window = (EditorUtils)EditorWindow.GetWindow(typeof(EditorUtils), true, "Session Reminder");
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);

            window.Show();

            buttonPressed = false;
        }

        static void CloseWindow()
        {
            if (window != null)
            {
                window.Close();
            }
        }

        // Waiting for user respond
        // If there is no response, the window will be closed after 20 mins and will exit editor's play mode
        static async Task WaitingForUserResponse()
        {
            float time = 0;

            // Waiting for 20 minutes
            while(time < waitTime && !buttonPressed)
            {
                await Task.Yield();
                time += Time.deltaTime;
            }

            // Verifying that no user response has been made
            // If the user has pressed a "continue" button and responded, play mode should not be exited.
            if (!buttonPressed)
            {
                CloseWindow();
                EditorApplication.ExitPlaymode();
            }
        }

        static void OnButtonPressed()
        {
            buttonPressed = true;
            CloseWindow();
        }

        // Checks whether the headset is currently worn by the user
        public static bool IsUserPresent()
        {
            bool isPresent;

            InputDevice currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out isPresent);

            return isPresent;
        }

        void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 400, 200), EditorGUIUtility.whiteTexture);

            GUI.Label(new Rect(30, 20, 350, 300), "Do you want to continue recording this session?\n\n" + "Press 'Continue' to keep recording or 'Stop' to end the session.", "normallabel");

            if (GUI.Button(new Rect(200, 150, 80, 30), new GUIContent("Continue")))
            {
                OnButtonPressed();
            }

            if (GUI.Button(new Rect(300, 150, 80, 30), new GUIContent("Stop")))
            {
                OnButtonPressed();
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
