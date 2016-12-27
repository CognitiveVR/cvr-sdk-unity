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
    namespace Json
    {
        class SDKVersion
        {
            public string version;
        }
    }

    [InitializeOnLoad]
    public class CognitiveVR_Settings : EditorWindow
    {
        public static Color Green = new Color(0.6f, 1f, 0.6f);
        public static Color Orange = new Color(1f, 0.6f, 0.3f);

        public static CognitiveVR_Settings Instance;

        System.DateTime lastUpdateDate;



        [MenuItem("cognitiveVR/Settings")]
        public static void Init()
        {
            EditorApplication.update -= EditorUpdate;

            // Get existing open window or if none, make a new one:
            Instance = GetWindow<CognitiveVR_Settings>(true,"cognitiveVR Settings");
            Vector2 size = new Vector2(300, 550);
            Instance.minSize = size;
            Instance.maxSize = size;
            Instance.Show();

            //TODO fix this. shouldn't have to re-read this value to parse it to the correct format
            if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out Instance.lastUpdateDate))
            {
                Instance.lastUpdateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
            }
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
                Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings");
                Vector2 size = new Vector2(300, 550);
                Instance.minSize = size;
                Instance.maxSize = size;
                Instance.Show();

                //System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), out Instance.lastUpdateDate);
            }

            System.DateTime remindDate;
            //TODO fix this. shouldn't have to re-read this value to parse it to the correct format
            if (Instance != null)
            {
                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), out Instance.lastUpdateDate))
                {
                    remindDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                    if (System.DateTime.UtcNow > remindDate)
                    {
                        Instance.CheckForUpdates();
                        Debug.Log("settings check for updates");
                    }
                    else
                    {
                        Debug.Log("check updates later " + remindDate);
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
                Debug.Log("NO INSTANCE TO CHECK UPDATES ON");
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

        public void OnGUI()
        {
            GUI.skin.label.richText = true;

            CognitiveVR.CognitiveVR_Preferences prefs = GetPreferences();
            
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

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Authenticate</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            prefs.UserName = GUILayout.TextField(prefs.UserName);
            password = EditorGUILayout.PasswordField(password);
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
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(loginResponse);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Select Organization</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            string lastOrganization = prefs.SelectedOrganization; //used to check if organizations changed. for displaying products
            if (!string.IsNullOrEmpty(prefs.SelectedOrganization))
            {
                for (int i = 0; i < prefs.UserData.organizations.Length; i++)
                {
                    if (prefs.UserData.organizations[i].name == prefs.SelectedOrganization)
                    {
                        organizationIndex = i;
                        break;
                    }
                }
            }

            string[] organizations = GetUserOrganizations();
            organizationIndex = EditorGUILayout.Popup(organizationIndex, organizations);
            if (organizations.Length > 0)
                prefs.SelectedOrganization = organizations[organizationIndex];

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Select Product</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            string organizationName = "";
            if (organizations.Length > 0)
                 organizationName = organizations[organizationIndex];

            string[] products = GetVRProductNames(organizationName);

            if (lastOrganization != prefs.SelectedOrganization)
            {
                //ie, changed organization
                prefs.SelectedProduct = "";
                productIndex = 0;
            }
            else
            {   
                if (!string.IsNullOrEmpty(prefs.SelectedProduct))
                {
                    for (int i = 0; i < products.Length; i++)
                    {
                        if (products[i] == prefs.SelectedProduct)
                        {
                            productIndex = i;
                            break;
                        }
                    }
                }
            }

            productIndex = EditorGUILayout.Popup(productIndex, products);
            if (products.Length > 0)
                prefs.SelectedProduct = products[productIndex];

            if (GUILayout.Button("Add new Product"))
            {
                PopupWindow.Show(productRect, new CognitiveVR_NewProductPopup());
            }
            if (Event.current.type == EventType.Repaint) productRect = GUILayoutUtility.GetLastRect();

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            //=========================
            //options and updates
            //=========================

            if (GUILayout.Button("Manage Tracking Options"))
            {
                CognitiveVR_ComponentSetup.Init();
            }
            if (GUILayout.Button("Manage Scene Explorer"))
            {
                CognitiveVR_SceneExportWindow.Init();
            }
            if (GUILayout.Button("Check for Updates"))
            {
                EditorPrefs.SetString("cvr_updateRemindDate", "");
                EditorPrefs.SetString("cvr_skipVersion", "");
                CheckForUpdates();
            }

            //=========================
            //select sdk
            //=========================

            if (GUILayout.Button("Select SDK"))
            {
                PopupWindow.Show(sdkRect, new CognitiveVR_SelectSDKPopup());
            }
            if (Event.current.type == EventType.Repaint) sdkRect = GUILayoutUtility.GetLastRect();


            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            //=========================
            //version
            //=========================

            if (GUILayout.Button("Save"))
            {
                //SaveSettings();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: " + Core.SDK_Version);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Last Updated: " + lastUpdateDate.ToShortDateString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return;


            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

           

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("CognitiveVR Customer ID");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUIContent addInitButtonText = new GUIContent("Add CognitiveVR Manager", "Does not Destroy on Load\nInitializes analytics system with basic device info");

            bool hasManager = FindObjectOfType<CognitiveVR.CognitiveVR_Manager>() != null;
            if (hasManager)
            {
                addInitButtonText.text = "CognitiveVR Manager Found!";
                addInitButtonText.tooltip = "";
            }

            /*if (Event.current.type == EventType.repaint && string.IsNullOrEmpty(newID))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                EditorGUILayout.TextField("companyname1234-productname-test", style);
            }
            else
            {
                newID = EditorGUILayout.TextField(newID);
            }

            bool validID = (newID != null && newID != "companyname1234-productname-test" && newID.Length > 0);*/
            bool validID = false;
            if (validID)
            {
                if (hasManager) { GUI.color = Green; }
                if (GUILayout.Button(addInitButtonText))
                {
                    if (!hasManager)
                    {
                        string sampleResourcePath = GetSamplesResourcePath();
                        Object basicInit = AssetDatabase.LoadAssetAtPath<Object>(sampleResourcePath + "CognitiveVR/Resources/CognitiveVR_Manager.prefab");
                        if (basicInit)
                        {
                            PrefabUtility.InstantiatePrefab(basicInit);
                        }
                        else
                        {
                            Debug.LogWarning("Couldn't find CognitiveVR_Manager.prefab");
                            GameObject go = new GameObject("CognitiveVR_Manager");
                            go.AddComponent<CognitiveVR_Manager>();
                            Selection.activeGameObject = go;
                        }
                    }
                }
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Button("Invalid Customer ID");
            }

            //=========================
            //SDK
            //=========================

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!validID);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Please Select your VR SDK");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            //=========================
            //save
            //=========================

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!validID);

            if (GUILayout.Button("Save"))
            {
                SaveSettings();
            }

            bool containsSDKSymbol = false;
            if (PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Contains("CVR_"))
            {
                containsSDKSymbol = true;
            }

            EditorGUI.BeginDisabledGroup(!containsSDKSymbol);
            //save and close
            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR.CognitiveVR_ComponentSetup.Init();

                Close();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
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
                    Debug.Log("got some error" + loginRequest.error);
                    loginResponse = "Could not log in!";
                }

                if (!string.IsNullOrEmpty(loginRequest.text))
                {
                    Debug.Log("got some data " + loginRequest.text);
                    loginResponse = "";
                    //save this text somewhere. preferences?
                    GetPreferences().UserData = JsonUtility.FromJson<Json.UserData>(loginRequest.text);
                    GetPreferences().sessionID = "1234";
                }

                EditorApplication.update -= CheckLoginResponse;
            }
        }

        WWW NewProductRequest;
        public void RequestNewProduct(string productName)
        {
            //productName
            //CognitiveVR_Settings.GetPreferences().SelectedOrganization

            //create www request to server somewhere

            var url = "http://testapi.cognitivevr.io/makenewproduct";
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            System.Text.StringBuilder json = new System.Text.StringBuilder();
            json.Append("{");
            json.Append("\"sessionId\":\"" + GetPreferences().sessionID + "\",");
            json.Append("\"organizationName\":\"" + GetPreferences().SelectedOrganization + "\",");
            json.Append("\"productName\":\"" + productName + "\"");
            json.Append("}");

            byte[] bytes = new System.Text.UTF8Encoding(true).GetBytes(json.ToString());
            NewProductRequest = new UnityEngine.WWW(url, bytes, headers);

            //used current organization
            //add product to list of product names

            EditorApplication.update += UpdateNewProductRequest;
        }

        public void UpdateNewProductRequest()
        {
            if (!NewProductRequest.isDone)
            {
                //check for timeout
            }
            else
            {
                if (!string.IsNullOrEmpty(NewProductRequest.error))
                {
                    Debug.Log("got some error " + NewProductRequest.error);
                }

                if (!string.IsNullOrEmpty(NewProductRequest.text))
                {
                    Debug.Log("got some data " + NewProductRequest.text);
                    //it would be nice for the response to include a customerid
                }

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
                    Debug.Log("got some error " + checkForUpdatesRequest.error);
                }

                if (!string.IsNullOrEmpty(checkForUpdatesRequest.text))
                {
                    //this should return the latest sdk version number
                    Debug.Log("got some data " + checkForUpdatesRequest.text);

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
        }

        public void SaveSettings()
        {
            //SetPlayerDefine(option);

            CognitiveVR.CognitiveVR_Preferences prefs = CognitiveVR_Settings.GetPreferences();
            //prefs.CustomerID = newID;
            var product = prefs.GetProduct(prefs.SelectedProduct);
            if (product != null)
            {
                prefs.CustomerID = product.customerId;
            }

            EditorUtility.SetDirty(prefs);
            AssetDatabase.SaveAssets();

            if (EditorPrefs.GetString("cvr_version") != CognitiveVR.Core.SDK_Version)
            {

                EditorPrefs.SetString("cvr_version", CognitiveVR.Core.SDK_Version);
                EditorPrefs.SetString("cvr_updateDate", System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture));
                lastUpdateDate = System.DateTime.UtcNow;
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
    }
}
 