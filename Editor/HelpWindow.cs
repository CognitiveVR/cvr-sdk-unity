using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;
using UnityEditor.SceneManagement;

//in-editor quick reference of sdk features and code examples

namespace Cognitive3D
{
    internal class HelpWindow : EditorWindow
    {
        readonly Color CommentAreaDarkGrey = new Color(0.21f, 0.21f, 0.21f);
        readonly Rect steptitlerect = new Rect(30, 5, 100, 440);
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

        bool foundReadyRoomScene;
        bool searchedForReadyRoom;
        string readyRoomPath;

        private void OnFocus()
        {
            searchedForReadyRoom = false;
            foundReadyRoomScene = false;
            readyRoomPath = string.Empty;
            LoadPagesFromPrefs();
        }

        private void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

            DrawFooter();

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
                    throw new System.NotSupportedException();
            }

            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        void MainUpdate()
        {
            GUI.Label(new Rect(30, 30, 440, 440), "This page is a quick reference of common features in the SDK. See the Online Documentation for more examples", "normallabel");

            //list of buttons
            //also checkboxes for 'has developer seen this content'

            int startListHeight = 140;
            int checkmarkLeft = 110;
            int leftEdge = 150;
            int width = 200;

            GUI.Label(new Rect(checkmarkLeft, startListHeight, 40, 30), HasBeenViewed(Page.Scene) ? EditorCore.CircleCheckmark : EditorCore.CircleEmpty);
            if (GUI.Button(new Rect(leftEdge, startListHeight, width, 30), "Scenes", HasBeenViewed(Page.Scene) ? "button_purple" : "button"))
            {
                currentPage = Page.Scene;
                AddToSeenPages(currentPage);
            }

            GUI.Label(new Rect(checkmarkLeft, startListHeight + 40, 40, 30), HasBeenViewed(Page.Dynamic) ? EditorCore.CircleCheckmark : EditorCore.CircleEmpty);
            if (GUI.Button(new Rect(leftEdge, startListHeight+40, width, 30), "Dynamic Objects", HasBeenViewed(Page.Dynamic) ? "button_purple" : "button"))
            {
                currentPage = Page.Dynamic;
                AddToSeenPages(currentPage);
            }

            GUI.Label(new Rect(checkmarkLeft, startListHeight + 80, 40, 30), HasBeenViewed(Page.CustomEvent) ? EditorCore.CircleCheckmark : EditorCore.CircleEmpty);
            if (GUI.Button(new Rect(leftEdge, startListHeight+80, width, 30), "Custom Events", HasBeenViewed(Page.CustomEvent)?"button_purple":"button"))
            {
                currentPage = Page.CustomEvent;
                AddToSeenPages(currentPage);
            }

            GUI.Label(new Rect(checkmarkLeft, startListHeight + 120, 40, 30), HasBeenViewed(Page.ExitPoll) ? EditorCore.CircleCheckmark : EditorCore.CircleEmpty);
            if (GUI.Button(new Rect(leftEdge, startListHeight+120, width, 30), "ExitPoll Survey", HasBeenViewed(Page.ExitPoll) ? "button_purple" : "button"))
            {
                currentPage = Page.ExitPoll;
                AddToSeenPages(currentPage);
            }

            GUI.Label(new Rect(checkmarkLeft, startListHeight + 160, 40, 30), HasBeenViewed(Page.Sensors) ? EditorCore.CircleCheckmark : EditorCore.CircleEmpty);
            if (GUI.Button(new Rect(leftEdge, startListHeight+160, width, 30), "Sensors", HasBeenViewed(Page.Sensors) ? "button_purple" : "button"))
            {
                currentPage = Page.Sensors;
                AddToSeenPages(currentPage);
            }

            GUI.Label(new Rect(checkmarkLeft, startListHeight + 200, 40, 30), HasBeenViewed(Page.Media) ? EditorCore.CircleCheckmark : EditorCore.CircleEmpty);
            if (GUI.Button(new Rect(leftEdge, startListHeight+200, width, 30), "Media", HasBeenViewed(Page.Media) ? "button_purple" : "button"))
            {
                currentPage = Page.Media;
                AddToSeenPages(currentPage);
            }

            GUI.Label(new Rect(checkmarkLeft, startListHeight + 240, 40, 30), HasBeenViewed(Page.ReadyRoom)? EditorCore.CircleCheckmark: EditorCore.CircleEmpty);
            if (GUI.Button(new Rect(leftEdge, startListHeight+240, width, 30), "Ready Room", HasBeenViewed(Page.ReadyRoom) ? "button_purple" : "button"))
            {
                currentPage = Page.ReadyRoom;
                AddToSeenPages(currentPage);
            }
        }

