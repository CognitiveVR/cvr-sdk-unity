using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    public class ReadyRoomSetupWindow : EditorWindow
    {
        //called from menu item script
        public static void Init()
        {
            ReadyRoomSetupWindow window = (ReadyRoomSetupWindow)EditorWindow.GetWindow(typeof(ReadyRoomSetupWindow), true, "");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();

            UseEyeTracking = EditorPrefs.GetInt("useEyeTracking", -1);
            UseRoomScale = EditorPrefs.GetInt("useRoomScale", -1);
            UseGrabbableObjects = EditorPrefs.GetInt("useGrabbable", -1);

            //check that window.Grabbables is empty
            /*if (UseEyeTracking != -1 && UseGrabbableObjects != -1 && UseRoomScale != -1)
            {
                window.RefreshGrabbables(true);
                if (window.Grabbables.Count > 0)
                {
                    //user has done most of the setup, but still has grabbables to fix
                    window.currentPage = 4;
                    //IMPROVEMENT search pageids for index of 'grab components'
                }
                else
                {
                    //user has already run through the Ready Room setup. Unlikely that they will need to change this basic stuff
                    window.currentPage = window.pageids.Count - 1;
                }
            }*/
        }

        //called after recompile if window already open
        void OnEnable()
        {
            UseEyeTracking = EditorPrefs.GetInt("useEyeTracking", -1);
            UseRoomScale = EditorPrefs.GetInt("useRoomScale", -1);
            UseGrabbableObjects = EditorPrefs.GetInt("useGrabbable", -1);
            Repaint();
        }

        List<AssessmentBase> AllAssessments = new List<AssessmentBase>();
        List<string> pageids = new List<string>() { "welcome", "player", "eye tracking", "room scale", "grab components", "custom", "scene menu", "overview" };
        public int currentPage;

        Rect steptitlerect = new Rect(30, 0, 100, 440);
        Rect boldlabelrect = new Rect(30, 100, 440, 440);

        public static int UseEyeTracking = -1;
        public static int UseRoomScale = -1;
        public static int UseGrabbableObjects = -1;

        string selectedSceneInfoPath;
        Vector2 dynamicScrollPosition = Vector2.zero;
        List<GrabComponentsRequired> Grabbables = null;
        SceneSelectMenu sceneSelect;

        public List<AssessmentBase> GetAllAssessments()
        {
            foreach (var assessment in AllAssessments)
            {
                if (assessment == null)
                {
                    RefreshAssessments();
                    break;
                }
            }
            return AllAssessments;
        }

        //Finds all assessments and sorts them
        public void RefreshAssessments()
        {
            AllAssessments = new List<AssessmentBase>(Object.FindObjectsOfType<AssessmentBase>());

            AllAssessments.Sort(delegate (AssessmentBase a, AssessmentBase b)
            {
                return a.Order.CompareTo(b.Order);
            });
        }

        void OnGUI()
        {
            GUI.skin = CognitiveVR.EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

            if (GetAllAssessments() == null || GetAllAssessments().Count == 0)
                RefreshAssessments();

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
            }

            DrawFooter();
        }

        void WelcomeUpdate()
        {
            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - WELCOME", "steptitle");

            GUI.Label(boldlabelrect, "Welcome to the Ready Room Setup.", "boldlabel");
            GUI.Label(new Rect(30, 170, 440, 440), "Ready Room is a simple & configurable environment for your participants to learn how to use VR properly.", "normallabel");

            GUI.Label(new Rect(30, 230, 440, 440), "The purpose is to allow your participants to explore and learn VR in a simple tutorial area before htey proceed to your VR experience. " +
                "This ensure that any interaction data from participants who are troubleshooting hardware, learning how to use controllers, or understand basic VR interactions is kept separate from your actual experience.", "normallabel");

            GUI.Label(new Rect(30, 370, 440, 440), "By default, we do not collect any data from the Ready Room.", "normallabel");

            GUI.Label(new Rect(30, 410, 440, 440), "This setup helps configure the Ready Room scene for your equipment.", "normallabel");
        }

        void PlayerUpdate()
        {
            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - PLAYER", "steptitle");

            GUI.Label(boldlabelrect, "Ensure your Player is configured correctly", "boldlabel");
            GUI.Label(new Rect(30, 200, 440, 30), "<b>Step 1:</b> Add your <b>Player</b> prefab to the Ready Room Scene", "normallabel_actionable");
            GUI.Label(new Rect(30, 260, 440, 30), "<b>Step 2:</b> If the player has controllers, add a <b>ControllerPointer</b> Component to each controller", "normallabel_actionable");
            GUI.Label(new Rect(30, 340, 440, 30), "<b>Step 3:</b> Add a <b>HMDPointer</b> Component to the player's HMD camera", "normallabel_actionable");
        }

        void EyeTrackingUpdate()
        {
            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - EYE TRACKING", "steptitle");

            GUI.Label(boldlabelrect, "Do you use eye tracking?", "boldlabel");

            if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseEyeTracking == 0 ? "button_blueoutline" : "button_disabledtext"))
            {
                UseEyeTracking = 0;
                EditorPrefs.SetInt("useEyeTracking", UseEyeTracking);
                UpdateActiveAssessments();
            }
            if (UseEyeTracking != 0)
            {
                GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
            }
            if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseEyeTracking == 1 ? "button_blueoutline" : "button_disabledtext"))
            {
                UseEyeTracking = 1;
                EditorPrefs.SetInt("useEyeTracking", UseEyeTracking);
                UpdateActiveAssessments();
            }
            if (UseEyeTracking != 1)
            {
                GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
            }

            if (UseEyeTracking == 0)
            {
                GUI.Label(new Rect(30, 200, 440, 440), "The participant will not see any instructions about calibrating eye tracking", "normallabel");
            }
            if (UseEyeTracking == 1)
            {
                if (GameplayReferences.SDKSupportsEyeTracking)
                {
                    GUI.Label(new Rect(30, 200, 440, 440), "A short test will appear for the participant to ensure eye tracking is correctly calibrated", "normallabel");
                    GUI.Label(new Rect(30, 260, 440, 440), "<b>Step 4:</b> Add any required Eye Tracking components to the scene", "normallabel_actionable");
                }
                else
                {
                    GUI.Label(new Rect(0, 200, 475, 40), CognitiveVR.EditorCore.Alert, "image_centered");
                    GUI.Label(new Rect(30, 260, 440, 440), "The SDK selected in the Cognitive3D Setup Wizard does not support eye tracking", "normallabel");
                }
            }
        }

        void RoomScaleUpdate()
        {
            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - ROOM SCALE", "steptitle");

            GUI.Label(boldlabelrect, "Do you use room scale?", "boldlabel");

            if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseRoomScale == 0 ? "button_blueoutline" : "button_disabledtext"))
            {
                UseRoomScale = 0;
                EditorPrefs.SetInt("useRoomScale", UseRoomScale);
                UpdateActiveAssessments();
            }
            if (UseRoomScale != 0)
            {
                GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
            }
            if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseRoomScale == 1 ? "button_blueoutline" : "button_disabledtext"))
            {
                UseRoomScale = 1;
                EditorPrefs.SetInt("useRoomScale", UseRoomScale);
                UpdateActiveAssessments();
            }
            if (UseRoomScale != 1)
            {
                GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
            }

            if (UseRoomScale == 0)
            {
                GUI.Label(new Rect(30, 200, 440, 440), "The participant will not see any instructions about moving around the room", "normallabel");
            }
            if (UseRoomScale == 1)
            {
                //TODO editorcore.roomscale
#if CVR_TOBIIVR || CVR_NEURABLE || CVR_PUPIL || CVR_AH || CVR_SNAPDRAGON || CVR_VIVEPROEYE
            GUI.Label(new Rect(30, 200, 440, 440), "There will be a short test to ask the participant to move around the room", "normallabel");
            GUI.Label(new Rect(30, 260, 440, 440), "<b>Step 5:</b> Add any required Room Scale components to the scene", "normallabel_actionable");
#elif CVR_STEAMVR || CVR_STEAMVR2
            GUI.Label(new Rect(30, 200, 440, 440), "There will be a short test to ask the participant to move around the room", "normallabel");
            GUI.Label(new Rect(30, 260, 440, 440), "<b>Step 5:</b> Add a SteamVR_PlayArea component to a new gameobject in this scene", "normallabel_actionable");
#else
                GUI.Label(new Rect(0, 200, 475, 40), CognitiveVR.EditorCore.Alert, "image_centered");
                GUI.Label(new Rect(30, 260, 440, 440), "The SDK selected in the Cognitive3D Setup Wizard does not support room scale", "normallabel");
#endif
            }
        }

        void RefreshGrabbables(bool forceRefresh = false)
        {
#if UNITY_2018_3_OR_NEWER
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
#endif
            if (forceRefresh || Grabbables == null || Grabbables.Contains(null))
            {
                Grabbables = new List<GrabComponentsRequired>(FindObjectsOfType<GrabComponentsRequired>());
            }
        }

        void GrabUpdate()
        {
            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - INTERACTIONS", "steptitle");

            GUI.Label(boldlabelrect, "Does the participant pick up anything?", "boldlabel");

            if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseGrabbableObjects == 0 ? "button_blueoutline" : "button_disabledtext"))
            {
                UseGrabbableObjects = 0;
                EditorPrefs.SetInt("useGrabbable", UseGrabbableObjects);
                UpdateActiveAssessments();
            }
            if (UseGrabbableObjects != 0)
            {
                GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
            }
            if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseGrabbableObjects == 1 ? "button_blueoutline" : "button_disabledtext"))
            {
                UseGrabbableObjects = 1;
                EditorPrefs.SetInt("useGrabbable", UseGrabbableObjects);
                UpdateActiveAssessments();
                RefreshGrabbables(true);
            }
            if (UseGrabbableObjects != 1)
            {
                GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
            }

            if (UseGrabbableObjects == 0)
            {
                GUI.Label(new Rect(30, 200, 440, 440), "The participant will not see any instructions about picking up objects", "normallabel");
            }
            if (UseGrabbableObjects == 1)
            {
                RefreshGrabbables();

                GUI.Label(new Rect(30, 200, 440, 440), "There will be a test asking the participant to pickup and examine a small cube", "normallabel");

                if (Grabbables.Count > 0)
                {
                    GUI.Label(new Rect(30, 260, 440, 440), "<b>Step 6:</b> These gameobject must to be configured to use your grabbing interaction:", "normallabel_actionable");

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
        }

        void CustomUpdate()
        {
            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - CUSTOM", "steptitle");

            GUI.Label(boldlabelrect, "Add your own assessments", "boldlabel");

            GUI.Label(new Rect(30, 150, 440, 200), "A good assessment will teach the participant how to use any unique tools or objects and explain common conventions in your experience", "normallabel");

            GUI.Label(new Rect(30, 230, 440, 200), "<b>Step 7a:</b> Create a new gameobject and add an <b>AssessmentBase</b> script, or a script that inherits from this.\n\n" +
                "<b>Step 7b:</b> Add any required game objects as children. These will be disable on <b>Awake()</b> and enabled when your assessment begins.\n\n" +
                "<b>Step 7c:</b> Call <b>CompleteAssesment()</b> when the participant has demonstrated understanding. This will disable child gameobjects and the next assessment will begin.", "normallabel_actionable");
        }

        bool hasDisplayedBuildPopup = false;
        void SceneMenuUpdate()
        {
            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - SCENE MENU", "steptitle");

            GUI.Label(boldlabelrect, "<b>Step 8:</b> Display scenes when the Ready Room is complete", "normallabel_actionable");

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
                GUI.Label(new Rect(0, 200, 475, 130), CognitiveVR.EditorCore.Alert, "image_centered");
                GUI.Label(new Rect(30, 300, 440, 440), "There is no Scene Select Menu in this scene. Make sure the participant can correctly exit Ready Room when completed", "normallabel");
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
                GUI.Label(dropArea, "Drag and Drop Scene Assets here\nto add to the Scene Select Menu", "ghostlabel_actionable");

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


                if (GUI.Button(new Rect(30, 450, 440, 30), "Start Session when participant selects a scene?", sceneSelect.StartSessionOnSceneChange ? "button_blueoutlineleft" : "button_disabledoutline"))
                {
                    sceneSelect.StartSessionOnSceneChange = !sceneSelect.StartSessionOnSceneChange;
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                GUI.Label(new Rect(425, 455, 24, 24), sceneSelect.StartSessionOnSceneChange ? EditorCore.Checkmark : EditorCore.EmptyCheckmark, "image_centered");
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
                GUI.Label(warningRect, new GUIContent(CognitiveVR.EditorCore.Alert, "Missing Scene Path"), "image_centered");
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

            GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - OVERVIEW", "steptitle");

            GUI.Label(boldlabelrect, "These are the <color=#8A9EB7FF>Assessments</color> currently found in the scene.", "boldlabel");

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
            UpdateActiveAssessments();
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

            bool forceWarning = false;
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

                if (SceneSelectAssessment != null)
                {
                    if (SceneSelectAssessment.Order != all.Count - 1)
                    {
                        forceWarning = true;
                    }
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

            bool needsRefresh = false;
            if (e.mousePosition.x < rect.x || e.mousePosition.x > rect.x + rect.width || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y + rect.height)
            {
            }
            else
            {
                if (GUI.Button(up, "^"))
                {
                    var all = GetAllAssessments();
                    int index = all.IndexOf(assessment);
                    if (index > 0)
                    {
                        all.Remove(assessment);
                        all.Insert(index - 1, assessment);
                        needsRefresh = true;
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
                        needsRefresh = true;
                    }
                }
            }

            Rect isActiveRect = new Rect(rect.x + 10, rect.y, 24, rect.height);
            Rect gameObjectRect = new Rect(rect.x + 60, rect.y, 420, rect.height);

            if (assessment.Active && !forceWarning)
            {
                GUI.Label(isActiveRect, CognitiveVR.EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(isActiveRect, CognitiveVR.EditorCore.Alert, "image_centered");
            }

            if (needsRefresh)
            {
                var all = GetAllAssessments();
                for (int i = 0; i < all.Count; i++)
                {
                    all[i].Order = i;
                }
                ReorderAssessmentsInScene();
            }

            string tooltip = "No Text Display";
            var textComponent = assessment.GetComponentInChildren<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                tooltip = textComponent.text;
            }
            if (forceWarning)
            {
                //scene assessment not in last position
                tooltip = "Scene Select Menu should be last";
            }
            GUI.Label(gameObjectRect, new GUIContent(assessment.gameObject.name, tooltip), "dynamiclabel");
        }

        void ReorderAssessmentsInScene()
        {
            var rootGameObjects = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects();

            //repeat this a number of times - reordering gameobjects will get moved around by other moving gameobjects
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                foreach (var g in rootGameObjects)
                {
                    //sort assessments by their order
                    var assessment = g.GetComponent<AssessmentBase>();
                    if (assessment != null)
                        g.transform.SetSiblingIndex(assessment.Order);
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        void DrawFooter()
        {
            GUI.color = CognitiveVR.EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            Rect backbuttonrect = new Rect(320, 510, 80, 30);
            if (GUI.Button(backbuttonrect, "Back", "button_disabled"))
            {
                currentPage = Mathf.Max(0, currentPage - 1);
                RefreshAssessments();
                Grabbables = null;
            }

            if (currentPage == pageids.Count - 1)
            {
                //last page
                Rect nextbuttonrect = new Rect(410, 510, 80, 30);
                if (GUI.Button(nextbuttonrect, "Refresh", "button"))
                {
                    RefreshAssessments();
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
                        RefreshAssessments();
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
                    if (UseEyeTracking == -1)
                    {
                        style = "button_disabled";
                        clickAction = null;
                    }
                }
                if (pageids[currentPage] == "room scale")
                {
                    if (UseRoomScale == -1)
                    {
                        style = "button_disabled";
                        clickAction = null;
                    }
                }
                if (pageids[currentPage] == "grab components")
                {
                    if (UseGrabbableObjects == -1)
                    {
                        style = "button_disabled";
                        clickAction = null;
                    }
                    else if (UseGrabbableObjects == 0)
                    {
                        //not using grabbable things, find to skip
                    }
                    else if (UseGrabbableObjects == 1)
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
                    RefreshAssessments();
                    Grabbables = null;
                }
            }
        }

        public static void SetupOculus(Object[] targets)
        {
#if CVR_OCULUS
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
            if (UnityEditor.EditorUtility.DisplayDialog("Can't complete setup", "Oculus SDK was not selected. Please run Scene Setup first.", "Open Scene Setup", "Ok"))
            {
                CognitiveVR.InitWizard.Init();
            }
#endif
        }
        public static void SetupSteamVR2(Object[] targets)
        {
#if CVR_STEAMVR2
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
            if (UnityEditor.EditorUtility.DisplayDialog("Can't complete setup", "SteamVR2 SDK was not selected. Please run Scene Setup first.", "Open Scene Setup", "Ok"))
            {
                CognitiveVR.InitWizard.Init();
            }
#endif
        }

        public static void SetupXRInteractionToolkit(Object[] targets)
        {
#if CVR_XRINTERACTION_TOOLKIT

        //add XRGrabInteractable component

        foreach(var v in targets)
        {
            var grab = (v as GrabComponentsRequired);
            grab.gameObject.AddComponent<XRGrabInteractable>();
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
            if (UnityEditor.EditorUtility.DisplayDialog("Can't complete setup", "XRInteraction Toolkit package was not selected. Please run Scene Setup first.", "Open Scene Setup", "Ok"))
            {
                CognitiveVR.InitWizard.Init();
            }
#endif
        }

        void UpdateActiveAssessments()
        {
            var all = GetAllAssessments();
            foreach (var a in all)
                a.Active = true;

            OnEnable();

            var tempAssessments = all.FindAll(delegate (AssessmentBase obj) { return obj.RequiresGrabbing && (UseGrabbableObjects != 1); });
            foreach (var a in tempAssessments)
            {
                a.Active = false;
            }
            tempAssessments = all.FindAll(delegate (AssessmentBase obj) { return obj.RequiresRoomScale && (UseRoomScale != 1); });
            foreach (var a in tempAssessments)
            {
                a.Active = false;
            }
            tempAssessments = all.FindAll(delegate (AssessmentBase obj) { return obj.RequiresEyeTracking && (UseEyeTracking != 1); });
            foreach (var a in tempAssessments)
            {
                a.Active = false;
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}