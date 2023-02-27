using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;

namespace Cognitive3D
{
    internal class ProjectSetupWindow : EditorWindow
    {
        Rect steptitlerect = new Rect(30, 0, 100, 440);

        internal static void Init()
        {
            ProjectSetupWindow window = (ProjectSetupWindow)EditorWindow.GetWindow(typeof(ProjectSetupWindow), true, "Project Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.Show();

            window.LoadKeys();
            window.GetSelectedSDKs();

            ExportUtility.ClearUploadSceneSettings();
        }

        enum Page
        {
            Welcome,
            APIKeys,
            SDKSelection,
            Glia,
            SRAnipal,
            Recompile,
            Wave,
            NextSteps,
            DynamicSetup
        }
        Page currentPage;

        static int lastDevKeyResponseCode = 0;
        private void OnGUI()
        {
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 500, 550), EditorGUIUtility.whiteTexture);

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Equals) { currentPage++; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Minus) { currentPage--; }

            switch (currentPage)
            {
                case Page.Welcome:
                    WelcomeUpdate();
                    break;
                case Page.APIKeys:
                    AuthenticateUpdate();
                    break;
                case Page.SDKSelection:
                    SelectSDKUpdate();
                    break;
                case Page.Glia:
                    GliaSetup();
                    break;
                case Page.SRAnipal:
                    SRAnipalSetup();
                    break;
                case Page.Recompile:
                    WaitForCompile();
                    break;
                case Page.Wave:
                    ViveFocusSetup();
                    break;
                case Page.NextSteps:
                    DoneUpdate();
                    break;
                case Page.DynamicSetup:
                    DoneUpdate();
                    break;
                default: break;
            }

            //header
            //GUI.Label(steptitlerect, "Project Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")", "steptitle");

            DrawFooter();
            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        void WelcomeUpdate()
        {
            //GUI.Label(new Rect(30, 45, 440, 440), "Welcome to the " + EditorCore.DisplayValue(DisplayKey.FullName) + " SDK Scene Setup.", "boldlabel");
            GUI.Label(new Rect(30, 45, 440, 440), "There is written documentation and a video guide to help you configure your project for Cognitive3D Analytics.", "normallabel");
            if (GUI.Button(new Rect(130, 175, 240, 30), "Open Documentation Site"))
            {
                Application.OpenURL("https://docs.cognitive3d.com/unity/minimal-setup-guide");
            }
        }

        #region Auth Keys