        //uses a bitarray to lookup if a page has been seen. loads/saves to editorprefs as integer
        BitArray seenEditorPages = new BitArray(8);
        void AddToSeenPages(Page page)
        {
            seenEditorPages.Set((int)page, true);
            SavePagesToPrefs();
        }

        void SavePagesToPrefs()
        {
            int[] array = new int[1];
            seenEditorPages.CopyTo(array, 0);
            EditorPrefs.SetInt("c3d-help", array[0]);
        }

        void LoadPagesFromPrefs()
        {
            int temp;
            temp = EditorPrefs.GetInt("c3d-help");
            seenEditorPages = new BitArray(new [] { temp });
        }

        bool HasBeenViewed(Page page)
        {
            return seenEditorPages.Get((int)page);
        }

        void DynamicUpdate()
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "A <b>Dynamic Object </b> is a specific object in your experience which you wish to track.", "normallabel");
            GUI.Box(new Rect(150, 90, 200, 150), EditorCore.DynamicsFeature, "image_centered");
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 260, 440, 440), "You can add or remove Dynamic Objects without uploading a new Scene Version.\n\nYou must attach a Dynamic Object Component onto each object you wish to track in your project. Dynamic Objects can move, be spawned or destroyed.\n\nThese objects must also have colliders attached so we can track user gaze.", "normallabel");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/dynamic-objects/");
            DrawDynamicManagerButton();
        }

        void SceneUpate()
        {
            GUI.Label(steptitlerect, "SCENES", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "A <b>Scene</b> is an approximation of your Unity scene and is uploaded to the Dashboard. It is all the non-moving and non-interactive things.", "normallabel");
            GUI.Box(new Rect(150, 90, 200, 150), EditorCore.SceneFeature, "image_centered");
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 270, 440, 440), "This will provide context to the data collected in your experience.\n\nIf you decide to change the scene in your Unity project (such as moving a wall), the data you collect may no longer represent your experience. You can upload a new Scene Version by running this setup again.", "normallabel");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/scenes/");
            DrawSceneWindowButton();
        }

        void CustomEventUpdate()
        {
            GUI.Label(steptitlerect, "CUSTOM EVENTS", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "A <b>Custom Event</b> is a feature to highlight specific interactions and incidents during the session.", "normallabel");
            GUI.Label(new Rect(30, 110, 440, 440), "You are able to view these Custom Events in the session details page or real-time in Scene Explorer.", "normallabel");
            EditorGUI.DrawRect(new Rect(30, 180, 440, 80), CommentAreaDarkGrey);
            EditorGUI.SelectableLabel(new Rect(40, 190, 420, 60), "new CustomEvent(\"Event Name\").Send()", "code_snippet");
            //video link
            if (GUI.Button(new Rect(115, 300, 270, 150), "", "video_centered"))
            {
                Application.OpenURL("https://vimeo.com/cognitive3d/videos");
            }
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/customevents/");
        }
        void ExitPollUpdate()
        {
            GUI.Label(steptitlerect, "EXITPOLL SURVEY", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "An <b>ExitPoll</b> survey is a feature to gather feedback from your users and aggregate results on the dashboard.", "normallabel");
            GUI.Label(new Rect(30, 110, 440, 440), "On the Dashboard, you can create an ExitPoll <b>Question Set</b> and display it using customizable prefabs in Unity.\n\nYou can even change the Question Set after your application is distributed.", "normallabel");
            GUI.Box(new Rect(150, 250, 200, 150), EditorCore.ExitPollFeature, "image_centered");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/exitpoll/");
        }
        void SensorsUpdate()
        {
            GUI.Label(steptitlerect, "SENSORS", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "<b>Sensors</b> are a feature to record a value or property over time.", "normallabel");
            GUI.Label(new Rect(30, 90, 440, 440), "If you have the hardware to support it, you can record Sensor data for Heart Rate, GSR, ECG,  and view it as a graph on the dashboard.\n\nSeveral types of data are recorded by default, such as FPS and Battery Temperature.", "normallabel");
            EditorGUI.DrawRect(new Rect(30, 230, 440, 110), CommentAreaDarkGrey);
            GUI.Box(new Rect(150, 345, 200, 150), EditorCore.SensorsFeature, "image_centered");
            //required for sensor code snippet to have unique controlid
            EditorGUI.SelectableLabel(new Rect(4000, 190, 420, 60), "ignore", "code_snippet");
            EditorGUI.SelectableLabel(new Rect(40, 240, 420, 90), "float sensorData = Random.Range(1, 100f);\nCognitive3D.SensorRecorder\n    .RecordDataPoint(\"SensorName\", sensorData);", "code_snippet");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/sensors/");
        }
        void MediaUpdate()
        {
            GUI.Label(steptitlerect, "SCENE MEDIA", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "<b>Scene Media</b> allows you detect and aggregate gaze data on media objects like images, videos, and 360 degree videos.", "normallabel");
            GUI.Label(new Rect(30, 110, 440, 440), "On the Dashboard, you can upload media files in the <b>Media Library</b> tab and define Points of Interest.\n\nYou can then add media to your scene and associate them with files on the dashboard to record gaze.", "normallabel");
            GUI.Box(new Rect(0, 220, 500, 260), EditorCore.MediaFeature, "image_centered");
            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/media/");
        }
        void ReadyRoomUpdate()
        {
            GUI.Label(steptitlerect, "READY ROOM", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "<b>Ready Room</b> provides a tutorial framework scene for users of an application to get familiar with the VR experiences. This is particularly useful if members of your target audience are new to VR or you want to ensure all equipment and software is configured correctly.", "normallabel");
            GUI.Label(new Rect(30, 150, 440, 440), "You can start setting up a Ready Room by importing the \"Ready Room\" sample from the Cognitive3D Unity SDK package in Package Manager.", "normallabel");
            GUI.Box(new Rect(150, 250, 200, 150), EditorCore.ReadyRoomFeature, "image_centered");

            if (!searchedForReadyRoom)
            {
                searchedForReadyRoom = true;
                foundReadyRoomScene = false;
                readyRoomPath = string.Empty;
                var found = AssetDatabase.FindAssets("t:scene readyroom");
                if (found.Length > 0)
                {
                    readyRoomPath = AssetDatabase.GUIDToAssetPath(found[0]);
                    foundReadyRoomScene = true;
                }
            }

            if (foundReadyRoomScene)
            {
                Rect buttonrect = new Rect(150, 460, 200, 30);
                if (GUI.Button(buttonrect, "Open Ready Room Scene"))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        

                        EditorSceneManager.OpenScene(readyRoomPath, OpenSceneMode.Single);
                    }
                }
            }
            else
            {
                DrawPackageManagerButton();
            }

            DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/ready-room/");
        }

        void DrawFooter()
        {
            GUI.color = EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            if (currentPage == Page.Main)
            {
                DrawSpecificDocsButton("https://docs.cognitive3d.com/unity/comprehensive-setup-guide/");
            }
            else
            {
                DrawBackButton();
            }
            
        }

        void DrawBackButton()
        {
            Rect buttonrect = new Rect(10, 510, 80, 30);
            if (GUI.Button(buttonrect, "Back"))
            {
                currentPage = Page.Main;
            }
        }

        void DrawSpecificDocsButton(string url)
        {
            Rect buttonrect = new Rect(150, 510, 200, 30);
            if (GUI.Button(buttonrect, new GUIContent("Open Online Documentation       ",url)))
            {
                Application.OpenURL(url);
            }
            Rect onlineRect = buttonrect;
            onlineRect.x += 82;
            GUI.Label(onlineRect, EditorCore.ExternalIcon);
        }

        void DrawDynamicManagerButton()
        {
            Rect buttonrect = new Rect(150, 460, 200, 30);
            if (GUI.Button(buttonrect, "Open Dynamic Object Window"))
            {
                DynamicObjectsWindow.Init();
            }
        }        
        void DrawSceneWindowButton()
        {
            Rect buttonrect = new Rect(150, 460, 200, 30);
            if (GUI.Button(buttonrect, "Open Scene Setup Window"))
            {
                SceneSetupWindow.Init();
            }
        }

        void DrawPackageManagerButton()
        {
            Rect buttonrect = new Rect(150, 460, 200, 30);
            if (GUI.Button(buttonrect, "Open Package Manager"))
            {
                UnityEditor.PackageManager.UI.Window.Open("com.cognitive3d.c3d-sdk");
            }
        }
    }
}