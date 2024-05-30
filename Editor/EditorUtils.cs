using UnityEngine;
using UnityEditor;
using UnityEngine.XR;
using System.Threading.Tasks;

namespace Cognitive3D
{
    public class EditorUtils : EditorWindow
    {
        /// <summary>
        /// Interval between user activity checks
        /// </summary>
        // To ensure accurate user presence detection upon entering play mode
        // User presence returns false within 1 second of entering play mode
        private const float INTERVAL_IN_SECONDS = 2;

        /// <summary>
        /// Max wait time for user inactivity before displaying popup
        /// </summary>
        private const float MAX_USER_INACTIVITY_IN_SECONDS = 900;

        /// <summary>
        /// Max time to wait for user response
        /// </summary>
        private const float WAIT_TIME_USER_RESPONSE_SECONDS = 300;

        private const string LOG_TAG = "[COGNITIVE3D] ";

        private static bool buttonPressed;
        private static EditorUtils window;
        private static bool pause;

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
            await Task.Delay((int)INTERVAL_IN_SECONDS * 1000);
            
            if (EditorApplication.isPlaying)
            {
                if (!IsUserPresent()) //player isn't present
                {
                    var waitForUserTaskTimeout = await WaitForUser(); //wait until they're back, or time has elapsed

                    if (!EditorApplication.isPlaying) { return; }

                    if (waitForUserTaskTimeout)
                    {
                        OpenWindow(LOG_TAG + "Session Reminder");
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
                if (pause)
                {
                    OpenWindow(LOG_TAG + "Session Paused");
                }
                return;
            }

            Util.logDebug("Session Continued");
            pause = false;
        }

        private static void OpenWindow(string windowTitle)
        {
            window = (EditorUtils)EditorWindow.GetWindow(typeof(EditorUtils), true, windowTitle);
            window.minSize = new Vector2(480, 210);
            window.maxSize = new Vector2(480, 210);

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
        // If there is no response, the window will be closed after 5 mins and will exit editor's play mode
        private static async Task WaitingForUserResponse()
        {
            float time = 0;

            // Waiting for 5 minutes
            while(time < WAIT_TIME_USER_RESPONSE_SECONDS && !buttonPressed)
            {
                await Task.Yield();
                time += Time.deltaTime;
            }

            // Verifying that no user response has been made
            // If the user has pressed a "continue" button and responded, play mode should not be exited.
            if (!buttonPressed)
            {
                CloseWindow();
                pause = true;
                EditorApplication.isPaused = true;
            }
        }

        // Waiting for user to become active
        // If user become active and put their headset back, wait time will stop
        private static async Task<bool> WaitForUser()
        {
            float time = 0;

            // Waiting for 15 minutes
            // If user is active again during wait time, breaks out of wait loop
            while(time < MAX_USER_INACTIVITY_IN_SECONDS)
            {
                if (IsUserPresent())
                {
                    return false;
                }
                await Task.Yield();
                time += Time.deltaTime;
            }
            return true;
        }

        private static void OnButtonPressed()
        {
            buttonPressed = true;
            CloseWindow();
        }

        static Vector3 lastPosition;

        /// <summary>
        /// Checks whether the headset is currently worn by the user in Editor
        /// </summary>
        private static bool IsUserPresent()
        {
            if (GameplayReferences.HMD == null)
            {
                return false;
            }

            if (Vector3.Distance(GameplayReferences.HMD.position, lastPosition) < 0.01) // Distance hasn't changed much since last check
            {
                return false;
            }
            else
            {
                lastPosition = GameplayReferences.HMD.position;
                return true;
            }
        }

        private void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 480, 210), EditorGUIUtility.whiteTexture);

            // Define the coordinates and dimensions of the cropping area
            Rect cropArea = new Rect(20, 40, 110, 100);

            // Begin a group with the cropping area
            GUI.BeginGroup(cropArea);
            // Draw the image within the cropping area
            GUI.Label(new Rect(-50, 0, 400, 80), EditorCore.LogoTexture, "image_centered");
            // End the group
            GUI.EndGroup();

            if (titleContent.text == LOG_TAG + "Session Reminder")
            {
                GUI.Label(new Rect(150, 30, 300, 800), "Do you want to continue recording this session?\n\n" + "Press 'Continue' to keep recording or 'Stop' to end the session.", "normallabel");
                if (GUI.Button(new Rect(230, 150, 100, 30), new GUIContent("Continue")))
                {
                    OnButtonPressed();
                }

                if (GUI.Button(new Rect(350, 150, 100, 30), new GUIContent("Stop")))
                {
                    OnButtonPressed();
                    EditorApplication.ExitPlaymode();
                }
            }
            else
            {
                GUI.Label(new Rect(150, 60, 300, 800), "Unity Editor session has been paused due to inactivity.", "normallabel");

                if (GUI.Button(new Rect(200, 150, 100, 30), new GUIContent("OK")))
                {
                    OnButtonPressed();
                }
            }
        }
    }
}