        string apikey = "";
        string developerkey = "";
        void AuthenticateUpdate()
        {
            GUI.Label(steptitlerect, "AUTHENTICATION", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "Please add your " + EditorCore.DisplayValue(DisplayKey.ShortName) + " authorization keys.", "boldlabel");

            GUI.Label(new Rect(30, 100, 440, 440), "These should be kept private to your organization.\n\nThe Developer Key is saved to Unity Editor Prefs (specific to the current user) and is never included in a build.\n\nThe Application Key is saved in Cognitive3D_Preferences asset.\n\nThese are available on the Project Dashboard. ", "normallabel");

            if (GUI.Button(new Rect(130, 290, 240, 30), "Open Dashboard"))
            {
                Application.OpenURL("https://app.cognitive3d.com");
            }
            //dev key
            GUI.Label(new Rect(30, 315, 100, 30), "Developer Key", "miniheader");
            if (string.IsNullOrEmpty(developerkey)) //empty
            {
                GUI.Label(new Rect(30, 345, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
                GUI.Label(new Rect(440, 345, 24, 40), EditorCore.EmptyCheckmark, "image_centered");
                lastDevKeyResponseCode = 0;
                developerkey = EditorCore.TextField(new Rect(30, 345, 400, 40), developerkey, 32);
            }
            else if (lastDevKeyResponseCode == 200) //valid key
            {
                GUI.Label(new Rect(440, 345, 24, 40), EditorCore.Checkmark, "image_centered");
                string previous = developerkey;
                developerkey = EditorCore.TextField(new Rect(30, 345, 400, 40), developerkey, 32);
                if (previous != developerkey)
                    lastDevKeyResponseCode = 0;
            }
            else if (lastDevKeyResponseCode == 0) //maybe valid key? needs to be checked
            {
                GUI.Label(new Rect(440, 345, 24, 40), new GUIContent(EditorCore.Question, "Not validated"), "image_centered");
                developerkey = EditorCore.TextField(new Rect(30, 345, 400, 40), developerkey, 32);
            }
            else //invalid key
            {
                GUI.Label(new Rect(440, 345, 24, 40), new GUIContent(EditorCore.Error, "Invalid or Expired"), "image_centered");
                string previous = developerkey;
                developerkey = EditorGUI.TextField(new Rect(30, 345, 400, 40), developerkey, "textfield_warning");
                if (previous != developerkey)
                    lastDevKeyResponseCode = 0;
            }

            if (lastDevKeyResponseCode != 200 && lastDevKeyResponseCode != 0)
            {
                GUI.Label(new Rect(30, 390, 400, 30), "This Developer Key is invalid or expired. Please ensure the developer key is valid on the dashboard. Developer Keys expire automatically after 90 days.", "miniwarning");
            }

            //api key
            GUI.Label(new Rect(30, 410, 100, 30), "Application Key", "miniheader");
            apikey = EditorCore.TextField(new Rect(30, 440, 400, 40), apikey, 32);
            if (string.IsNullOrEmpty(apikey))
            {
                GUI.Label(new Rect(30, 440, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
                GUI.Label(new Rect(440, 440, 24, 40), EditorCore.EmptyCheckmark, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(440, 440, 24, 40), EditorCore.Checkmark, "image_centered");
            }

        }

        void SaveKeys()
        {
            EditorPrefs.SetString("developerkey", developerkey);
            EditorCore.GetPreferences().ApplicationKey = apikey;

            EditorUtility.SetDirty(EditorCore.GetPreferences());
            AssetDatabase.SaveAssets();
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

        bool hasDoneInitialSDKSelection = false;
        void GetSelectedSDKs()
        {
            if (hasDoneInitialSDKSelection == false)
            {
                hasDoneInitialSDKSelection = true;
                selectedsdks.Clear();
#if C3D_STEAMVR2
                selectedsdks.Add("C3D_STEAMVR2");
#endif
#if C3D_OCULUS
            selectedsdks.Add("C3D_OCULUS");
#endif
#if C3D_SRANIPAL
        selectedsdks.Add("C3D_SRANIPAL");
#endif
#if C3D_VIVEWAVE
        selectedsdks.Add("C3D_VIVEWAVE");
#endif
#if C3D_VARJOVR
        selectedsdks.Add("C3D_VARJOVR");
#endif
#if C3D_VARJOXR
        selectedsdks.Add("C3D_VARJOXR");
#endif
#if C3D_PICOVR
        selectedsdks.Add("C3D_PICOVR");
#endif
#if C3D_PICOXR
        selectedsdks.Add("C3D_PICOXR");
#endif
#if C3D_WINDOWSMR
            selectedsdks.Add("C3D_WINDOWSMR");
#endif
#if C3D_OMNICEPT
            selectedsdks.Add("C3D_OMNICEPT");
#endif
#if C3D_MRTK
            selectedsdks.Add("C3D_MRTK");
#endif
            }

            //C3D_Default doesn't enable or change any behaviour - only used in scene setup window and written to define symbols for debugging purposes
            if (selectedsdks.Count == 0)
            {
                selectedsdks.Add("C3D_DEFAULT");
            }
        }

        Vector2 sdkScrollPos;
        List<string> selectedsdks = new List<string>();

        class SDKDefine
        {
            public string Name;
            public string Define;
            public string Tooltip;
            public SDKDefine(string name, string define, string tooltip="")
            {
                Name = name;
                Define = define;
                Tooltip = tooltip;
            }
        }

        List<SDKDefine> SDKNamesDefines = new List<SDKDefine>()
        {
            new SDKDefine("Default","C3D_DEFAULT", "Uses UnityEngine.InputDevice Features to broadly support all XR SDKs" ),
            new SDKDefine("SteamVR 2.7.3 and OpenVR","C3D_STEAMVR2", "OpenVR Input System" ),
            new SDKDefine("Oculus Integration 32.0","C3D_OCULUS", "Adds Social Features" ),
            new SDKDefine("HP Omnicept Runtime 1.12","C3D_OMNICEPT", "Adds Eye Tracking and Sensors" ),
            new SDKDefine("SRanipal Runtime","C3D_SRANIPAL","Adds Eyetracking for the Vive Pro Eye" ), //previously C3D_VIVEPROEYE
            new SDKDefine("Varjo XR 3.0.0","C3D_VARJOXR", "Adds Eye Tracking for Varjo Headsets"),
            new SDKDefine("Vive Wave 5.0.2","C3D_VIVEWAVE", "Adds Eye Tracking for Focus 3" ),
            new SDKDefine("Pico Unity XR Platform 2.1.3","C3D_PICOXR", "Adds Eye Tracking for Pico Neo 3 Eye" ),
            new SDKDefine("MRTK 2.5.4","C3D_MRTK", "Adds Eye Tracking for Hololens 2" ),
            new SDKDefine("Windows Mixed Reality XR","C3D_WINDOWSMR", "Deprecated. Select 'Default'" ), //legacy
            new SDKDefine("Varjo VR","C3D_VARJOVR", "Prefer to upgrade to Varjo XR instead" ), //legacy
            new SDKDefine("PicoVR Unity SDK 2.8.12","C3D_PICOVR", "Prefer to upgrade to Pico XR instead" ), //legacy
        };

        void SelectSDKUpdate()
        {
            //additional SDK features

            GUI.Label(steptitlerect, "ADDITIONAL SDK SUPPORT", "steptitle");
            //GUI.Label(new Rect(30, 45, 440, 440), "Please select any SDKs you are using in this project.", "boldlabel");

            GUI.Label(new Rect(30, 45, 440, 440), "By default, we support most XR features, but some additional software may be required to support specific features.\n\nShift click to select multiple", "normallabel");

            Rect innerScrollSize = new Rect(30, 0, 420, SDKNamesDefines.Count * 36);
            sdkScrollPos = GUI.BeginScrollView(new Rect(30, 200, 440, 270), sdkScrollPos, innerScrollSize, false, false);

            for (int i = 0; i < SDKNamesDefines.Count; i++)
            {
                bool selected = selectedsdks.Contains(SDKNamesDefines[i].Define);
                GUIContent content = new GUIContent(SDKNamesDefines[i].Name);
                float separator = 0;
                if (i > 8)
                {
                    separator = 32;
                }
                if (!string.IsNullOrEmpty(SDKNamesDefines[i].Tooltip))
                {
                    content.tooltip = SDKNamesDefines[i].Tooltip;
                }

                if (GUI.Button(new Rect(30, i * 32+ separator, 420, 30), content, selected ? "button_blueoutlineleft" : "button_disabledoutline"))
                {
                    if (selected)
                    {
                        selectedsdks.Remove(SDKNamesDefines[i].Define);
                    }
                    else
                    {
                        if (Event.current.shift) //add
                        {
                            selectedsdks.Add(SDKNamesDefines[i].Define);
                        }
                        else //set
                        {
                            selectedsdks.Clear();
                            selectedsdks.Add(SDKNamesDefines[i].Define);
                        }
                    }
                }
                GUI.Label(new Rect(420, i * 32 + separator, 24, 30), selected ? EditorCore.Checkmark : EditorCore.EmptyCheckmark, "image_centered");
                if (i == 9)
                {
                    int kerning = 4;
                    GUI.Label(new Rect(30, i * 32 + kerning, 420, 30), "Legacy Support", "boldlabel");
                }
            }

            GUI.EndScrollView();
        }

        #region Glia Setup
        //added to tell developer to add assemblies so C3D can use Glia api

        bool hasDoneGliaStartCheck = false;
        bool gliaAssemblyExists = false;

        void GliaStart()
        {
            if (hasDoneGliaStartCheck) { return; }
            hasDoneGliaStartCheck = true;

            var assets = AssetDatabase.FindAssets("GliaAssembly");
            var editorAssets = AssetDatabase.FindAssets("GliaEditorAssembly");
            gliaAssemblyExists = assets.Length > 0 && editorAssets.Length > 0;
        }

        void GliaSetup()
        {
            if (!selectedsdks.Contains("C3D_OMNICEPT"))
            {
                currentPage++;
                return;
            }
            GliaStart();

            GUI.Label(steptitlerect, "OMNICEPT ASSEMBLY SETUP", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "Check for Assembly Definition Files.", "boldlabel");

            GUI.Label(new Rect(30, 100, 440, 440), "To automatically access Omnicept's Glia API, the Cognitive3D SDK needs to reference the Glia Assembly, which doesn't exist by default." +
                "\n\nUse the button below to create the expected Assembly Definition files if they do not already exist.", "normallabel");

            if (GUI.Button(new Rect(130, 290, 240, 30), "Create Assemblies"))
            {
                var assets = AssetDatabase.FindAssets("GliaAssembly");
                if (assets.Length == 0)
                {
                    //new text document?
                    string assemblyDefinitionContent = "{\"name\": \"GliaAssembly\",\"rootNamespace\": \"\",\"references\": [],\"includePlatforms\": [],\"excludePlatforms\": [],\"allowUnsafeCode\": false,\"overrideReferences\": false,\"precompiledReferences\": [],\"autoReferenced\": true,\"defineConstraints\": [],\"versionDefines\": [],\"noEngineReferences\": false}";
                    string filepath = Application.dataPath + "/Glia/";

                    System.IO.File.WriteAllText(filepath + "GliaAssembly.asmdef", assemblyDefinitionContent);
                    EditorUtility.SetDirty(EditorCore.GetPreferences());
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                assets = AssetDatabase.FindAssets("GliaEditorAssembly");
                if (assets.Length == 0)
                {
                    //new text document?
                    string assemblyDefinitionContent = "{\"name\": \"GliaEditorAssembly\",\"rootNamespace\": \"\",\"references\": [\"GliaAssembly\"],\"includePlatforms\": [\"Editor\"],\"excludePlatforms\": [],\"allowUnsafeCode\": false,\"overrideReferences\": false,\"precompiledReferences\": [],\"autoReferenced\": true,\"defineConstraints\": [],\"versionDefines\": [],\"noEngineReferences\": false}";
                    string filepath = Application.dataPath + "/Glia/Editor/";

                    System.IO.File.WriteAllText(filepath + "GliaEditorAssembly.asmdef", assemblyDefinitionContent);
                    EditorUtility.SetDirty(EditorCore.GetPreferences());
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                hasDoneGliaStartCheck = false;
            }

            if (gliaAssemblyExists == false)
            {
                //empty checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.EmptyCheckmark, "image_centered");

            }
            else
            {
                //full checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.Checkmark, "image_centered");
            }

        }

        #endregion

        #region SRAnipal Setup
        //added to tell developer to add assemblies so C3D can use sranipal api

        bool hasDoneSRAnipalStartCheck = false;
        bool sranipalAssemblyExists = false;

        void SRAnipalStart()
        {
            if (hasDoneSRAnipalStartCheck) { return; }
            hasDoneSRAnipalStartCheck = true;

            var assets = AssetDatabase.FindAssets("SRAnipalAssembly");
            sranipalAssemblyExists = assets.Length > 0;
        }    

        void SRAnipalSetup()
        {
            if (!selectedsdks.Contains("C3D_SRANIPAL"))
            {
                currentPage++;
                return;
            }
            SRAnipalStart();

            GUI.Label(steptitlerect, "SRANIPAL ASSEMBLY SETUP", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "Check for Assembly Definition Files.", "boldlabel");

            

            GUI.Label(new Rect(30, 100, 440, 440), "To automatically access the SRAnipal API, the Cognitive3D SDK needs to reference the SRAnipal Assembly, which doesn't exist by default." +
    "\n\nUse the button below to create the expected Assembly Definition files if they do not already exist.", "normallabel");

            //button to add assemblies to sranipal folder
            if (GUI.Button(new Rect(130, 290, 240, 30), "Create Assemblies"))
            {
                var assets = AssetDatabase.FindAssets("SRAnipalAssembly");
                if (assets.Length == 0)
                {
                    //new text document?
                    string assemblyDefinitionContent = "{\"name\": \"SRAnipalAssembly\",\"rootNamespace\": \"\",\"references\": [],\"includePlatforms\": [],\"excludePlatforms\": [],\"allowUnsafeCode\": false,\"overrideReferences\": false,\"precompiledReferences\": [],\"autoReferenced\": true,\"defineConstraints\": [],\"versionDefines\": [],\"noEngineReferences\": false}";
                    string filepath = Application.dataPath+"/ViveSR/";

                    System.IO.File.WriteAllText(filepath + "SRAnipalAssembly.asmdef", assemblyDefinitionContent);
                    EditorUtility.SetDirty(EditorCore.GetPreferences());
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                hasDoneSRAnipalStartCheck = false;
            }

            //a checkmark if the assembly already exists
            if (sranipalAssemblyExists == false)
            {
                //empty checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.EmptyCheckmark, "image_centered");
                
            }
            else
            {
                //full checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.Checkmark, "image_centered");
            }    
        }

        void ViveFocusSetup()
        {
            if (!selectedsdks.Contains("C3D_VIVEWAVE"))
            {
                currentPage++;
                return;
            }

#if C3D_VIVEWAVE
            GUI.Label(steptitlerect, "VIVE WAVE SETUP", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "Add EyeManager to the scene.", "boldlabel");
            var eyeManager = Object.FindObjectOfType<Wave.Essence.Eye.EyeManager>();
            bool eyeManagerExists = eyeManager != null;

            GUI.Label(new Rect(30, 100, 440, 440), "To utilise the WaveVR Eye Tracking features, the scene needs a WaveEyeManager object, which doesn't exist by default." +
    "\n\nUse the button below to add the WaveEyeManager to the scene if it does not already exist.", "normallabel");

            //button to add assemblies to sranipal folder
            if (GUI.Button(new Rect(130, 290, 240, 30), "Create EyeManager"))
            {
                if (eyeManager == null)
                {
                    var m_EyeManager = new GameObject("WaveEyeManager");
                    m_EyeManager.AddComponent<Wave.Essence.Eye.EyeManager>();
                    UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                    eyeManagerExists = true;
                } 
            }

            //a checkmark if the assembly already exists
            if (eyeManagerExists == false)
            {
                //empty checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.EmptyCheckmark, "image_centered");

            }
            else
            {
                //full checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.Checkmark, "image_centered");
            }
#endif
        }

        #endregion

        #region WaitForCompile

        void WaitForCompile()
        {
            GUI.Label(steptitlerect, "WAIT FOR COMPILE", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "Applying selected SDKs and compiling.", "boldlabel");
            GUI.Label(new Rect(30, 100, 440, 440), "This may take a moment.", "normallabel");

            EditorCore.SetPlayerDefine(selectedsdks);

            if (EditorApplication.isCompiling) { return; }

            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found == null) //add Cognitive3D_manager
            {
                EditorCore.SpawnManager(EditorCore.DisplayValue(DisplayKey.ManagerName));
            }

            currentPage++;
        }


#endregion

        void DoneUpdate()
        {
            GUI.Label(steptitlerect, "DONE!", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "The <color=#8A9EB7FF>" + EditorCore.DisplayValue(DisplayKey.ManagerName) + "</color> in your scene will record user position, gaze and basic device information.\n\nYou can view sessions from the Dashboard.", "boldlabel");
            if (GUI.Button(new Rect(150, 150, 200, 40), "Open Dashboard", "button_bluetext"))
            {
                Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Dashboard);
            }

            GUI.Label(new Rect(30, 385, 440, 440), "Make sure your users understand your experience with a simple training scene. Add the Ready Room sample to your project", "boldlabel");
            if (GUI.Button(new Rect(150, 440, 200, 40), "Ready Room Setup", "button_bluetext"))
            {
                //can i open a specific package from the package manager window
                var readyRoomScenes = AssetDatabase.FindAssets("t:scene readyroom");
                if (readyRoomScenes.Length == 1)
                {
                    //ask if want save
                    if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(readyRoomScenes[0]));
                        Close();
                    }
                }
            }

            GUI.Label(new Rect(30, 205, 440, 440), "-Want to ask users about their experience?\n-Need to add more Dynamic Objects?\n-Have some Sensors?\n-Tracking user's gaze on a video or image?\n-Multiplayer?\n", "boldlabel");
            if (GUI.Button(new Rect(150, 320, 200, 40), "Open Documentation", "button_bluetext"))
            {
                Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Documentation);
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

            switch (currentPage)
            {
                case Page.Welcome:
                    break;
                case Page.APIKeys:
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
                        onclick += () => UnityEditor.VSAttribution.Cognitive3D.VSAttribution.SendAttributionEvent("Login", "Cognitive3D", apikey);
                    }

                    buttonDisabled = apikey == null || apikey.Length == 0 || developerkey == null || developerkey.Length == 0;
                    if (buttonDisabled)
                    {
                        text = "Keys Required";
                    }

                    if (buttonDisabled == false && lastDevKeyResponseCode != 200)
                    {
                        text = "Validate";
                    }

                    if (buttonDisabled == false && lastDevKeyResponseCode == 200)
                    {
                        text = "Next";
                    }
                    break;
                case Page.SDKSelection:
                    break;
                case Page.Recompile:
                    onclick = null;
                    buttonDisabled = true;
                    break;
                case Page.DynamicSetup:
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
            Rect buttonrect = new Rect(10, 510, 80, 30);

            switch (currentPage)
            {
                case Page.Welcome: buttonDisabled = true; break;
                case Page.APIKeys:
                case Page.Glia:
                case Page.SRAnipal:
                case Page.Recompile:
                case Page.Wave:
                case Page.DynamicSetup:
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