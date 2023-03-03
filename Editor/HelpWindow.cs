using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;

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

            GUI.Label(new Rect(30, 45, 440, 440), "A <color=#8A9EB7FF>Dynamic Object </color> is an object that moves around during an experience which you wish to track.", "boldlabel");

            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

            GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);

            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsHightlight, "image_centered");

            GUI.color = Color.white;

            GUI.Label(new Rect(30, 350, 440, 440), "You can add or remove Dynamic Objects without uploading a new Scene Version.\n\nYou must attach a Dynamic Object Component onto each object you wish to track in your project. These objects must also have colliders attached so we can track user gaze.", "normallabel");
        }

        void SceneUpate()
        {
            GUI.Label(steptitlerect, "WHAT IS A SCENE?", "steptitle");

            GUI.Label(new Rect(30, 45, 440, 440), "A <color=#8A9EB7FF>Scene</color> is an approximation of your Unity scene and is uploaded to the Dashboard. It is all the non-moving and non-interactive things.", "boldlabel");


            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

            GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);
            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneHighlight, "image_centered");
            GUI.color = Color.white;

            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

            GUI.Label(new Rect(30, 350, 440, 440), "This will provide context to the data collected in your experience.\n\nIf you decide to change the scene in your Unity project (such as moving a wall), the data you collect may no longer represent your experience. You can upload a new Scene Version by running this setup again.", "normallabel");
        }

        void CustomEventUpdate()
        {

        }
        void ExitPollUpdate()
        {

        }
        void SensorsUpdate()
        {

        }
        void MediaUpdate()
        {

        }
        void ReadyRoomUpdate()
        {

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

    }
}