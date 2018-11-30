using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

namespace CognitiveVR
{
public class InitWizard : EditorWindow
{
    Rect steptitlerect = new Rect(30, 0, 100, 440);
    Rect boldlabelrect = new Rect(30, 100, 440, 440);

    public static void Init()
    {
        InitWizard window = (InitWizard)EditorWindow.GetWindow(typeof(InitWizard), true, "");
        window.minSize = new Vector2(500, 550);
        window.maxSize = new Vector2(500, 550);
        window.Show();

        window.LoadKeys(); 
        window.selectedExportQuality = ExportSettings.HighSettings;

        window.GetSelectedSDKs();

        CognitiveVR_SceneExportWindow.ClearUploadSceneSettings();
    }

    List<string> pageids = new List<string>() { "welcome", "authenticate","selectsdk", "explainscene", "explaindynamic", "setupcontrollers", "listdynamics", "uploadscene", /*"upload",*/ "uploadsummary", "done" };
    public int currentPage;

    private void OnGUI()
    {
        GUI.skin = EditorCore.WizardGUISkin;
        GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);
        
        //if (Event.current.keyCode == KeyCode.Equals && Event.current.type == EventType.keyDown) { currentPage++; }
        //if (Event.current.keyCode == KeyCode.Minus && Event.current.type == EventType.keyDown) { currentPage--; }
        switch (pageids[currentPage])
        {
            case "welcome":WelcomeUpdate(); break;
            case "authenticate": AuthenticateUpdate(); break;
            case "selectsdk": SelectSDKUpdate(); break;
            case "explainscene": SceneExplainUpdate(); break;
            case "explaindynamic": DynamicExplainUpdate(); break;
            case "setupcontrollers": ControllerUpdate(); break;
            case "listdynamics": ListDynamicUpdate(); break;
            case "uploadscene": UploadSceneUpdate(); break;
            case "upload": UploadUpdate(); break;
            case "uploadsummary": UploadSummaryUpdate(); break;
            case "done": DoneUpdate(); break;
        }

        DrawFooter();
        Repaint(); //manually repaint gui each frame to make sure it's responsive
    }

    void WelcomeUpdate()
    {
        GUI.Label(steptitlerect, "STEP 1 - WELCOME", "steptitle");

        var settings = CognitiveVR_Preferences.FindCurrentScene();
        if (settings != null && !string.IsNullOrEmpty(settings.SceneId))
        {
            //upload new version
            GUI.Label(boldlabelrect, "Welcome to the " + EditorCore.DisplayValue(DisplayKey.FullName) + " SDK Scene Setup.", "boldlabel");
            GUI.Label(new Rect(0, 140, 475, 130), EditorCore.Alert, "image_centered");
            GUI.Label(new Rect(30, 140, 440, 440), "This will guide you through the initial setup of your scene, and will have production ready analytics at the end of this setup.\n\n\n\n"+
                "<color=#8A9EB7FF>This scene has already been uploaded to " + EditorCore.DisplayValue(DisplayKey.ViewerName) + "!</color> Unless there are meaningful changes to the static scene geometry you probably don't need to upload this scene again.\n\n" +
                "Use <color=#8A9EB7FF>Manage Dynamic Objects</color> if you want to upload new Dynamic Objects to your existing scene.", "normallabel");
        }
        else
        {
            GUI.Label(boldlabelrect, "Welcome to the " + EditorCore.DisplayValue(DisplayKey.FullName) + " SDK Scene Setup.", "boldlabel");
            GUI.Label(new Rect(30, 200, 440, 440), "This will guide you through the initial setup of your scene, and will have production ready analytics at the end of this setup.", "normallabel");
        }
    }

    #region Auth Keys

