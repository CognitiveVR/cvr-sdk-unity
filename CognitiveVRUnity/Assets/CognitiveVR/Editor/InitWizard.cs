using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

public class InitWizard : EditorWindow
{
    Rect steptitlerect = new Rect(30, 0, 100, 440);
    Rect boldlabelrect = new Rect(30, 100, 440, 440);

    public static void Init()
    {
        InitWizard window = (InitWizard)EditorWindow.GetWindow(typeof(InitWizard), true, "");
        window.minSize = new Vector2(500, 500);
        window.maxSize = new Vector2(500, 500);
        window.Show();

        window.LoadKeys(); 
        window.selectedExportQuality = ExportSettings.HighSettings;

        window.GetSelectedSDKs();

        CognitiveVR_SceneExportWindow.ClearUploadSceneSettings();
    }

    List<string> pageids = new List<string>() { "welcome", "authenticate","selectsdk", "explaindynamic", "explainscene", "listdynamics", "uploadscene", "upload", "uploadsummary", "done" };
    public int currentPage;

    private void OnGUI()
    {
        GUI.skin = EditorCore.WizardGUISkin;
        GUI.DrawTexture(new Rect(0, 0, 500, 500), EditorGUIUtility.whiteTexture);

        switch (pageids[currentPage])
        {
            case "welcome":WelcomeUpdate(); break;
            case "authenticate": AuthenticateUpdate(); break;
            case "selectsdk": SelectSDKUpdate(); break;
            case "explaindynamic": DynamicExplainUpdate(); break;
            case "explainscene": SceneExplainUpdate(); break;
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
            GUI.Label(boldlabelrect, "Welcome to the Cognitive3D SDK Scene Setup.", "boldlabel");
            GUI.Label(new Rect(30, 140, 440, 440), "This will guide you through the initial setup of your scene, and will have production ready analytics at the end of this setup.\n\n\n"+
                "<color=#8A9EB7FF>This scene has already been uploaded to SceneExplorer!</color> Unless there are meaningful changes to the static scene geometry you probably don't need to upload this scene again.\n\n" +
                "Use <color=#8A9EB7FF>Manage Dynamic Objects</color> if you want to upload new Dynamic Objects to your existing scene.", "normallabel");
        }
        else
        {
            GUI.Label(boldlabelrect, "Welcome to the Cognitive3D SDK Scene Setup.", "boldlabel");
            GUI.Label(new Rect(30, 200, 440, 440), "This will guide you through the initial setup of your scene, and will have production ready analytics at the end of this setup.", "normallabel");
        }
    }

    #region Auth Keys

