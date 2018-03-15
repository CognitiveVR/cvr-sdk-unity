using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;

public class ManageDynamicObjects : EditorWindow
{
    Rect steptitlerect = new Rect(30, 0, 100, 440);
    Rect boldlabelrect = new Rect(30, 100, 440, 440);

    public static void Init()
    {
        ManageDynamicObjects window = (ManageDynamicObjects)EditorWindow.GetWindow(typeof(ManageDynamicObjects), true, "");
        window.minSize = new Vector2(500, 500);
        window.maxSize = new Vector2(500, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUI.skin = EditorCore.WizardGUISkin;

        GUI.DrawTexture(new Rect(0, 0, 500, 500), EditorGUIUtility.whiteTexture);

        var currentscene = CognitiveVR_Preferences.FindCurrentScene();

        if (currentscene != null)
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS "+ currentscene.SceneName + " Version: " + currentscene.VersionNumber, "steptitle");
            
        }
        else
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS Scene Not Saved", "steptitle");
        }

        GUI.Label(new Rect(30, 45, 440, 440), "These are the current <color=#8A9EB7FF>Dynamic Objects</color> tracked in your scene:", "boldlabel");

        //headers
        Rect mesh = new Rect(30, 95, 120, 30);
        GUI.Label(mesh, "Dynamic Mesh Name", "dynamicheader");
        Rect gameobject = new Rect(190, 95, 120, 30);
        GUI.Label(gameobject, "GameObject", "dynamicheader");
        Rect ids = new Rect(320, 95, 120, 30);
        GUI.Label(ids, "Ids", "dynamicheader");
        Rect uploaded = new Rect(380, 95, 120, 30);
        GUI.Label(uploaded, "Uploaded", "dynamicheader");


        //content
        DynamicObject[] tempdynamics = GetDynamicObjects;
        Rect innerScrollSize = new Rect(30, 0, 420, tempdynamics.Length * 30); //TODO generate from the number of dynamic object lines there are
        dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 120, 440, 270), dynamicScrollPosition, innerScrollSize, false, true);

        Rect dynamicrect;
        for (int i = 0; i < tempdynamics.Length; i++)
        {
            if (tempdynamics[i] == null) { RefreshSceneDynamics(); GUI.EndScrollView(); return; }
            dynamicrect = new Rect(30, i * 30, 460, 30);
            DrawDynamicObject(tempdynamics[i], dynamicrect, i % 2 == 0,i%3==0, i % 5 == 0);
        }
        GUI.EndScrollView();
        GUI.Box(new Rect(30, 120, 425, 270), "", "box_sharp_alpha");

        //buttons
        if (GUI.Button(new Rect(180, 400, 140, 40), "Upload All", "button_bluetext"))
        {
            Debug.Log("upload all dynamics");
            CognitiveVR_SceneExportWindow.ExportAllDynamicsInScene();
            CognitiveVR_SceneExportWindow.UploadDynamicObjects(true);
        }

        DrawFooter();
        Repaint(); //manually repaint gui each frame to make sure it's responsive
    }
    
    #region Dynamic Objects

    Vector2 dynamicScrollPosition;

    DynamicObject[] _cachedDynamics;
    DynamicObject[] GetDynamicObjects { get { if (_cachedDynamics == null || _cachedDynamics.Length == 0) { _cachedDynamics = FindObjectsOfType<DynamicObject>(); } return _cachedDynamics; } }

    private void OnFocus()
    {
        RefreshSceneDynamics();
    }
    
    void RefreshSceneDynamics()
    {
        _cachedDynamics = FindObjectsOfType<DynamicObject>();
    }

    //each row is 30 pixels
    void DrawDynamicObject(DynamicObject dynamic, Rect rect, bool darkbackground, bool deleted, bool notuploaded)
    {
        if (deleted)
        {
            //warning or color red
            GUI.color = new Color(1,0,0,0.3f);
        }
        if (notuploaded)
        {
            //ie new
            //color green
            GUI.color = new Color(0, 1, 0, 0.3f);
        }

        if (darkbackground)
            GUI.Box(rect, "", "dynamicentry_even");
        else
            GUI.Box(rect, "", "dynamicentry_odd");

        GUI.color = Color.white;

        Rect mesh = new Rect(rect.x + 10, rect.y, 120, rect.height);
        Rect gameobject = new Rect(rect.x + 160, rect.y, 120, rect.height);
        Rect id = new Rect(rect.x + 290, rect.y, 120, rect.height);

        Rect collider = new Rect(rect.x + 320, rect.y, 24, rect.height);
        Rect uploaded = new Rect(rect.x + 380, rect.y, 24, rect.height);

        GUI.Label(mesh, dynamic.MeshName, "dynamiclabel");
        GUI.Label(gameobject, dynamic.gameObject.name, "dynamiclabel");
        GUI.Label(id, dynamic.CustomId.ToString(), "dynamiclabel");
        if (!dynamic.HasCollider())
        {
            GUI.Label(collider, new GUIContent(EditorCore.Alert,"Tracking Gaze requires a collider"), "image_centered");
        }
        GUI.Label(uploaded, EditorCore.Checkmark, "image_centered");
    }

    #endregion
    
    void DrawFooter()
    {
        GUI.color = EditorCore.BlueishGrey;
        GUI.DrawTexture(new Rect(0, 450, 500, 50), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;

        //DrawBackButton();

        //DrawNextButton();
    }
    /*
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
                text = "I understand, Continue";
                buttonrect = new Rect(290, 460, 200, 30);
                break;
            case "uploadsummary":
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
    }*/
}