    string apikey ="";
    string developerkey = "";
    void AuthenticateUpdate()
    {
        GUI.Label(steptitlerect, "STEP 2 - AUTHENTICATION", "steptitle");
        GUI.Label(boldlabelrect, "Please add your "+EditorCore.DisplayValue(DisplayKey.ShortName)+" authorization keys below to continue.\n\nThese are available on the Project Dashboard.", "boldlabel");

        //dev key
        GUI.Label(new Rect(30, 250, 100, 30), "Developer Key", "miniheader");
        developerkey = EditorCore.TextField(new Rect(30, 280, 400, 40), developerkey, 32);
        if (string.IsNullOrEmpty(developerkey))
        {
            GUI.Label(new Rect(30, 280, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
        }
        else
        {
            GUI.Label(new Rect(440, 280, 24, 40), EditorCore.Checkmark, "image_centered");
        }

        //api key
        GUI.Label(new Rect(30, 350, 100, 30), "Application Key", "miniheader");
        apikey = EditorCore.TextField(new Rect(30, 380, 400, 40), apikey, 32);
        if (string.IsNullOrEmpty(apikey))
        {
            GUI.Label(new Rect(30, 380, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
        }
        else
        {
            GUI.Label(new Rect(440, 380, 24, 40), EditorCore.Checkmark, "image_centered");
        }

    }

    void SaveKeys()
    {
        EditorPrefs.SetString("developerkey", developerkey);
        EditorCore.GetPreferences().APIKey = apikey;

        EditorUtility.SetDirty(EditorCore.GetPreferences());
        AssetDatabase.SaveAssets();
    }

    //write gateway/dashboard/viewer urls to preferences
    void ApplyBrandingUrls()
    {
        if (!string.IsNullOrEmpty(EditorCore.DisplayValue(DisplayKey.GatewayURL)))
        {
            EditorCore.GetPreferences().Gateway = EditorCore.DisplayValue(DisplayKey.GatewayURL);
        }
        if (!string.IsNullOrEmpty(EditorCore.DisplayValue(DisplayKey.DashboardURL)))
        {
            EditorCore.GetPreferences().Dashboard = EditorCore.DisplayValue(DisplayKey.DashboardURL);
        }
        if (!string.IsNullOrEmpty(EditorCore.DisplayValue(DisplayKey.ViewerURL)))
        {
            EditorCore.GetPreferences().Viewer = EditorCore.DisplayValue(DisplayKey.ViewerURL);
        }
        if (!string.IsNullOrEmpty(EditorCore.DisplayValue(DisplayKey.DocumentationURL)))
        {
            EditorCore.GetPreferences().Documentation = EditorCore.DisplayValue(DisplayKey.DocumentationURL);
        }
    }

    void LoadKeys()
    {
        developerkey = EditorPrefs.GetString("developerkey");
        apikey = EditorCore.GetPreferences().APIKey;
        if (apikey == null)
        {
            apikey = "";
        }
    }

        #endregion

        void GetSelectedSDKs()
        {
            selectedsdks.Clear();
#if CVR_STEAMVR
            selectedsdks.Add("CVR_STEAMVR");
#endif
#if CVR_STEAMVR2
            selectedsdks.Add("CVR_STEAMVR2");
#endif
#if CVR_OCULUS
            selectedsdks.Add("CVR_OCULUS");
#endif
#if CVR_GOOGLEVR
            selectedsdks.Add("CVR_GOOGLEVR");
#endif
#if CVR_DEFAULT
            selectedsdks.Add("CVR_DEFAULT");
#endif
#if CVR_FOVE
            selectedsdks.Add("CVR_FOVE");
#endif
#if CVR_PUPIL
            selectedsdks.Add("CVR_PUPIL");
#endif
#if CVR_TOBIIVR
        selectedsdks.Add("CVR_TOBIIVR");
#endif
#if CVR_AH
        selectedsdks.Add("CVR_AH");
#endif
#if CVR_ARKIT //apple
            selectedsdks.Add("CVR_ARKIT");
#endif
#if CVR_ARCORE //google
            selectedsdks.Add("CVR_ARCORE");
#endif
#if CVR_META
            selectedsdks.Add("CVR_META");
#endif
#if CVR_NEURABLE
            selectedsdks.Add("CVR_NEURABLE");
#endif
#if CVR_SNAPDRAGON
            selectedsdks.Add("CVR_SNAPDRAGON");
#endif
        }

        Vector2 sdkScrollPos;
        List<string> selectedsdks = new List<string>();
    void SelectSDKUpdate()
    {
        GUI.Label(steptitlerect, "STEP 3 - SELECT SDK", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "Please select the hardware SDK you will be including in this project.", "boldlabel");

        List<string> sdknames = new List<string>() { "Unity Default", "Oculus SDK 1.30", "SteamVR SDK 1.2", "SteamVR SDK 2.0", "Fove SDK 2.1.1 (eye tracking)", "Pupil Labs SDK 0.5.1 (eye tracking)", "Tobii Pro VR (eye tracking)", "Adhawk Microsystems SDK (eye tracking)", "ARCore SDK (Android)", "ARKit SDK (iOS)", "Hololens SDK", "Meta 2", "Neurable 1.4","SnapdragonVR SDK" };
        List<string> sdkdefines = new List<string>() { "CVR_DEFAULT", "CVR_OCULUS", "CVR_STEAMVR", "CVR_STEAMVR2", "CVR_FOVE", "CVR_PUPIL", "CVR_TOBIIVR", "CVR_AH", "CVR_ARCORE", "CVR_ARKIT", "CVR_HOLOLENS", "CVR_META", "CVR_NEURABLE", "CVR_SNAPDRAGON" };

        Rect innerScrollSize = new Rect(30, 0, 420, sdknames.Count * 32);
        sdkScrollPos = GUI.BeginScrollView(new Rect(30, 120, 440, 340), sdkScrollPos, innerScrollSize, false, true);

        for (int i = 0;i <sdknames.Count;i++)
        {
            bool selected = selectedsdks.Contains(sdkdefines[i]);
            if (GUI.Button(new Rect(30, i * 32, 420, 30), sdknames[i], selected ? "button_blueoutlineleft" : "button_disabledoutline"))
            {
                if (selected)
                {
                    selectedsdks.Remove(sdkdefines[i]);
                }
                else
                {
                    if (Event.current.shift) //add
                    {
                        selectedsdks.Add(sdkdefines[i]);
                    }
                    else //set
                    {
                        selectedsdks.Clear();
                        selectedsdks.Add(sdkdefines[i]);
                    }
                }
            }
            GUI.Label(new Rect(420, i * 32, 24, 30), selected ? EditorCore.Checkmark : EditorCore.EmptyCheckmark, "image_centered");
        }

        GUI.EndScrollView();
    }

    #region Terminology

    void DynamicExplainUpdate()
    {
        GUI.Label(steptitlerect, "STEP 4b - WHAT IS A DYNAMIC OBJECT?", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "A <color=#8A9EB7FF>Dynamic Object </color> is an object that moves around during an experience which you wish to track.", "boldlabel");

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

        GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsHightlight, "image_centered");

        GUI.color = Color.white;

        GUI.Label(new Rect(30, 350, 440, 440), "You can add or remove Dynamic Objects without uploading a new Scene Version.\n\nYou must attach a Dynamic Object Component onto each object you wish to track in your project. These objects must also have colliders attached so we can track user gaze.", "normallabel");
        }

    void SceneExplainUpdate()
    {
        GUI.Label(steptitlerect, "STEP 4a - WHAT IS A SCENE?", "steptitle");

        //GUI.Label(new Rect(30, 45, 440, 440), "A <color=#8A9EB7FF>Scene</color> is the base geometry of your level. A scene does not require colliders on it to detect user gaze.", "boldlabel");
        GUI.Label(new Rect(30, 45, 440, 440), "A <color=#8A9EB7FF>Scene</color> is an approximation of your Unity scene and is uploaded to the Dashboard. It is all the non-moving and non-interactive things.", "boldlabel");
        

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

        GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);
        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneHighlight, "image_centered");
        GUI.color = Color.white;
        
        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

        //GUI.Label(new Rect(30, 350, 440, 440), "The Scene will be uploaded in one large step, and can be updated at a later date, resulting in a new Scene Version.", "normallabel");
        GUI.Label(new Rect(30, 350, 440, 440), "This will provide context to the data collected in your experience.\n\nIf you decide to change the scene in your Unity project (such as moving a wall), the data you collect may no longer represent your experience. You can upload a new Scene Version by running this setup again.", "normallabel");
    }

        #endregion

        #region Controllers

        GameObject cameraBase;
        GameObject leftcontroller;
        GameObject rightcontroller;

        void ControllerUpdate()
        {
            GUI.Label(steptitlerect, "STEP 5 - CONTROLLER SETUP", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "Dynamic Objects can also easily track your controllers. Please check that the GameObjects below are the controllers you are using.\n\nThen press <color=#8A9EB7FF>Setup Controller Dynamics</color>, or you can skip this step by pressing <color=#8A9EB7FF>Next</color>.", "normallabel");

            bool setupComplete = false;
            bool leftSetupComplete = false;
            bool rightSetupComplete = false;

#if CVR_STEAMVR
            if (cameraBase == null)
            {
                //basic setup
                var manager = FindObjectOfType<SteamVR_ControllerManager>();
                if (manager != null)
                {
                    cameraBase = manager.gameObject;
                    leftcontroller = manager.left;
                    rightcontroller = manager.right;
                }
                else
                {
                    //interaction system setup
                    var player = FindObjectOfType<Valve.VR.InteractionSystem.Player>();
                    if (player)
                    {
                        leftcontroller = player.hands[0].gameObject;
                        rightcontroller = player.hands[1].gameObject;
                    }
                }
            }

            if (leftcontroller != null)
            {
                var dyn = leftcontroller.GetComponent<DynamicObject>();
                if (dyn != null && dyn.CommonMesh == DynamicObject.CommonDynamicMesh.ViveController && dyn.UseCustomMesh == false && leftcontroller.GetComponent<ControllerInputTracker>() != null)
                {
                    leftSetupComplete = true;
                }
            }
            if (rightcontroller != null)
            {
                var dyn = rightcontroller.GetComponent<DynamicObject>();
                if (dyn != null && dyn.CommonMesh == DynamicObject.CommonDynamicMesh.ViveController && dyn.UseCustomMesh == false && rightcontroller.GetComponent<ControllerInputTracker>() != null)
                {
                    rightSetupComplete = true;
                }
            }
            if (rightSetupComplete && leftSetupComplete)
            {
                setupComplete = true;
            }

#elif CVR_OCULUS
            //GUI.Label(new Rect(30, 45, 440, 440), "looks like oculus", "boldlabel");
            if (cameraBase == null)
            {
                //basic setup
                var manager = FindObjectOfType<OVRCameraRig>();
                if (manager != null)
                {
                    cameraBase = manager.gameObject;
                    leftcontroller = manager.leftHandAnchor.gameObject;
                    rightcontroller = manager.rightHandAnchor.gameObject;
                }
            }

            if (leftcontroller != null)
            {
                var dyn = leftcontroller.GetComponent<DynamicObject>();
                if (dyn != null && dyn.CommonMesh == DynamicObject.CommonDynamicMesh.OculusTouchLeft && dyn.UseCustomMesh == false)
                {
                    leftSetupComplete = true;
                }
            }
            if (rightcontroller != null)
            {
                var dyn = rightcontroller.GetComponent<DynamicObject>();
                if (dyn != null && dyn.CommonMesh == DynamicObject.CommonDynamicMesh.OculusTouchRight && dyn.UseCustomMesh == false)
                {
                    rightSetupComplete = true;
                }
            }

            if (rightSetupComplete && leftSetupComplete)
            {
                var manager = FindObjectOfType<ControllerInputTracker>();
                if (manager.LeftHand == leftcontroller.GetComponent<DynamicObject>() && manager.RightHand == rightcontroller.GetComponent<DynamicObject>())
                {
                    setupComplete = true;
                }
            }

#else
            //TODO add support for this stuff
            //hand motion stuff (hololens, meta, leapmotion, magicleap)
            //ar stuff (arkit, arcore)
            //other oculus stuff (gear, go, quest_touch)
            //magic leap, snapdragon, daydream
            GUI.Label(new Rect(30, 245, 440, 30), "We do not automatically support input tracking for the selected SDK at this time.", "boldlabel");
            return;
#endif

            //left hand label
            GUI.Label(new Rect(30, 245, 50, 30), "Left", "boldlabel");

            string leftname = "null";
            if (leftcontroller != null)
                leftname = leftcontroller.gameObject.name;
            if(GUI.Button(new Rect(80, 245, 290, 30), leftname, "button_blueoutline"))
            {
                Selection.activeGameObject = leftcontroller;
            }

            int pickerID = 5689465;
            if (GUI.Button(new Rect(370, 245, 100, 30), "Select..."))
            {
                GUI.skin = null;
                EditorGUIUtility.ShowObjectPicker<GameObject>(
                    leftcontroller, true, "", pickerID);
                GUI.skin = EditorCore.WizardGUISkin;
            }
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == pickerID)
                {
                    leftcontroller = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                    Debug.Log("selected " + leftcontroller.name);
                }
            }

