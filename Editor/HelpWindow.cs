using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;
using UnityEditor.SceneManagement;

namespace Cognitive3D
{
    internal class HelpWindow : EditorWindow
    {
        Rect steptitlerect = new Rect(30, 0, 100, 440);
        internal static void Init()
        {
            HelpWindow window = (HelpWindow)EditorWindow.GetWindow(typeof(HelpWindow), true, "Help (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();
        }

        enum Page
        {
            Main,
            Scene,
            Dynamic,
            CustomEvent,
            ExitPoll,
            Sensors,
            Media,
            ReadyRoom
        };
        Page currentPage;

        private void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Equals) { currentPage++; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Minus) { currentPage--; }

            switch (currentPage)
            {
                case Page.Main:
                    MainUpdate();
                    break;
                case Page.Scene:
                    SceneUpate();
                    break;
                case Page.Dynamic:
                    DynamicUpdate();
                    break;
                case Page.CustomEvent:
                    CustomEventUpdate();
                    break;
                case Page.ExitPoll:
                    ExitPollUpdate();
                    break;
                case Page.Sensors:
                    SensorsUpdate();
                    break;
                case Page.Media:
                    MediaUpdate();
                    break;
                case Page.ReadyRoom:
                    ReadyRoomUpdate();
                    break;
                default:
                    break;
            }

            //GUI.Label(steptitlerect, "Help (Version " + Cognitive3D_Manager.SDK_VERSION + ")", "steptitle");

            DrawFooter();
            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        void MainUpdate()
        {
            //GUI.Label(new Rect(30, 45, 440, 440), "Welcome to the " + EditorCore.DisplayValue(DisplayKey.FullName) + " SDK Scene Setup.", "boldlabel");
            GUI.Label(new Rect(30, 45, 440, 440), "This page is a quick reference of common features in the SDK. See the Full documentation for more examples", "normallabel");

            //list of buttons
            //also checkboxes for 'has seen this content'. save to editor prefs?

            int leftEdge = 80;
            int width = 360;
            int heightOffset = 100;

            GUI.Label(new Rect(40, heightOffset, 40, 30), EditorCore.BlueCheckmark);
            if (GUI.Button(new Rect(leftEdge, 100, width, 30), "Scenes"))
            {
                currentPage = Page.Scene;
            }

            GUI.Label(new Rect(40, heightOffset+40, 40, 30), EditorCore.EmptyCheckmark);
            if (GUI.Button(new Rect(leftEdge, 140, width, 30), "Dynamic Objects"))
            {
                currentPage = Page.Dynamic;
            }

            GUI.Label(new Rect(40, heightOffset+80, 40, 30), EditorCore.EmptyCheckmark);
            if (GUI.Button(new Rect(leftEdge, 180, width, 30), "Custom Events"))
            {
                currentPage = Page.CustomEvent;
            }

            GUI.Label(new Rect(40, heightOffset+120, 40, 30), EditorCore.EmptyCheckmark);
            if (GUI.Button(new Rect(leftEdge, 220, width, 30), "ExitPoll Survey"))
            {
                currentPage = Page.ExitPoll;
            }

            GUI.Label(new Rect(40, heightOffset+160, 40, 30), EditorCore.EmptyCheckmark);
            if (GUI.Button(new Rect(leftEdge, 260, width, 30), "Sensors"))
            {
                currentPage = Page.Sensors;
            }

            GUI.Label(new Rect(40, heightOffset+200, 40, 30), EditorCore.BlueCheckmark);
            if (GUI.Button(new Rect(leftEdge, 300, width, 30), "Media"))
            {
                currentPage = Page.Media;
            }

            GUI.Label(new Rect(40, heightOffset+240, 40, 30), EditorCore.EmptyCheckmark);
            if (GUI.Button(new Rect(leftEdge, 340, width, 30), "Ready Room"))
            {
                currentPage = Page.ReadyRoom;
            }
            if (GUI.Button(new Rect(40, heightOffset+300, 400, 40), "Full Documentation"))
            {
                Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Documentation);
            }

        }