    string apikey ="";
    string developerkey = "";
    void AuthenticateUpdate()
    {
        GUI.Label(steptitlerect, "STEP 2 - AUTHENTICATION", "steptitle");
        GUI.Label(boldlabelrect, "Please add your Cognitive3D authorization keys below to continue.", "boldlabel");

        //dev key
        GUI.Label(new Rect(30, 200, 100, 30), "Developer Key", "miniheader");
        developerkey = EditorCore.TextField(new Rect(30, 230, 400, 40), developerkey, 32);
        if (string.IsNullOrEmpty(developerkey))
        {
            GUI.Label(new Rect(30, 230, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
        }
        else
        {
            GUI.Label(new Rect(440, 230, 24, 40), EditorCore.Checkmark, "image_centered");
        }

        //api key
        GUI.Label(new Rect(30, 300, 100, 30), "API Key", "miniheader");
        apikey = EditorCore.TextField(new Rect(30, 330, 400, 40), apikey, 32);
        if (string.IsNullOrEmpty(apikey))
        {
            GUI.Label(new Rect(30, 330, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
        }
        else
        {
            GUI.Label(new Rect(440, 330, 24, 40), EditorCore.Checkmark, "image_centered");
        }

    }

    void SaveKeys()
    {
        EditorPrefs.SetString("developerkey", developerkey);
        EditorCore.GetPreferences().APIKey = apikey;

        EditorUtility.SetDirty(EditorCore.GetPreferences());
        AssetDatabase.SaveAssets();
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
#if CVR_ARKIT //apple
            selectedsdks.Add("CVR_ARKIT");
#endif
#if CVR_ARCORE //google
            selectedsdks.Add("CVR_ARCORE");
#endif
#if CVR_META 
            selectedsdks.Add("CVR_META");
#endif
    }

    List<string> selectedsdks = new List<string>();
    void SelectSDKUpdate()
    {
        GUI.Label(steptitlerect, "STEP 3 - SELECT SDK", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "Please select the hardware SDK you will be including in this project.", "boldlabel");

        List<string> sdknames = new List<string>() { "Unity Default", "Oculus SDK", "SteamVR SDK", "Fove SDK (eye tracking)", "Pupil Labs SDK (eye tracking)", "ARCore SDK (Android)", "ARKit SDK (iOS)", "Hololens SDK", "Meta 2" };
        List<string> sdkdefines = new List<string>() { "CVR_DEFAULT", "CVR_OCULUS", "CVR_STEAMVR", "CVR_FOVE", "CVR_PUPIL", "CVR_ARCORE", "CVR_ARKIT", "CVR_HOLOLENS", "CVR_META" };

        for(int i = 0;i <sdknames.Count;i++)
        {
            bool selected = selectedsdks.Contains(sdkdefines[i]);
            if (GUI.Button(new Rect(30, i * 32 + 120, 440, 30), sdknames[i], selected ? "button_blueoutlineleft" : "button_disabledoutline"))
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
            GUI.Label(new Rect(420, i * 32 + 120, 24, 30), selected ? EditorCore.Checkmark : EditorCore.EmptyCheckmark, "image_centered");
        }
    }

    #region Terminology

    void DynamicExplainUpdate()
    {
        GUI.Label(steptitlerect, "STEP 4a - WHAT IS A DYNAMIC OBJECT?", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "A <color=#8A9EB7FF>Dynamic Object </color> is an object that moves around during a scene which you wish to track.", "boldlabel");

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

        GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsHightlight, "image_centered");

        GUI.color = Color.white;

        GUI.Label(new Rect(30, 350, 440, 440), "You must attach Dynamic Object Components onto any objects you wish to track in your scene. These objects must also have colliders attached to them so we can track user gaze on them.", "normallabel");
    }

    void SceneExplainUpdate()
    {
        GUI.Label(steptitlerect, "STEP 4b - WHAT IS A SCENE?", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "A <color=#8A9EB7FF>Scene</color> is the base geometry of your level. A scene does not require colliders on it to detect user gaze.", "boldlabel");

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

        GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);
        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneHighlight, "image_centered");
        GUI.color = Color.white;
        
        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

        GUI.Label(new Rect(30, 350, 440, 440), "The Scene will be uploaded in one large step, and can be updated at a later date, resulting in a new Scene Version.", "normallabel");
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
        GUI.Label(steptitlerect, "STEP 5 - PREPARE OBJECTS", "steptitle");

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
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 270), dynamicScrollPosition, innerScrollSize,false,true);

