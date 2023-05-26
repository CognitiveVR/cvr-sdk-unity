using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D.ReadyRoom
{
    public class ReadyRoomSetupWindow : EditorWindow
    {
        public static void Init()
        {
            ReadyRoomSetupWindow window = (ReadyRoomSetupWindow)EditorWindow.GetWindow(typeof(ReadyRoomSetupWindow), true, "");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();
        }

        List<string> pageids = new List<string>() {
            "welcome",
            "player",
            "eye tracking",
            "room scale",
            "grab components",
            "custom",
            "scene menu",
            "overview" };
        public int currentPage;

        Rect steptitlerect = new Rect(30, 0, 100, 440);
        Rect boldlabelrect = new Rect(30, 45, 440, 440);

        public enum FeatureUsage
        {
            NotSet,
            Enable,
            Disable,
        }
        public static FeatureUsage UseEyeTracking;
        public static FeatureUsage UseRoomScale;
        public static FeatureUsage UseGrabbableObjects;

        string selectedSceneInfoPath;
        Vector2 dynamicScrollPosition = Vector2.zero;
        List<GrabComponentsRequired> Grabbables = null;
        SceneSelectMenu sceneSelect;

        public List<AssessmentBase> GetAllAssessments()
        {
            if (AssessmentManager.Instance != null)
            {
                return AssessmentManager.Instance.AllAssessments;
            }
            return null;
        }

        void OnGUI()
        {
            GUI.skin = Cognitive3D.EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

            //if (Event.current.keyCode == KeyCode.Equals && Event.current.type == EventType.KeyDown) { currentPage++; }
            //if (Event.current.keyCode == KeyCode.Minus && Event.current.type == EventType.KeyDown) { currentPage--; }

            switch (pageids[currentPage])
            {
                case "welcome": WelcomeUpdate(); break;
                case "player": PlayerUpdate(); break;
                case "eye tracking": EyeTrackingUpdate(); break;
                case "room scale": RoomScaleUpdate(); break;
                case "grab components": GrabUpdate(); break;
                case "custom": CustomUpdate(); break;
                case "scene menu": SceneMenuUpdate(); break;
                case "overview": OverviewUpdate(); break;
                default: throw new System.NotSupportedException();
            }

            DrawFooter();
        }

        void WelcomeUpdate()
        {
            GUI.Label(steptitlerect, "WELCOME", "steptitle");

            GUI.Label(boldlabelrect, "Welcome to the Ready Room Setup.", "boldlabel");
            GUI.Label(new Rect(30, 100, 440, 440), "Ready Room is a simple & configurable environment to teach your players some basic VR conventions.", "normallabel");

            GUI.Label(new Rect(30, 150, 440, 440), "The purpose is to allow your players to explore and learn VR in a simple tutorial area before they proceed to your VR experience." +
                "\n\nThis gives you a chance to troubleshoot hardware and make sure your players are comfortable before recording data.", "normallabel");
        }

        //TODO quick setup buttons here - add OVR prefabs or SteamVR prefabs based on what's been set in scene seutp window
        void PlayerUpdate()
        {
            GUI.Label(steptitlerect, "CONFIGURE PLAYER PREFAB", "steptitle");

            GUI.Label(boldlabelrect, "Ensure your Player prefab is configured correctly", "boldlabel");
            GUI.Label(new Rect(30, 100, 440, 30), "- Add your <color=#62B4F3FF>Player prefab</color> to this scene." +
                "\n  This may be OVRCameraRig or SteamVR's [CameraRig].", "normallabel");
            GUI.Label(new Rect(30, 160, 440, 30), "- If the player prefab has controllers, add a <color=#62B4F3FF>ControllerPointer</color>\n  Component to each controller.", "normallabel");
            GUI.Label(new Rect(30, 220, 440, 30), "- Add a <color=#62B4F3FF>HMDPointer</color> Component to the player's HMD camera.", "normallabel");
        }

        void EyeTrackingUpdate()
        {
            GUI.Label(steptitlerect, "EYE TRACKING", "steptitle");

            GUI.Label(boldlabelrect, "Do you use eye tracking?", "boldlabel");

            if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseEyeTracking == FeatureUsage.Disable ? "button_blueoutline" : "button_disabledtext"))
            {
                UseEyeTracking = FeatureUsage.Disable;
            }
            if (UseEyeTracking != FeatureUsage.Disable)
            {
                GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
            }
            if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseEyeTracking == FeatureUsage.Enable ? "button_blueoutline" : "button_disabledtext"))
            {
                UseEyeTracking = FeatureUsage.Enable;
            }
            if (UseEyeTracking != FeatureUsage.Enable)
            {
                GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
            }

            if (UseEyeTracking == FeatureUsage.Disable)
            {
                GUI.Label(new Rect(30, 200, 440, 440), "The player will not see any instructions about calibrating eye tracking", "normallabel");
            }
            if (UseEyeTracking == FeatureUsage.Enable)
            {
                if (GameplayReferences.SDKSupportsEyeTracking)
                {
                    GUI.Label(new Rect(30, 200, 440, 440), "A short test will appear for the player to ensure eye tracking is correctly calibrated", "normallabel");
                    GUI.Label(new Rect(30, 260, 440, 440), "- Add any required Eye Tracking prefabs to the scene\n  (such as GliaBehaviour or SRAnipal Eye Framework)", "normallabel_actionable");
                }
                else
                {
                    GUI.Label(new Rect(0, 200, 475, 40), Cognitive3D.EditorCore.Alert, "image_centered");
                    GUI.Label(new Rect(30, 260, 440, 440), "The SDK selected in the Cognitive3D Setup Wizard does not support eye tracking", "normallabel");
                }
            }
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.AllowEyeTrackingAssessments = UseEyeTracking == FeatureUsage.Enable;
            }
        }

        void RoomScaleUpdate()
        {
            GUI.Label(steptitlerect, "ROOM SCALE", "steptitle");

            GUI.Label(boldlabelrect, "Do you use room scale?", "boldlabel");

            if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseRoomScale == FeatureUsage.Disable ? "button_blueoutline" : "button_disabledtext"))
            {
                UseRoomScale = FeatureUsage.Disable;
            }
            if (UseRoomScale != FeatureUsage.Disable)
            {
                GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
            }
            if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseRoomScale == FeatureUsage.Enable ? "button_blueoutline" : "button_disabledtext"))
            {
                UseRoomScale = FeatureUsage.Enable;
            }
            if (UseRoomScale != FeatureUsage.Enable)
            {
                GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
            }

            if (UseRoomScale == FeatureUsage.Disable)
            {
                GUI.Label(new Rect(30, 200, 440, 440), "The player will not see any instructions about moving around the room", "normallabel");
            }
            if (UseRoomScale == FeatureUsage.Enable)
            {
                if (GameplayReferences.SDKSupportsRoomSize)
                {
                    GUI.Label(new Rect(30, 200, 440, 440), "There will be a short test to ask the player to move around the room", "normallabel");
                    GUI.Label(new Rect(30, 260, 440, 440), "- Make sure the room boundaries are configured beforehand" +
                        "\n\n- Add any required Room Scale prefabs to the scene\n  (such as SteamVR's [CameraRig])", "normallabel_actionable");
                }
                else
                {
                    GUI.Label(new Rect(0, 200, 475, 40), Cognitive3D.EditorCore.Alert, "image_centered");
                    GUI.Label(new Rect(30, 260, 440, 440), "The SDK selected in the Cognitive3D Setup Wizard does not support room scale", "normallabel");
                }
            }
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.AllowRoomScaleAssessments = UseRoomScale == FeatureUsage.Enable;
            }
        }

        void RefreshGrabbables(bool forceRefresh = false)
        {
            //destroyed components won't trigger Grabbables.Contains(null). probably some internal nested prefab reason?
            if (Grabbables != null)
            {
                foreach (var v in Grabbables)
                {
                    if (v == null)
                    {
                        forceRefresh = true;
                        break;
                    }
                }
            }
            if (forceRefresh || Grabbables == null || Grabbables.Contains(null))
            {
                Grabbables = new List<GrabComponentsRequired>(FindObjectsOfType<GrabComponentsRequired>());
            }
        }

        void GrabUpdate()
        {
            GUI.Label(steptitlerect, "PICKING UP ITEMS", "steptitle");

            GUI.Label(boldlabelrect, "Does the player pick up anything?", "boldlabel");

            if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseGrabbableObjects == FeatureUsage.Disable ? "button_blueoutline" : "button_disabledtext"))
            {
                UseGrabbableObjects = FeatureUsage.Disable;
            }
            if (UseGrabbableObjects != FeatureUsage.Disable)
            {
                GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
            }
            if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseGrabbableObjects == FeatureUsage.Enable ? "button_blueoutline" : "button_disabledtext"))
            {
                UseGrabbableObjects = FeatureUsage.Enable;
                RefreshGrabbables(true);
            }
            if (UseGrabbableObjects != FeatureUsage.Enable)
            {
                GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
            }

            if (UseGrabbableObjects == FeatureUsage.Disable)
            {
                GUI.Label(new Rect(30, 200, 440, 440), "The player will not see any instructions about picking up objects", "normallabel");
            }
            if (UseGrabbableObjects == FeatureUsage.Enable)
            {
                RefreshGrabbables();

                GUI.Label(new Rect(30, 200, 440, 440), "There will be a test asking the player to pick up and examine a small cube", "normallabel");

                if (Grabbables.Count > 0)
                {
                    //steamvr's Interactable or Oculus's Grabbable
                    GUI.Label(new Rect(30, 260, 440, 440), "- These Game Objects must to be configured to use your grabbing interaction:", "normallabel_actionable");

                    Rect innerScrollSize = new Rect(30, 0, 420, Grabbables.Count * 30);
                    dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 330, 440, 100), dynamicScrollPosition, innerScrollSize, false, true);

                    for (int i = 0; i < Grabbables.Count; i++)
                    {
                        if (Grabbables[i] == null) { continue; }
                        string background = (i % 2 == 0) ? "dynamicentry_even" : "dynamicentry_odd";
                        if (GUILayout.Button(Grabbables[i].gameObject.name, background))
                        {
                            Selection.activeGameObject = Grabbables[i].gameObject;
                        }
                    }

                    GUI.EndScrollView();

                    GUI.Box(new Rect(30, 330, 425, 100), "", "box_sharp_alpha");

                    if (GUI.Button(new Rect(180, 440, 140, 35), "Select All", "button"))
                    {
                        List<GameObject> grabGameObjects = new List<GameObject>();
                        for (int i = 0; i < Grabbables.Count; i++)
                        {
                            grabGameObjects.Add(Grabbables[i].gameObject);
                        }

                        Selection.objects = grabGameObjects.ToArray();
                        EditorGUIUtility.PingObject(grabGameObjects[0]);
                    }
                }
            }
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.AllowGrabbingAssessments = UseGrabbableObjects == FeatureUsage.Enable;
            }
        }

        void CustomUpdate()
        {
            GUI.Label(steptitlerect, "CUSTOM", "steptitle");

            GUI.Label(boldlabelrect, "Add your own assessments", "boldlabel");

            GUI.Label(new Rect(30, 100, 440, 200), "A good assessment will teach the player how to use any unique tools or objects and explain common conventions in your experience." +
                "\n\nSee Documentation for an example", "normallabel");

            if (GUI.Button(new Rect(150, 230, 240, 30), "Open Documentation Site"))
            {
                Application.OpenURL("https://docs.cognitive3d.com/unity/ready-room/#example-custom-assessment");
            }
        }

        bool hasDisplayedBuildPopup = false;
        void SceneMenuUpdate()
        {
            GUI.Label(steptitlerect, "SCENE MENU", "steptitle");

            GUI.Label(boldlabelrect, "Display Scene Selection when completed", "boldlabel");
            GUI.Label(new Rect(30, 100, 440, 200), "When all tests in the Ready Room are complete, the player will see one or more scenes to load", "normallabel");

            if (!hasDisplayedBuildPopup)
            {
                hasDisplayedBuildPopup = true;

                //popup to add scene to build settings
                var editorSceneList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                var foundScene = editorSceneList.Find(delegate (EditorBuildSettingsScene obj) { return obj.path.Contains("ReadyRoom"); });

                //ready room isn't in build settings or not first in build settings
                if (foundScene == null || editorSceneList[0] != foundScene)
                {
                    bool result = EditorUtility.DisplayDialog("Ready Room not in Build Settings", "Ready Room scene should be first scene loaded in build settings. Do you want to change this now?", "Yes", "No");
                    if (result)
                    {
                        //if it exists, remove ready room scene from list
                        if (foundScene != null)
                            editorSceneList.Remove(foundScene);

                        //get scene asset
                        var foundSceneAssets = AssetDatabase.FindAssets("t:scene ReadyRoom");
                        if (foundSceneAssets.Length > 0)
                        {
                            string readyRoomPath = AssetDatabase.GUIDToAssetPath(foundSceneAssets[0]);

                            EditorBuildSettingsScene ebss = new EditorBuildSettingsScene(readyRoomPath, true);
                            editorSceneList.Insert(0, ebss);
                            EditorBuildSettings.scenes = editorSceneList.ToArray();
                            Debug.Log("Added " + readyRoomPath + " to Editor Build Settings");
                        }
                    }
                }
            }

            if (sceneSelect == null)
                sceneSelect = FindObjectOfType<SceneSelectMenu>();
            if (sceneSelect == null)
            {
                //display warning
                GUI.Label(new Rect(0, 200, 475, 130), Cognitive3D.EditorCore.Alert, "image_centered");
                GUI.Label(new Rect(30, 300, 440, 440), "There is no Scene Select Menu in this scene. Make sure the player can correctly exit Ready Room when completed", "normallabel");
            }
            else
            {
                Rect dropArea = new Rect(30, 150, 440, 100);

                GUI.color = new Color(0, .8f, 0);
                GUI.Box(dropArea, "", "box_sharp_alpha");
                GUI.color = Color.white;

                if (dropArea.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    if (Event.current.type == EventType.DragPerform)
                    {
                        foreach (var v in DragAndDrop.objectReferences)
                        {
                            SceneAsset sceneAsset = v as SceneAsset;

                            if (sceneAsset == null) { continue; }
                            string path = AssetDatabase.GetAssetPath(v);

                            var foundSceneByPath = sceneSelect.SceneInfos.Find(delegate (SceneInfo obj) { return obj.ScenePath == path; });
                            if (string.IsNullOrEmpty(foundSceneByPath.ScenePath))
                            {
                                string filename = System.IO.Path.GetFileNameWithoutExtension(path);

                                string displayname = ObjectNames.NicifyVariableName(filename.Replace('_', ' '));
                                sceneSelect.SceneInfos.Add(new SceneInfo() { ScenePath = path, DisplayName = displayname });

                                //popup to add scene to build settings
                                var editorSceneList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                                var foundScene = editorSceneList.Find(delegate (EditorBuildSettingsScene obj) { return obj.path == path; });
                                if (foundScene == null)
                                {
                                    bool result = EditorUtility.DisplayDialog("Scene not in Build Settings", "The selected scene is not currently in the build settings. Do you want to add this now?", "Yes", "No");
                                    if (result)
                                    {
                                        EditorBuildSettingsScene ebss = new EditorBuildSettingsScene(path, true);
                                        editorSceneList.Add(ebss);
                                        EditorBuildSettings.scenes = editorSceneList.ToArray();
                                        Debug.Log("Added " + path + " to Editor Build Settings");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }
                GUI.Label(dropArea, "Drag and Drop Scene Assets here", "ghostlabel");

                Rect innerScrollSize = new Rect(30, 0, 420, sceneSelect.SceneInfos.Count * 30);
                dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 280, 440, 150), dynamicScrollPosition, innerScrollSize, false, true);

                Rect dynamicrect;
                for (int i = 0; i < sceneSelect.SceneInfos.Count; i++)
                {
                    dynamicrect = new Rect(30, i * 30, 425, 30);
                    bool darkBackground = (i % 2 == 0);
                    DrawSceneInfo(dynamicrect, darkBackground, sceneSelect.SceneInfos[i], sceneSelect);
                }
                GUI.EndScrollView();
                Repaint();

                GUI.Box(new Rect(30, 280, 425, 150), "", "box_sharp_alpha");
            }
        }

        private void DrawSceneInfo(Rect dynamicrect, bool darkBackground, SceneInfo sceneInfo, SceneSelectMenu sceneSelect)
        {
            string background = darkBackground ? "dynamicentry_even" : "dynamicentry_odd";

            Event e = Event.current;
            if (e.isMouse && e.type == EventType.MouseDown)
            {
                if (e.mousePosition.x < dynamicrect.x || e.mousePosition.x > dynamicrect.x + dynamicrect.width - 100 || e.mousePosition.y < dynamicrect.y || e.mousePosition.y > dynamicrect.y + dynamicrect.height)
                {
                }
                else
                {
                    Selection.activeObject = sceneSelect;
                    selectedSceneInfoPath = sceneInfo.ScenePath;
                }
            }

            GUI.Label(dynamicrect, sceneInfo.DisplayName, background);
            if (string.IsNullOrEmpty(sceneInfo.ScenePath))
            {
                Rect warningRect = dynamicrect;
                warningRect.x = 30;
                warningRect.width = 30;
                GUI.Label(warningRect, new GUIContent(Cognitive3D.EditorCore.Alert, "Missing Scene Path"), "image_centered");
            }

            if (e.mousePosition.x < dynamicrect.x || e.mousePosition.x > dynamicrect.x + dynamicrect.width || e.mousePosition.y < dynamicrect.y || e.mousePosition.y > dynamicrect.y + dynamicrect.height)
            {
            }
            else
            {
                //draw up/down arrows
                float height = dynamicrect.height / 2;
                float offset = dynamicrect.height / 2;
                Rect up = new Rect(435, dynamicrect.y, 18, height - 1);
                Rect down = new Rect(435, dynamicrect.y + offset + 1, 18, height - 1);

                if (GUI.Button(up, "^"))
                {
                    selectedSceneInfoPath = sceneInfo.ScenePath;
                    var all = sceneSelect.SceneInfos;
                    int index = all.IndexOf(sceneInfo);
                    if (index > 0)
                    {
                        all.Remove(sceneInfo);
                        all.Insert(index - 1, sceneInfo);
                    }
                }
                if (GUI.Button(down, "v"))
                {
                    selectedSceneInfoPath = sceneInfo.ScenePath;
                    var all = sceneSelect.SceneInfos;
                    int index = all.IndexOf(sceneInfo);
                    if (index < all.Count - 1)
                    {
                        all.Remove(sceneInfo);
                        all.Insert(index + 1, sceneInfo);
                    }
                }
            }

            if (selectedSceneInfoPath == sceneInfo.ScenePath)
            {
                GUI.Box(dynamicrect, "", "box_sharp_alpha");
            }
        }

        SceneSelectMenu SceneSelectAssessment;

        void OverviewUpdate()
        {
            var all = GetAllAssessments();

            GUI.Label(steptitlerect, "OVERVIEW", "steptitle");

            GUI.Label(boldlabelrect, "These are the <color=#8A9EB7FF>Assessments</color> that will be shown:", "boldlabel");

            Rect enabled = new Rect(30, 145, 120, 30);
            GUI.Label(enabled, "Enabled", "dynamicheader");

            Rect gameobject = new Rect(90, 145, 120, 30);
            GUI.Label(gameobject, "GameObject", "dynamicheader");

            if (all.Count == 0)
            {
                GUI.Label(new Rect(30, 170, 420, 270), "No objects found.\n\nDo you have assessment components in your scene?\n\nAre they active in your hierarchy?", "button_disabledtext");
            }

            Rect innerScrollSize = new Rect(30, 0, 420, all.Count * 30);
            dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 170, 440, 280), dynamicScrollPosition, innerScrollSize, false, true);

            Rect dynamicrect;
            for (int i = 0; i < all.Count; i++)
            {
                dynamicrect = new Rect(30, i * 30, 460, 30);
                DrawAssessment(all[i], dynamicrect, i % 2 == 0);
            }
            GUI.EndScrollView();
            GUI.Box(new Rect(30, 170, 425, 280), "", "box_sharp_alpha");
            Repaint();
        }

        void DrawAssessment(AssessmentBase assessment, Rect rect, bool darkbackground)
        {
            Event e = Event.current;
            if (e.isMouse && e.type == EventType.MouseDown)
            {
                if (e.mousePosition.x < rect.x || e.mousePosition.x > rect.x + rect.width - 100 || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y + rect.height)
                {
                }
                else
                {
                    if (e.shift) //add to selection
                    {
                        GameObject[] gos = new GameObject[Selection.transforms.Length + 1];
                        Selection.gameObjects.CopyTo(gos, 0);
                        gos[gos.Length - 1] = assessment.gameObject;
                        Selection.objects = gos;
                    }
                    else
                    {
                        Selection.activeTransform = assessment.transform;
                    }
                }
            }

            if (darkbackground)
                GUI.Box(rect, "", "dynamicentry_even");
            else
                GUI.Box(rect, "", "dynamicentry_odd");

            if (assessment.GetType() == typeof(SceneSelectMenu))
            {
                var all = GetAllAssessments();
                //warning if scene select assessment exists and is not last
                if (SceneSelectAssessment == null)
                {
                    var sceneMenu = all.Find(delegate (AssessmentBase obj) { return obj.GetType() == typeof(SceneSelectMenu); });
                    if (sceneMenu != null)
                        SceneSelectAssessment = (SceneSelectMenu)sceneMenu;
                }
            }

            if (Selection.activeTransform == assessment.transform)
            {
                GUI.Box(rect, "", "box_sharp_alpha");
            }

            float height = rect.height / 2;
            float offset = rect.height / 2;

            Rect up = new Rect(rect.x + 407, rect.y, 18, height - 1);
            Rect down = new Rect(rect.x + 407, rect.y + offset + 1, 18, height - 1);

            if (e.mousePosition.x < rect.x || e.mousePosition.x > rect.x + rect.width || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y + rect.height)
            {
            }
            else
            {
                //TODO make a tiny icon to display here instead of text
                if (GUI.Button(up, "^"))
                {
                    var all = GetAllAssessments();
                    int index = all.IndexOf(assessment);
                    if (index > 0)
                    {
                        all.Remove(assessment);
                        all.Insert(index - 1, assessment);
                    }
                }
                if (GUI.Button(down, "v"))
                {
                    var all = GetAllAssessments();
                    int index = all.IndexOf(assessment);
                    if (index < all.Count - 1)
                    {
                        all.Remove(assessment);
                        all.Insert(index + 1, assessment);
                    }
                }
            }

            Rect isActiveRect = new Rect(rect.x + 10, rect.y, 24, rect.height);
            Rect gameObjectRect = new Rect(rect.x + 60, rect.y, 420, rect.height);

            if (assessment.IsValid())
            {
                GUI.Label(isActiveRect, Cognitive3D.EditorCore.CircleCheckmark, "image_centered");
            }
            else
            {
                GUI.Label(isActiveRect, Cognitive3D.EditorCore.Alert, "image_centered");
            }

            string tooltip = assessment.InvalidReason();
            GUI.Label(gameObjectRect, new GUIContent(assessment.gameObject.name, tooltip), "dynamiclabel");
        }

        void DrawFooter()
        {
            GUI.color = Cognitive3D.EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            Rect backbuttonrect = new Rect(320, 510, 80, 30);
            if (GUI.Button(backbuttonrect, "Back", "button_disabled"))
            {
                currentPage = Mathf.Max(0, currentPage - 1);
                Grabbables = null;
            }

            if (currentPage == pageids.Count - 1)
            {
                //last page
                Rect nextbuttonrect = new Rect(410, 510, 80, 30);
                if (GUI.Button(nextbuttonrect, "Done", "button"))
                {
                    Close();
                }
            }
            else
            {
                Rect nextbuttonrect = new Rect(410, 510, 80, 30);
                string style = "button";

                System.Action clickAction = () => { currentPage = Mathf.Min(pageids.Count - 1, currentPage + 1); };

                if (pageids[currentPage] == "welcome")
                {

                    if (GUI.Button(nextbuttonrect, "Next", style))
                    {
                        currentPage = Mathf.Min(pageids.Count - 1, currentPage + 1);
                        Grabbables = null;
                        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name != "ReadyRoom")
                        {
                            if (EditorUtility.DisplayDialog("Open Ready Room Scene?", "Do you want to open the Ready Room scene?", "Yes", "No"))
                            {
                                var readyRoomScenes = AssetDatabase.FindAssets("t:scene readyroom");
                                if (readyRoomScenes.Length == 1)
                                {
                                    //ask if want save
                                    if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                                    {
                                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(readyRoomScenes[0]));
                                    }
                                }
                            }
                        }
                    }
                    return;
                }
                if (pageids[currentPage] == "player")
                {
                    //style = "button_disabled";
                    //clickAction = null;
                }
                if (pageids[currentPage] == "eye tracking")
                {
                    if (UseEyeTracking == FeatureUsage.NotSet)
                    {
                        style = "button_disabled";
                        clickAction = null;
                    }
                }
                if (pageids[currentPage] == "room scale")
                {
                    if (UseRoomScale == FeatureUsage.NotSet)
                    {
                        style = "button_disabled";
                        clickAction = null;
                    }
                }
                if (pageids[currentPage] == "grab components")
                {
                    if (UseGrabbableObjects == FeatureUsage.NotSet)
                    {
                        style = "button_disabled";
                        clickAction = null;
                    }
                    else if (UseGrabbableObjects == FeatureUsage.Disable)
                    {
                        //not using grabbable things, fine to skip
                    }
                    else if (UseGrabbableObjects == FeatureUsage.Enable)
                    {
                        if (Grabbables != null && Grabbables.Count > 0)
                        {
                            style = "button_disabled";
                            clickAction = null;
                        }
                    }
                }

                if (GUI.Button(nextbuttonrect, "Next", style))
                {
                    if (clickAction != null)
                        clickAction.Invoke();
                    Grabbables = null;
                }
            }
        }

        //TODO test these quick setup functions
        public static void SetupOculus(Object[] targets)
        {
#if C3D_OCULUS
        foreach(var v in targets)
        {
            var grab = (v as GrabComponentsRequired);
            grab.gameObject.AddComponent<OVRGrabbable>();
            if (grab.GetComponentInChildren<Collider>() == null)
            {
                grab.gameObject.AddComponent<BoxCollider>();
            }
        }
        for(int i = 0; i< targets.Length;i++)
        {
            DestroyImmediate(targets[i]);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#else
            if (UnityEditor.EditorUtility.DisplayDialog("Can't complete setup", "Oculus SDK was not selected. Please run Project Setup first.", "Open Project Setup", "Ok"))
            {
                ProjectSetupWindow.Init();
            }
#endif
        }
        public static void SetupSteamVR2(Object[] targets)
        {
#if C3D_STEAMVR2
        foreach(var v in targets)
        {
            var grab = (v as GrabComponentsRequired);
            grab.gameObject.AddComponent<Valve.VR.InteractionSystem.Interactable>();
            grab.gameObject.AddComponent<Valve.VR.InteractionSystem.Sample.InteractableExample>();
            if (grab.GetComponentInChildren<Collider>() == null)
            {
                grab.gameObject.AddComponent<BoxCollider>();
            }
        }
        for(int i = 0; i< targets.Length;i++)
        {
            DestroyImmediate(targets[i]);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#else
            if (UnityEditor.EditorUtility.DisplayDialog("Can't complete setup", "SteamVR2 SDK was not selected. Please run Project Setup first.", "Open Project Setup", "Ok"))
            {
                ProjectSetupWindow.Init();
            }
#endif
        }
    }
}