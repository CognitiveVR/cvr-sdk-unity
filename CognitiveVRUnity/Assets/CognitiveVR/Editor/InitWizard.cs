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

        //cognitive3dLogo = EditorCore.LogoTexture;
    }

    List<string> pageids = new List<string>() { "welcome", "authenticate", "explaindynamic", "explainscene", "listdynamics", "uploadscene", "upload", "uploadsummary" };
    public int currentPage;

    private void OnGUI()
    {
        GUI.skin = EditorCore.WizardGUISkin;

        GUI.DrawTexture(new Rect(0, 0, 500, 500), EditorGUIUtility.whiteTexture);

        switch (pageids[currentPage])
        {
            case "welcome":WelcomeUpdate(); break;
            case "authenticate": AuthenticateUpdate(); break;
            case "scenetype": SceneTypeUpdate(); break;
            case "explaindynamic": DynamicExplainUpdate(); break;
            case "explainscene": SceneExplainUpdate(); break;
            case "listdynamics": ListDynamicUpdate(); break;
            case "uploadscene": UploadSceneUpdate(); break;
            case "upload": UploadUpdate(); break;
            case "uploadsummary": UploadSummaryUpdate(); break;
        }

        DrawFooter();
        Repaint(); //manually repaint gui each frame to make sure it's responsive
    }

    void WelcomeUpdate()
    {
        GUI.Label(steptitlerect, "STEP 1 - WELCOME", "steptitle");
        GUI.Label(boldlabelrect, "Welcome to the Cognitive3D SDK Setup.","boldlabel");
        GUI.Label(new Rect(30, 200, 440, 440), "This will guide you through the initial setup of your scene, and your scene's analytics will be ready for production at the end of this setup.","normallabel");
    }

    #region Auth Keys

    string apikey ="";
    string developerkey = "";
    void AuthenticateUpdate()
    {
        GUI.Label(steptitlerect, "STEP 2 - AUTHENTICATION", "steptitle");
        GUI.Label(boldlabelrect, "Please add your Cognitive3D authorization keys below to continue.", "boldlabel");

        //api key
        GUI.Label(new Rect(30, 200, 100, 30), "API Key", "miniheader");
        apikey = GUI.TextField(new Rect(30, 230, 400, 40), apikey);
        if (string.IsNullOrEmpty(apikey))
        {
            GUI.Label(new Rect(30, 230, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
        }
        else
        {
            GUI.Label(new Rect(440, 230, 24, 40), EditorCore.Checkmark, "image_centered");
        }

        //dev key
        GUI.Label(new Rect(30, 300, 100, 30), "Developer Key", "miniheader");
        developerkey = GUI.TextField(new Rect(30, 330, 400, 40), developerkey);
        if (string.IsNullOrEmpty(developerkey))
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

    void SceneTypeUpdate()
    {
        GUI.Label(steptitlerect, "STEP 3 - SELECT SCENE TYPE", "steptitle");

        GUI.Button(new Rect(10, 300, 235, 40), "360");
        GUI.Button(new Rect(255, 300, 240, 40), "3D");
    }

    #region Terminology

    void DynamicExplainUpdate()
    {
        GUI.Label(steptitlerect, "STEP 4 - WHAT IS A DYNAMIC OBJECT?", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "A Dynamic Object can be any GameObject that changes during a session.", "boldlabel");

        
        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

        GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsHightlight, "image_centered");

        GUI.color = Color.white;

        GUI.Label(new Rect(30, 350, 440, 440), "Not everything that moves needs to be a dynamic object. Small details that don't meaningfully impact a user's experience can be skipped. Anything the player grabs should be a dynamic object", "normallabel");
    }

    void SceneExplainUpdate()
    {
        GUI.Label(steptitlerect, "STEP 4 - WHAT IS A DYNAMIC OBJECT?", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "A Dynamic Object can be any GameObject that changes during a session.", "boldlabel");

        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneBackground, "image_centered");

        GUI.color = new Color(1, 1, 1, Mathf.Sin(Time.realtimeSinceStartup * 4) * 0.4f + 0.6f);
        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.SceneHighlight, "image_centered");
        GUI.color = Color.white;
        
        GUI.Box(new Rect(100, 70, 300, 300), EditorCore.ObjectsBackground, "image_centered");

        GUI.Label(new Rect(30, 350, 440, 440), "Not everything that moves needs to be a dynamic object. Small details that don't meaningfully impact a user's experience can be skipped. Anything the player grabs should be a dynamic object", "normallabel");
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
    }
    
    void RefreshSceneDynamics()
    {
        _cachedDynamics = FindObjectsOfType<DynamicObject>();
    }

    void ListDynamicUpdate()
    {
        GUI.Label(steptitlerect, "STEP 3 - PREPARE OBJECTS", "steptitle");

        GUI.Label(new Rect(30, 45, 440, 440), "These are the current <color=#8A9EB7FF>Dynamic Objects</color> currently tracked in your scene:", "boldlabel");

        Rect mesh = new Rect(30, 95, 120, 30);
        GUI.Label(mesh, "Dynamic Mesh Name", "dynamicheader");
        Rect gameobject = new Rect(190, 95, 120, 30);
        GUI.Label(gameobject, "GameObject", "dynamicheader");
        Rect uploaded = new Rect(380, 95, 120, 30);
        GUI.Label(uploaded, "Uploaded", "dynamicheader");

        DynamicObject[] tempdynamics = GetDynamicObjects;


        Rect innerScrollSize = new Rect(30, 0, 420, tempdynamics.Length * 30); //TODO generate from the number of dynamic object lines there are
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

        if (GUI.Button(new Rect(180,400,140,40),"Upload All", "button_bluetext"))
        {
            Debug.Log("upload all dynamics");
        }
    }

    //each row is 30 pixels
    void DrawDynamicObject(DynamicObject dynamic, Rect rect, bool darkbackground)
    {
        if (darkbackground)
            GUI.Box(rect, "", "dynamicentry_even");
        else
            GUI.Box(rect, "", "dynamicentry_odd");
        Rect mesh = new Rect(rect.x + 10, rect.y, 120, rect.height);
        Rect gameobject = new Rect(rect.x + 160, rect.y, 120, rect.height);

        Rect collider = new Rect(rect.x + 320, rect.y, 24, rect.height);
        Rect uploaded = new Rect(rect.x + 360, rect.y, 24, rect.height);

        GUI.Label(mesh, dynamic.MeshName, "dynamiclabel");
        GUI.Label(gameobject, dynamic.gameObject.name, "dynamiclabel");
        if (!dynamic.HasCollider())
        {
            GUI.Label(collider, new GUIContent(EditorCore.Alert,"Tracking Gaze requires a collider"), "image_centered");
        }

        if (EditorCore.GetExportedDynamicObjectNames().Contains(dynamic.MeshName))
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
        GUI.Label(steptitlerect, "STEP 4 - SCENE UPLOAD", "steptitle");


        GUI.Label(new Rect(30, 45, 440, 440), "All geometry without a <color=#8A9EB7FF>Dynamic Object</color> component will be exported and uploaded to <color=#8A9EB7FF>SceneExplorer</color>.", "boldlabel");

        string selectBlender = "Select Blender.exe";
#if UNITY_EDITOR_OSX
        selectBlender = "Select Blender.app";
#endif
        GUI.Label(new Rect(30, 120, 100, 30), selectBlender, "miniheader");

        
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

        if (GUI.Button(new Rect(180, 400, 120, 40), "Export Scene", "button_bluetext"))
        {
            Debug.Log("export scene");

            CognitiveVR_SceneExportWindow.ExportScene(true, selectedExportQuality.ExportStaticOnly, selectedExportQuality.MinExportGeoSize, selectedExportQuality.TextureQuality, "companyname", selectedExportQuality.DiffuseTextureName);
        }

        if (EditorCore.HasSceneExportFiles(CognitiveVR_Preferences.FindCurrentScene()))
        {
            GUI.Label(new Rect(300, 400, 24, 40), EditorCore.Checkmark, "image_centered");
        }
        else
        {
            GUI.Label(new Rect(300, 400, 24, 40), EditorCore.EmptyCheckmark, "image_centered");
        }
    }

    void UploadUpdate()
    {
        GUI.Label(steptitlerect, "STEP 5 - UPLOAD", "steptitle");
        GUI.Label(new Rect(30, 100, 440, 440), "In the final step, we complete the upload process to our <color=#8A9EB7FF>SceneExplorer</color> servers.\n\n\n" +
            "Once your Scene is uploaded, you will have to create a new version if you would like to edit the base geometry.\n\n\n"+
            "For <color=#8A9EB7FF>Dynamic Objects</color>, you will be able to continue editing those later in the \"<color=#8A9EB7FF>Manage Objects</color>\" menu.", "normallabel");
    }

    void UploadSummaryUpdate()
    {
        GUI.Label(steptitlerect, "STEP 7 - UPLOAD", "steptitle");
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

        GUI.Label(new Rect(30, 390, 440, 440), "You can add <color=#8A9EB7FF>ExitPoll</color> surveys, update <color=#8A9EB7FF>Dynamic Objects</color>, and add user engagement scripts after this process is complete", "normallabel");
    }

    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 450, 500, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        DrawBackButton();

        DrawNextButton();
    }

    void DrawNextButton()
    {
        bool buttonDisabled = false;
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
                    text = "Keys Accepted";
                }
                break;
            case "tagdynamics":
                break;
            case "listdynamics":

                int dynamiccount = GetDynamicObjects.Length;

                text = dynamiccount +"/" + dynamiccount + " Uploaded";
                buttonrect = new Rect(350, 460, 140, 30);
                break;
            case "uploadscene":
                //buttonDisabled = true;
                break;
            case "upload":
                //onclick += () => EditorCore.RefreshSceneVersion();
                text = "I understand, Continue";
                buttonrect = new Rect(290, 460, 200, 30);
                break;
            case "uploadsummary":

                System.Action completeSceneUpload = delegate () {
                    CognitiveVR_Preferences.SceneSettings current = CognitiveVR_Preferences.FindCurrentScene();
                    CognitiveVR_SceneExportWindow.UploadDynamicObjects();
                };

                System.Action completeScreenshot = delegate(){
                    CognitiveVR_Preferences.SceneSettings current = CognitiveVR_Preferences.FindCurrentScene();
                    CognitiveVR_SceneExportWindow.UploadDecimatedScene(current, completeSceneUpload);
                };

                onclick = () => EditorCore.SaveCurrentScreenshot(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
                
                text = "Upload";
                break;
        }

        if (buttonDisabled)
        {
            GUI.Button(buttonrect, text, "button_disabled");
        }
        else
        {
            if (GUI.Button(buttonrect, text))
            {
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
            case "uploadsummary":
                //buttonDisabled = true;
                text = "Cancel";
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
                onclick.Invoke();
            }
        }
    }
}