            if (leftSetupComplete)
            {
                GUI.Label(new Rect(320, 245, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(320, 245, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            //right hand label
            GUI.Label(new Rect(30, 285, 50, 30), "Right", "boldlabel");

            string rightname = "null";
            if (rightcontroller != null)
                rightname = rightcontroller.gameObject.name;

            if (GUI.Button(new Rect(80, 285, 290, 30), rightname, "button_blueoutline"))
            {
                Selection.activeGameObject = rightcontroller;
            }

            pickerID = 5689469;
            if (GUI.Button(new Rect(370, 285, 100, 30), "Select..."))
            {
                GUI.skin = null;
                EditorGUIUtility.ShowObjectPicker<GameObject>(
                    rightcontroller, true, "", pickerID);
                GUI.skin = EditorCore.WizardGUISkin;
            }
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == pickerID)
                {
                    rightcontroller = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                    Debug.Log("selected " + rightcontroller.name);
                }
            }

            if (rightSetupComplete)
            {
                GUI.Label(new Rect(320, 285, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(320, 285, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            //drag and drop
            if (new Rect(30, 285, 440, 30).Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.dragPerform)
                {
                    rightcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(30, 245, 440, 30).Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.dragPerform)
                {
                    leftcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }

            if (GUI.Button(new Rect(125, 400, 250, 30), "Setup Controller Dynamics"))
            {
                SetupControllers(leftcontroller, rightcontroller);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Event.current.Use();
            }

            if (setupComplete)
            {
                GUI.Label(new Rect(360, 400, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(360, 400, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }
        }

        void SetupControllers(GameObject left, GameObject right)
        {
            Debug.Log("setup controllers");

            if (left != null && left.GetComponent<DynamicObject>() == null)
            {
                left.AddComponent<DynamicObject>();
            }
            if (right != null && right.GetComponent<DynamicObject>() == null)
            {
                right.AddComponent<DynamicObject>();
            }

#if CVR_STEAMVR
            
            if (left != null && left.GetComponent<ControllerInputTracker>() == null)
            {
                left.AddComponent<ControllerInputTracker>();
            }
            if (right != null && right.GetComponent<ControllerInputTracker>() == null)
            {
                right.AddComponent<ControllerInputTracker>();
            }

            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveController;
                dyn.TrackGaze = false;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveController;
                dyn.TrackGaze = false;
            }
#elif CVR_OCULUS
            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusTouchLeft;
                dyn.TrackGaze = false;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusTouchRight;
                dyn.TrackGaze = false;
            }

            if (cameraBase != null)
            {
                //add controller tracker to camera base
                var tracker = cameraBase.AddComponent<ControllerInputTracker>();
                if (left != null)
                    tracker.LeftHand = left.GetComponent<DynamicObject>();
                if (right != null)
                    tracker.RightHand = right.GetComponent<DynamicObject>();
            }
            else
            {
                var trackergo = new GameObject("Controller Tracker");
                var tracker = trackergo.AddComponent<ControllerInputTracker>();
                if (left != null)
                    tracker.LeftHand = left.GetComponent<DynamicObject>();
                if (right != null)
                    tracker.RightHand = right.GetComponent<DynamicObject>();
            }
#endif
        }

        #endregion

        #region Dynamic Objects

        Vector2 dynamicScrollPosition;

    DynamicObject[] _cachedDynamics;
    DynamicObject[] GetDynamicObjects { get { if (_cachedDynamics == null || _cachedDynamics.Length == 0) { _cachedDynamics = FindObjectsOfType<DynamicObject>(); } return _cachedDynamics; } }

    private void OnFocus()
    {
        RefreshSceneDynamics();
        EditorCore.ExportedDynamicObjects = null; //force refresh
        GetSelectedSDKs();
    }
    
    void RefreshSceneDynamics()
    {
        _cachedDynamics = FindObjectsOfType<DynamicObject>();
    }

    int delayDisplayUploading = -1;
    void ListDynamicUpdate()
    {
        GUI.Label(steptitlerect, "STEP 6 - PREPARE DYNAMIC OBJECTS", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "These are the active <color=#8A9EB7FF>Dynamic Object components</color> currently found in your scene.", "boldlabel");

        Rect mesh = new Rect(30, 95, 120, 30);
        GUI.Label(mesh, "Dynamic Mesh Name", "dynamicheader");
        Rect gameobject = new Rect(190, 95, 120, 30);
        GUI.Label(gameobject, "GameObject", "dynamicheader");
        Rect uploaded = new Rect(380, 95, 120, 30);
        GUI.Label(uploaded, "Uploaded", "dynamicheader");

        DynamicObject[] tempdynamics = GetDynamicObjects;


        if (tempdynamics.Length == 0)
        {
            GUI.Label(new Rect(30, 120, 420, 270), "No objects found.\n\nHave you attached any Dynamic Object components to objects?\n\nAre they active in your hierarchy?","button_disabledtext");
        }

        Rect innerScrollSize = new Rect(30, 0, 420, tempdynamics.Length * 30);
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 320), dynamicScrollPosition, innerScrollSize,false,true);

        Rect dynamicrect;
        for (int i = 0; i< tempdynamics.Length;i++)
        {
            if (tempdynamics[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, i*30, 460, 30);
            DrawDynamicObject(tempdynamics[i], dynamicrect, i % 2 == 0);
        }

        GUI.EndScrollView();

        GUI.Box(new Rect(30, 120, 425, 320), "","box_sharp_alpha");
        if (delayDisplayUploading>0)
        {
            GUI.Button(new Rect(180, 450, 140, 40), "Preparing...", "button_bluetext"); //fake replacement for button
            delayDisplayUploading--;
        }
        else if (delayDisplayUploading == 0)
        {
            GUI.Button(new Rect(180, 450, 140, 40), "Preparing...", "button_bluetext"); //fake replacement for button
            CognitiveVR_SceneExportWindow.ExportAllDynamicsInScene();
            delayDisplayUploading--;
        }
        else
        {
            //GUI.Label(new Rect(180, 450, 140, 40), "", "button_blueoutline");
            if (GUI.Button(new Rect(180, 450, 140, 40), "Prepare All", "button_bluetext"))
            {
                delayDisplayUploading = 2;
            }
        }
    }

    //each row is 30 pixels
    void DrawDynamicObject(DynamicObject dynamic, Rect rect, bool darkbackground)
    {
        Event e = Event.current;
        if (e.isMouse && e.type == EventType.mouseDown)
        {
            if (e.mousePosition.x < rect.x || e.mousePosition.x > rect.x + rect.width || e.mousePosition.y < rect.y || e.mousePosition.y > rect.y + rect.height)
            {
            }
            else
            {
                if (e.shift) //add to selection
                {
                    GameObject[] gos = new GameObject[Selection.transforms.Length + 1];
                    Selection.gameObjects.CopyTo(gos, 0);
                    gos[gos.Length - 1] = dynamic.gameObject;
                    Selection.objects = gos;
                }
                else
                {
                    Selection.activeTransform = dynamic.transform;
                }
            }
        }

        if (darkbackground)
            GUI.Box(rect, "", "dynamicentry_even");
        else
            GUI.Box(rect, "", "dynamicentry_odd");
        Rect mesh = new Rect(rect.x + 10, rect.y, 120, rect.height);
        Rect gameobject = new Rect(rect.x + 160, rect.y, 120, rect.height);

        Rect collider = new Rect(rect.x + 320, rect.y, 24, rect.height);
        Rect uploaded = new Rect(rect.x + 360, rect.y, 24, rect.height);

        if (dynamic.UseCustomMesh)
            GUI.Label(mesh, dynamic.MeshName, "dynamiclabel");
        else
            GUI.Label(mesh, dynamic.CommonMesh.ToString(), "dynamiclabel");
        GUI.Label(gameobject, dynamic.gameObject.name, "dynamiclabel");
        if (!dynamic.HasCollider())
        {
            GUI.Label(collider, new GUIContent(EditorCore.Alert,"Tracking Gaze requires a collider"), "image_centered");
        }

        if (EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.MeshName) || !dynamic.UseCustomMesh)
        {
            GUI.Label(uploaded, EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(uploaded, EditorCore.EmptyCheckmark, "image_centered");
        }
        
    }

#endregion


    int qualityindex = 2; //0 low, 1 normal, 2 maximum
    ExportSettings selectedExportQuality;

    void UploadSceneUpdate()
    {
        GUI.Label(steptitlerect, "STEP 7 - PREPARE SCENE", "steptitle");


        //GUI.Label(new Rect(30, 45, 440, 440), "All geometry without a <color=#8A9EB7FF>Dynamic Object</color> component will be exported and uploaded to <color=#8A9EB7FF>" + EditorCore.DisplayValue(DisplayKey.ViewerName) + "</color>.", "boldlabel");
        GUI.Label(new Rect(30, 45, 440, 440), "The <color=#8A9EB7FF>Scene</color> will be exported and prepared from all geometry without a <color=#8A9EB7FF>Dynamic Object</color> component.", "boldlabel");

        GUI.Label(new Rect(30, 110, 440, 440), "You can reduce load times on the Dashboard by reducing scene geometry and textures. We can automatically do this using Blender. Blender is free and open source.", "normallabel");

        string selectBlender = "Select Blender.exe";
#if UNITY_EDITOR_OSX
        selectBlender = "Select Blender.app";
#endif
        GUI.Label(new Rect(30, 200, 100, 30), selectBlender, "miniheader");
        
        //GUI.Label(new Rect(130, 170, 30, 30), new GUIContent(EditorGUIUtility.FindTexture("d_console.infoicon.sml"), "Blender is used to reduce complex scene geometry. It is free and open source.\nDownload from Blender.org"),"image_centered");
        
        if (GUI.Button(new Rect(30, 230, 100, 30), new GUIContent("Website", "https://www.blender.org/"), "button"))
        {
            Application.OpenURL("https://www.blender.org/");
            //EditorCore.BlenderPath = EditorUtility.OpenFilePanel("Select Blender", string.IsNullOrEmpty(EditorCore.BlenderPath) ? "c:\\" : EditorCore.BlenderPath, "");
        }

        if (GUI.Button(new Rect(140, 230, 100, 30), "Browse...", "button"))
        {
            EditorCore.BlenderPath = EditorUtility.OpenFilePanel("Select Blender", string.IsNullOrEmpty(EditorCore.BlenderPath) ? "c:\\" : EditorCore.BlenderPath, "");
        }

        GUI.Label(new Rect(30,275,430,60), EditorCore.BlenderPath, "label_disabledtext");

        if (!EditorCore.IsBlenderPathValid)
        {
            qualityindex = -1;
        }
        else if (qualityindex < 0)
        {
            qualityindex = 2;
        }

        GUI.Label(new Rect(30, 320, 200, 30), "Scene Export Quality", "miniheader");

        if (GUI.Button(new Rect(30, 350, 140, 100), "Low\n\n", qualityindex == 0 ? "button_blueoutline" : "button_disabledtext"))
        {
            qualityindex = 0;
            selectedExportQuality = ExportSettings.LowSettings;
        }
        if (qualityindex == 0)
        {
            GUI.Label(new Rect(88, 355, 24, 100), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(88, 355, 24, 100), EditorCore.EmptyCheckmark, "image_centered");
            GUI.Box(new Rect(30, 350, 140, 100), "","box_sharp_alpha");
        }
            
        if (GUI.Button(new Rect(180, 350, 140, 100), "Medium\n\n", qualityindex == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            qualityindex = 1;
            selectedExportQuality = ExportSettings.DefaultSettings;
        }
        if (qualityindex == 1)
        {
            GUI.Label(new Rect(238, 355, 24, 100), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(238, 355, 24, 100), EditorCore.EmptyCheckmark, "image_centered");
            GUI.Box(new Rect(180, 350, 140, 100), "","box_sharp_alpha");
        }

        if (GUI.Button(new Rect(330, 350, 140, 100), "Maximum\n\n", qualityindex == 2 ? "button_blueoutline" : "button_disabledtext"))
        {
            qualityindex = 2;
            selectedExportQuality = ExportSettings.HighSettings;
        }
        if (qualityindex == 2)
        {
            GUI.Label(new Rect(388, 355, 24, 100), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(388, 355, 24, 100), EditorCore.EmptyCheckmark, "image_centered");
            GUI.Box(new Rect(330, 350, 140, 100), "","box_sharp_alpha");
        }

        
        //GUI.Label(new Rect(255, 465, 215, 30), "", "button_blueoutline"); //full
        //GUI.Label(new Rect(367, 465, 103, 30), "", "button_blueoutline"); //partial
        if (GUI.Button(new Rect(260, 460, 220, 40), "Augmented Reality?  Skip Scene Export", "miniheader"))
        {
            if  (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                if (EditorUtility.DisplayDialog("Export Failed", "Cannot export scene that is not saved.\n\nDo you want to save now?", "Save","Cancel"))
                {
                    if (UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                    {
                    }
                    else
                    {
                        return;//cancel from save scene window
                    }
                }
                else
                {
                    return;//cancel from 'do you want to save' popup
                }
            }
            CognitiveVR_SceneExportWindow.ExportSceneAR();
            CognitiveVR_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.SetDirty(EditorCore.GetPreferences());

            UnityEditor.AssetDatabase.SaveAssets();
            currentPage++;
        }

        /*if (EditorCore.HasSceneExportFiles(CognitiveVR_Preferences.FindCurrentScene()))
        {
            GUI.Label(new Rect(300, 400, 24, 40), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(300, 400, 24, 40), EditorCore.EmptyCheckmark, "image_centered");
        }*/
    }

    bool delayUnderstandButton = true;
    double understandRevealTime;

    void UploadUpdate()
    {
        if (delayUnderstandButton)
        {
            delayUnderstandButton = false;
            understandRevealTime = EditorApplication.timeSinceStartup + 3;
        }

        var settings = CognitiveVR_Preferences.FindCurrentScene();
        if (settings != null && !string.IsNullOrEmpty(settings.SceneId))
        {
            //upload new version
            GUI.Label(steptitlerect, "STEP 8 - UPLOAD", "steptitle");

            Color scene1color = Color.HSVToRGB(0.55f, 0.5f, 1);
            Color scene2color = Color.HSVToRGB(0.55f, 1f, 1);

            GUI.color = scene1color;
            GUI.Box(new Rect(100,40, 125, 125), EditorCore.SceneBackground, "image_centered");
            GUI.color = Color.white;

            GUI.Box(new Rect(100, 40, 125, 125), EditorCore.ObjectsBackground, "image_centered");

            GUI.color = scene2color;
            GUI.Box(new Rect(250, 40, 125, 125), EditorCore.SceneBackground, "image_centered");
            GUI.color = Color.white;

            GUI.Label(new Rect(30, 180, 440, 440), "In the final step, we will upload version <color=#62B4F3FF>" + (settings.VersionNumber+1)+ " </color>of the scene to <color=#8A9EB7FF>" + EditorCore.DisplayValue(DisplayKey.ViewerName) + "</color>.\n\n\n" +
                "This will archive the previous version <color=#62B4F3FF>" + (settings.VersionNumber) + " </color> of this scene. You will be prompted to copy the Dynamic Objects to the new version.\n\n\n" +
                "For <color=#8A9EB7FF>Dynamic Objects</color>, you will be able to continue editing those later in the <color=#8A9EB7FF>Manage Dynamic Objects</color> window.", "normallabel");
        }
        else
        {
            GUI.Label(steptitlerect, "STEP 7 - UPLOAD", "steptitle");
            GUI.Label(new Rect(30, 100, 440, 440), "In the final step, we will complete the upload process to our <color=#8A9EB7FF>" + EditorCore.DisplayValue(DisplayKey.ViewerName) + "</color> servers.\n\n\n" +
                //"After your Scene is uploaded, if you make changes to your scene, you may want to open this window again and upload a new version of the scene.\n\n\n" +
                "For <color=#8A9EB7FF>Dynamic Objects</color>, you will be able to continue editing those later in the <color=#8A9EB7FF>Manage Dynamic Objects</color> window.", "normallabel");
        }
    }

    void UploadSummaryUpdate()
    {
        if (delayUnderstandButton)
        {
            delayUnderstandButton = false;
            understandRevealTime = EditorApplication.timeSinceStartup + 3;
        }

        GUI.Label(steptitlerect, "STEP 9 - UPLOAD", "steptitle");
        GUI.Label(new Rect(30, 45, 440, 440), "Here is a final summary of what will be uploaded to <color=#8A9EB7FF>" + EditorCore.DisplayValue(DisplayKey.ViewerName) + "</color>:", "boldlabel");

        var settings = CognitiveVR_Preferences.FindCurrentScene();
        if (settings != null && !string.IsNullOrEmpty(settings.SceneId))
        {
            //has been uploaded. this is a new version
            int dynamicObjectCount = GetDynamicObjects.Length;
            string scenename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scenename))
            {
                scenename = "SCENE NOT SAVED";
            }
            string settingsname = "Maximum Quality";
            if (qualityindex == 0) { settingsname = "Low Quality"; }
            if (qualityindex == 1) { settingsname = "Medium Quality"; }
            GUI.Label(new Rect(30, 120, 440, 440), "You will be uploading a new version of <color=#62B4F3FF>" + scenename + "</color> with <color=#62B4F3FF>" + settingsname + "</color>. "+
            "Version " + settings.VersionNumber + " will be archived.", "label_disabledtext_large");

            GUI.Label(new Rect(30, 170, 440, 440), "You will be uploading <color=#62B4F3FF>" + dynamicObjectCount + "</color> Dynamic Objects", "label_disabledtext_large");
        }
        else
        {
            int dynamicObjectCount = GetDynamicObjects.Length;
            string scenename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scenename))
            {
                scenename = "SCENE NOT SAVED";
            }
            string settingsname = "Maximum Quality";
            if (qualityindex == 0) { settingsname = "Low Quality"; }
            if (qualityindex == 1) { settingsname = "Medium Quality"; }
            GUI.Label(new Rect(30, 120, 440, 440), "You will be uploading <color=#62B4F3FF>" + scenename + "</color> with <color=#62B4F3FF>" + settingsname + "</color>", "label_disabledtext_large");
            
            GUI.Label(new Rect(30, 170, 440, 440), "You will be uploading <color=#62B4F3FF>" + dynamicObjectCount + "</color> Dynamic Objects", "label_disabledtext_large");
        }
        GUI.Label(new Rect(30, 200, 440, 440), "The display image on the Dashboard will be this:", "label_disabledtext_large");

        var sceneRT = EditorCore.GetSceneRenderTexture();
        if (sceneRT != null)
            GUI.Box(new Rect(125, 230, 250, 250), sceneRT, "image_centeredboxed");

        //GUI.Label(new Rect(30, 390, 440, 440), "You can add <color=#8A9EB7FF>ExitPoll</color> surveys, update <color=#8A9EB7FF>Dynamic Objects</color>, and add user engagement scripts after this process is complete.", "normallabel");
    }

    void DoneUpdate()
    {
        GUI.Label(steptitlerect, "STEP 10 - DONE", "steptitle");
        GUI.Label(new Rect(30, 45, 440, 440), "That's it!\n\nThe <color=#8A9EB7FF>"+EditorCore.DisplayValue(DisplayKey.ManagerName)+"</color> in your scene will record user position, gaze and basic device information.\n\nYou can view sessions from the Dashboard.", "boldlabel");
        if (GUI.Button(new Rect(150,200,200,40),"Open Dashboard","button_bluetext"))
        {
            Application.OpenURL("https://" + CognitiveVR_Preferences.Instance.Dashboard);
        }

        GUI.Label(new Rect(30, 295, 440, 440), "-Want to ask users about their experience?\n-Need to add more Dynamic Objects?\n-Have some Sensors?\n-Tracking user's gaze on a video or image?\n-Multiplayer?\n", "boldlabel");
        if (GUI.Button(new Rect(150,420,200,40),"Open Documentation","button_bluetext"))
        {
            Application.OpenURL("https://" + CognitiveVR_Preferences.Instance.Documentation);
        }
    }

    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        DrawBackButton();

        if (pageids[currentPage] == "uploadscene")
        {
            Rect buttonrect = new Rect(350, 510, 140, 30);
            if (GUI.Button(buttonrect, "Prepare Scene"))
            {
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                {
                    if (EditorUtility.DisplayDialog("Export Failed", "Cannot export scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
                    {
                        if (UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                        {
                        }
                        else
                        {
                            return;//cancel from save scene window
                        }
                    }
                    else
                    {
                        return;//cancel from 'do you want to save' popup
                    }
                }
                CognitiveVR_SceneExportWindow.ExportScene(true, selectedExportQuality.ExportStaticOnly, selectedExportQuality.MinExportGeoSize, selectedExportQuality.TextureQuality, "companyname", selectedExportQuality.DiffuseTextureName);
                CognitiveVR_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorUtility.SetDirty(EditorCore.GetPreferences());

                UnityEditor.AssetDatabase.SaveAssets();
                currentPage++;
                EditorCore.RefreshSceneVersion(null);
            }
        }

        DrawNextButton();
    }

    void DrawNextButton()
    {
        bool buttonDisabled = false;
        bool appearDisabled = false; //used on dynamic upload page to skip step
        string text = "Next";
        System.Action onclick = () => currentPage++;
        Rect buttonrect = new Rect(410, 510, 80, 30);

        switch (pageids[currentPage])
        {
            case "welcome":
                break;
            case "authenticate":
                buttonrect = new Rect(350, 510, 140, 30);
                onclick += () => SaveKeys();
                onclick += () => ApplyBrandingUrls();
                buttonDisabled = apikey == null || apikey.Length == 0 || developerkey == null || developerkey.Length == 0;
                if (buttonDisabled)
                {
                    text = "Keys Required";
                }
                else
                {
                    text = "Next";
                }
                break;
            case "tagdynamics":
                break;
            case "selectsdk":
                onclick += () => EditorCore.SetPlayerDefine(selectedsdks);
                onclick += () =>
                {
                    var found = Object.FindObjectOfType<CognitiveVR_Manager>();
                    if (found == null) //add cognitivevr_manager
                    {
                        EditorCore.SpawnManager(EditorCore.DisplayValue(DisplayKey.ManagerName));
                    }
                };
                break;
            case "listdynamics":

                var dynamics = GetDynamicObjects;
                int dynamicsFromSceneExported=0;
                
                for(int i = 0;i <dynamics.Length;i++)
                {
                    if (EditorCore.GetExportedDynamicObjectNames().Contains(dynamics[i].MeshName) || !dynamics[i].UseCustomMesh)
                    {
                        dynamicsFromSceneExported++;
                    }
                }
                appearDisabled = dynamicsFromSceneExported != dynamics.Length;
                if (appearDisabled)
                {
                    onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without uploading all Dynamic Objects?", "Yes", "No")) { currentPage++; } };
                }
                if (dynamics.Length == 0 && dynamicsFromSceneExported == 0)
                {
                    text = "Skip Dynamics";
                }
                else
                {
                    text = dynamicsFromSceneExported + "/" + dynamics.Length + " Prepared";
                }
                buttonrect = new Rect(350, 510, 140, 30);
                break;
            case "uploadscene":
                //buttonDisabled = !EditorCore.HasSceneExportFiles(CognitiveVR_Preferences.FindCurrentScene());

                buttonrect = new Rect(1000, 1000, 100, 100);
                onclick = () => { Debug.Log("custom button"); };

                /*appearDisabled = !EditorCore.HasSceneExportFiles(CognitiveVR_Preferences.FindCurrentScene());
                if (appearDisabled)
                {
                    onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without exporting this scene?", "Yes", "No")) { currentPage++; } };
                }*/
                break;
            case "upload":
                onclick += () => EditorCore.RefreshSceneVersion(null);
                if (understandRevealTime > EditorApplication.timeSinceStartup)
                {
                    buttonDisabled = true;
                }
                text = "I understand, Continue";
                buttonrect = new Rect(290, 510, 200, 30);
                break;
            case "uploadsummary":

                System.Action completedmanifestupload = delegate ()
                {
                    CognitiveVR_SceneExportWindow.UploadAllDynamicObjects(true);
                    currentPage++;
                };

                //fifth upload manifest
                System.Action completedRefreshSceneVersion = delegate ()
                {
                    ManageDynamicObjects.UploadManifest(completedmanifestupload, completedmanifestupload);
                };

                //fourth upload dynamics
                System.Action completeSceneUpload = delegate () {
                    EditorCore.RefreshSceneVersion(completedRefreshSceneVersion); //likely completed in previous step, but just in case
                };

                //third upload scene
                System.Action completeScreenshot = delegate(){

                    CognitiveVR_Preferences.SceneSettings current = CognitiveVR_Preferences.FindCurrentScene();

                    if (current == null || string.IsNullOrEmpty(current.SceneId))
                    {
                        if (EditorUtility.DisplayDialog("Upload New Scene", "Upload " + current.SceneName + " to " + EditorCore.DisplayValue(DisplayKey.ViewerName) + "?", "Ok", "Cancel"))
                        {
                            //new scene
                            CognitiveVR_SceneExportWindow.UploadDecimatedScene(current, completeSceneUpload);
                        }
                    }
                    else
                    {
                        //new version
                        if (EditorUtility.DisplayDialog("Upload New Version", "Upload a new version of this existing scene? Will archive previous version", "Ok","Cancel"))
                        {
                            CognitiveVR_SceneExportWindow.UploadDecimatedScene(current, completeSceneUpload);
                        }
                    }
                };

                //second save screenshot
                System.Action completedRefreshSceneVersion1 = delegate ()
                {
                    EditorCore.SaveCurrentScreenshot(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
                };

                //first refresh scene version
                onclick = () =>
                {
                    if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) //scene not saved. "do want save?" popup
                    {
                        if (EditorUtility.DisplayDialog("Upload Failed", "Cannot upload scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
                        {
                            if (UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                            {
                                EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
                            }
                            else
                            {
                                return;//cancel from save scene window
                            }
                        }
                        else
                        {
                            return;//cancel from 'do you want to save' popup
                        }
                    }
                    else
                    {
                        EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
                    }
                };

                buttonDisabled = !EditorCore.HasSceneExportFolder(CognitiveVR_Preferences.FindCurrentScene());
                if (understandRevealTime > EditorApplication.timeSinceStartup && !buttonDisabled)
                {
                    buttonDisabled = true;
                }
                text = "Upload";
                break;
            case "done":
                onclick = () => Close();
                text = "Close";
                break;
        }

        if (appearDisabled)
        {
            if (GUI.Button(buttonrect, text, "button_disabled"))
            {
                onclick.Invoke();
            }
        }
        else if (buttonDisabled)
        {
            GUI.Button(buttonrect, text, "button_disabled");
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

    void DrawBackButton()
    {
        bool buttonDisabled = false;
        string text = "Back";
        System.Action onclick = () => currentPage--;
        Rect buttonrect = new Rect(320, 510, 80, 30);

        switch (pageids[currentPage])
        {
            case "welcome": buttonDisabled = true; break;
            case "authenticate":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(260, 510, 80, 30);
                break;
            case "listdynamics":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(260, 510, 80, 30);
                break;
            case "upload":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(200, 510, 80, 30);
                break;
            case "uploadscene":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(260, 510, 80, 30);
                break;
            case "uploadsummary":
                //buttonDisabled = true;
                //text = "Cancel";
                break;
            case "done":
                onclick = null;
                break;
        }

        if (buttonDisabled)
        {
            GUI.Button(buttonrect, text, "button_disabledtext");
        }
        else
        {
            if (GUI.Button(buttonrect, text, "button_disabled"))
            {
                if (onclick != null)
                    onclick.Invoke();
            }
        }
    }
}
}