        Rect dynamicrect;
        for (int i = 0; i< tempdynamics.Length;i++)
        {
            if (tempdynamics[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, i*30, 460, 30);
            DrawDynamicObject(tempdynamics[i], dynamicrect, i % 2 == 0);
        }

        GUI.EndScrollView();

        GUI.Box(new Rect(30, 120, 425, 270), "","box_sharp_alpha");
        if (delayDisplayUploading>0)
        {
            GUI.Button(new Rect(180, 400, 140, 40), "Uploading...", "button_bluetext"); //fake replacement for button
            delayDisplayUploading--;
        }
        else if (delayDisplayUploading == 0)
        {
            GUI.Button(new Rect(180, 400, 140, 40), "Uploading...", "button_bluetext"); //fake replacement for button
            CognitiveVR_SceneExportWindow.ExportAllDynamicsInScene();
            delayDisplayUploading--;
        }
        else
        {
            if (GUI.Button(new Rect(180, 400, 140, 40), "Upload All", "button_bluetext"))
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
        GUI.Label(steptitlerect, "STEP 6 - SCENE UPLOAD", "steptitle");


        GUI.Label(new Rect(30, 45, 440, 440), "All geometry without a <color=#8A9EB7FF>Dynamic Object</color> component will be exported and uploaded to <color=#8A9EB7FF>SceneExplorer</color>.", "boldlabel");

        string selectBlender = "Select Blender.exe";
#if UNITY_EDITOR_OSX
        selectBlender = "Select Blender.app";
#endif
        GUI.Label(new Rect(30, 120, 100, 30), selectBlender, "miniheader");
        
        GUI.Label(new Rect(130, 120, 30, 30), new GUIContent(EditorGUIUtility.FindTexture("d_console.infoicon.sml"), "Blender is used to reduce complex scene geometry. It is free and open source.\nDownload from Blender.org"),"image_centered");

        if (GUI.Button(new Rect(30, 160, 100, 30), "Browse...", "button_disabled"))
        {
            EditorCore.BlenderPath = EditorUtility.OpenFilePanel("Select Blender", string.IsNullOrEmpty(EditorCore.BlenderPath) ? "c:\\" : EditorCore.BlenderPath, "");
        }

        GUI.Label(new Rect(140,165,300,60), EditorCore.BlenderPath, "label_disabledtext");



        GUI.Label(new Rect(30, 220, 200, 30), "Scene Export Quality", "miniheader");

        if (GUI.Button(new Rect(30, 250, 140, 100), "Low\n\n", qualityindex == 0 ? "button_blueoutline" : "button_disabledtext"))
        {
            qualityindex = 0;
            selectedExportQuality = ExportSettings.LowSettings;
        }
        if (qualityindex == 0)
        {
            GUI.Label(new Rect(88, 265, 24, 100), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(88, 265, 24, 100), EditorCore.EmptyCheckmark, "image_centered");
        }
            
        if (GUI.Button(new Rect(180, 250, 140, 100), "Medium\n\n", qualityindex == 1 ? "button_blueoutline" : "button_disabledtext"))
        {
            qualityindex = 1;
            selectedExportQuality = ExportSettings.DefaultSettings;
        }
        if (qualityindex == 1)
        {
            GUI.Label(new Rect(238, 265, 24, 100), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(238, 265, 24, 100), EditorCore.EmptyCheckmark, "image_centered");
        }

        if (GUI.Button(new Rect(330, 250, 140, 100), "Maximum\n\n", qualityindex == 2 ? "button_blueoutline" : "button_disabledtext"))
        {
            qualityindex = 2;
            selectedExportQuality = ExportSettings.HighSettings;
        }
        if (qualityindex == 2)
        {
            GUI.Label(new Rect(388, 265, 24, 100), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(388, 265, 24, 100), EditorCore.EmptyCheckmark, "image_centered");
        }

        
        if (GUI.Button(new Rect(270, 410, 220, 40), "Augmented Reality? Skip Scene Export", "miniheader"))
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
            GUI.Label(steptitlerect, "STEP 7 - UPLOAD", "steptitle");

            Color scene1color = Color.HSVToRGB(0.55f, 0.5f, 1);
            Color scene2color = Color.HSVToRGB(0.55f, 1f, 1);

            GUI.color = scene1color;
            GUI.Box(new Rect(100,40, 125, 125), EditorCore.SceneBackground, "image_centered");
            GUI.color = Color.white;

            GUI.Box(new Rect(100, 40, 125, 125), EditorCore.ObjectsBackground, "image_centered");

            GUI.color = scene2color;
            GUI.Box(new Rect(250, 40, 125, 125), EditorCore.SceneBackground, "image_centered");
            GUI.color = Color.white;

            GUI.Label(new Rect(30, 180, 440, 440), "In the final step, we will upload version <color=#62B4F3FF>" + (settings.VersionNumber+1)+ " </color>of the scene to <color=#8A9EB7FF>SceneExplorer</color>.\n\n\n" +
                "This will archive the previous version <color=#62B4F3FF>" + (settings.VersionNumber) + " </color> of this scene. You will be prompted to copy the Dynamic Objects to the new version.\n\n\n" +
                "For <color=#8A9EB7FF>Dynamic Objects</color>, you will be able to continue editing those later in the <color=#8A9EB7FF>Manage Dynamic Objects</color> window.", "normallabel");
        }
        else
        {
            GUI.Label(steptitlerect, "STEP 7 - UPLOAD", "steptitle");
            GUI.Label(new Rect(30, 100, 440, 440), "In the final step, we will complete the upload process to our <color=#8A9EB7FF>SceneExplorer</color> servers.\n\n\n" +
                "After your Scene is uploaded, if you make changes to your scene, you may want to open this window again and upload a new version of the scene.\n\n\n" +
                "For <color=#8A9EB7FF>Dynamic Objects</color>, you will be able to continue editing those later in the <color=#8A9EB7FF>Manage Dynamic Objects</color> window.", "normallabel");
        }
    }

