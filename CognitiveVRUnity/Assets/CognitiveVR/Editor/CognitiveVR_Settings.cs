using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//cvr_version - current version installed

//------//cvr_newVersion - version from update response DONT NEED TO SAVE THIS
//cvr_skipVersion - if newVersion == skipVersion, don't show update window

//cvr_updateDate - when cvr_version was last set
//cvr_updateRemindDate - current date must be this or beyond to show popup window



namespace CognitiveVR
{
    [InitializeOnLoad]
    public class CognitiveVR_Settings : EditorWindow
    {
        public static Color DarkerGreen = new Color(0.6f, 1f, 0.6f);

        public static Color GreenText = new Color(0.6f, 1f, 0.6f);
        //public static Color GreenText = new Color(0.48f, 0.66f, 0.23f);
        public static Color GreenButton = new Color(0.6f, 1f, 0.6f);

        //this colour is the green from the 'collab' checkmark that appears next to Authenticate after the user has logged in
        //0.48
        //0.66
        //0.23


        public static Color OrangeText = new Color(1f, 0.6f, 0.3f);

        static CognitiveVR_Settings _instance;
        public static CognitiveVR_Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings");
                }
                return _instance;
            }
        }

        //TODO make a better name for this
        System.DateTime lastSdkUpdateDate;


        [MenuItem("cognitiveVR/Settings")]
        public static void Init()
        {
            EditorApplication.update -= EditorUpdate;

            // Get existing open window or if none, make a new one:
            _instance = GetWindow<CognitiveVR_Settings>(true,"cognitiveVR Settings");
            Vector2 size = new Vector2(300, 550);
            Instance.minSize = size;
            Instance.maxSize = size;
            Instance.Show();

            //TODO fix this. shouldn't have to re-read this value to parse it to the correct format
            if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out Instance.lastSdkUpdateDate))
            {
                Instance.lastSdkUpdateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
            }

            //if (System.DateTime.TryParseExact(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), "", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out Instance.updateDate);
            //EditorPrefs.GetInt
        }

        static CognitiveVR_Settings()
        {
            EditorApplication.update += EditorUpdate;
        }

        static void EditorUpdate()
        {
            bool show = true;

#if CVR_STEAMVR || CVR_OCULUS || CVR_GOOGLEVR || CVR_DEFAULT || CVR_FOVE
            show = false;
#endif

            string version = EditorPrefs.GetString("cvr_version");
            if (string.IsNullOrEmpty(version) || version != CognitiveVR.Core.SDK_Version)
            {
                show = true;
                //new version installed
            }

            if (show)
            {
                _instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings");
                Vector2 size = new Vector2(300, 550);
                Instance.minSize = size;
                Instance.maxSize = size;
                Instance.Show();

                
                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out Instance.lastSdkUpdateDate))
                {
                    //Instance.updateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                }

                //System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), out Instance.lastUpdateDate);
            }

            System.DateTime remindDate;
            
            if (Instance != null)
            {
                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), out remindDate))
                {
                    //remindDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                    if (System.DateTime.UtcNow > remindDate)
                    {
                        Instance.CheckForUpdates();
                        //Debug.Log("settings check for updates");
                    }
                    else
                    {
                        //Debug.Log("check updates later " + remindDate);
                    }
                }
                else
                {
                    Debug.Log("failed to parse cvr_updateRemindDate " + EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"));
                    Instance.CheckForUpdates();
                }
            }
            else
            {
                //Debug.Log("NO INSTANCE TO CHECK UPDATES ON");
            }

            EditorApplication.update -= EditorUpdate;
        }

        string GetSamplesResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = System.IO.Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "CognitiveVR/Editor".Length) + "";
        }

        string GetResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = System.IO.Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Editor".Length) + "";
        }

        //static List<string> option = new List<string>();
        static int productIndex = 0; //THIS SHOULD BE SAVED IN THE EDITOR PREFERENCES OR SOMETHING
        static int organizationIndex = 0;
        string password;
        Rect productRect;
        Rect sdkRect;

        int testprodSelection = -1;

        public void OnGUI()
        {
            GUI.skin.label.richText = true;
            GUI.skin.button.richText = true;

            CognitiveVR.CognitiveVR_Preferences prefs = GetPreferences();

            if (testprodSelection == -1)
            {
                testprodSelection = prefs.CustomerID.EndsWith("-prod") ? testprodSelection = 1 : testprodSelection = 0;
            }

            //LOGO
            var resourcePath = GetResourcePath();
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(resourcePath + "Textures/logo-light.png");
            //var rect = GUILayoutUtility.GetRect(position.width, 40, GUI.skin.window);

            var rect = GUILayoutUtility.GetRect(0, 0, 0, 100);

            if (logo)
            {
                //rect.height = 100;
                rect.width -= 20;
                rect.x += 10;
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);
            }

            //=========================
            //Authenticate
            //=========================

            var greencheck = EditorGUIUtility.FindTexture("Collab");

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Authenticate</b></size>");
            if (IsUserLoggedIn())
                GUILayout.Label(greencheck);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(IsUserLoggedIn());

            prefs.UserName = CognitiveVR_SceneExportWindow.GhostTextField("name@email.com", "", prefs.UserName);

            password = CognitiveVR_SceneExportWindow.GhostPasswordField("password", "", password);

            EditorGUI.EndDisabledGroup();

            if (IsUserLoggedIn())
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
                    RequestLogin(prefs.UserName, password);
                }
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (EditorGUIUtility.isProSkin)
                {
                    GUIStyle s = new GUIStyle(EditorStyles.whiteLabel);
                    s.normal.textColor = new Color(0.6f, 0.6f, 1);
                    if (GUILayout.Button("Create Account", s))
                        Application.OpenURL("https://dashboard.cognitivevr.io/");
                }
                else
                {
                    GUIStyle s = new GUIStyle(EditorStyles.whiteLabel);
                    s.normal.textColor = Color.blue;
                    if (GUILayout.Button("Create Account", s))
                        Application.OpenURL("https://dashboard.cognitivevr.io/");
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(loginResponse) || !IsUserLoggedIn())
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(loginResponse);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            /*
            GUILayout.Label("Version: " + Core.SDK_Version);

            GUILayout.Label("Version: " + Core.SDK_Version);

            //links
            if (GUILayout.Button("Sign Up", EditorStyles.whiteLabel))
                Application.OpenURL("https://dashboard.cognitivevr.io/");

            if (GUILayout.Button("Documentation", EditorStyles.whiteLabel))
                Application.OpenURL("https://github.com/CognitiveVR/cvr-sdk-unity/wiki");
                */


            //=========================
            //Select Product
            //=========================

            EditorGUI.BeginDisabledGroup(!IsUserLoggedIn());

            //GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            //GUILayout.Label("<size=14><b>Select Organization</b></size>");
            GUILayout.Label("Select Organization", CognitiveVR_Settings.HeaderStyle);
            //GUILayout.FlexibleSpace();
            //GUILayout.EndHorizontal();

            string lastOrganization = prefs.SelectedOrganization.name; //used to check if organizations changed. for displaying products
            if (!string.IsNullOrEmpty(prefs.SelectedOrganization.name))
            {
                for (int i = 0; i < prefs.UserData.organizations.Length; i++)
                {
                    if (prefs.UserData.organizations[i].name == prefs.SelectedOrganization.name)
                    {
                        organizationIndex = i;
                        break;
                    }
                }
            }

            string[] organizations = GetUserOrganizations();
            organizationIndex = EditorGUILayout.Popup(organizationIndex, organizations);
            if (organizations.Length > 0)
                prefs.SelectedOrganization = GetPreferences().GetOrganization(organizations[organizationIndex]);

            //GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            //GUILayout.Label("<size=14><b>Select Product</b></size>");
            GUILayout.Label("Select Product", CognitiveVR_Settings.HeaderStyle);
            //GUILayout.FlexibleSpace();
            //GUILayout.EndHorizontal();

            string organizationName = "";
            if (organizations.Length > 0)
                 organizationName = organizations[organizationIndex];

            string[] products = GetVRProductNames(organizationName);

            if (lastOrganization != prefs.SelectedOrganization.name)
            {
                //ie, changed organization
                prefs.SelectedProduct = null;
                productIndex = 0;
            }
            else
            {   
                if (!string.IsNullOrEmpty(prefs.SelectedProduct.name))
                {
                    for (int i = 0; i < products.Length; i++)
                    {
                        if (products[i] == prefs.SelectedProduct.name)
                        {
                            productIndex = i;
                            break;
                        }
                    }
                }
            }

            GUILayout.BeginHorizontal();

            productIndex = EditorGUILayout.Popup(productIndex, products);
            if (products.Length > 0)
                prefs.SelectedProduct = GetPreferences().GetProduct(products[productIndex]);

            if (GUILayout.Button("New", GUILayout.Width(40)))
            {
                PopupWindow.Show(productRect, new CognitiveVR_NewProductPopup());
            }
            if (Event.current.type == EventType.Repaint) productRect = GUILayoutUtility.GetLastRect();

            GUILayout.EndHorizontal();

            /*int oldTestProd = testprodSelection;

            testprodSelection = GUILayout.Toolbar(testprodSelection, TestProd);
            if (oldTestProd != testprodSelection) //update suffix test/prod on customerid
            {
                if (prefs.CustomerID.EndsWith("-test") || prefs.CustomerID.EndsWith("-prod"))
                {
                    prefs.CustomerID = prefs.CustomerID.Substring(0, prefs.CustomerID.Length - 5);
                }
                
                //-test or -prod suffix appended in SaveSettings()
                SaveSettings();
            }*/
            
            GUILayout.BeginHorizontal();

            GUIStyle testStyle = new GUIStyle(EditorStyles.miniButtonLeft);
            GUIStyle prodStyle = new GUIStyle(EditorStyles.miniButtonRight);
            if (testprodSelection == 0)
            {
                testStyle.normal = testStyle.active;
            }
            else
            {
                prodStyle.normal = prodStyle.active;
            }

            if (GUILayout.Button(new GUIContent("Test", "Send data to your internal development server"), testStyle))
            {
                testprodSelection = 0;
                prefs.CustomerID = prefs.CustomerID.Substring(0, prefs.CustomerID.Length - 5);
                SaveSettings();
            }

            if (GUILayout.Button(new GUIContent("Prod", "Send data to your server for players"), prodStyle))
            {
                testprodSelection = 1;
                prefs.CustomerID = prefs.CustomerID.Substring(0, prefs.CustomerID.Length - 5);
                SaveSettings();
            }

            GUILayout.EndHorizontal();
            

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            //=========================
            //options
            //=========================

            if (GUILayout.Button("Open Component Options Window"))
            {
                CognitiveVR_ComponentSetup.Init();
            }
            if (GUILayout.Button("Open Scene Export Window"))
            {
                CognitiveVR_SceneExportWindow.Init();
            }

            

            //=========================
            //select vr sdk
            //=========================
            if (GUILayout.Button("Select SDK"))
            {
                PopupWindow.Show(sdkRect, new CognitiveVR_SelectSDKPopup());
            }
            if (Event.current.type == EventType.Repaint) sdkRect = GUILayoutUtility.GetLastRect();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(20);

            //updates
            if (GUILayout.Button("Check for Updates"))
            {
                EditorPrefs.SetString("cvr_updateRemindDate", "");
                EditorPrefs.SetString("cvr_skipVersion", "");
                CheckForUpdates();
            }


            //GUILayout.Space(10);
            //GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            //GUILayout.Space(10);

            //=========================
            //version
            //=========================

            if (GUILayout.Button("Save"))
            {
                SaveSettings();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: " + Core.SDK_Version);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (lastSdkUpdateDate.Year < 100)
            {
                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out Instance.lastSdkUpdateDate))
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

        public void RequestLogin(string userEmail, string password)
        {
            var url = "http://testapi.cognitivevr.io/sessions";
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            string json = "{\"email\":\"" + userEmail + "\",\"password\":\"" + password + "\"}";
            byte[] bytes = new System.Text.UTF8Encoding(true).GetBytes(json);

            loginRequest = new UnityEngine.WWW(url, bytes, headers);

            Debug.Log("request login");

            EditorApplication.update += CheckLoginResponse;
        }

        WWW loginRequest;
        string loginResponse;

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
                    //TODO put a little warning icon by the authenticate header
                    Debug.LogWarning("Could not log in to cognitiveVR SDK. Error: " + loginRequest.error);
                }
                if (!string.IsNullOrEmpty(loginRequest.text))
                {
                    Debug.Log("loginRequest.text: " + loginRequest.text);
                    loginResponse = "";

                    try
                    {
                        if (!string.IsNullOrEmpty(loginRequest.text))
                        {
                            GetPreferences().UserData = JsonUtility.FromJson<Json.UserData>(loginRequest.text);
                            GetPreferences().sessionID = "1234";
                            AssetDatabase.SaveAssets();
                        }
                        else
                        {
                            loginResponse = "Could not log in!";
                        }
                    }
                    catch (System.Exception e)
                    {
                        //this can rarely happen when json is not formatted correctly
                        loginResponse = "Could not log in!";
                        Debug.LogError("Cannot log in. " + e.Message);
                        throw;
                    }
                }

                EditorApplication.update -= CheckLoginResponse;
            }
        }

        WWW NewProductRequest;
        public void RequestNewProduct(string productName)
        {
            if (NewProduct != null)
            {
                //Debug.LogError("currently trying to create product " + NewProduct);
                Debug.LogError("currently trying to create product " + NewProduct.name);
                return;
            }

            var url = "http://testapi.cognitivevr.io/organizations/"+ GetPreferences().SelectedOrganization.prefix + "/products";
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            System.Text.StringBuilder json = new System.Text.StringBuilder();
            json.Append("{");
            json.Append("\"sessionId\":\"" + GetPreferences().sessionID + "\",");
            json.Append("\"organizationName\":\"" + GetPreferences().SelectedOrganization.prefix + "\",");
            json.Append("\"productName\":\"" + productName + "\"");
            json.Append("}");

            byte[] bytes = new System.Text.UTF8Encoding(true).GetBytes(json.ToString());
            NewProductRequest = new UnityEngine.WWW(url, bytes, headers);

            NewProduct = new Json.Product();
            NewProduct.name = productName;
            NewProduct.orgId = GetPreferences().SelectedOrganization.id;
            NewProduct.customerId = GetPreferences().SelectedOrganization.prefix + "-" + productName;
            NewProduct.customerId = NewProduct.customerId.ToLower().Replace(" ", "");

            EditorApplication.update += UpdateNewProductRequest;
        }

        Json.Product NewProduct = null;

        public void UpdateNewProductRequest()
        {
            if (!NewProductRequest.isDone)
            {
                //www timeout defaults to 10 seconds
            }
            else
            {
                if (!string.IsNullOrEmpty(NewProductRequest.error))
                {
                    Debug.Log("got some error " + NewProductRequest.error);
                }
                else
                {
                    GetPreferences().UserData.AddProduct(NewProduct.name, NewProduct.customerId, NewProduct.orgId);

                    if (!string.IsNullOrEmpty(NewProductRequest.text))
                    {
                        Debug.Log("got some data " + NewProductRequest.text);
                        //it would be nice for the response to include a customerid
                    }
                }

                NewProduct = null;
                EditorApplication.update -= UpdateNewProductRequest;
            }
        }

        WWW checkForUpdatesRequest;
        void CheckForUpdates()
        {
            //www request to some server for current sdk version number
            //check response with current version

            //create www request to server somewhere

            var url = "https://s3.amazonaws.com/cvr-test/sdkversion.txt";
            //var headers = new Dictionary<string, string>();
            //headers.Add("Content-Type", "application/json");
            //headers.Add("X-HTTP-Method-Override", "POST");

            //System.Text.StringBuilder json = new System.Text.StringBuilder();
            /*json.Append("{");
            json.Append("\"sessionId\":\"" + GetPreferences().sessionID + "\"");
            json.Append("}");*/

            //byte[] bytes = new System.Text.UTF8Encoding(true).GetBytes(json.ToString());
            checkForUpdatesRequest = new UnityEngine.WWW(url);//, bytes, headers);


            EditorApplication.update += UpdateCheckForUpdates;
        }

        public void UpdateCheckForUpdates()
        {
            if (!checkForUpdatesRequest.isDone)
            {
                //check for timeout
            }
            else
            {
                /*//DEBUG
                var sdkVersion = new Json.SDKVersion() { version = "0.4.11" };
                string sdkSummary = "stuff";

                if (sdkVersion != null)
                {
                    Debug.Log("found sdk version " + sdkVersion.version);
                    Debug.Log("skip version "+EditorPrefs.GetString("cvr_skipVersion"));
                    if (EditorPrefs.GetString("cvr_skipVersion") == sdkVersion.version)
                    {
                        //skip
                    }
                    else
                    {
                        Debug.Log("init update window");
                        CognitiveVR_UpdateSDKWindow.Init(sdkVersion.version,sdkSummary);
                    }
                }
                EditorApplication.update -= UpdateCheckForUpdates;


                return;*/


                if (!string.IsNullOrEmpty(checkForUpdatesRequest.error))
                {
                    //Debug.Log("got some error " + checkForUpdatesRequest.error);
                }

                if (!string.IsNullOrEmpty(checkForUpdatesRequest.text))
                {
                    //this should return the latest sdk version number
                    //Debug.Log("got some data " + checkForUpdatesRequest.text);

                    string[] split = checkForUpdatesRequest.text.Split('|');

                    var version = split[0];
                    string summary = split[1];

                    if (version != null)
                    {
                        if (EditorPrefs.GetString("cvr_skipVersion") == version)
                        {
                            //skip this version. limit this check to once a day
                            EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            CognitiveVR_UpdateSDKWindow.Init(version,summary);
                        }
                    }
                }

                EditorApplication.update -= UpdateCheckForUpdates;
            }
        }

        public void Logout()
        {
            var prefs = GetPreferences();
            prefs.UserData = Json.UserData.Empty;
            prefs.sessionID = string.Empty;
            prefs.SelectedOrganization = null;
            prefs.SelectedProduct = null;
            //atm you do not need to stay logged in to keep your customerid for your product.
            AssetDatabase.SaveAssets();
        }

        public void SaveSettings()
        {
            CognitiveVR.CognitiveVR_Preferences prefs = CognitiveVR_Settings.GetPreferences();
            //prefs.CustomerID = newID;
            if (prefs.SelectedProduct != null)
            {
                prefs.CustomerID = prefs.SelectedProduct.customerId;
                if (testprodSelection == 0)
                    prefs.CustomerID += "-test";
                if (testprodSelection == 1)
                    prefs.CustomerID += "-prod";
            }

            EditorUtility.SetDirty(prefs);
            AssetDatabase.SaveAssets();

            if (EditorPrefs.GetString("cvr_version") != CognitiveVR.Core.SDK_Version)
            {
                EditorPrefs.SetString("cvr_version", CognitiveVR.Core.SDK_Version);
                EditorPrefs.SetString("cvr_updateDate", System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture));
                lastSdkUpdateDate = System.DateTime.UtcNow;
            }
        }

        //TODO should be in preferences
        public string[] GetUserOrganizations()
        {
            List<string> organizationNames = new List<string>();
            
            for (int i = 0; i<GetPreferences().UserData.organizations.Length;i++)
            {
                organizationNames.Add(GetPreferences().UserData.organizations[i].name);
            }

            return organizationNames.ToArray();
        }

        //TODO should be in preferences
        public string[] GetVRProductNames(string organization)
        {
            List<string> productNames = new List<string>();

            CognitiveVR.CognitiveVR_Preferences prefs = GetPreferences();

            Json.Organization org = prefs.GetOrganization(organization);
            if (org == null) { return productNames.ToArray(); }

            for (int i = 0; i < GetPreferences().UserData.products.Length; i++)
            {
                if (prefs.UserData.products[i].orgId == org.id)
                    productNames.Add(prefs.UserData.products[i].name);
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
            for (int i = 0; i< symbols.Length; i++)
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
            for(int i = 0; i<symbols.Length; i++)
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

        public bool IsUserLoggedIn()
        {
            if (string.IsNullOrEmpty(GetPreferences().sessionID))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// when the user is guided toward a specific action to complete a process
        /// </summary>
        /*public static bool ActionButton(string label,string tooltip, bool center = false)
        {
            bool clicked = false;
            if (center)
            {
                GUI.color = GreenButton;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                clicked = GUILayout.Button(new GUIContent(label, tooltip));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = GreenButton;
                clicked = GUILayout.Button(new GUIContent(label, tooltip));
                GUI.color = Color.white;
            }
            return clicked;
        }

        /// <summary>
        /// major separation of functions
        /// </summary>
        public static void HeaderLabel(string label, bool center = false)
        {
            if (center)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("<size=14><b>" + label + "</b></size>");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("<size=14><b>" + label + "</b></size>");
            }
        }

        /// <summary>
        /// minor separation of functions
        /// </summary>
        public static void SubHeaderLabel(string label, bool center = false)
        {
            if (center)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("<b>"+label+"</b>");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("<b>" + label + "</b>");
            }
        }

        /// <summary>
        /// show current selected items
        /// </summary>
        public static void HighlightLabel(string label, bool center = false)
        {
            if (center)
            {
                GUI.color = GreenText;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(label);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = GreenText;
                GUILayout.Label(label);
                GUI.color = Color.white;
            }
        }

        public static void CenterLabel(string label, string tooltip)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent(label,tooltip));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }*/

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
    }
}
 