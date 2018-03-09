using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

public class InitWizard : EditorWindow
{
    Texture2D cognitive3dLogo;

    Rect steptitlerect = new Rect(30, 0, 100, 440);
    Rect boldlabelrect = new Rect(30, 100, 440, 440);

    public static void Init()
    {
        InitWizard window = (InitWizard)EditorWindow.GetWindow(typeof(InitWizard), true, "");
        window.minSize = new Vector2(500, 500);
        window.maxSize = new Vector2(500, 500);
        window.Show();

        //cognitive3dLogo = EditorCore.LogoTexture;
    }

    List<string> pageids = new List<string>() { "welcome", "authenticate", "scenetype", "tagdynamics", "listdynamics", "uploadscene" };
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
            case "tagdynamics": DynamicUpdate(); break;
            case "listdynamics": ListDynamicUpdate(); break;
            case "uploadscene": UploadSceneUpdate(); break;
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

    void AuthenticateUpdate()
    {
        GUI.Label(steptitlerect, "STEP 2 - AUTHENTICATE", "steptitle");
        GUI.Label(boldlabelrect, "Enter your authentication tokens.", "boldlabel");

        GUI.Label(new Rect(10, 200, 240, 40), "Developer Token", "labelfield");
        GUI.TextField(new Rect(250, 200, 240, 40), "1234-5678");
        GUI.Label(new Rect(10, 250, 240, 40), "Player Token", "labelfield");
        GUI.TextField(new Rect(250, 250, 240, 40), "1234-5678");
    }

    void SceneTypeUpdate()
    {
        GUI.Label(steptitlerect, "STEP 3 - SELECT SCENE TYPE", "steptitle");

        GUI.Button(new Rect(10, 300, 235, 40), "Sad 360");
        GUI.Button(new Rect(255, 300, 240, 40), "Awesome 3D");
    }

    void DynamicUpdate()
    {
        GUI.Label(steptitlerect, "STEP 4 - WHAT IS A DYNAMIC OBJECT?", "steptitle");

        Rect sinerect = new Rect(Mathf.Sin(Time.realtimeSinceStartup) * 50 + 240, 250, 20, 20);
        GUI.Box(sinerect, "box");

        Rect sinerect2 = new Rect(Mathf.Sin(Time.realtimeSinceStartup+1) * 25 + 240, Mathf.Sin(Time.realtimeSinceStartup + 1) * 25 + 200, 20, 20);
        GUI.Box(sinerect2, "box");

        GUI.Box(new Rect(100,180,300,150), "box");

        GUI.Label(boldlabelrect, "A Dynamic Object can be any GameObject that changes during a session.", "boldlabel");
        GUI.Label(new Rect(30, 350, 440, 440), "Not everything that moves needs to be a dynamic object. Small details that don't meaningfully impact a user's experience can be skipped. Anything the player grabs should be a dynamic object", "normallabel");
    }

    Vector2 dynamicScrollPosition;
    void ListDynamicUpdate()
    {
        GUI.Label(steptitlerect, "STEP X - PREPARE OBJECTS", "steptitle");

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
            dynamicrect = new Rect(30, i*30, 460, 30);
            DrawDynamicObject(tempdynamics[i], dynamicrect, i % 2 == 0);
        }

        GUI.EndScrollView();

        GUI.Box(new Rect(30, 120, 425, 270), "","box_sharp_alpha");

        if (GUI.Button(new Rect(30,400,440,40),"Upload All", "button_bluetext"))
        {
            Debug.Log("upload all dynamics");
        }

    }

    DynamicObject[] GetDynamicObjects //cache or something smart
    {
        get
        {
            return FindObjectsOfType<DynamicObject>();
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
        Rect uploaded = new Rect(rect.x + 350, rect.y, 40, rect.height);

        GUI.Label(mesh, dynamic.MeshName, "dynamiclabel");
        GUI.Label(gameobject, dynamic.gameObject.name, "dynamiclabel");
        GUI.Label(uploaded, EditorCore.Checkmark, "image_centered");
    }


    void UploadSceneUpdate()
    {
        GUI.Label(steptitlerect, "STEP 6 - UPLOAD SCENE", "steptitle");

        GUI.Button(new Rect(10, 300, 235, 40), "Take Screenshot");
        GUI.Button(new Rect(255, 300, 240, 40), "Upload Scene");
    }

    void DrawNextButton()
    {
        bool buttonDisabled = false;
        string text = "Next";
        System.Action onclick = () => currentPage++;
        Rect buttonrect = new Rect(410, 460, 80, 30);

        switch (currentPage)
        {
            case 4:
                buttonDisabled = true;
                text = "2/8 Uploaded";
                buttonrect = new Rect(350, 460, 140, 30);
                break;

            case 5: buttonDisabled = true;break;
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

        switch (currentPage)
        {
            case 0: buttonDisabled = true; break;
            case 4:
                buttonDisabled = true;
                text = "Cancel";
                buttonrect = new Rect(260, 460, 80, 30);
                break;
        }

        if (buttonDisabled)
        {
            GUI.Button(buttonrect, text, "button_disabledtext");
        }
        else
        {
            if (GUI.Button(buttonrect, text))
            {
                onclick.Invoke();
            }
        }
    }

    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 450, 500, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        //GUIStyle SquareButton = new GUIStyle(EditorStyles);       


        //GUILayout.BeginArea(new Rect(0, 470, 500, 30));
        //GUILayout.BeginHorizontal();

        //GUI.Box(new Rect(300, 470, 100, 30),"box");

        DrawBackButton();

        DrawNextButton();
        //GUILayout.Button("Next", SquareButton,GUILayout.Width(100));

        /*

        var rect = GUILayoutUtility.GetRect(0, 0, 0, 100);

        rect.width -= 20;
        rect.x += 10;
        GUI.DrawTexture(rect, EditorCore.LogoTexture, ScaleMode.ScaleToFit);

        GUILayout.Label((currentPage+1) + " / " + pageids.Count);
        EditorGUI.BeginDisabledGroup(!IsBackPageEnabled());
        if (GUILayout.Button("back"))
        {
            currentPage--;
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!IsNextPageEnabled());
        if (GUILayout.Button("next"))
        {
            currentPage++;
        }
        EditorGUI.EndDisabledGroup();*/

        //GUILayout.EndHorizontal();
        //GUILayout.EndArea();
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
