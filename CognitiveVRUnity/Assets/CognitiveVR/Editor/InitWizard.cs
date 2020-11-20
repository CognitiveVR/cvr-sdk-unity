using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

#if CVR_STEAMVR2
using Valve.VR;
using System.IO;
using Valve.Newtonsoft.Json;
#endif

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
        window.GetSelectedSDKs();

        ExportUtility.ClearUploadSceneSettings();
    }
    
    List<string> pageids = new List<string>() { "welcome", "authenticate","selectsdk", "explainscene", "explaindynamic", "setupcontrollers", "listdynamics", "uploadscene", "uploadsummary", "done" };
    public int currentPage;

    static int lastDevKeyResponseCode = 0;
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
            //case "upload": UploadUpdate(); break;
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
        if (string.IsNullOrEmpty(developerkey)) //empty
        {
            GUI.Label(new Rect(30, 280, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
            GUI.Label(new Rect(440, 280, 24, 40), EditorCore.EmptyCheckmark, "image_centered");
            lastDevKeyResponseCode = 0;
            developerkey = EditorCore.TextField(new Rect(30, 280, 400, 40), developerkey, 32);
        }
        else if (lastDevKeyResponseCode == 200) //valid key
        {
            GUI.Label(new Rect(440, 280, 24, 40), EditorCore.Checkmark, "image_centered");
            string previous = developerkey;
            developerkey = EditorCore.TextField(new Rect(30, 280, 400, 40), developerkey, 32);
            if (previous != developerkey)
                lastDevKeyResponseCode = 0;
        }
        else if (lastDevKeyResponseCode == 0) //maybe valid key? needs to be checked
        {
            GUI.Label(new Rect(440, 280, 24, 40), new GUIContent(EditorCore.Question, "Not validated"), "image_centered");
            developerkey = EditorCore.TextField(new Rect(30, 280, 400, 40), developerkey, 32);
        }
        else //invalid key
        {
            GUI.Label(new Rect(440, 280, 24, 40), new GUIContent(EditorCore.Error, "Invalid or Expired"), "image_centered");
            string previous = developerkey;
            developerkey = EditorCore.TextField(new Rect(30, 280, 400, 40), developerkey, 32, "textfield_warning");
            if (previous != developerkey)
                lastDevKeyResponseCode = 0;
        }

        if (lastDevKeyResponseCode != 200 && lastDevKeyResponseCode != 0)
        {
            GUI.Label(new Rect(30, 325, 400, 30), "This Developer Key is invalid or expired. Please visit this project on our dashboard and " +
                "ensure you have a valid Developer Key. This is a requirement to upload or update any Scene or Dynamic Object. Developer Keys expire " +
                "automatically after 90 days.", "miniwarning");
        }

        //api key
        GUI.Label(new Rect(30, 360, 100, 30), "Application Key", "miniheader");
        apikey = EditorCore.TextField(new Rect(30, 390, 400, 40), apikey, 32);
        if (string.IsNullOrEmpty(apikey))
        {
            GUI.Label(new Rect(30, 390, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
            GUI.Label(new Rect(440, 390, 24, 40), EditorCore.EmptyCheckmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(440, 390, 24, 40), EditorCore.Checkmark, "image_centered");
        }

    }

    void SaveKeys()
    {
        EditorPrefs.SetString("developerkey", developerkey);
        EditorCore.GetPreferences().ApplicationKey = apikey;

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
        apikey = EditorCore.GetPreferences().ApplicationKey;
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
#if CVR_VIVEPROEYE
        selectedsdks.Add("CVR_VIVEPROEYE");
#endif
#if CVR_VIVEWAVE
        selectedsdks.Add("CVR_VIVEWAVE");
#endif
#if CVR_VARJO
        selectedsdks.Add("CVR_VARJO");
#endif
#if CVR_PICONEO2EYE
        selectedsdks.Add("CVR_PICONEO2EYE");
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
#if CVR_HOLOLENS
            selectedsdks.Add("CVR_HOLOLENS");
#endif
#if CVR_WINDOWSMR
            selectedsdks.Add("CVR_WINDOWSMR");
#endif
#if CVR_XR
            selectedsdks.Add("CVR_XR");
#endif
#if CVR_OMNICEPT
            selectedsdks.Add("CVR_OMNICEPT");
#endif
        }

        Vector2 sdkScrollPos;
        List<string> selectedsdks = new List<string>();
    void SelectSDKUpdate()
    {
        GUI.Label(steptitlerect, "STEP 3 - SELECT SDK", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "Please select the hardware SDK you will be including in this project.", "boldlabel");

        List<string> sdknames = new List<string>() { "Unity Default", "Unity XR", "HP Omnicept 1.6", "Oculus SDK 1.38", "SteamVR SDK 1.2", "SteamVR SDK 2.5.0", "Pupil Labs SDK 1.0 (eye tracking)", "Vive Pro Eye (eye tracking)", "Vive Wave 3.0.1", "Varjo 2.3 (eye tracking)", "Pico Neo 2 Eye 2.8.4 (eye tracking)", "Windows Mixed Reality", "Tobii XR 1.8.0.168 (eye tracking)", "Fove SDK 3.1.2 (eye tracking)", "ARCore SDK (Android)", "ARKit SDK (iOS)", "Hololens SDK", "Neurable 1.4", "SnapdragonVR 3.0.1 SDK" };
        List<string> sdkdefines = new List<string>() { "CVR_DEFAULT", "CVR_XR", "CVR_OMNICEPT", "CVR_OCULUS", "CVR_STEAMVR", "CVR_STEAMVR2", "CVR_PUPIL", "CVR_VIVEPROEYE", "CVR_VIVEWAVE", "CVR_VARJO", "CVR_PICONEO2EYE", "CVR_WINDOWSMR", "CVR_TOBIIVR", "CVR_FOVE", "CVR_ARCORE", "CVR_ARKIT", "CVR_HOLOLENS", "CVR_NEURABLE", "CVR_SNAPDRAGON" };
        //removed CVR_META and CVR_AH

        Rect innerScrollSize = new Rect(30, 0, 420, sdknames.Count * 32);
        sdkScrollPos = GUI.BeginScrollView(new Rect(30, 120, 440, 340), sdkScrollPos, innerScrollSize, false, true);

        for (int i = 0;i <sdknames.Count;i++)
        {
            bool selected = selectedsdks.Contains(sdkdefines[i]);
            GUIContent content = new GUIContent(sdknames[i]);

            if (sdkdefines[i] == "CVR_XR") content.tooltip = "requires 2019.4+\nrequires XR Legacy Input Helpers";
            if (sdkdefines[i] == "CVR_OMNICEPT") content.tooltip = "requires 2018+";
            if (GUI.Button(new Rect(30, i * 32, 420, 30), content, selected ? "button_blueoutlineleft" : "button_disabledoutline"))
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

        static GameObject cameraBase;
        static GameObject leftcontroller;
        static GameObject rightcontroller;

        static string controllerDisplayName; //used to set SE display

#if CVR_STEAMVR2
        bool steamvr2bindings = false;
        bool steamvr2actionset = false;
#endif

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

            leftSetupComplete = leftcontroller != null;
            rightSetupComplete = rightcontroller != null;

            if (rightSetupComplete && leftSetupComplete)
            {
                var rdyn = rightcontroller.GetComponent<DynamicObject>();
                if (rdyn != null && rdyn.CommonMesh == DynamicObject.CommonDynamicMesh.ViveController && rdyn.UseCustomMesh == false && rightcontroller.GetComponent<ControllerInputTracker>() != null)
                {
                    var ldyn = leftcontroller.GetComponent<DynamicObject>();
                    if (ldyn != null && ldyn.CommonMesh == DynamicObject.CommonDynamicMesh.ViveController && ldyn.UseCustomMesh == false && leftcontroller.GetComponent<ControllerInputTracker>() != null)
                    {
                        setupComplete = true;
                    }
                }
            }
#elif CVR_STEAMVR2
            if (cameraBase == null)
            {
                //interaction system setup
                var player = FindObjectOfType<Valve.VR.InteractionSystem.Player>();
                if (player)
                {
                    leftcontroller = player.hands[0].gameObject;
                    rightcontroller = player.hands[1].gameObject;
                }
            }

            leftSetupComplete = leftcontroller != null;
            rightSetupComplete = rightcontroller != null;

            if (rightSetupComplete && leftSetupComplete)
            {
                var rdyn = rightcontroller.GetComponent<DynamicObject>();
                if (rdyn != null && rdyn.CommonMesh == DynamicObject.CommonDynamicMesh.ViveController && rdyn.UseCustomMesh == false && rightcontroller.GetComponent<ControllerInputTracker>() != null)
                {
                    var ldyn = leftcontroller.GetComponent<DynamicObject>();
                    if (ldyn != null && ldyn.CommonMesh == DynamicObject.CommonDynamicMesh.ViveController && ldyn.UseCustomMesh == false && leftcontroller.GetComponent<ControllerInputTracker>() != null)
                    {
                        setupComplete = true;
                    }
                }
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

            leftSetupComplete = leftcontroller != null;
            rightSetupComplete = rightcontroller != null;

            if (rightSetupComplete && leftSetupComplete)
            {
                var manager = FindObjectOfType<ControllerInputTracker>();
                if (manager != null
                    && manager.LeftHand != null
                    && manager.LeftHand == leftcontroller.GetComponent<DynamicObject>()
                    && manager.RightHand != null
                    && manager.RightHand == rightcontroller.GetComponent<DynamicObject>())
                {
                    setupComplete = true;
                }
            }
#elif CVR_VIVEWAVE
            leftSetupComplete = leftcontroller != null;
            rightSetupComplete = rightcontroller != null;

            setupComplete = false;

            var g = Resources.Load<GameObject>("AdaptiveController");
            if (g != null)
            {
                var inputtracker = g.GetComponent<ControllerInputTracker>();
                if (inputtracker != null)
                {
                    setupComplete = true;
                }
            }
#elif CVR_WINDOWSMR
            leftSetupComplete = leftcontroller != null;
            rightSetupComplete = rightcontroller != null;

            setupComplete = false;
            if (rightSetupComplete && leftSetupComplete)
            {
                //add input tracker + left/right controllers set
                var tracker = CognitiveVR_Manager.Instance.GetComponent<ControllerInputTracker>();
                if (tracker != null && tracker.LeftHand.gameObject == leftcontroller && tracker.RightHand.gameObject == rightcontroller)
                {
                    setupComplete = true;
                }
            }
#elif CVR_PICONEO2EYE
            if (cameraBase == null)
            {
                //basic setup
                var manager = FindObjectOfType<Pvr_Controller>();
                if (manager != null)
                {
                    if (Camera.main != null)
                        cameraBase = Camera.main.gameObject;
                    if (manager.controller0 != null)
                        leftcontroller = manager.controller0;
                    if (manager.controller1 != null)
                        rightcontroller = manager.controller1;
                }
            }

            leftSetupComplete = leftcontroller != null;
            rightSetupComplete = rightcontroller != null;

            setupComplete = false;
            if (rightSetupComplete && leftSetupComplete)
            {
                //add input tracker + left/right controllers set
                var tracker = CognitiveVR_Manager.Instance.GetComponent<ControllerInputTracker>();
                if (tracker != null && tracker.LeftHand.gameObject == leftcontroller && tracker.RightHand.gameObject == rightcontroller)
                {
                    setupComplete = true;
                }
            }
#elif CVR_XR
            if (cameraBase == null && leftcontroller == null && rightcontroller == null)
            {
                cameraBase = Camera.main.gameObject;
                var tracked = FindObjectsOfType<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                foreach (var v in tracked)
                {
                    if (v.deviceType == UnityEngine.SpatialTracking.TrackedPoseDriver.DeviceType.GenericXRController)
                    {
                        if (v.poseSource == UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.LeftPose)
                        {
                            leftcontroller = v.gameObject;
                        }
                        if (v.poseSource == UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.RightPose)
                        {
                            rightcontroller = v.gameObject;
                        }
                    }
                }
            }

            leftSetupComplete = leftcontroller != null;
            rightSetupComplete = rightcontroller != null;

            setupComplete = false;
            if (rightSetupComplete && leftSetupComplete)
            {
                //add input tracker + left/right controllers set
                var tracker = CognitiveVR_Manager.Instance.GetComponent<ControllerInputTracker>();
                if (tracker != null
                        && tracker.LeftHand != null
                        && tracker.LeftHand.gameObject == leftcontroller
                        && tracker.RightHand != null
                        && tracker.RightHand.gameObject == rightcontroller)
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

            //what controllers do we support. add scroll list here!
            //vive, oculus, microsoft, pico

#pragma warning disable 162
            int offset = 0; //indicates how much vertical offset to add to setup features so controller selection has space
#pragma warning restore 162

#if CVR_XR
            offset = 80;

            List<string> controllerNames = new List<string>() { "Vive", "Oculus Rift", "Oculus Quest", "Windows MR","Pico Neo 2" };

            Rect innerScrollSize = new Rect(30, 0, 120, controllerNames.Count * 32);
            sdkScrollPos = GUI.BeginScrollView(new Rect(30, 180, 440, 128), sdkScrollPos, innerScrollSize, false, true);

            for (int i = 0; i < controllerNames.Count; i++)
            {
                bool selected = controllerDisplayName == controllerNames[i];
                if (GUI.Button(new Rect(30, i * 32, 420, 30), controllerNames[i], selected ? "button_blueoutlineleft" : "button_disabledoutline"))
                {
                    controllerDisplayName = controllerNames[i];
                }
                GUI.Label(new Rect(420, i * 32, 24, 30), selected ? EditorCore.Checkmark : EditorCore.EmptyCheckmark, "image_centered");
            }

            GUI.EndScrollView();

#endif //cvr_xr controller selection

            //left hand label
            GUI.Label(new Rect(30, 245 + offset, 50, 30), "Left", "boldlabel");

            string leftname = "null";
            if (leftcontroller != null)
                leftname = leftcontroller.gameObject.name;
            if(GUI.Button(new Rect(80, 245 + offset, 290, 30), leftname, "button_blueoutline"))
            {
                Selection.activeGameObject = leftcontroller;
            }

            int pickerID = 5689465;
            if (GUI.Button(new Rect(370, 245 + offset, 100, 30), "Select..."))
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
                GUI.Label(new Rect(320, 245 + offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(320, 245 + offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            //right hand label
            GUI.Label(new Rect(30, 285 + offset, 50, 30), "Right", "boldlabel");

            string rightname = "null";
            if (rightcontroller != null)
                rightname = rightcontroller.gameObject.name;

            if (GUI.Button(new Rect(80, 285 + offset, 290, 30), rightname, "button_blueoutline"))
            {
                Selection.activeGameObject = rightcontroller;
            }

            pickerID = 5689469;
            if (GUI.Button(new Rect(370, 285 + offset, 100, 30), "Select..."))
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
                GUI.Label(new Rect(320, 285 + offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(320, 285 + offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            //drag and drop
            if (new Rect(30, 285 + offset, 440, 30).Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    rightcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else if (new Rect(30, 245 + offset, 440, 30).Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragPerform)
                {
                    leftcontroller = (GameObject)DragAndDrop.objectReferences[0];
                }
            }
            else
            {
                //DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }

            if (GUI.Button(new Rect(125, 360 + offset, 250, 30), "Setup Controller Dynamics"))
            {
                SetupControllers(leftcontroller, rightcontroller);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Event.current.Use();
            }

            if (setupComplete)
            {
                GUI.Label(new Rect(360, 360 + offset, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(360, 360 + offset, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

#if CVR_STEAMVR2
            GUI.Label(new Rect(135, 390, 300, 20), "You must have an 'actions.json' file generated from SteamVR");
            if (GUI.Button(new Rect(125, 410, 250, 30), "Append Cognitive Action Set"))
            {
                steamvr2actionset = true;
                AppendSteamVRActionSet();
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Event.current.Use();
            }
            if (steamvr2actionset)
            {
                GUI.Label(new Rect(360, 410, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(360, 410, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            if (GUI.Button(new Rect(125, 450, 250, 30), "Add Default Bindings"))
            {
                steamvr2bindings = true;
                SetDefaultBindings();
                Event.current.Use();
            }
            if (steamvr2bindings)
            {
                GUI.Label(new Rect(360, 450, 64, 30), EditorCore.Checkmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(360, 450, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
            }

            if (steamvr2bindings && steamvr2actionset && leftSetupComplete && rightSetupComplete && setupComplete)
            {
                GUI.Label(new Rect(105, 480, 300, 20), "Need to open SteamVR Input window and press 'Save and generate' button");
            }
#endif
        }

        public static void SetupControllers(GameObject left, GameObject right)
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
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.vivecontroller;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveController;
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.vivecontroller;
            }
#elif CVR_STEAMVR2
            
            if (left != null && left.GetComponent<ControllerInputTracker>() == null)
            {
                var listener = left.AddComponent<ControllerInputTracker>();
                listener.Hand_InputSource = Valve.VR.SteamVR_Input_Sources.LeftHand;
                listener.dynamic = listener.GetComponent<DynamicObject>();
            }
            if (right != null && right.GetComponent<ControllerInputTracker>() == null)
            {
                var listener = right.AddComponent<ControllerInputTracker>();
                listener.Hand_InputSource = Valve.VR.SteamVR_Input_Sources.RightHand;
                listener.dynamic = listener.GetComponent<DynamicObject>();
            }

            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveController;
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.vivecontroller;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveController;
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.vivecontroller;
            }
#elif CVR_VIVEWAVE
            var g = Resources.Load<GameObject>("AdaptiveController");

            if (g == null)
            {
                Debug.LogWarning("could not load AdaptiveController");
            }
            else
            {
                var dyn = g.GetComponent<DynamicObject>();
                if (dyn == null)
                {
                    dyn = g.AddComponent<DynamicObject>();
                }
                dyn.UseCustomMesh = false;
                dyn.IsController = true;
                //dyn.IsRight = false; //set from dominant/non dominant hand
                dyn.ControllerType = DynamicObject.ControllerDisplayType.vivefocuscontroller;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveFocusController;

                var ct = g.GetComponent<ControllerInputTracker>();
                if (ct == null)
                {
                    ct = g.AddComponent<ControllerInputTracker>();
                }
            }

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
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveFocusController;
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.vivefocuscontroller;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveFocusController;
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.vivefocuscontroller;
            }
#elif CVR_WINDOWSMR


            //add component to cognitice manager
            var inputTracker = CognitiveVR_Manager.Instance.gameObject.GetComponent<ControllerInputTracker>();
            if (inputTracker == null)
            {
                inputTracker = CognitiveVR_Manager.Instance.gameObject.AddComponent<ControllerInputTracker>();
            }

            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                if (dyn == null)
                    dyn = left.AddComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.WindowsMixedRealityLeft;
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.windows_mixed_reality_controller_left;
                inputTracker.LeftHand = dyn;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                if (dyn == null)
                    dyn = right.AddComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.WindowsMixedRealityRight;
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.windows_mixed_reality_controller_right;
                inputTracker.RightHand = dyn;
            }

            try
            {
                EditorCore.BindAxis(new EditorCore.Axis("LeftTouchpadH", 16));
                EditorCore.BindAxis(new EditorCore.Axis("LeftTouchpadV", 17,true));
                EditorCore.BindAxis(new EditorCore.Axis("RightTouchpadH", 18));
                EditorCore.BindAxis(new EditorCore.Axis("RightTouchpadV", 19, true));
                EditorCore.BindAxis(new EditorCore.Axis("LeftJoystickH", 0));
                EditorCore.BindAxis(new EditorCore.Axis("LeftJoystickV", 1, true));
                EditorCore.BindAxis(new EditorCore.Axis("RightJoystickH", 3));
                EditorCore.BindAxis(new EditorCore.Axis("RightJoystickV", 4, true));
                EditorCore.BindAxis(new EditorCore.Axis("LeftTrigger", 8));
                EditorCore.BindAxis(new EditorCore.Axis("RightTrigger", 9));
            }
            catch (System.Exception e)
            {
                Debug.LogError("Cognitive Init Wizard error writing input axes:\n" + e);
            }
#elif CVR_PICONEO2EYE
            //add component to cognitice manager
            var inputTracker = CognitiveVR_Manager.Instance.gameObject.GetComponent<ControllerInputTracker>();
            if (inputTracker == null)
            {
                inputTracker = CognitiveVR_Manager.Instance.gameObject.AddComponent<ControllerInputTracker>();
            }

            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                if (dyn == null)
                    dyn = left.AddComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.PicoNeoControllerLeft;
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.pico_neo_2_eye_controller_left;
                inputTracker.LeftHand = dyn;
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                if (dyn == null)
                    dyn = right.AddComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.PicoNeoControllerRight;
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.pico_neo_2_eye_controller_right;
                inputTracker.RightHand = dyn;
            }
#elif CVR_XR
            //add component to cognitice manager
            var inputTracker = CognitiveVR_Manager.Instance.gameObject.GetComponent<ControllerInputTracker>();
            if (inputTracker == null)
            {
                inputTracker = CognitiveVR_Manager.Instance.gameObject.AddComponent<ControllerInputTracker>();
            }

            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                if (dyn == null)
                    dyn = left.AddComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.IsRight = false;
                dyn.IsController = true;
                inputTracker.LeftHand = dyn;

                if (controllerDisplayName == "Vive")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.vivecontroller;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveController;
                }
                else if (controllerDisplayName == "Oculus Rift")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.oculustouchleft;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusRiftTouchLeft;
                }
                else if (controllerDisplayName == "Oculus Quest")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.oculusquesttouchleft;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusQuestTouchLeft;
                }
                else if (controllerDisplayName == "Windows MR")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.windows_mixed_reality_controller_left;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.WindowsMixedRealityLeft;
                }
                else if (controllerDisplayName == "Pico Neo 2")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.pico_neo_2_eye_controller_left;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.PicoNeoControllerLeft;
                }
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                if (dyn == null)
                    dyn = right.AddComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.IsRight = true;
                dyn.IsController = true;
                inputTracker.RightHand = dyn;

                if (controllerDisplayName == "Vive")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.vivecontroller;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.ViveController;
                }
                else if (controllerDisplayName == "Oculus Rift")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.oculustouchright;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusRiftTouchRight;
                }
                else if (controllerDisplayName == "Oculus Quest")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.oculusquesttouchright;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusQuestTouchRight;
                }
                else if (controllerDisplayName == "Windows MR")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.windows_mixed_reality_controller_right;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.WindowsMixedRealityRight;
                }
                else if (controllerDisplayName == "Pico Neo 2")
                {
                    dyn.ControllerType = DynamicObject.ControllerDisplayType.pico_neo_2_eye_controller_right;
                    dyn.CommonMesh = DynamicObject.CommonDynamicMesh.PicoNeoControllerRight;
                }
            }
#elif CVR_OCULUS
            if (left != null)
            {
                var dyn = left.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusRiftTouchLeft;
                dyn.IsRight = false;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.oculustouchleft;
#if UNITY_ANDROID //check for oculus quest controllers
                var config = OVRProjectConfig.GetProjectConfig();
                if (config.targetDeviceTypes.Count > 0)
                {
                    if (config.targetDeviceTypes[0] == OVRProjectConfig.DeviceType.Quest)
                    {
                        dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusQuestTouchLeft;
                        dyn.ControllerType = DynamicObject.ControllerDisplayType.oculusquesttouchleft;
                    }
                }
#endif
            }
            if (right != null)
            {
                var dyn = right.GetComponent<DynamicObject>();
                dyn.UseCustomMesh = false;
                dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusRiftTouchRight;
                dyn.IsRight = true;
                dyn.IsController = true;
                dyn.ControllerType = DynamicObject.ControllerDisplayType.oculustouchright;
#if UNITY_ANDROID //check for oculus quest controllers
                var config = OVRProjectConfig.GetProjectConfig();
                if (config.targetDeviceTypes.Count > 0)
                {
                    if (config.targetDeviceTypes[0] == OVRProjectConfig.DeviceType.Quest)
                    {
                        dyn.CommonMesh = DynamicObject.CommonDynamicMesh.OculusQuestTouchRight;
                        dyn.ControllerType = DynamicObject.ControllerDisplayType.oculusquesttouchright;
                    }
                }
#endif
            }

            if (cameraBase != null)
            {
                var tracker = FindObjectOfType<ControllerInputTracker>();
                if (tracker == null)
                {
                    //add controller tracker to camera base
                    tracker = cameraBase.AddComponent<ControllerInputTracker>();
                }
                if (left != null)
                    tracker.LeftHand = left.GetComponent<DynamicObject>();
                if (right != null)
                    tracker.RightHand = right.GetComponent<DynamicObject>();
            }
            else
            {
                var tracker = FindObjectOfType<ControllerInputTracker>();
                GameObject trackergo;
                if (tracker == null)
                {
                    trackergo = new GameObject("Controller Tracker");
                    tracker = trackergo.AddComponent<ControllerInputTracker>();
                }
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
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 280), dynamicScrollPosition, innerScrollSize,false,true);

        Rect dynamicrect;
        for (int i = 0; i< tempdynamics.Length;i++)
        {
            if (tempdynamics[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, i*30, 460, 30);
            DrawDynamicObject(tempdynamics[i], dynamicrect, i % 2 == 0);
        }

        GUI.EndScrollView();

        GUI.Box(new Rect(30, 120, 425, 280), "", "box_sharp_alpha");

        if (CognitiveVR_Preferences.Instance.TextureResize > 4) { CognitiveVR_Preferences.Instance.TextureResize = 4; }

        //resolution settings here

        if (GUI.Button(new Rect(30, 410, 140, 35), new GUIContent("1/4 Resolution", "Quarter resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 4 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 4;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 4)
        {
            GUI.Box(new Rect(30, 410, 140, 35), "", "box_sharp_alpha");
        }

        if (GUI.Button(new Rect(180, 410, 140, 35), new GUIContent("1/2 Resolution", "Half resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 2 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 2;
            //selectedExportQuality = ExportSettings.DefaultSettings;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 2)
        {
            GUI.Box(new Rect(180, 410, 140, 35), "", "box_sharp_alpha");
        }

        if (GUI.Button(new Rect(330, 410, 140, 35), new GUIContent("1/1 Resolution", "Full resolution of dynamic object textures"), CognitiveVR_Preferences.Instance.TextureResize == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 1;
            //selectedExportQuality = ExportSettings.HighSettings;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 1)
        {
            GUI.Box(new Rect(330, 410, 140, 35), "", "box_sharp_alpha");
        }
        
        
        if (delayDisplayUploading>0)
        {
            GUI.Button(new Rect(180, 455, 140, 35), "Preparing...", "button_bluetext"); //fake replacement for button
            delayDisplayUploading--;
        }
        else if (delayDisplayUploading == 0)
        {
            GUI.Button(new Rect(180, 455, 140, 35), "Preparing...", "button_bluetext"); //fake replacement for button
            Selection.objects = GameObject.FindObjectsOfType<GameObject>();
            ExportUtility.ExportAllDynamicsInScene();
            delayDisplayUploading--;
            EditorCore.ExportedDynamicObjects = null; //force refresh
        }
        else
        {
            //GUI.Label(new Rect(180, 450, 140, 40), "", "button_blueoutline");
            if (GUI.Button(new Rect(180, 455, 140, 35), "Prepare All"))
            {
                delayDisplayUploading = 2;
            }
        }
    }

    //each row is 30 pixels
    void DrawDynamicObject(DynamicObject dynamic, Rect rect, bool darkbackground)
    {
        Event e = Event.current;
        if (e.isMouse && e.type == EventType.MouseDown)
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

    void UploadSceneUpdate()
    {
        GUI.Label(steptitlerect, "STEP 7 - PREPARE SCENE", "steptitle");
        GUI.Label(new Rect(30, 45, 440, 440), "The <color=#8A9EB7FF>Scene</color> will be exported and prepared from all geometry without a <color=#8A9EB7FF>Dynamic Object</color> component.", "boldlabel");

        GUI.Label(new Rect(30, 320, 200, 30), "Scene Export Texture Resolution", "miniheader");
        
        //texture resolution settings

        if (CognitiveVR_Preferences.Instance.TextureResize > 4) { CognitiveVR_Preferences.Instance.TextureResize = 4; }

        //resolution settings here

        if (GUI.Button(new Rect(30, 360, 140, 35), new GUIContent("1/4 Resolution", "Quarter resolution of scene textures"), CognitiveVR_Preferences.Instance.TextureResize == 4 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 4;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 4)
        {
            GUI.Box(new Rect(30, 360, 140, 35), "", "box_sharp_alpha");
        }
        else
        {
            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackgroundQuarter, "image_centered");
        }

        if (GUI.Button(new Rect(180, 360, 140, 35), new GUIContent("1/2 Resolution", "Half resolution of scene textures"), CognitiveVR_Preferences.Instance.TextureResize == 2 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 2;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 2)
        {
            GUI.Box(new Rect(180, 360, 140, 35), "", "box_sharp_alpha");
        }
        else
        {
            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackgroundHalf, "image_centered");
        }

        if (GUI.Button(new Rect(330, 360, 140, 35), new GUIContent("1/1 Resolution","Full resolution of scene textures"), CognitiveVR_Preferences.Instance.TextureResize == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            CognitiveVR_Preferences.Instance.TextureResize = 1;
        }
        if (CognitiveVR_Preferences.Instance.TextureResize != 1)
        {
            GUI.Box(new Rect(330, 360, 140, 35), "", "box_sharp_alpha");
        }
        else
        {
            GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");
        }
        
        if (EditorCore.HasSceneExportFiles(CognitiveVR_Preferences.FindCurrentScene()))
        {
            float sceneSize = EditorCore.GetSceneFileSize(CognitiveVR_Preferences.FindCurrentScene());
            string displayString = "";
            if (sceneSize < 1)
            {
                displayString = "Exported File Size: <1 MB";
            }
            else if (sceneSize > 500)
            {
                displayString = "<color=red>Warning. Exported File Size: " + string.Format("{0:0}", sceneSize) + " MB. This scene will take a while to upload and view</color>";
            }
            else
            {
                displayString = "Exported File Size: "+string.Format("{0:0}", sceneSize)+" MB";
            }
            if (GUI.Button(new Rect(0, 395, 500, 35), displayString, "miniheadercenter"))
            {
                EditorUtility.RevealInFinder(EditorCore.GetSceneExportDirectory(CognitiveVR_Preferences.FindCurrentScene()));
            }
        }

        if (GUI.Button(new Rect(0, 420, 500, 35), "Augmented Reality?  Skip Scene Export", "miniheadercenter"))
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
            ExportUtility.ExportSceneAR();
            CognitiveVR_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.SetDirty(EditorCore.GetPreferences());

            UnityEditor.AssetDatabase.SaveAssets();
            currentPage++;
        }

        if (GUI.Button(new Rect(180, 455, 140, 35), "Export Scene"))
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
            ExportUtility.ExportGLTFScene();

            string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            string objPath = EditorCore.GetSubDirectoryPath(fullName);
            string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\"" + fullName + "\",\"sdkVersion\":\"" + Core.SDK_VERSION + "\"}";
            System.IO.File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

            string debugContent = DebugInformationWindow.GetDebugContents();
            System.IO.File.WriteAllText(objPath + "debug.log", debugContent);

            CognitiveVR_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.SetDirty(EditorCore.GetPreferences());

            UnityEditor.AssetDatabase.SaveAssets();
            EditorCore.RefreshSceneVersion(null);
        }

    }

    bool delayUnderstandButton = true;
    double understandRevealTime;

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
        if (settings != null && !string.IsNullOrEmpty(settings.SceneId)) //has been uploaded. this is a new version
        {
            int dynamicObjectCount = EditorCore.GetExportedDynamicObjectNames().Count;
            string scenename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scenename))
            {
                scenename = "SCENE NOT SAVED";
            }
            string settingsname = "1/1 Resolution";
            if (CognitiveVR_Preferences.Instance.TextureResize == 4) { settingsname = "1/4 Resolution"; }
            if (CognitiveVR_Preferences.Instance.TextureResize == 2) { settingsname = "1/2 Resolution"; }
            GUI.Label(new Rect(30, 120, 440, 440), "You will be uploading a new version of <color=#62B4F3FF>" + scenename + "</color> with <color=#62B4F3FF>" + settingsname + "</color>. "+
            "Version " + settings.VersionNumber + " will be archived.", "label_disabledtext_large");

            GUI.Label(new Rect(30, 170, 440, 440), "You will be uploading <color=#62B4F3FF>" + dynamicObjectCount + "</color> Dynamic Object Meshes", "label_disabledtext_large");
        }
        else
        {
            int dynamicObjectCount = EditorCore.GetExportedDynamicObjectNames().Count; ;
            string scenename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scenename))
            {
                scenename = "SCENE NOT SAVED";
            }
            string settingsname = "1/1 Resolution";
            if (CognitiveVR_Preferences.Instance.TextureResize == 4) { settingsname = "1/4 Resolution"; }
            if (CognitiveVR_Preferences.Instance.TextureResize == 2) { settingsname = "1/2 Resolution"; }
            GUI.Label(new Rect(30, 120, 440, 440), "You will be uploading <color=#62B4F3FF>" + scenename + "</color> with <color=#62B4F3FF>" + settingsname + "</color>", "label_disabledtext_large");
            
            GUI.Label(new Rect(30, 170, 440, 440), "You will be uploading <color=#62B4F3FF>" + dynamicObjectCount + "</color> Dynamic Objects Meshes", "label_disabledtext_large");
        }
        GUI.Label(new Rect(30, 200, 440, 440), "The display image on the Dashboard will be this:", "label_disabledtext_large");


        var sceneRT = EditorCore.GetSceneRenderTexture();
        if (sceneRT != null)
            GUI.Box(new Rect(125, 230, 250, 250), sceneRT, "image_centeredboxed");
    }

    void DoneUpdate()
    {
        GUI.Label(steptitlerect, "STEP 10 - DONE", "steptitle");
        GUI.Label(new Rect(30, 45, 440, 440), "The <color=#8A9EB7FF>"+EditorCore.DisplayValue(DisplayKey.ManagerName)+"</color> in your scene will record user position, gaze and basic device information.\n\nYou can view sessions from the Dashboard.", "boldlabel");
        if (GUI.Button(new Rect(150,150,200,40),"Open Dashboard","button_bluetext"))
        {
            Application.OpenURL("https://" + CognitiveVR_Preferences.Instance.Dashboard);
        }

        GUI.Label(new Rect(30, 205, 440, 440), "-Want to ask users about their experience?\n-Need to add more Dynamic Objects?\n-Have some Sensors?\n-Tracking user's gaze on a video or image?\n-Multiplayer?\n", "boldlabel");
        if (GUI.Button(new Rect(150, 320,200,40),"Open Documentation","button_bluetext"))
        {
            Application.OpenURL("https://" + CognitiveVR_Preferences.Instance.Documentation);
        }

        GUI.Label(new Rect(30, 385, 440, 440), "Make sure your users understand your experience with a simple training scene.", "boldlabel");
        if (GUI.Button(new Rect(150,440,200,40),"Ready Room Setup", "button_bluetext"))
        {
            var readyRoomScenes = AssetDatabase.FindAssets("t:scene readyroom");
            if (readyRoomScenes.Length == 1)
            {
                //ask if want save
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(readyRoomScenes[0]));
                    Close();
                    ReadyRoomSetupWindow.Init();
                }
            }
        }
    }

    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 500, 500, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        DrawBackButton();

        DrawNextButton();
    }

    void GetDevKeyResponse(int responseCode, string error, string text)
    {
        lastDevKeyResponseCode = responseCode;
        if (responseCode == 200)
        {
            //dev key is fine
            currentPage++;
            SaveKeys();
        }
        else
        {
            //EditorUtility.DisplayDialog("Your developer key has expired", "Please log in to the dashboard, select your project, and generate a new developer key.\n\nNote:\nDeveloper keys allow you to upload and modify Scenes, and the keys expire after 90 days.\nApplication keys authorize your app to send data to our server, and they never expire.", "Ok");
            Debug.LogError("Developer Key invalid or expired");
        }
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
                if (lastDevKeyResponseCode == 200)
                {
                    //next. use default action
                    onclick += () => SaveKeys();
                }
                else
                {
                    //check and wait for response
                    onclick = () => SaveKeys();
                    onclick += () => EditorCore.CheckForExpiredDeveloperKey(GetDevKeyResponse);
                }
                
                //onclick += () => ApplyBrandingUrls();
                buttonDisabled = apikey == null || apikey.Length == 0 || developerkey == null || developerkey.Length == 0;
                if (buttonDisabled)
                {
                    text = "Keys Required";
                }

                if (buttonDisabled == false && lastDevKeyResponseCode != 200)
                {
                    text = "Validate";
                    //MuteDevKeyPopupWindow = true;
                }

                if (buttonDisabled == false && lastDevKeyResponseCode == 200)
                {
                    text = "Next";
                }
                break;
            case "tagdynamics":
                break;
            case "selectsdk":
                    onclick += () =>
                    {
                        EditorCore.SetPlayerDefine(selectedsdks);
                        if (selectedsdks.Contains("CVR_TOBIIVR") || selectedsdks.Contains("CVR_NEURABLE") || selectedsdks.Contains("CVR_FOVE") || selectedsdks.Contains("CVR_PUPIL") || selectedsdks.Contains("CVR_AH") || selectedsdks.Contains("CVR_SNAPDRAGON") || selectedsdks.Contains("CVR_VIVEPROEYE"))
                        {
                            //eye tracking
                            CognitiveVR_Preferences.Instance.GazeType = GazeType.Physics;
                        }
                    };
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
                    appearDisabled = !EditorCore.HasSceneExportFiles(CognitiveVR_Preferences.FindCurrentScene());

                    if (appearDisabled)
                    {
                        onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without exporting your scene?", "Yes", "No")) { currentPage++; } };
                    }
                    text = "Next";
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
                    ExportUtility.UploadAllDynamicObjectMeshes(true);
                    currentPage++;
                };

                //fifth upload manifest
                System.Action completedRefreshSceneVersion = delegate ()
                {
                    ManageDynamicObjects.AggregationManifest manifest = new ManageDynamicObjects.AggregationManifest();
                    ManageDynamicObjects.AddOrReplaceDynamic(manifest, ManageDynamicObjects.GetDynamicObjectsInScene());
                    ManageDynamicObjects.UploadManifest(manifest, completedmanifestupload, completedmanifestupload);
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
                            ExportUtility.UploadDecimatedScene(current, completeSceneUpload);
                        }
                    }
                    else
                    {
                        //new version
                        if (EditorUtility.DisplayDialog("Upload New Version", "Upload a new version of this existing scene? Will archive previous version", "Ok","Cancel"))
                        {
                            ExportUtility.UploadDecimatedScene(current, completeSceneUpload);
                        }
                    }
                };

                //second save screenshot
                System.Action completedRefreshSceneVersion1 = delegate ()
                {
#if UNITY_2018_3_OR_NEWER
                    //EditorCore.SceneViewCameraScreenshot(UnityEditor.SceneView.GetAllSceneCameras()[0], UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
                    EditorCore.SaveScreenshot(EditorCore.GetSceneRenderTexture(), UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
#else
                    EditorCore.SaveCurrentScreenshot(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
#endif
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
                text = "Back";
                buttonrect = new Rect(260, 510, 80, 30);
                break;
            case "listdynamics":
                text = "Back";
                buttonrect = new Rect(260, 510, 80, 30);
                break;
            case "upload":
                text = "Back";
                buttonrect = new Rect(200, 510, 80, 30);
                break;
            case "uploadscene":
                text = "Back";
                break;
            case "uploadsummary":
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

#if CVR_STEAMVR2
        public static void AppendSteamVRActionSet()
        {
            SteamVR_Input_ActionFile actionfile;
            if (LoadActionFile(out actionfile))
            {
                var cognitiveActionSet = new SteamVR_Input_ActionFile_ActionSet() { name = "/actions/CVR_Input", usage = "single" };

                //if actions.json already contains cognitive action set
                if (actionfile.action_sets.Contains(cognitiveActionSet))
                {
                    Debug.Log("SteamVR action set already contains Cognitive Action Set. Skip adding action set to actions.json");
                    return;
                }

                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/CVR_Input/in/Grip", type = "boolean" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/CVR_Input/in/Trigger", type = "vector1" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/CVR_Input/in/Touchpad", type = "vector2" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/CVR_Input/in/Touchpad_Press", type = "boolean" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/CVR_Input/in/Touchpad_Touch", type = "boolean" });
                actionfile.actions.Add(new SteamVR_Input_ActionFile_Action() { name = "/actions/CVR_Input/in/Menu", type = "boolean" });
                actionfile.action_sets.Add(cognitiveActionSet);

                SaveActionFile(actionfile);
                Debug.Log("InitWizard::AppendSteamVRActionSet Added CognitiveVR Action Set");
            }
            else
            {
                Debug.LogError("InitWizard::AppendSteamVRActionSet SteamVR LoadActionFile failed!");
            }
        }

        static bool LoadActionFile(out SteamVR_Input_ActionFile actionFile)
        {
            string actionsFilePath = SteamVR_Input.GetActionsFileFolder(true) + "/actions.json";
            if (!File.Exists(actionsFilePath))
            {
                Debug.LogErrorFormat("<b>[SteamVR]</b> Actions file doesn't exist: {0}", actionsFilePath);
                actionFile = new SteamVR_Input_ActionFile();
                return false;
            }

            actionFile = JsonConvert.DeserializeObject<SteamVR_Input_ActionFile>(File.ReadAllText(actionsFilePath));

            return true;
        }

        static bool SaveActionFile(SteamVR_Input_ActionFile actionFile)
        {
            string actionsFilePath = SteamVR_Input.GetActionsFileFolder(true) +"/actions.json";
            string newJSON = JsonConvert.SerializeObject(actionFile, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            File.WriteAllText(actionsFilePath, newJSON);

            Debug.Log("saved " + SteamVR_Settings.instance.actionsFilePath + " with CognitiveVR input action set");

            return true;
        }

        public static void SetDefaultBindings()
        {
            SteamVR_Input_BindingFile bindingfile;
            if (LoadBindingFile(out bindingfile))
            {
                if (bindingfile.bindings.ContainsKey("/actions/cvr_input"))
                {
                    bindingfile.bindings.Remove("/actions/cvr_input");
                }

                SteamVR_Input_BindingFile_ActionList actionlist = new SteamVR_Input_BindingFile_ActionList();

                actionlist.sources.Add(createSource("button", "/user/hand/left/input/grip","click", "/actions/cvr_input/in/grip"));
                actionlist.sources.Add(createSource("button", "/user/hand/right/input/grip", "click", "/actions/cvr_input/in/grip"));

                actionlist.sources.Add(createSource("button", "/user/hand/left/input/menu","click", "/actions/cvr_input/in/menu"));
                actionlist.sources.Add(createSource("button", "/user/hand/right/input/menu", "click", "/actions/cvr_input/in/menu"));

                actionlist.sources.Add(createSource("trigger", "/user/hand/left/input/trigger", "pull", "/actions/cvr_input/in/trigger"));
                actionlist.sources.Add(createSource("trigger", "/user/hand/right/input/trigger", "pull", "/actions/cvr_input/in/trigger"));

                //left touchpad
                SteamVR_Input_BindingFile_Source bindingSource_left_pad = new SteamVR_Input_BindingFile_Source();
                bindingSource_left_pad.mode = "trackpad";
                bindingSource_left_pad.path = "/user/hand/left/input/trackpad";
                {
                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_press = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_press.Add("output", "/actions/cvr_input/in/touchpad_press");
                    bindingSource_left_pad.inputs.Add("click", stringDictionary_press);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_touch = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_touch.Add("output", "/actions/cvr_input/in/touchpad_touch");
                    bindingSource_left_pad.inputs.Add("touch", stringDictionary_touch);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_pos = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_pos.Add("output", "/actions/cvr_input/in/touchpad");
                    bindingSource_left_pad.inputs.Add("position", stringDictionary_pos);
                }
                actionlist.sources.Add(bindingSource_left_pad);

                //right touchpad
                SteamVR_Input_BindingFile_Source bindingSource_right_pad = new SteamVR_Input_BindingFile_Source();
                bindingSource_right_pad.mode = "trackpad";
                bindingSource_right_pad.path = "/user/hand/right/input/trackpad";
                {
                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_press = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_press.Add("output", "/actions/cvr_input/in/touchpad_press");
                    bindingSource_right_pad.inputs.Add("click", stringDictionary_press);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_touch = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_touch.Add("output", "/actions/cvr_input/in/touchpad_touch");
                    bindingSource_right_pad.inputs.Add("touch", stringDictionary_touch);

                    SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary_pos = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
                    stringDictionary_pos.Add("output", "/actions/cvr_input/in/touchpad");
                    bindingSource_right_pad.inputs.Add("position", stringDictionary_pos);
                }
                actionlist.sources.Add(bindingSource_right_pad);

                bindingfile.bindings.Add("/actions/cvr_input", actionlist);
                Debug.Log("InitWizard::SetDefaultBindings save CognitiveVR input bindings");
                SaveBindingFile(bindingfile);
            }
            else
            {
                Debug.Log("InitWizard::SetDefaultBindings failed to load steamvr actions");
            }
        }

        //mode = button, path = "/user/hand/left/input/grip", actiontype = "click", action = "/actions/cvr_input/in/grip"
        static SteamVR_Input_BindingFile_Source createSource(string mode, string path, string actiontype, string action)
        {
            SteamVR_Input_BindingFile_Source bindingSource = new SteamVR_Input_BindingFile_Source();
            bindingSource.mode = mode;
            bindingSource.path = path;

            SteamVR_Input_BindingFile_Source_Input_StringDictionary stringDictionary = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();
            stringDictionary.Add("output", action);
            bindingSource.inputs.Add(actiontype, stringDictionary);

            return bindingSource;
        }

        static bool LoadBindingFile(out SteamVR_Input_BindingFile bindingfile)
        {
            string bindingFilePath = SteamVR_Input.GetActionsFileFolder(true) + "/bindings_vive_controller.json";
            if (!File.Exists(bindingFilePath))
            {
                Debug.LogErrorFormat("<b>[SteamVR]</b> binding file doesn't exist: {0}", bindingFilePath);
                bindingfile = new SteamVR_Input_BindingFile();
                return false;
            }

            bindingfile = JsonConvert.DeserializeObject<SteamVR_Input_BindingFile>(File.ReadAllText(bindingFilePath));

            return true;
        }

        static bool SaveBindingFile(SteamVR_Input_BindingFile bindingfile)
        {
            string bindingFilePath = SteamVR_Input.GetActionsFileFolder(true) + "/bindings_vive_controller.json";

            string newJSON = JsonConvert.SerializeObject(bindingfile, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            File.WriteAllText(bindingFilePath, newJSON);

            Debug.Log("saved default bindings for CognitiveVR inputs");

            return true;
        }
#endif
    }
}