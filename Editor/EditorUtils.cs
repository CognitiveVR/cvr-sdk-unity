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

        public static void Init()
        {
            Cognitive3D_Manager.OnSessionBegin += CheckUserActivity;
            Cognitive3D_Manager.OnPreSessionEnd += CleanUp;

            EditorApplication.pauseStateChanged += OnPauseStateChanged;
        }

        private static void CleanUp()
        {
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;

            Cognitive3D_Manager.OnSessionBegin -= CheckUserActivity;
            Cognitive3D_Manager.OnPreSessionEnd -= CleanUp;
        }
        

        // 15 minute wait time starts when user becomes inactive (headset is unmounted)
        private static async void CheckUserActivity()
        {
            await Task.Delay((int)initialDelay * 1000);
            
            if (EditorApplication.isPlaying)
            {
                if (!IsUserPresent())
                {
                    await WaitForUser();

                    // Check if still in play or pause mode before initializing the window
                    if (EditorApplication.isPlaying && !IsUserPresent())
                    {
                        // EditorApplication.pauseStateChanged
                        OpenWindow("Session Reminder");
                        await WaitingForUserResponse();
                    }
                }

                CheckUserActivity();
            }       
        }

        /// <summary>
        /// Handles pause state after not receiving user response
        /// </summary>
        /// <param name="pauseState"></param>
        private static void OnPauseStateChanged(PauseState pauseState)
        {
            if (pauseState == PauseState.Paused)
            {
                Util.logDebug("Session Paused");
                OpenWindow("Session Paused");
            }
            else
            {
                Util.logDebug("Session Continued");
            }
        }

        private static void OpenWindow(string windowTitle)
        {
            window = (EditorUtils)EditorWindow.GetWindow(typeof(EditorUtils), true, windowTitle);
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);

            window.Show();

            buttonPressed = false;
        }

        private static void CloseWindow()
        {
            if (window != null)
            {
                window.Close();
            }
        }

        // Waiting for user respond
        // If there is no response, the window will be closed after 20 mins and will exit editor's play mode
        private static async Task WaitingForUserResponse()
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
                // EditorApplication.ExitPlaymode();
                EditorApplication.isPaused = true;
            }
        }

        // Waiting for user to become active
        // If user become active and put their headset back, wait time will stop
        private static async Task WaitForUser()
        {
            float time = 0;

            // Waiting for 15 minutes
            // If user is active again during wait time, breaks out of wait loop
            while(time < interval && !IsUserPresent())
            {
                await Task.Yield();
                time += Time.deltaTime;
            }
        }

        private static void OnButtonPressed()
        {
            buttonPressed = true;
            CloseWindow();
        }

        /// <summary>
        /// Checks whether the headset is currently worn by the user in Editor
        /// </summary>
        private static bool IsUserPresent()
        {
            bool isPresent;

#if C3D_OCULUS
            InputDevice currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out isPresent);
#else
            Vector3 velocity;
            InputDevice currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);

            currentHmd.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity);

            if (velocity != Vector3.zero)
            {
                isPresent = true;
            }
            else
            {
                isPresent = false;
            }
#endif

            return isPresent;
        }

        private void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 400, 200), EditorGUIUtility.whiteTexture);

            if (titleContent.text == "Session Reminder")
            {
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
            else
            {
                GUI.Label(new Rect(30, 20, 350, 300), "Editor (session) is paused due to inactivity!", "normallabel");

                if (GUI.Button(new Rect(165, 150, 80, 30), new GUIContent("OK")))
                {
                    OnButtonPressed();
                }
            }
        }
    }
}
