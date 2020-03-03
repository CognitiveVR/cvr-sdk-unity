using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class ReadyRoomSetupWindow : EditorWindow
{
    //called from menu item script
    public static void Init()
    {
        ReadyRoomSetupWindow window = (ReadyRoomSetupWindow)EditorWindow.GetWindow(typeof(ReadyRoomSetupWindow), true, "");
        window.minSize = new Vector2(500, 550);
        window.maxSize = new Vector2(500, 550);
        window.Show();

        window.UseEyeTracking = EditorPrefs.GetInt("useEyeTracking", -1);
        window.UseRoomScale = EditorPrefs.GetInt("useRoomScale", -1);
        window.UseGrabbableObjects = EditorPrefs.GetInt("useGrabbable", -1);

        if (window.UseEyeTracking != -1 && window.UseGrabbableObjects != -1 && window.UseRoomScale != -1)
        {
            //editor prefs will be between multiple projects

            //TODO how to mark that setup was completed??? set default enable/disable values

            //if already gone through setup, just jump to overview
            window.currentPage = window.pageids.Count - 1;
        }
    }


    List<AssessmentBase> AllAssessments = new List<AssessmentBase>();

    //Debugging in editor
    public List<AssessmentBase> GetAllAssessments()
    {
        return AllAssessments;
    }

    //Debugging in editor. Finds all assessments and sorts them
    public void RefreshAssessments()
    {
        AllAssessments = new List<AssessmentBase>(Object.FindObjectsOfType<AssessmentBase>());

        AllAssessments.Sort(delegate (AssessmentBase a, AssessmentBase b)
        {
            return a.Order.CompareTo(b.Order);
        });
    }

    List<string> pageids = new List<string>() { "welcome", "eye tracking", "room scale", "grab components", "custom", "scene menu", "overview" };
    public int currentPage;

    Rect steptitlerect = new Rect(30, 0, 100, 440);
    Rect boldlabelrect = new Rect(30, 100, 440, 440);

    void OnGUI()
    {
        GUI.skin = CognitiveVR.EditorCore.WizardGUISkin;
        GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

        if (GetAllAssessments() == null || GetAllAssessments().Count == 0)
            RefreshAssessments();

        switch (pageids[currentPage])
        {
            case "welcome": WelcomeUpdate(); break;
            case "eye tracking": EyeTrackingUpdate(); break;
            case "room scale": RoomScaleUpdate(); break;
            case "grab components": GrabUpdate(); break;
            case "custom": CustomUpdate(); break;
            case "scene menu": SceneMenuUpdate(); break;
            case "overview": OverviewUpdate(); break;
        }

        DrawFooter();
    }

    int UseEyeTracking = -1;
    int UseRoomScale = -1;
    int UseGrabbableObjects = -1;

    void WelcomeUpdate()
    {
        GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - WELCOME", "steptitle");

        GUI.Label(boldlabelrect, "Welcome to the Ready Room Setup.", "boldlabel");
        GUI.Label(new Rect(30, 200, 440, 440), "The Ready Room provides a simple learning environment so users can understand how to interact more naturally with your experience.", "normallabel");

        GUI.Label(new Rect(30, 300, 440, 440), "This setup will ensure your experience starts correctly configured and that your users have familiarity with VR.", "normallabel");
    }

    void EyeTrackingUpdate()
    {
        GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - EYE TRACKING", "steptitle");

        GUI.Label(boldlabelrect, "Do you use eye tracking?", "boldlabel");

        if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseEyeTracking==0 ? "button_blueoutline" : "button_disabledtext"))
        {
            UseEyeTracking = 0;
            UpdateActiveAssessments();
            EditorPrefs.SetInt("useEyeTracking", UseEyeTracking);
        }
        if (UseEyeTracking != 0)
        {
            GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
        }
        if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseEyeTracking == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            UseEyeTracking = 1;
            UpdateActiveAssessments();
            EditorPrefs.SetInt("useEyeTracking", UseEyeTracking);
        }
        if (UseEyeTracking != 1)
        {
            GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
        }

        if (UseEyeTracking == 1)
        {
            GUI.Label(new Rect(30, 200, 440, 440), "A short test will appear for the user to ensure eye tracking is correctly calibrated", "normallabel");
            //TODO editorcore/core.HasEyetrackingSDK
#if CVR_TOBIIVR || CVR_FOVE || CVR_NEURABLE || CVR_PUPIL || CVR_AH || CVR_SNAPDRAGON || CVR_VIVEPROEYE

#else
            GUI.Label(new Rect(0, 200, 475, 130), CognitiveVR.EditorCore.Alert, "image_centered");
            GUI.Label(new Rect(30, 300, 440, 440), "Eye Tracking SDK is not selected in the Cognitive3D Setup Wizard", "normallabel");
#endif
        }
    }

    void RoomScaleUpdate()
    {
        GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - ROOM SCALE", "steptitle");

        GUI.Label(boldlabelrect, "Do you use room scale?", "boldlabel");

        if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseRoomScale == 0 ? "button_blueoutline" : "button_disabledtext"))
        {
            UseRoomScale = 0;
            UpdateActiveAssessments();
            EditorPrefs.SetInt("useRoomScale", UseRoomScale);
        }
        if (UseRoomScale != 0)
        {
            GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
        }
        if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseRoomScale == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            UseRoomScale = 1;
            UpdateActiveAssessments();
            EditorPrefs.SetInt("useRoomScale", UseRoomScale);
        }
        if (UseRoomScale != 1)
        {
            GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
        }

        if (UseRoomScale == 1)
        {
            GUI.Label(new Rect(30, 200, 440, 440), "There will be a short test to ask the user to move around the room", "normallabel");
            //TODO editorcore.roomscale
#if CVR_TOBIIVR || CVR_NEURABLE || CVR_PUPIL || CVR_AH || CVR_SNAPDRAGON || CVR_VIVEPROEYE || CVR_STEAMVR || CVR_STEAMVR2

#else
            GUI.Label(new Rect(0, 200, 475, 130), CognitiveVR.EditorCore.Alert, "image_centered");
            GUI.Label(new Rect(30, 300, 440, 440), "Room Scale is not supported with the SDK selected in the Cognitive3D Setup Wizard", "normallabel");
#endif
        }
    }

    Vector2 dynamicScrollPosition = Vector2.zero;
    List<GrabComponentsRequired> Grabbables = null;
    void GrabUpdate()
    {
        GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - INTERACTIONS", "steptitle");

        GUI.Label(boldlabelrect, "Does the user pick up anything?", "boldlabel");

        if (GUI.Button(new Rect(100, 150, 100, 30), "NO", UseGrabbableObjects == 0 ? "button_blueoutline" : "button_disabledtext"))
        {
            UseGrabbableObjects = 0;
            UpdateActiveAssessments();
            EditorPrefs.SetInt("useGrabbable", UseGrabbableObjects);
        }
        if (UseGrabbableObjects != 0)
        {
            GUI.Box(new Rect(100, 150, 100, 30), "", "box_sharp_alpha");
        }
        if (GUI.Button(new Rect(300, 150, 100, 30), "YES", UseGrabbableObjects == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            UseGrabbableObjects = 1;
            UpdateActiveAssessments();
            EditorPrefs.SetInt("useGrabbable", UseGrabbableObjects);
        }
        if (UseGrabbableObjects != 1)
        {
            GUI.Box(new Rect(300, 150, 100, 30), "", "box_sharp_alpha");
        }
        
        if (UseGrabbableObjects == 1)
        {
            if (Grabbables == null || Grabbables.Contains(null))
            {
                Grabbables = new List<GrabComponentsRequired>(FindObjectsOfType<GrabComponentsRequired>());
            }

            if (Grabbables.Count > 0)
            {

                GUI.Label(new Rect(30, 200, 440, 440), "These gameobject need to be configured to use your grabbing interaction logic", "normallabel");

                Rect innerScrollSize = new Rect(30, 0, 420, Grabbables.Count * 30);
                dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 250, 440, 100), dynamicScrollPosition, innerScrollSize, false, true);

                Rect dynamicrect;
                for (int i = 0; i < Grabbables.Count; i++)
                {
                    dynamicrect = new Rect(30, i * 30, 100, 30);
                    string background = (i % 2 == 0) ? "dynamicentry_even" : "dynamicentry_odd";
                    if (GUILayout.Button(Grabbables[i].gameObject.name, background))
                    {
                        Selection.activeGameObject = Grabbables[i].gameObject;
                    }
                }

                GUI.EndScrollView();

                GUI.Box(new Rect(30, 250, 425, 100), "", "box_sharp_alpha");

                if (GUI.Button(new Rect(30, 360, 440, 35), "Select All", "button"))
                {
                    Selection.objects = Grabbables.ToArray();
                }
                //GUI.Box(new Rect(30, 360, 140, 35), "", "box_sharp_alpha");

                /*
                //a couple horizontal buttons
                if (GUI.Button(new Rect( 30, 360, 140, 35), new GUIContent("Oculus", "OVRGrabbable script"), "button_disabledtext"))
                {
                    SetupOculus(Grabbables.ToArray());
                }
                GUI.Box(new Rect(30, 360, 140, 35), "", "box_sharp_alpha");
                if (GUI.Button(new Rect(180, 360, 140, 35), new GUIContent("SteamVR2", "Interactable and Interactable Example scripts"), "button_disabledtext"))
                {
                    SetupSteamVR2(Grabbables.ToArray());
                }
                GUI.Box(new Rect(180, 360, 140, 35), "", "box_sharp_alpha");
                if (GUI.Button(new Rect(330, 360, 140, 35), new GUIContent("UnityXR", "XRInteractable script"), "button_disabledtext"))
                {
                    SetupXRInteractionToolkit(Grabbables.ToArray());
                }
                GUI.Box(new Rect(330, 360, 140, 35), "", "box_sharp_alpha");*/

                GUI.Label(new Rect(30, 410, 440, 440), "There will be a test asking the user to pickup and examine a small cube", "normallabel");
            }
            else
            {
                GUI.Label(new Rect(30, 200, 440, 440), "There will be a test asking the user to pickup and examine a small cube", "normallabel");
            }
        }
    }

    AssessmentBase FindAssessment(bool eyetracking = false, bool roomscale = false, bool grabbable = false)
    {
        var all = GetAllAssessments();
        return all.Find(delegate (AssessmentBase obj)
        {
            if (eyetracking && obj.RequiresEyeTracking) { return true; }
            if (roomscale && obj.RequiresRoomScale) { return true; }
            if (grabbable && obj.RequiresGrabbing) { return true; }
            return false;
        });
    }

    void UpdateActiveAssessments()
    {
        var all = GetAllAssessments();
        foreach (var a in all)
            a.Active = true;
        
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
    }

    void CustomUpdate()
    {
        GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - CUSTOM", "steptitle");

        GUI.Label(boldlabelrect, "Add your own assessments - any VR interactions that the user might not understand, such as using tools or interacting with distant objects", "boldlabel");

        GUI.Label(new Rect(30,200,440,200), "Step 1: Create a new gameobject and add an assessment base script, or a script that inherits from that\n\n" +
            "Step 2: Add any required game objects as children. These will be disable on awake and enabled when this assessment begins\n\n" +
            "Step 3: Call <color=#8A9EB7FF>CompleteAssesment()</color> when the user has demonstrated understanding. This will disable child gameobjects", "normallabel");
    }

    SceneSelectMenu sceneSelect;
    void SceneMenuUpdate()
    {
        GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - SCENE MENU", "steptitle");

        GUI.Label(boldlabelrect, "Choose which scenes to display after completing the Ready Room", "boldlabel");

        if (sceneSelect == null)
            sceneSelect = FindObjectOfType<SceneSelectMenu>();
        if (sceneSelect == null)
        {
            //display warning
            GUI.Label(new Rect(0, 200, 475, 130), CognitiveVR.EditorCore.Alert, "image_centered");
            GUI.Label(new Rect(30, 300, 440, 440), "There is no Scene Select Menu in this scene. Make sure the user can correctly exit Ready Room when completed", "normallabel");
        }
        else
        {
            Rect dropArea = new Rect(30, 150, 440, 100);
            Rect dropLabelArea = new Rect(60, 150, 380, 100);

            GUI.Box(dropArea, "", "box_sharp_alpha");

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
                            sceneSelect.SceneInfos.Add(new SceneInfo() { ScenePath = path, DisplayName = ObjectNames.NicifyVariableName(filename) });
                        }
                    }
                }
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            GUI.Label(dropArea, "Drag and Drop Scene Assets here to add to the Scene Select Menu", "ghostlabel");



            Rect innerScrollSize = new Rect(30, 0, 420, sceneSelect.SceneInfos.Count * 30);
            dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 280, 440, 140), dynamicScrollPosition, innerScrollSize, false, true);

            Rect dynamicrect;
            for (int i = 0; i < sceneSelect.SceneInfos.Count; i++)
            {
                dynamicrect = new Rect(30, i * 30, 425, 30);
                bool darkBackground = (i % 2 == 0);
                DrawSceneInfo(dynamicrect, darkBackground, sceneSelect.SceneInfos[i], sceneSelect);
            }
            GUI.EndScrollView();
            Repaint();

            GUI.Box(new Rect(30, 280, 425, 140), "", "box_sharp_alpha");
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
            }
        }

        /*if ()
        {
            Selection.activeGameObject = sceneSelect.gameObject;
        }*/
        GUI.Label(dynamicrect, sceneInfo.DisplayName, background);
        if (string.IsNullOrEmpty(sceneInfo.ScenePath))
        {
            Rect warningRect = dynamicrect;
            warningRect.x = 50;
            warningRect.width = 30;
            GUI.Label(warningRect, new GUIContent(CognitiveVR.EditorCore.Alert,"Missing Scene Path"), "image_centered");
        }

        if (e.mousePosition.x < dynamicrect.x || e.mousePosition.x > dynamicrect.x + dynamicrect.width || e.mousePosition.y < dynamicrect.y || e.mousePosition.y > dynamicrect.y + dynamicrect.height)
        {
        }
        else
        {
            //draw up/down arrows
            float height = dynamicrect.height / 2;
            float offset = dynamicrect.height / 2;
            Rect up = new Rect(437, dynamicrect.y, 18, height);
            Rect down = new Rect(437, dynamicrect.y + offset, 18, height);

            if (GUI.Button(up, "^"))
            {
                Debug.Log("move " + sceneInfo.DisplayName + " up ");
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
                Debug.Log("move " + sceneInfo.DisplayName + " down");
                var all = sceneSelect.SceneInfos;
                int index = all.IndexOf(sceneInfo);
                if (index < all.Count - 1)
                {
                    all.Remove(sceneInfo);
                    all.Insert(index + 1, sceneInfo);
                }
            }

            //draw x button
            Rect removeButtonRect = new Rect(30, dynamicrect.y, 18, dynamicrect.height);
            if (GUI.Button(removeButtonRect, "X"))
            {
                Debug.Log("remove " + sceneInfo.DisplayName + " from scene infos");
                sceneSelect.SceneInfos.Remove(sceneInfo);
            }
        }
    }

    void OverviewUpdate()
    {
        var all = GetAllAssessments();

        GUI.Label(steptitlerect, "STEP " + (currentPage + 1) + " - OVERVIEW", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "These are the <color=#8A9EB7FF>Assessments</color> currently found in the scene.", "boldlabel");
        
        Rect enabled = new Rect(40, 95, 120, 30);
        GUI.Label(enabled, "Active", "dynamicheader");

        Rect gameobject = new Rect(90, 95, 120, 30);
        GUI.Label(gameobject, "GameObject", "dynamicheader");

        if (all.Count == 0)
        {
            GUI.Label(new Rect(30, 120, 420, 270), "No objects found.\n\nHave you attached any Dynamic Object components to objects?\n\nAre they active in your hierarchy?", "button_disabledtext");
        }

        Rect innerScrollSize = new Rect(30, 0, 420, all.Count * 30);
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 280), dynamicScrollPosition, innerScrollSize, false, true);

        Rect dynamicrect;
        for (int i = 0; i < all.Count; i++)
        {
            //if (all[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, i * 30, 460, 30);
            DrawAssessment(all[i], dynamicrect, i % 2 == 0);
        }
        GUI.EndScrollView();
        GUI.Box(new Rect(30, 120, 425, 280), "", "box_sharp_alpha");
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

        if (Selection.activeTransform == assessment.transform)
        {
            GUI.Box(rect, "", "box_sharp_alpha");
        }

        float height = rect.height / 2;
        float offset = rect.height / 2;

        Rect up = new Rect(rect.x + 407, rect.y, 18, height);
        Rect down = new Rect(rect.x + 407, rect.y + offset, 18, height);

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

        Rect isActive = new Rect(rect.x + 10, rect.y, 24, rect.height);
        Rect gameobject = new Rect(rect.x + 60, rect.y, 220, rect.height);

        if (assessment.Active)
        {
            GUI.Label(isActive, CognitiveVR.EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(isActive, CognitiveVR.EditorCore.EmptyCheckmark, "image_centered");
        }
        
        if (needsRefresh)
        {
            var all = GetAllAssessments();
            for (int i = 0; i < all.Count ; i++)
            {
                all[i].Order = i;
            }
        }

        GUI.Label(gameobject, assessment.gameObject.name, "dynamiclabel");
    }

    void DrawFooter()
    {
        GUI.color = CognitiveVR.EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        Rect backbuttonrect = new Rect(320, 510, 80, 30);
        if (GUI.Button(backbuttonrect,"Back", "button_disabled"))
        {
            currentPage = Mathf.Max(0, currentPage - 1);
            RefreshAssessments();
            Grabbables = null;
        }

        if (currentPage == pageids.Count -1)
        {
            //last page
            Rect nextbuttonrect = new Rect(410, 510, 80, 30);
            GUI.Button(nextbuttonrect, "Next", "button_disabled");
        }
        else
        {
            Rect nextbuttonrect = new Rect(410, 510, 80, 30);
            string style = "button";

            //, "room scale", "grab components"
            if (pageids[currentPage] == "eye tracking")
            {
                if (UseEyeTracking == -1)
                    style = "button_disabled";
            }
            if (pageids[currentPage] == "room scale")
            {
                if (UseRoomScale == -1)
                    style = "button_disabled";
            }
            if (pageids[currentPage] == "grab components")
            {
                if (UseGrabbableObjects == -1)
                    style = "button_disabled";
            }

            if (GUI.Button(nextbuttonrect, "Next", style))
            {
                currentPage = Mathf.Min(pageids.Count - 1, currentPage + 1);
                RefreshAssessments();
                Grabbables = null;
            }
        }
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (GetAllAssessments() == null || GetAllAssessments().Count == 0)
            RefreshAssessments();
        var all = GetAllAssessments();

        List<Vector3> assessmentPoints = new List<Vector3>();
        foreach(var v in all)
        {
            if (v == null) return;
            assessmentPoints.Add(v.transform.position);
        }
        Handles.DrawPolyLine(assessmentPoints.ToArray());
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
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
}
