using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D;

namespace Cognitive3D
{
    internal class ProjectSetupWindow : EditorWindow
    {
        Rect steptitlerect = new Rect(30, 5, 100, 440);
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

        internal static void Init(Rect position)
        {
            ProjectSetupWindow window = (ProjectSetupWindow)EditorWindow.GetWindow(typeof(ProjectSetupWindow), true, "Project Setup (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(500, 550);
            window.position = new Rect(position.x + 5, position.y + 5, 500, 550);
            window.Show();

            window.LoadKeys();
            window.GetSelectedSDKs();

            ExportUtility.ClearUploadSceneSettings();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnReload()
        {
            SubscriptionExpirationDate = null;
        }

        enum Page
        {
            Welcome,
            APIKeys,
            Organization,
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
                case Page.Organization:
                    OrganizationUpdate();
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
                    DynamicUpdate();
                    break;
                default: break;
            }

            DrawFooter();
            Repaint(); //manually repaint gui each frame to make sure it's responsive
        }

        void WelcomeUpdate()
        {
            GUI.Label(new Rect(30, 30, 440, 80), EditorCore.LogoTexture, "image_centered");

            GUI.Label(new Rect(30, 120, 440, 440), "Welcome to the " + EditorCore.DisplayValue(DisplayKey.FullName) + " SDK Project Setup! This window will guide you through setting up our SDK in your project and ensuring the features available from packages in your project are automatically recorded." +
                "\n\nAt the end of this setup process, you will have production ready analytics and a method to replay individual sessions", "normallabel");
            GUI.Label(new Rect(30, 390, 440, 440), "There is written documentation and a video guide to help you configure your project.", "normallabel");
            string url = "https://docs.cognitive3d.com/unity/minimal-setup-guide";
            if (GUI.Button(new Rect(150, 340, 200, 30), new GUIContent("Open Online Documentation",url)))
            {
                Application.OpenURL(url);
            }
        }

        #region Auth Keys

        string apikey = "";
        string developerkey = "";
        void AuthenticateUpdate()
        {
            GUI.Label(steptitlerect, "AUTHENTICATION", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "Please add your " + EditorCore.DisplayValue(DisplayKey.ShortName) + " Developer Key." +
                "\n\nThe Developer Key is saved to Unity Editor Preferences (specific to the current user) and is never included in a build." +
                "\n\nThis should be kept private to your organization."+
                "\n\nThis is available on the Project Dashboard.", "normallabel");

            if (GUI.Button(new Rect(130, 280, 240, 30), "Open Dashboard"))
            {
                Application.OpenURL("https://app.cognitive3d.com");
            }
            //dev key
            GUI.Label(new Rect(30, 315, 100, 30), "Developer Key", "miniheader");
            if (string.IsNullOrEmpty(developerkey)) //empty
            {
                GUI.Label(new Rect(30, 345, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
                GUI.Label(new Rect(440, 345, 24, 40), EditorCore.CircleEmpty32, "image_centered");
                lastDevKeyResponseCode = 0;
                developerkey = EditorCore.TextField(new Rect(30, 345, 400, 40), developerkey, 32);
            }
            else if (lastDevKeyResponseCode == 200) //valid key
            {
                GUI.Label(new Rect(440, 345, 30, 40), EditorCore.CircleCheckmark32, "image_centered");
                string previous = developerkey;
                developerkey = EditorCore.TextField(new Rect(30, 345, 400, 40), developerkey, 32);
                if (previous != developerkey)
                    lastDevKeyResponseCode = 0;
            }
            else if (lastDevKeyResponseCode == 0) //maybe valid key? needs to be checked
            {
                GUI.Label(new Rect(440, 345, 30, 40), new GUIContent(EditorCore.Info, "Not validated"), "image_centered");
                developerkey = EditorCore.TextField(new Rect(30, 345, 400, 40), developerkey, 32);
            }
            else //invalid key
            {
                GUI.Label(new Rect(440, 345, 30, 40), new GUIContent(EditorCore.Error, "Invalid or Expired"), "image_centered");
                string previous = developerkey;
                developerkey = EditorCore.TextField(new Rect(30, 345, 400, 40), developerkey, 32, "textfield_warning");
                if (previous != developerkey)
                    lastDevKeyResponseCode = 0;
            }

            if (lastDevKeyResponseCode != 200 && lastDevKeyResponseCode != 0)
            {
                GUI.Label(new Rect(30, 390, 400, 30), "This Developer Key is invalid or expired. Please ensure the developer key is valid on the dashboard. Developer Keys expire automatically after 90 days.", "miniwarning");
            }
        }

        string OrganizationName;
        string SubscriptionPlan;
        long SubscriptionExpirationDateLong;
        static System.DateTime? SubscriptionExpirationDate;
        bool SubscriptionTrial;

        void OrganizationUpdate()
        {
            GUI.Label(steptitlerect, "ORGANIZATION", "steptitle");

            GUI.Label(new Rect(60, 30, 440, 440), "Organization Name: " + OrganizationName, "normallabel");
            GUI.Label(new Rect(60, 60, 440, 440), "Current Subscription Plan: " + SubscriptionPlan + (SubscriptionTrial ? " (Trial)" : ""), "normallabel");

            string expirationDateString = string.Empty;
            if (string.IsNullOrEmpty(expirationDateString) && SubscriptionExpirationDateLong > 0L)
            {
                if (!SubscriptionExpirationDate.HasValue)
                {
                    SubscriptionExpirationDate = UnixTimeStampToDateTime(SubscriptionExpirationDateLong);
                }
                expirationDateString = SubscriptionExpirationDate.Value.Date.ToString("dd MMMM yyyy");
            }
            else
            {
                expirationDateString = "Never";
            }

            GUI.Label(new Rect(60, 90, 440, 440), "Expiration Date: " + expirationDateString, "normallabel");

            GUI.Label(new Rect(30, 150, 440, 440), "The Application Key is saved in Cognitive3D_Preferences asset. It is used to identify where session data should be collected.\n\nThis is included with a build, but otherwise should be kept private to your organization.", "normallabel");

            //api key
            GUI.Label(new Rect(30, 315, 100, 30), "Application Key", "miniheader");
            apikey = EditorCore.TextField(new Rect(30, 345, 400, 40), apikey, 32);
            if (string.IsNullOrEmpty(apikey))
            {
                GUI.Label(new Rect(30, 345, 400, 40), "asdf-hjkl-1234-5678", "ghostlabel");
                GUI.Label(new Rect(440, 345, 30, 40), EditorCore.CircleEmpty32, "image_centered");
            }
            else
            {
                GUI.Label(new Rect(440, 345, 30, 40), EditorCore.CircleCheckmark32, "image_centered");
            }

        }

        private void SaveDevKey()
        {
            EditorPrefs.SetString("developerkey", developerkey);
        }

        private void SaveApplicationKey()
        {
            EditorCore.GetPreferences().ApplicationKey = apikey;
            EditorUtility.SetDirty(EditorCore.GetPreferences());
            AssetDatabase.SaveAssets();
        }

        private void LoadKeys()
        {
            developerkey = EditorPrefs.GetString("developerkey");
            apikey = EditorCore.GetPreferences().ApplicationKey;
        }

        [System.Serializable]
        private class ApplicationKeyResponseData
        {
            public string apiKey;
            public bool valid;
        }

        private void GetApplicationKeyResponse(int responseCode, string error, string text)
        {
            if (responseCode != 200)
            {
                Debug.LogError("GetApplicationKeyResponse response code: " + responseCode);
                return;
            }
            ApplicationKeyResponseData responseData = JsonUtility.FromJson<ApplicationKeyResponseData>(text);
            apikey = responseData.apiKey;
        }

        [System.Serializable]
        private class SubscriptionResponseData
        {
            public long beginning;
            public long expiration;
            public string planType;
            public bool isFreeTrial;
        }

        private System.DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp/1000).ToLocalTime();
            return dateTime;
        }

        private void GetSubscriptionResponse(int responseCode, string error, string text)
        {
            if (responseCode != 200)
            {
                Debug.LogError("GetSubscriptionResponse response code: " + responseCode);
                return;
            }

            SubscriptionResponseData[] data = Util.GetJsonArray<SubscriptionResponseData>(text);
            if (data == null || data.Length == 0)
            {
                Debug.LogError("GetSubscriptionResponse data is null or invalid. Please get in touch");
            }
            else
            {
                foreach(var v in data)
                {
                    SubscriptionPlan = v.planType;
                    SubscriptionTrial = v.isFreeTrial;
                    SubscriptionExpirationDateLong = v.expiration;
                }
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

        bool hasDoneSDKRecommendation;
        void OnGetPackages(UnityEditor.PackageManager.PackageCollection packages)
        {
            //search from specific sdks (single headset support) to general runtimes (openvr, etc)
            foreach(var package in packages)
            {
                if (package.name == "com.unity.xr.picoxr")
                {
                    DisplayRecommendationPopup("C3D_PICOXR","Pico Unity Integration SDK");
                    return;
                }
                if (package.name == "com.varjo.xr")
                {
                    DisplayRecommendationPopup("C3D_VARJOXR", "Varjo XR Package");
                    return;
                }
                if (package.name == "com.htc.upm.wave.xrsdk")
                {
                    DisplayRecommendationPopup("C3D_WAVE", "Vive Wave Package");
                    return;
                }
            }

            //specific assets
            var SRAnipalAssets = AssetDatabase.FindAssets("SRanipal.dll");
            if (SRAnipalAssets.Length > 0)
            {
                DisplayRecommendationPopup("C3D_SRANIPAL","SRanipal");
                return;
            }

            var GliaAssets = AssetDatabase.FindAssets("lib-client-csharp");
            if (GliaAssets.Length > 0)
            {
                DisplayRecommendationPopup("C3D_OMNICEPT","Omnicept SDK");
                return;
            }

            var OculusIntegrationAssets = AssetDatabase.FindAssets("OVRPlugin.cs");
            if (OculusIntegrationAssets.Length > 0)
            {
                DisplayRecommendationPopup("C3D_OCULUS","Oculus Integration");
                return;
            }

            var HololensAssets = AssetDatabase.FindAssets("WindowsMRAssembly");
            if (HololensAssets.Length > 0)
            {
                DisplayRecommendationPopup("C3D_MRTK","Hololens Package");
                return;
            }

            //general packages
            foreach (var package in packages)
            {
                if (package.name == "com.openvr")
                {
                    DisplayRecommendationPopup("C3D_STEAMVR2", "SteamVR Package");
                    return;
                }
            }

            //default fallback
            DisplayRecommendationPopup("C3D_DEFAULT", string.Empty);
        }

        //TODO CONSIDER write a static list of features that each SDK enables (eye tracking, room size, social features, sensors, etc)
        //TODO rewrite description on this popup
        void DisplayRecommendationPopup(string selection, string friendlyName)
        {
            string description = friendlyName + " was found in your project. Selecting this will enable additional feature support";

            if (string.IsNullOrEmpty(friendlyName))
            {
                description = "No supported SDKs or packages were found in your project. We recommend default for a general implementation";
            }

            var result = EditorUtility.DisplayDialog("Recommended Setup", description, "Ok", "Manually Select");
            if (result)
            {
                //ok
                selectedsdks = new List<string>() { selection };
            }
            else
            {
                //cancel or close popup
                selectedsdks = new List<string>() { "C3D_DEFAULT" };
            }
        }

        void SelectSDKUpdate()
        {
            if (!hasDoneSDKRecommendation)
            {
                hasDoneSDKRecommendation = true;
                if (!EditorCore.HasC3DDefine())
                {
                    EditorCore.GetPackages(OnGetPackages);
                }
            }

            GUI.Label(steptitlerect, "SDK FEATURES", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "By default, we support most XR features, but some additional software may be required to support specific features.\n\nShift click to select multiple", "normallabel");

            int startHeight = 150;
            int scrollAreaHeight = 320;

            Rect innerScrollSize = new Rect(30, 0, 420, SDKNamesDefines.Count * 36);
            sdkScrollPos = GUI.BeginScrollView(new Rect(30, startHeight, 440, scrollAreaHeight), sdkScrollPos, innerScrollSize, false, false);

            for (int i = 0; i < SDKNamesDefines.Count; i++)
            {
                bool selected = selectedsdks.Contains(SDKNamesDefines[i].Define);
                GUIContent content = new GUIContent("  "+SDKNamesDefines[i].Name);
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
                GUI.Label(new Rect(30, i * 32 + separator, 24, 30), selected ? EditorCore.BoxCheckmark32 : EditorCore.BoxEmpty32, "image_centered");
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
            GUI.Label(steptitlerect, "SDK VALIDATION", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "To automatically access Omnicept's Glia API, the Cognitive3D SDK needs to reference the Glia Assembly, which doesn't exist by default." +
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
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.CircleEmpty32, "image_centered");

            }
            else
            {
                //full checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.CircleCheckmark32, "image_centered");
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
            GUI.Label(steptitlerect, "SDK VALIDATION", "steptitle");
            GUI.Label(new Rect(30, 45, 440, 440), "Check for Assembly Definition Files.", "normallabel");
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
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.CircleEmpty32, "image_centered");
                
            }
            else
            {
                //full checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.CircleCheckmark32, "image_centered");
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
            GUI.Label(new Rect(30, 45, 440, 440), "Add EyeManager to the scene.", "normallabel");
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
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.CircleEmpty32, "image_centered");

            }
            else
            {
                //full checkmark
                GUI.Label(new Rect(360, 290, 64, 30), EditorCore.CircleCheckmark32, "image_centered");
            }
#endif
        }

        #endregion

        #region WaitForCompile

        //static so it will reset on recompile
        static double compileStartTime = -1;

        void WaitForCompile()
        {
            GUI.Label(steptitlerect, "RECOMPILE", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "Applying selected SDKs and compiling. This will take a moment.", "normallabel");
            EditorCore.SetPlayerDefine(selectedsdks);

            if (compileStartTime < 0)
            {
                compileStartTime = EditorApplication.timeSinceStartup;
            }

            //calculate fill amount
            float fillAmount = (float)(EditorApplication.timeSinceStartup - compileStartTime) / 10f;
            fillAmount = Mathf.Clamp(fillAmount, 0.02f, 1f);
            var progressBackground = new Rect(30, 150, 440, 30);
            var progressPartial = new Rect(30, 150, 440 * fillAmount, 30);

            //calculate duration text
            double compileTimeInSeconds = EditorApplication.timeSinceStartup - compileStartTime;
            int compileTimeMinutes = 0;
            while (compileTimeInSeconds > 59.99f)
            {
                compileTimeMinutes++;
                compileTimeInSeconds = compileTimeInSeconds - 59.99f;
            }
            string compileDuration = compileTimeMinutes+":"+Mathf.Floor((float)compileTimeInSeconds).ToString("00");

            //display ui elements
            GUI.Box(progressBackground, "", "box");
            GUI.Box(progressPartial, "", "button");
            GUI.Label(progressBackground, compileDuration, "image_centered");

            //done
            if (EditorApplication.isCompiling) { return; }
            compileStartTime = -1;

            currentPage++;
        }


#endregion

        void DoneUpdate()
        {
            GUI.Label(steptitlerect, "NEXT STEPS", "steptitle");
            GUI.Label(new Rect(30, 30, 440, 440), "The project settings are complete. Next you'll be guided to upload a scene to give context to the data you record.", "normallabel");
            if (GUI.Button(new Rect(150, 100, 200, 30), "Quick Setup"))
            {
                SceneSetupWindow.Init(position);
                Close();
            }

            GUI.Label(new Rect(30, 200, 440, 440), "Alternatively, you can use Dynamic Object Components to identify key objects in your environment", "normallabel");
            if (GUI.Button(new Rect(150, 250, 200, 30), "Advanced Setup"))
            {
                //show dynamic page
                currentPage = Page.DynamicSetup;
            }
        }

        void DynamicUpdate()
        {
            GUI.Label(steptitlerect, "DYNAMIC OBJECTS", "steptitle");
            //display some text about what dynamics are and how to define them with a component
            //also brief on the upcoming dynamic objects screen

            GUI.Label(new Rect(30, 30, 440, 440), "Dynamic Objects record engagements with things in your experience. This includes gaze times and the position of moving objects. These can be used to enhance Objectives and quickly evaluate your users.", "normallabel");
            GUI.Label(new Rect(30, 130, 440, 440), "Some examples include Billboards, Vehicles or Tools.", "normallabel");
            GUI.Label(new Rect(30, 170, 440, 440), "To mark a GameObject in your Scene as a Dynamic Object, simply add the Dynamic Object component.", "normallabel");
            GUI.Label(new Rect(30, 300, 440, 440), "The next screen is an overview of all the Dynamic Objects in your scene and what Dynamic Objects already exist on the dashboard. For now, simply add Dynamic Object components then continue with the Scene Setup window.", "normallabel");
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
                SaveDevKey();
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
                    buttonrect = new Rect(410, 510, 80, 30);
                    if (lastDevKeyResponseCode == 200)
                    {
                        //next. use default action
                        onclick += () => SaveDevKey();
                    }
                    else
                    {
                        //check and wait for response
                        onclick = () => SaveDevKey();
                        onclick += () => EditorCore.CheckForExpiredDeveloperKey(GetDevKeyResponse);
                        if (!EditorCore.GetPreferences().IsApplicationKeyValid)
                        {
                            onclick += () => EditorCore.CheckForApplicationKey(developerkey, GetApplicationKeyResponse); 
                        }
                        onclick += () => EditorCore.CheckSubscription(developerkey, GetSubscriptionResponse);
                    }

                    buttonDisabled = developerkey == null || developerkey.Length == 0;
                    if (buttonDisabled)
                    {
                        text = "Key Required";
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
                case Page.Organization:
                    onclick += () => SaveApplicationKey();
                    onclick += () => UnityEditor.VSAttribution.Cognitive3D.VSAttribution.SendAttributionEvent("Login", "Cognitive3D", apikey);
                    buttonDisabled = apikey == null || string.IsNullOrEmpty(apikey);
                    if (buttonDisabled)
                    {
                        text = "Key Required";
                    }
                    else
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
                    onclick = () =>
                    {
                        DynamicObjectsWindow.Init(position);
                        Close();
                    };
                    
                    text = "Open Dynamic Objects";
                    buttonrect = new Rect(280, 510, 200, 30);
                    break;
                case Page.NextSteps:
                    buttonrect = new Rect(600, 0, 0, 0);
                    break;
                case Page.SRAnipal:
                    appearDisabled = !sranipalAssemblyExists;
                    if (appearDisabled)
                    {
                        onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without creating the necessary files?", "Yes", "No")) { currentPage++; } };
                    }
                    break;
                case Page.Glia:
                    appearDisabled = !gliaAssemblyExists;
                    if (appearDisabled)
                    {
                        onclick = () => { if (EditorUtility.DisplayDialog("Continue", "Are you sure you want to continue without creating the necessary files?", "Yes", "No")) { currentPage++; } };
                    }
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
                    onclick = () => currentPage = Page.Welcome;
                    break;
                case Page.Glia:
                case Page.SRAnipal:
                case Page.Wave:
                case Page.Recompile:
                case Page.NextSteps:
                    onclick = () => currentPage = Page.SDKSelection;
                    break;
                case Page.DynamicSetup:
                    onclick = () => currentPage = Page.NextSteps;
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
                    if (onclick != null)
                        onclick.Invoke();
                }
            }
        }
    }
}