    void UploadSummaryUpdate()
    {
        GUI.Label(steptitlerect, "STEP 8 - UPLOAD", "steptitle");
        GUI.Label(new Rect(30, 45, 440, 440), "Here is a final summary of what will be uploaded to <color=#8A9EB7FF>SceneExplorer</color>:", "boldlabel");

        int dynamicObjectCount = GetDynamicObjects.Length;
        GUI.Label(new Rect(30, 120, 440, 440), "You will be uploading <color=#62B4F3FF>" + dynamicObjectCount + "</color> Dynamic Objects", "label_disabledtext_large");

        string scenename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(scenename))
        {
            scenename = "SCENE NOT SAVED";
        }
        string settingsname = "Maximum Quality";
        if (qualityindex == 0) { settingsname = "Low Quality"; }
        if (qualityindex == 1) { settingsname = "Medium Quality"; }
        GUI.Label(new Rect(30, 150, 440, 440), "You will be uploading <color=#62B4F3FF>" + scenename + "</color> with <color=#62B4F3FF>" + settingsname + "</color>", "label_disabledtext_large");

        GUI.Label(new Rect(30, 200, 440, 440), "Your scene display image will be this:", "label_disabledtext_large");

        var sceneRT = EditorCore.GetSceneRenderTexture();
        if (sceneRT != null)
            GUI.Box(new Rect(175, 230, 150, 150), sceneRT, "image_centeredboxed");

        GUI.Label(new Rect(30, 390, 440, 440), "You can add <color=#8A9EB7FF>ExitPoll</color> surveys, update <color=#8A9EB7FF>Dynamic Objects</color>, and add user engagement scripts after this process is complete.", "normallabel");
    }

    void DoneUpdate()
    {
        GUI.Label(steptitlerect, "STEP 9 - DONE", "steptitle");
        GUI.Label(new Rect(30, 45, 440, 440), "That's it!\n\nThe <color=#8A9EB7FF>CognitiveVR_Manager</color> in your scene will record user position, gaze and basic device information.\n\nYou can view sessions from the Dashboard", "boldlabel");
        if (GUI.Button(new Rect(150,300,200,40),"Open Dashboard","button_bluetext"))
        {
            Application.OpenURL(Constants.DASHBOARD);
        }
    }

    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 450, 500, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        DrawBackButton();

        if (pageids[currentPage] == "uploadscene")
        {
            Rect buttonrect = new Rect(350, 460, 140, 30);
            if (GUI.Button(buttonrect, "Export Scene"))
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
        Rect buttonrect = new Rect(410, 460, 80, 30);

        switch (pageids[currentPage])
        {
            case "welcome":
                break;
            case "authenticate":
                buttonrect = new Rect(350, 460, 140, 30);
                onclick += () => SaveKeys();
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
                        GameObject newManager = new GameObject("CognitiveVR_Manager");
                        Undo.RegisterCreatedObjectUndo(newManager, "Create CognitiveVR Manager");
                        newManager.AddComponent<CognitiveVR_Manager>();
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
                    text = dynamicsFromSceneExported + "/" + dynamics.Length + " Uploaded";
                }
                buttonrect = new Rect(350, 460, 140, 30);
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
                buttonrect = new Rect(290, 460, 200, 30);
                break;
            case "uploadsummary":

                //fifth upload manifest
                System.Action completedRefreshSceneVersion = delegate ()
                {
                    //TODO this might cause a race condition for uploading dynamics and manifest
                    ManageDynamicObjects.UploadManifest();
                    CognitiveVR_SceneExportWindow.UploadAllDynamicObjects(true);
                    currentPage = 9;
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
                        if (EditorUtility.DisplayDialog("Upload New Scene", "Upload " + current.SceneName + " to SceneExplorer?", "Ok", "Cancel"))
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
        Rect buttonrect = new Rect(320, 460, 80, 30);

        switch (pageids[currentPage])
        {
            case "welcome": buttonDisabled = true; break;
            case "authenticate":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(260, 460, 80, 30);
                break;
            case "listdynamics":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(260, 460, 80, 30);
                break;
            case "upload":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(200, 460, 80, 30);
                break;
            case "uploadscene":
                //buttonDisabled = true;
                text = "Back";
                buttonrect = new Rect(260, 460, 80, 30);
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
