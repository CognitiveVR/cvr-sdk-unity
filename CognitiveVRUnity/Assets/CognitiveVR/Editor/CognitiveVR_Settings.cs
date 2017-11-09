using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace CognitiveVR
{
    [InitializeOnLoad]
    public class CognitiveVR_Settings : EditorWindow
    {
        public static Color GreenButton = new Color(0.4f, 1f, 0.4f);


        public static Color OrangeButton = new Color(1f, 0.6f, 0.3f);
        public static Color OrangeButtonPro = new Color(1f, 0.8f, 0.5f);

        public static CognitiveVR_Settings Instance;

        public string UserName;
        public CognitiveVR.Json.UserData UserData;

        /// <summary>
        /// used for 'step' box text
        /// </summary>
        public static string GreyTextColorString
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return "<color=#aaaaaaff>";
                return "<color=#666666ff>";
            }
        }

        //data about the last sdk release on github
        public class ReleaseInfo
        {
            public string tag_name;
            public string body;
            public string created_at;
        }

        static string SdkVersionUrl = "https://api.github.com/repos/cognitivevr/cvr-sdk-unity/releases/latest";

        static System.DateTime lastSdkUpdateDate; // when cvr_version was last set
        //cvr_skipVersion - EditorPref if newVersion == skipVersion, don't show update window

        static float windowWidth = 300;

        //[MenuItem("Window/cognitiveVR/Settings Window", priority = 1)]
        public static void Init()
        {
            EditorApplication.update -= EditorUpdate;

            // Get existing open window or if none, make a new one:
            Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Account Settings");
            Vector2 size = new Vector2(windowWidth, 550);
            Instance.minSize = size;
            Instance.maxSize = size;
            Instance.Show();
            SaveEditorVersion();

            //fix this. shouldn't have to re-read this value to parse it to the correct format
            if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out lastSdkUpdateDate))
            {
                //Instance.lastSdkUpdateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                Debug.Log("CognitiveVR_Settings failed to parse UpdateDate");
            }
        }

        static CognitiveVR_Settings()
        {
            EditorApplication.update += EditorUpdate;
        }

        static void EditorUpdate()
        {
            //HACK there is a bug with serializing the cognitivevr_preferences file
            //this will check if the cognitivevr file exists but cannot be loaded - if that is the case, it will recompile the scripts which will fix this

            //without this, you may LOSE YOUR SAVED PREFERENCES, including sceneIDs used to upload to scene explorer

            if (System.IO.File.Exists(Application.dataPath + "/CognitiveVR/Resources/CognitiveVR_Preferences.asset") && null == AssetDatabase.LoadAssetAtPath<CognitiveVR_Preferences>("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset"))
            {
                Debug.Log("CognitiveVR Prefs file exists but cannot be loaded - Recompile");
                AssetDatabase.StartAssetEditing();

                string[] allassetpaths = AssetDatabase.GetAllAssetPaths();
                foreach (var v in allassetpaths)
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath(v, typeof(MonoScript)) as MonoScript;
                    if (script != null)
                    {
                        //recompile a script
                        AssetDatabase.ImportAsset(v);
                        break;
                    }
                }
                AssetDatabase.StopAssetEditing();
            }


            bool displaySettings = true;

            if (EditorPrefs.GetBool("CognitiveHasShownInit", false))
            {
                displaySettings = false;
            }

            EditorPrefs.SetBool("CognitiveHasShownInit", true);

            SaveEditorVersion();

            string version = EditorPrefs.GetString("cvr_version");
            if (string.IsNullOrEmpty(version) || version != CognitiveVR.Core.SDK_Version)
            {
                displaySettings = true;
                //new version installed
            }

            if (displaySettings)
            {
                Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings");
                Vector2 size = new Vector2(windowWidth, 550);
                Instance.minSize = size;
                Instance.maxSize = size;
                Instance.Show();

                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out lastSdkUpdateDate))
                {
                    //Instance.updateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            System.DateTime remindDate; //current date must be this or beyond to show popup window

            if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), out remindDate))
            {
                if (System.DateTime.UtcNow > remindDate)
                {
                    EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    CheckForUpdates();
                }
            }
            else
            {
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            EditorApplication.update -= EditorUpdate;
        }

        string GetResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = System.IO.Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Editor".Length) + "";
        }

        string password;
        Rect sdkRect;

        //set on login from prefs org name
        int lastOrganizationIndex;
        //set on login from prefs product name
        int lastProductIndex;

        public void OnGUI()
        {
            GUI.skin.label.richText = true;
            GUI.skin.button.richText = true;
            GUI.skin.box.richText = true;

            CognitiveVR.CognitiveVR_Preferences prefs = GetPreferences();

            //if (SelectedOrganization == null) { SelectedOrganization = new Json.Organization(); }
            //if (SelectedProduct == null) { SelectedProduct = new Json.Product(); }

            //LOGO
            var resourcePath = GetResourcePath();
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(resourcePath + "Textures/logo-light.png");

            var rect = GUILayoutUtility.GetRect(0, 0, 0, 100);

            if (logo)
            {
                rect.width -= 20;
                rect.x += 10;
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);
            }

            //=========================
            //Authenticate
            //=========================

            GUILayout.BeginHorizontal();
            UserStartupBox("1", IsUserLoggedIn);
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Authenticate</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(IsUserLoggedIn && !string.IsNullOrEmpty(UserName));
            UserName = GhostTextField("name@email.com", "", UserName);
            if (Event.current.character == '\n' && Event.current.type == EventType.KeyDown)
            {
                RequestLogin(UserName, password);
            }
            password = GhostPasswordField("password", "", password);
            EditorGUI.EndDisabledGroup();

            if (IsUserLoggedIn)
            {
                if (GUILayout.Button("Logout"))
                {
                    Logout();
                }
            }
            else
            {
                if (GUILayout.Button("Login"))
                {
                    RequestLogin(UserName, password);
                }
            }

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);


            //=========================
            //Select organization
            //=========================
            if (IsUserLoggedIn || prefs.IsCustomerIDValid)
            {
                GUILayout.BeginHorizontal();
                UserStartupBox("2", prefs.IsCustomerIDValid);
                GUILayout.FlexibleSpace();
                GUILayout.Label("<size=14><b>Select Product</b></size>");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (OrganizationsCached())
                {
                    bool shouldsave = false;
                    int newOrganizationIndex = lastOrganizationIndex; //used in dropdown menu
                    

                    //=========================
                    //select organization
                    //=========================

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Organization", GUILayout.Width(80));

                    //display that any orgs exist
                    string[] organizations = GetUserOrganizationNames();
                    if (organizations.Length > 0)
                    {
                        newOrganizationIndex = EditorGUILayout.Popup(newOrganizationIndex, organizations);
                    }
                    else
                    {
                        GUILayout.Label("No Organizations Exist!", new GUIStyle(EditorStyles.popup));
                    }

                    if (newOrganizationIndex != lastOrganizationIndex || string.IsNullOrEmpty(prefs.OrgName))
                    {
                        prefs.OrgName = GetUserOrganization(newOrganizationIndex).name;
                        lastOrganizationIndex = newOrganizationIndex;
                        shouldsave = true;
                    }
                    EditorGUILayout.EndHorizontal();

                    //=========================
                    //select product
                    //=========================

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Product", GUILayout.Width(80));

                    int newProductIndex = lastProductIndex;

                    string[] products = GetProductNames(GetUserOrganization(newOrganizationIndex));

                    if (products.Length > 0)
                    {
                        newProductIndex = EditorGUILayout.Popup(newProductIndex, products);
                    }
                    else
                    {
                        GUILayout.Label("No Products Exist!", new GUIStyle(EditorStyles.popup));
                        prefs.ProductName = "";
                    }

                    if (newProductIndex != lastProductIndex || string.IsNullOrEmpty(prefs.ProductName))
                    {
                        prefs.ProductName = GetUserProduct(GetUserOrganization(newOrganizationIndex), newProductIndex).name;
                        lastProductIndex = newProductIndex;
                        var product = GetUserProduct(GetUserOrganization(newOrganizationIndex), newProductIndex);
                        prefs.CustomerID = product.customerId;
                        shouldsave = true;
                    }

                    //=========================
                    //new product
                    //=========================

                    if (GUILayout.Button("New", GUILayout.Width(40), GUILayout.Height(15)))
                    {
                        Application.OpenURL("https://dashboard.cognitivevr.io/admin/products/create");
                    }

                    EditorGUILayout.EndHorizontal();

                    //=========================
                    //test / prod
                    //=========================

                    DrawTestProdButtons(products.Length > 0);

                    //saved product or saved organization
                    if (shouldsave)
                    {

                        prefs.OrgName = GetUserOrganization(newOrganizationIndex).name;
                        prefs.ProductName = GetUserProduct(GetUserOrganization(newOrganizationIndex), newProductIndex).name;

                        //'soft save'
                        var release = prefs.ReleaseType;
                        
                        prefs.SetReleaseType(release);

                        EditorUtility.SetDirty(prefs);
                        AssetDatabase.SaveAssets();

                        SaveEditorVersion();

                    }

                    GUILayout.Space(10);
                    if (GUILayout.Button("Save"))
                    {
                        SaveSettings(GetUserProduct(GetUserOrganization(newOrganizationIndex), newProductIndex).customerId);
                        //SaveSettings(SelectedProduct.customerId);

                        //clear organization and product name
                        //set customerid
                    }                    
                    EditorGUI.EndDisabledGroup();

                }
                else //organizations are empty
                {
                    EditorGUI.BeginDisabledGroup(true);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Organization", GUILayout.Width(80));
                    EditorGUILayout.Popup(0, new string[1]{ prefs.OrgName});
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Product", GUILayout.Width(80));
                    EditorGUILayout.Popup(0, new string[1] { prefs.ProductName });
                    GUILayout.Button("New", GUILayout.Width(40), GUILayout.Height(15));
                    GUILayout.EndHorizontal();

                    EditorGUI.EndDisabledGroup();

                    DrawTestProdButtons(true);

                    GUILayout.Space(10);
                    if (!IsUserLoggedIn || string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(password))
                    {
                        //can't log in
                        EditorGUI.BeginDisabledGroup(true);
                        var gc = new GUIContent("Change Product", "Must log in to change id");
                        GUILayout.Button(gc);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        if (GUILayout.Button("Change Product"))
                        {
                            RequestLogin(UserName, password);
                        }
                    }
                }
            }
            else
            {
                GUILayout.Space(89);
            }

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            if (prefs.IsCustomerIDValid)
            {
                //=========================
                //select vr sdk
                //=========================

                GUILayout.BeginHorizontal();
#if CVR_STEAMVR || CVR_OCULUS || CVR_GOOGLEVR || CVR_DEFAULT || CVR_FOVE || CVR_PUPIL || CVR_ARKIT || CVR_ARCORE
                UserStartupBox("3", true);
#else
                UserStartupBox("3", false);
#endif
                if (GUILayout.Button("Select VR/AR SDK"))
                {
                    sdkRect.x += 280;
                    sdkRect.y -= 20;

                    PopupWindow.Show(sdkRect, new CognitiveVR_SelectSDKPopup());
                }
                if (Event.current.type == EventType.Repaint) sdkRect = GUILayoutUtility.GetLastRect();
                GUILayout.EndHorizontal();

                //=========================
                //options
                //=========================

                GUILayout.BeginHorizontal();

                UserStartupBox("4", FindObjectOfType<CognitiveVR_Manager>() != null);

                if (GUILayout.Button("Track Player Actions"))
                {
                    CognitiveVR_ComponentSetup.Init();
                }
                GUILayout.EndHorizontal();

                //=========================
                //scene export
                //=========================

                GUILayout.BeginHorizontal();
                UserStartupBox("5", prefs.sceneSettings.Find(x => x.SceneId != "") != null);
                if (GUILayout.Button("Upload Scene"))
                {
                    CognitiveVR_SceneExportWindow.Init();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(67);
            }

            //EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            //updates
            if (GUILayout.Button("Check for Updates"))
            {
                EditorPrefs.SetString("cvr_updateRemindDate", "");
                EditorPrefs.SetString("cvr_skipVersion", "");
                CheckForUpdates();
            }


            //=========================
            //version
            //=========================

            if (GUILayout.Button("Open Web Dashboard..."))
            {
                Application.OpenURL("http://dashboard.cognitivevr.io");
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: " + Core.SDK_Version);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Instance == null) { Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings"); }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (lastSdkUpdateDate.Year < 100)
            {
                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out lastSdkUpdateDate))
                {
                    //Instance.updateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    lastSdkUpdateDate.AddYears(600);
                }
            }
            if (lastSdkUpdateDate.Year > 1000)
            {
                GUILayout.Label("Last Updated: " + lastSdkUpdateDate.ToShortDateString());
            }
            else
            {
                GUILayout.Label("Last Updated: Never");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void DrawTestProdButtons(bool enabled)
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!enabled);
            GUIStyle testStyle = new GUIStyle(EditorStyles.miniButtonLeft);
            GUIStyle prodStyle = new GUIStyle(EditorStyles.miniButtonRight);
            var prefs = GetPreferences();
            
            if (prefs.ReleaseType == ReleaseType.Test)
            {
                testStyle.normal = testStyle.active;
            }
            else
            {
                prodStyle.normal = prodStyle.active;
            }

            if (GUILayout.Button(new GUIContent("Test"), testStyle, GUILayout.Width(windowWidth/2-5)))
            {
                prefs.SetReleaseType(ReleaseType.Test);
            }

            if (GUILayout.Button(new GUIContent("Production"), prodStyle, GUILayout.Width(windowWidth / 2-5)))
            {
                prefs.SetReleaseType(ReleaseType.Prod);
            }
            GUILayout.EndHorizontal();
        }

        public void RequestLogin(string userEmail, string password)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                Debug.LogError("Missing Username!");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                Debug.LogError("Missing Password!");
                return;
            }

            var url = "https://api.cognitivevr.io/sessions";
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            string json = "{\"email\":\"" + userEmail + "\",\"password\":\"" + password + "\"}";
            byte[] bytes = new System.Text.UTF8Encoding(true).GetBytes(json);

            loginRequest = new UnityEngine.WWW(url, bytes, headers);

            Debug.Log("cognitiveVR Request Login");

            EditorApplication.update += CheckLoginResponse;
        }

        WWW loginRequest;

        void CheckLoginResponse()
        {
            if (!loginRequest.isDone)
            {
                //check for timeout
            }
            else
            {
                if (!string.IsNullOrEmpty(loginRequest.error))
                {
                    //loginResponse = "Could not log in!";
                    Debug.LogWarning("Could not log in to cognitiveVR SDK. Error: " + loginRequest.error);
                }
                if (!string.IsNullOrEmpty(loginRequest.text))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(loginRequest.text))
                        {
                            UserData = JsonUtility.FromJson<Json.UserData>(loginRequest.text);
                            //System.Array.Sort(UserData.organizations, (x, y) => String.Compare(x.name, y.name));

                            foreach (var v in loginRequest.responseHeaders)
                            {
                                if (v.Key.ToUpper() == "SET-COOKIE")
                                {
                                    EditorPrefs.SetString("sessionToken", v.Value);
                                    //GetPreferences().sessionToken = v.Value;
                                    //split semicolons. ignore everything except split[0]
                                    string[] split = v.Value.Split(';');
                                    //GetPreferences().sessionID = split[0].Substring(18);
                                    EditorPrefs.SetString("sessionId", split[0].Substring(18));
                                    IsUserLoggedIn = true;
                                }
                            }

                            lastOrganizationIndex = GetOrganizationIndex(GetPreferences().OrgName);

                            lastProductIndex = GetProductIndex(lastOrganizationIndex, GetPreferences().ProductName);

                            AssetDatabase.SaveAssets();
                            SaveEditorVersion();
                        }
                        else
                        {
                            Debug.Log("cognitiveVR login request returned empty string!");
                        }
                    }
                    catch (System.Exception e)
                    {
                        //this can rarely happen when json is not formatted correctly
                        Debug.LogError("Cannot log in to cognitiveVR. Error: " + e.Message);
                        throw;
                    }
                }
                else
                {
                    Debug.LogWarning("Could not log in to cognitiveVR SDK. Password or Username incorrect");
                }

                EditorApplication.update -= CheckLoginResponse;

                Repaint();
            }
        }

        static WWW checkForUpdatesRequest;
        static void CheckForUpdates()
        {
            SaveEditorVersion();

            checkForUpdatesRequest = new UnityEngine.WWW(SdkVersionUrl);
            EditorApplication.update += UpdateCheckForUpdates;
        }

        public static void UpdateCheckForUpdates()
        {
            if (!checkForUpdatesRequest.isDone)
            {
                //check for timeout
            }
            else
            {
                if (!string.IsNullOrEmpty(checkForUpdatesRequest.error))
                {
                    Debug.Log("Check for cognitiveVR SDK version update error: " + checkForUpdatesRequest.error);
                }

                if (!string.IsNullOrEmpty(checkForUpdatesRequest.text))
                {
                    var info = JsonUtility.FromJson<ReleaseInfo>(checkForUpdatesRequest.text);

                    var version = info.tag_name;
                    string summary = info.body;

                    if (!string.IsNullOrEmpty(version))
                    {
                        if (version != CognitiveVR.Core.SDK_Version)
                        {
                            //new version
                            CognitiveVR_UpdateSDKWindow.Init(version, summary);
                        }
                        else if (EditorPrefs.GetString("cvr_skipVersion") == version)
                        {
                            //skip this version. limit this check to once a day
                            EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            //up to date
                            Debug.Log("Version "+ version + ". You are up to date");
                        }
                    }
                }

                EditorApplication.update -= UpdateCheckForUpdates;
            }
        }

        public void Logout()
        {
            UserData = Json.UserData.Empty;
            UserName = string.Empty;
            password = string.Empty;
            EditorPrefs.DeleteKey("sessionId");
            EditorPrefs.DeleteKey("authToken");
            EditorPrefs.DeleteKey("sessionToken");
            IsUserLoggedIn = false;
            AssetDatabase.SaveAssets();
        }

        public void SaveSettings(string customerid)
        {
            CognitiveVR.CognitiveVR_Preferences prefs = CognitiveVR_Settings.GetPreferences();

            UserData = null;
            var release = prefs.ReleaseType;
            prefs.CustomerID = customerid;
            prefs.SetReleaseType(release);

            EditorUtility.SetDirty(prefs);
            AssetDatabase.SaveAssets();

            SaveEditorVersion();
        }

        private static void SaveEditorVersion()
        {
            if (EditorPrefs.GetString("cvr_version") != CognitiveVR.Core.SDK_Version)
            {
                EditorPrefs.SetString("cvr_version", CognitiveVR.Core.SDK_Version);
                EditorPrefs.SetString("cvr_updateDate", System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture));
                lastSdkUpdateDate = System.DateTime.UtcNow;
            }
        }

        public string[] GetUserOrganizationNames()
        {
            List<string> organizationNames = new List<string>();

            if (UserData == null)
            {
                return organizationNames.ToArray();
            }

            for (int i = 0; i < UserData.organizations.Length; i++)
            {
                organizationNames.Add(UserData.organizations[i].name);
            }

            return organizationNames.ToArray();
        }

        public string[] GetProductNames(Json.Organization organization)
        {
            List<string> productNames = new List<string>();

            if (string.IsNullOrEmpty(organization.name))
            {
                Debug.LogWarning("get product names - organization is null");
                return productNames.ToArray();
            }

            for (int i = 0; i < UserData.products.Length; i++)
            {
                if (UserData.products[i].orgId == organization.id)
                {
                    productNames.Add(UserData.products[i].name);
                }
            }

            return productNames.ToArray();
        }

        public void SetPlayerDefine(List<string> newDefines)
        {
            //get all scripting define symbols
            string s = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string[] symbols = s.Split(';');


            int cvrDefineCount = 0;
            int newDefineContains = 0;
            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].StartsWith("CVR_"))
                {
                    cvrDefineCount++;
                }
                if (newDefines.Contains(symbols[i]))
                {
                    newDefineContains++;
                }
            }

            if (newDefineContains == cvrDefineCount && cvrDefineCount != 0)
            {
                //all defines already exist
                return;
            }

            //remove all CVR_ symbols
            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].Contains("CVR_"))
                {
                    symbols[i] = "";
                }
            }

            //rebuild symbols
            string alldefines = "";
            for (int i = 0; i < symbols.Length; i++)
            {
                if (!string.IsNullOrEmpty(symbols[i]))
                {
                    alldefines += symbols[i] + ";";
                }
            }

            foreach (string define in newDefines)
            {
                alldefines += define + ";";
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, alldefines);

        }

        /// <summary>
        /// Gets the cognitivevr_preferences or creates and returns new default preferences
        /// </summary>
        /// <returns>Preferences</returns>
        public static CognitiveVR_Preferences GetPreferences()
        {
            CognitiveVR_Preferences asset = AssetDatabase.LoadAssetAtPath<CognitiveVR_Preferences>("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CognitiveVR_Preferences>();
                AssetDatabase.CreateAsset(asset, "Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
                AssetDatabase.Refresh();
            }
            return asset;
        }

        //userdata contains any organization data
        public bool OrganizationsCached()
        {
            if (GetUserOrganizationNames().Length == 0)
            {
                return false;
            }
            return true;
        }

        /*public bool HasSessionId()
        {
            if (string.IsNullOrEmpty(EditorPrefs.GetString("sessionId")))
            {
                return false;
            }
            return true;
        }*/

        //temporary. true if successfully logged in
        public bool IsUserLoggedIn;

        static GUIStyle headerStyle;
        public static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle(EditorStyles.largeLabel);
                    headerStyle.fontSize = 14;
                    headerStyle.alignment = TextAnchor.UpperCenter;
                    headerStyle.fontStyle = FontStyle.Bold;
                    headerStyle.richText = true;
                }
                return headerStyle;
            }
        }

        public static string GhostTextField(string ghostText, string label, string actualText)
        {
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(actualText))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

                EditorGUILayout.TextField(label, ghostText, style);
                return "";
            }
            else
            {
                actualText = EditorGUILayout.TextField(label, actualText);
            }
            return actualText;
        }

        public static string GhostPasswordField(string ghostText, string label, string actualText)
        {
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(actualText))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

                EditorGUILayout.TextField(label, ghostText, style);
                return "";
            }
            else
            {
                actualText = EditorGUILayout.PasswordField(label, actualText);
            }

            return actualText;
        }

        public static void UserStartupBox(string number, bool showCheckbox)
        {
            var greencheck = EditorGUIUtility.FindTexture("Collab");

            if (showCheckbox)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Box(greencheck, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Box(CognitiveVR_Settings.GreyTextColorString + number + "</color>", GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
            }
        }

        public delegate void ResponseHandler(int responseCode);
        public static event ResponseHandler AuthResponse;
        public static void OnAuthResponse(int responseCode) { if (AuthResponse != null) { AuthResponse(responseCode); } }

        //auth token

        static WWW authTokenRequest;

        public static void RequestAuthToken(string url)
        {
            Debug.Log("cognitivevr - request auth token");
            var headers = new Dictionary<string, string>();
            headers.Add("X-HTTP-Method-Override", "POST");
            headers.Add("Cookie", EditorPrefs.GetString("sessionToken"));

            authTokenRequest = new WWW(url, new System.Text.UTF8Encoding(true).GetBytes("ignored"), headers);
            EditorApplication.update += UpdateGetAuthToken;
        }

        static void UpdateGetAuthToken()
        {
            if (!authTokenRequest.isDone) { return; }
            EditorApplication.update -= UpdateGetAuthToken;

            var responseCode = Util.GetResponseCode(authTokenRequest.responseHeaders);

            if (responseCode >= 500)
            {
                //internal server error
                OnAuthResponse(responseCode);
            }
            else if (responseCode >= 400)
            {
                if (responseCode == 401)
                {
                    //session token not authorized
                    Debug.Log("Session token not authorized to get auth token. Please log in");

                    Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Account Settings");
                    Vector2 size = new Vector2(300, 550);
                    Instance.minSize = size;
                    Instance.maxSize = size;
                    Instance.Show();
                    Instance.Logout();

                    OnAuthResponse(responseCode);
                }
                else
                {
                    //request is wrong
                    OnAuthResponse(responseCode);
                }
            }

            var tokenResponse = JsonUtility.FromJson<AuthTokenResponse>(authTokenRequest.text);
            EditorPrefs.SetString("authToken", tokenResponse.token);

            authTokenRequest = null;
            OnAuthResponse(responseCode);
        }

        public class AuthTokenResponse
        {
            public string token;
        }

        //index is local to organization. so 2 orgs with 2 products would each be 0 and 1
        public Json.Product GetUserProduct(Json.Organization org, int index)
        {
            int productIndex = 0;

            for (int i = 0; i < UserData.products.Length; i++)
            {
                if (UserData.products[i].orgId == org.id)
                {
                    if (index == productIndex)
                    {
                        return UserData.products[i];
                    }
                    productIndex++;
                }
            }
            return new Json.Product();
        }

        public Json.Organization GetUserOrganization(int index)
        {
            if (UserData.organizations == null || UserData.organizations.Length < index) { return new Json.Organization(); }
            return UserData.organizations[index];
        }

        public int GetOrganizationIndex(string orgName)
        {
            if (string.IsNullOrEmpty(orgName)) { orgName = ""; return 0; }
            string orgNameLower = orgName.ToLower();
            int orgIndex = 0; 
            for (int i = 0; i<UserData.organizations.Length;i++)
            {
                if (UserData.organizations[i].name.ToLower() == orgNameLower)
                {
                    return orgIndex;
                }
                orgIndex++;
            }

            return 0;
        }

        public int GetProductIndex(int orgIndex, string productName)
        {
            if (string.IsNullOrEmpty(productName)) { productName = ""; return 0; }
            string productNameLower = productName.ToLower();
            var org = GetUserOrganization(orgIndex);

            int productIndex = 0;

            for (int i = 0; i < UserData.products.Length; i++)
            {
                if (UserData.products[i].orgId == org.id)
                {
                    if (UserData.products[i].name.ToLower() == productNameLower)
                    {
                        return productIndex;
                    }
                    productIndex++;
                }                
            }

            return 0;
        }
    }
}