        void DynamicUpdate()
        {
            GUI.Label(steptitlerect, "WHAT IS A DYNAMIC OBJECT?", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "A <b>Dynamic Object </b> is an object that moves around during an experience which you wish to track.", "boldlabel");
            GUI.Box(new Rect(150, 90, 200, 200), EditorCore.SceneBackground, "image_centered");
            GUI.Box(new Rect(150, 90, 200, 200), EditorCore.ObjectsBackground, "image_centered");
            GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);
            GUI.Box(new Rect(150, 90, 200, 200), EditorCore.ObjectsHightlight, "image_centered");
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 280, 440, 440), "You can add or remove Dynamic Objects without uploading a new Scene Version.\n\nYou must attach a Dynamic Object Component onto each object you wish to track in your project. These objects must also have colliders attached so we can track user gaze.", "normallabel");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/dynamic-objects/");
            DrawDynamicManagerButton();
        }

        void SceneUpate()
        {
            GUI.Label(steptitlerect, "WHAT IS A SCENE?", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "A <b>Scene</b> is an approximation of your Unity scene and is uploaded to the Dashboard. It is all the non-moving and non-interactive things.", "boldlabel");
            GUI.Box(new Rect(150, 90, 200, 200), EditorCore.SceneBackground, "image_centered");
            GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);
            GUI.Box(new Rect(150, 90, 200, 200), EditorCore.SceneHighlight, "image_centered");
            GUI.color = Color.white;
            GUI.Box(new Rect(150, 90, 200, 200), EditorCore.ObjectsBackground, "image_centered");
            GUI.Label(new Rect(30, 270, 440, 440), "This will provide context to the data collected in your experience.\n\nIf you decide to change the scene in your Unity project (such as moving a wall), the data you collect may no longer represent your experience. You can upload a new Scene Version by running this setup again.", "normallabel");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/scenes/");
            DrawSceneWindowButton();
        }

        void CustomEventUpdate()
        {
            GUI.Label(steptitlerect, "CUSTOM EVENTS", "steptitle");
            GUI.Label(new Rect(25, 45, 450, 440), "A <b>Custom Event</b> is a way to highlight specific interactions and incidents during the session.", "boldlabel");
            GUI.Label(new Rect(25, 110, 450, 440), "You will be able to view the events in the session timeline or in real-time in the Scene Explorer.", "boldlabel");
            EditorGUI.DrawRect(new Rect(25, 180, 450, 80), Color.black);
            GUI.Label(new Rect(60, 210, 300, 440), "new CustomEvent(\"Event Name\").Send()", "code_snippet");
            //video link
            if (GUI.Button(new Rect(100, 300, 300, 100), "video"))
            {
                Application.OpenURL("https://vimeo.com/cognitive3d/videos");
            }
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/customevents/");
        }
        void ExitPollUpdate()
        {
            GUI.Label(steptitlerect, "EXIT POLL SURVEY", "steptitle");
            GUI.Label(new Rect(25, 45, 450, 440), "An <b>Exit Poll Survey</b> is a way to gather feedback from your users and aggregate results in the dashboard.", "boldlabel");
            GUI.Label(new Rect(25, 110, 450, 440), "You can create an exit poll in the dashboard and access it from the Unity Editor via a hook.", "boldlabel");
            GUI.Box(new Rect(122, 180, 256, 230), EditorCore.ExitPollExample, "image_centered"); // the numbers are quite strange because of the aspect ratio
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/exitpoll/");
        }
        void SensorsUpdate()
        {
            GUI.Label(steptitlerect, "SENSORS", "steptitle");
            GUI.Label(new Rect(25, 45, 450, 440), "A <b>Sensor</b> is a way to access and track a value or property throughout the session.", "boldlabel");
            GUI.Label(new Rect(25, 110, 450, 440), "You can send sensor values for values like FPS, heartrate, HMD Battery Level, and view it as a graph on the dashboards.", "boldlabel");
            EditorGUI.DrawRect(new Rect(25, 200, 450, 100), Color.black);
            GUI.Label(new Rect(60, 230, 230, 440), "float sensorData = Random.Range(1, 100f);\nCognitive3D.SensorRecorder\n\t.RecordDataPoint(\"SensorName\", sensorData);", "code_snippet");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/sensors/");
        }
        void MediaUpdate()
        {
            GUI.Label(steptitlerect, "SCENE MEDIA", "steptitle");
            GUI.Label(new Rect(25, 45, 450, 440), "<b>Scene Media</b> allows you detect and aggregate gaze data on media objects like images, videos, and 360 degree videos.", "boldlabel");
            GUI.Label(new Rect(25, 110, 450, 440), "\nYou can upload media files in the <b>Media Library</b> tab on the dashboard. You can then add media to your scene and associate them with files on the dashboard to record gaze.", "boldlabel");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/media/");
        }
        void ReadyRoomUpdate()
        {
            GUI.Label(steptitlerect, "READY ROOM", "steptitle");
            GUI.Label(new Rect(25, 45, 450, 440), "<b>Ready Room</b> provides a tutorial scene for users of an application to get familiar with the VR experiences. This is particularly useful if members of your target audience are new to VR or you want to ensure all equipment and software is configured correctly.", "boldlabel");
            GUI.Label(new Rect(25, 150, 450, 440), "\nYou can set up ready room by importing the \"Ready Room\" sample from the Cognitive3D Unity SDK package in Package Manager.", "boldlabel");
            DrawPackageManagerButton();
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/ready-room/");
        }

        void DrawFooter()
        {
            if (currentPage == Page.Main)
            {
                return;
            }

            GUI.color = EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            
            DrawBackButton();
        }

        void DrawBackButton()
        {
            bool buttonDisabled = false;
            string text = "Back";
            System.Action onclick = () => currentPage = Page.Main;
            Rect buttonrect = new Rect(10, 510, 80, 30);

            if (buttonDisabled)
            {
                GUI.Button(buttonrect, text, "button_disabledtext");
            }
            else
            {
                if (GUI.Button(buttonrect, text))
                {
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }

        void DrawSpecificDocsButton(string url)
        {
            bool buttonDisabled = false;
            string text = "Full Documentation";
            System.Action onclick = () => Application.OpenURL(url);
            Rect buttonrect = new Rect(100, 420, 300, 30);

            if (buttonDisabled)
            {
                GUI.Button(buttonrect, text, "button_disabledtext");
            }
            else
            {
                if (GUI.Button(buttonrect, text))
                {
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }

        void DrawDynamicManagerButton()
        {
            bool buttonDisabled = false;
            string text = "Dynamic Manager";
            System.Action onclick = () =>
            {
                DynamicObjectsWindow.Init();
                this.Close();
            };
            Rect buttonrect = new Rect(150, 460, 200, 30);
            if (buttonDisabled)
            {
                GUI.Button(buttonrect, text, "button_disabledtext");
            }
            else
            {
                if (GUI.Button(buttonrect, text))
                {
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }        
        void DrawSceneWindowButton()
        {
            bool buttonDisabled = false;
            string text = "Scene Setup";
            System.Action onclick = () =>
            {
                SceneSetupWindow.Init();
                this.Close();
            };
            Rect buttonrect = new Rect(150, 460, 200, 30);
            if (buttonDisabled)
            {
                GUI.Button(buttonrect, text, "button_disabledtext");
            }
            else
            {
                if (GUI.Button(buttonrect, text))
                {
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }

        void DrawPackageManagerButton()
        {
            bool buttonDisabled = false;
            string text = "Open Package Manager";
            System.Action onclick = () =>
            {
                UnityEditor.PackageManager.UI.Window.Open("com.cognitive3d.c3d-sdk");
            };
            Rect buttonrect = new Rect(150, 360, 200, 30);
            if (buttonDisabled)
            {
                GUI.Button(buttonrect, text, "button_disabledtext");
            }
            else
            {
                if (GUI.Button(buttonrect, text))
                {
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }
    }
}