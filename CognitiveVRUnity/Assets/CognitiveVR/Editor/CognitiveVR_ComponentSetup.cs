using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

/// <summary>
/// this window is simply for adding and removing analytics components from the cognitiveVR manager gameobject. this can also be done in the inspector
/// </summary>

namespace CognitiveVR
{
    public class CognitiveVR_ComponentSetup : EditorWindow
    {
        static bool remapHotkey;

        System.Collections.Generic.IEnumerable<Type> childTypes;
        Vector2 canvasPos;

        Texture2D tex;

        //[MenuItem("Window/cognitiveVR/Tracker Options Window", priority = 2)]
        public static void Init()
        {
            CognitiveVR_ComponentSetup window = (CognitiveVR_ComponentSetup)EditorWindow.GetWindow(typeof(CognitiveVR_ComponentSetup),true, "cognitiveVR Preferences");
            window.minSize = new Vector2(500,500);
            window.Show();

            window.tex = EditorGUIUtility.FindTexture("d_UnityEditor.InspectorWindow"); //component info icon
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

        void GetAnalyticsComponentTypes()
        {
            if (childTypes != null) { return; }
            int iterations = 1;
            Type pType = typeof(Components.CognitiveVRAnalyticsComponent);
            childTypes = Enumerable.Range(1, iterations)
               .SelectMany(i => Assembly.GetAssembly(pType).GetTypes()
                                .Where(t => t.IsClass && t != pType && pType.IsAssignableFrom(t))
                                .Select(t => t));
        }

        public void OnGUI()
        {
            if (tex == null)
                tex = EditorGUIUtility.FindTexture("d_UnityEditor.InspectorWindow");

            EditorGUIUtility.labelWidth = 200;

            GUI.skin.label.wordWrap = true;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            CognitiveVR_Manager manager = FindObjectOfType<CognitiveVR_Manager>();

            GetAnalyticsComponentTypes();

            canvasPos = GUILayout.BeginScrollView(canvasPos, false, true);
            EditorGUI.BeginDisabledGroup(manager == null);

            Color infoboxColor = Color.white;
            if (manager == null)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUI.Box(new Rect(canvasPos.x, canvasPos.y, position.width, position.height), "");
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
            }
            else
            {
                if (EditorGUIUtility.isProSkin)
                {
                    infoboxColor = new Color(0, 1, 0);
                }
                else
                {
                    infoboxColor = CognitiveVR_Settings.GreenButton;
                }
            }

            //==============
            //general settings
            //==============

            GUI.skin.label.richText = true;

            GUILayout.BeginHorizontal();
            GUILayout.Label("cognitiveVR Preferences", CognitiveVR_Settings.HeaderStyle);
            var prefs = CognitiveVR_Settings.GetPreferences();


            GUIContent standardInfo = new GUIContent(tex, "Record Player gaze and position\nGather GPU,CPU,RAM,OS,etc");

            Color c = GUI.color;
            GUI.color = infoboxColor;
            GUILayout.Box(standardInfo);
            GUI.color = c;
            GUILayout.EndHorizontal();

            if (manager != null)
            {
                prefs.SnapshotInterval = EditorGUILayout.FloatField(new GUIContent("Interval for Player Snapshots", "Delay interval for:\nArm Length\nHMD Height\nController Collision\nHMD Collision"), prefs.SnapshotInterval);
                prefs.SnapshotInterval = Mathf.Max(prefs.SnapshotInterval, 0.1f);

                prefs.DynamicObjectSearchInParent = EditorGUILayout.Toggle(new GUIContent("Dynamic Object Search in Collider Parent", "When capturing gaze on a dynamic object, also search in the collider's parent for the dynamic object component"), prefs.DynamicObjectSearchInParent);

                GUILayout.Space(10);
                GUILayout.Label("<size=12><b>Batching Data</b></size>");

                prefs.EvaluateGazeRealtime = EditorGUILayout.Toggle(new GUIContent("Evaluate Gaze in Real Time", "Send the gaze points during gameplay. False will send all the gaze points OnLevelLoaded, OnQuit, OnHMDRemove or when the threshold is reached"), prefs.EvaluateGazeRealtime);
                prefs.GazeSnapshotCount = EditorGUILayout.IntField(new GUIContent("Gaze Snapshot Threshold", "Automatically send gaze snapshots when this many have been taken"), prefs.GazeSnapshotCount);
                prefs.TransactionSnapshotCount = EditorGUILayout.IntField(new GUIContent("Transaction Threshold", "Automatically send event snapshots when this many have been taken"), prefs.TransactionSnapshotCount);
                prefs.DynamicSnapshotCount = EditorGUILayout.IntField(new GUIContent("Dynamic Object Snapshot Threshold", "Automatically send dynamic object snapshots when this many have been taken"), prefs.DynamicSnapshotCount);
                prefs.SensorSnapshotCount = EditorGUILayout.IntField(new GUIContent("Sensor Data Threshold", "Automatically send sensor snapshots when this many have been taken"), prefs.SensorSnapshotCount);

                prefs.GazeSnapshotCount = Mathf.Max(prefs.GazeSnapshotCount, 1);
                prefs.TransactionSnapshotCount = Mathf.Max(prefs.TransactionSnapshotCount, 1);
                prefs.DynamicSnapshotCount = Mathf.Max(prefs.DynamicSnapshotCount, 1);
                prefs.SensorSnapshotCount = Mathf.Max(prefs.SensorSnapshotCount, 1);

                GUILayout.Space(10);
                GUILayout.Label("<size=12><b>Sending Data</b></size>");
                prefs.SendDataOnLevelLoad = EditorGUILayout.Toggle(new GUIContent("Send Data on Level Load", "Send all snapshots on Level Loaded"), prefs.SendDataOnLevelLoad);
                prefs.SendDataOnQuit = EditorGUILayout.Toggle(new GUIContent("Send Data on Quit", "Sends all snapshots on Application OnQuit\nNot reliable on Mobile"), prefs.SendDataOnQuit);
                prefs.SendDataOnHMDRemove = EditorGUILayout.Toggle(new GUIContent("Send data on HMD remove", "Send all snapshots on HMD remove event"), prefs.SendDataOnHMDRemove);

                GUILayout.Space(10);
                GUILayout.Label("<size=12><b>Debug</b></size>");

                //prefs.DebugWriteToFile = EditorGUILayout.Toggle(new GUIContent("DEBUG - Write snapshots to file", "Write snapshots to file AND upload to SceneExplorer"), prefs.DebugWriteToFile);
                prefs.SendDataOnHotkey = EditorGUILayout.Toggle(new GUIContent("DEBUG - Send Data on Hotkey", "Press a hotkey to send data"), prefs.SendDataOnHotkey);

                EditorGUI.BeginDisabledGroup(!prefs.SendDataOnHotkey);

                if (remapHotkey)
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.wordWrap = true;
                    style.normal.textColor = new Color(0.5f, 1.0f, 0.5f, 1.0f);

                    GUILayout.BeginHorizontal();

                    GUILayout.Label(new GUIContent("Hotkey", "Shift, Ctrl and Alt modifier keys are not allowed"), GUILayout.Width(125));

                    GUI.color = Color.green;
                    GUILayout.Button("Any Key", GUILayout.Width(70));
                    GUI.color = Color.white;

                    string displayKey = (prefs.HotkeyCtrl ? "Ctrl + " : "") + (prefs.HotkeyShift ? "Shift + " : "") + (prefs.HotkeyAlt ? "Alt + " : "") + prefs.SendDataHotkey.ToString();
                    GUILayout.Label(displayKey);
                    GUILayout.EndHorizontal();
                    Event e = Event.current;

                    //shift, ctrl, alt
                    if (e.type == EventType.keyDown && e.keyCode != KeyCode.None && e.keyCode != KeyCode.LeftShift && e.keyCode != KeyCode.RightShift && e.keyCode != KeyCode.LeftControl && e.keyCode != KeyCode.RightControl && e.keyCode != KeyCode.LeftAlt && e.keyCode != KeyCode.RightAlt)
                    {
                        prefs.HotkeyAlt = e.alt;
                        prefs.HotkeyShift = e.shift;
                        prefs.HotkeyCtrl = e.control;
                        prefs.SendDataHotkey = e.keyCode;
                        remapHotkey = false;
                        //this is kind of a hack, but it works
                        GetWindow<CognitiveVR_SceneExportWindow>().Repaint();
                        GetWindow<CognitiveVR_SceneExportWindow>().Close();
                    }
                }
                else
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.wordWrap = true;
                    style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Hotkey", "Shift, Ctrl and Alt modifier keys are not allowed"), GUILayout.Width(125));
                    if (GUILayout.Button("Remap", GUILayout.Width(70)))
                    {
                        remapHotkey = true;
                    }
                    string displayKey = (prefs.HotkeyCtrl ? "Ctrl + " : "") + (prefs.HotkeyShift ? "Shift + " : "") + (prefs.HotkeyAlt ? "Alt + " : "") + prefs.SendDataHotkey.ToString();
                    GUILayout.Label(displayKey);
                    GUILayout.EndHorizontal();
                }

                EditorGUI.EndDisabledGroup();

                GUILayout.Space(10);
                GUILayout.Label("<size=12><b>Scene Type</b></size>");
                DisplayVideoRadioButtons();
                GUILayout.Space(10);
            }
            else
            {
                GUILayout.Space(10);
            }
            

            if (GUI.changed)
            {
                EditorUtility.SetDirty(prefs);
            }


            //==============
            //component list
            //==============

            foreach (var v in childTypes)
            {
                GUILayout.Space(10);
                GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                TogglableComponent(manager, v);
            }
            GUI.color = Color.white;
            EditorGUI.EndDisabledGroup();
            GUILayout.EndScrollView();


            


            //==============
            //footer
            //==============

            //add/select manager
            if (manager == null)
            {
                var rect = new Rect(position.width / 2 - 90, position.height / 2 - 25, 180, 50);

                if (GUI.Button(rect, new GUIContent("Add cognitiveVR Manager\n(required)", "Persists between scenes\nInitializes analytics system and gathers basic device info")))
                {
                    AddCognitiveVRManager();
                }
            }

            //close

            GUI.color = CognitiveVR_Settings.GreenButton;
            if (GUILayout.Button("Save and Close", GUILayout.Height(40)))
            {
                Close();
            }
            GUI.color = Color.white;
        }

        public static void AddCognitiveVRManager()
        {
            GameObject newManager = new GameObject("CognitiveVR_Manager");
            Selection.activeGameObject = newManager;

            Selection.activeGameObject = newManager;
            Undo.RegisterCreatedObjectUndo(newManager, "Create CognitiveVR Manager");
            newManager.AddComponent<CognitiveVR_Manager>();
            AddTrackerComponents(newManager);
        }

        static void AddTrackerComponents(GameObject cognitiveManager)
        {
            //go through all components that inherit from analytics component
#if CVR_STEAMVR
            cognitiveManager.AddComponent<CognitiveVR.Components.ArmLength>();
            cognitiveManager.AddComponent<CognitiveVR.Components.BoundaryEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.ControllerCollisionEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HMDCollisionEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HMDPresentEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HMDHeight>();
            cognitiveManager.AddComponent<CognitiveVR.Components.OcclusionEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.RoomSize>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Framerate>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Comfort>();
            

#elif CVR_OCULUS && !UNITY_ANDROID //rift

            cognitiveManager.AddComponent<CognitiveVR.Components.ArmLength>();
            cognitiveManager.AddComponent<CognitiveVR.Components.BoundaryEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.ControllerCollisionEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HMDCollisionEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HMDPresentEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HMDHeight>();
            cognitiveManager.AddComponent<CognitiveVR.Components.OcclusionEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.RoomSize>();
            cognitiveManager.AddComponent<CognitiveVR.Components.RecenterEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Framerate>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Comfort>();

#elif CVR_OCULUS && UNITY_ANDROID //gear

            cognitiveManager.AddComponent<CognitiveVR.Components.HMDPresentEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HeadphoneState>();
            cognitiveManager.AddComponent<CognitiveVR.Components.BatteryLevel>();
            cognitiveManager.AddComponent<CognitiveVR.Components.ScreenResolution>();
            cognitiveManager.AddComponent<CognitiveVR.Components.RecenterEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Framerate>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Comfort>();

#elif CVR_ARKIT || CVR_ARCORE

            cognitiveManager.AddComponent<CognitiveVR.Components.BatteryLevel>();
            cognitiveManager.AddComponent<CognitiveVR.Components.ScreenResolution>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Framerate>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Comfort>();

#elif CVR_FOVE

            cognitiveManager.AddComponent<CognitiveVR.Components.HMDCollisionEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.HMDPresentEvent>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Framerate>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Comfort>();

#elif CVR_DEFAULT

            cognitiveManager.AddComponent<CognitiveVR.Components.Framerate>();
            cognitiveManager.AddComponent<CognitiveVR.Components.Comfort>();
#endif
        }

        void DisplayVideoRadioButtons()
        {
            var prefs = CognitiveVR_Settings.GetPreferences();
            
            //radio buttons
            GUIStyle selectedRadio = new GUIStyle(GUI.skin.label);
            selectedRadio.normal.textColor = new Color(0, 0.0f, 0, 1.0f);
            selectedRadio.fontStyle = FontStyle.Bold;

            //3D content
            GUILayout.BeginHorizontal();

            bool contentIs3D = prefs.PlayerDataType == 0;
            bool newContentIs3D = GUILayout.Toggle(prefs.PlayerDataType == 0, "3D (default)", EditorStyles.radioButton);
            if (newContentIs3D != contentIs3D)
            {
                prefs.PlayerDataType = 0;
            }

            bool contentIs360 = prefs.PlayerDataType == 1;
            bool newContentIs360 = GUILayout.Toggle(prefs.PlayerDataType == 1, "360 Video", EditorStyles.radioButton);
            if (newContentIs360 != contentIs360)
            {
                prefs.PlayerDataType = 1;
            }
            GUILayout.EndHorizontal();

            if (GUI.changed)
            {
                if (prefs.PlayerDataType == 0) //3d content
                {
                    //prefs.TrackPosition = true;
                    prefs.TrackGazePoint = true;
                    //prefs.TrackGazeDirection = false;
                    //prefs.GazePointFromDirection = false;
                }
                else //video content
                {
                    //prefs.TrackPosition = true;
                    prefs.TrackGazePoint = false;
                    //prefs.TrackGazeDirection = false;
                    //prefs.GazePointFromDirection = true;
                }
            }

            if (prefs.PlayerDataType == 0) //3d content
            {
            }
            else //video content
            {
                prefs.GazeDirectionMultiplier = EditorGUILayout.FloatField(new GUIContent("360 Video Sphere Radius", "Multiplies the normalized GazeDirection"), prefs.GazeDirectionMultiplier);
                prefs.GazeDirectionMultiplier = Mathf.Max(0.1f, prefs.GazeDirectionMultiplier);
                prefs.VideoSphereDynamicObjectId = EditorGUILayout.IntField(new GUIContent("360 Video Sphere Custom ID", "The Custom ID used to identify the video sphere\n-1 means there is no video sphere"), prefs.VideoSphereDynamicObjectId);
                prefs.VideoSphereDynamicObjectId = Mathf.Clamp(prefs.VideoSphereDynamicObjectId, -1, 1000);
            }
        }

        void TogglableComponent(CognitiveVR_Manager manager, System.Type componentType)
        {
            Component component = null;
            if (manager != null)
            {
                component = manager.GetComponent(componentType);
            }

            Color infoboxColor = Color.white;

            GUILayout.BeginHorizontal();

            GUI.skin.label.richText = true;

            GUILayout.Label(componentType.Name, CognitiveVR_Settings.HeaderStyle);

            //open script button
            /*if (GUILayout.Button("Open Script",GUILayout.Width(100)))
            {
                //temporarily add script, open from component, remove component
                var tempComponent = manager.gameObject.AddComponent(componentType);
                AssetDatabase.OpenAsset(MonoScript.FromMonoBehaviour(tempComponent as MonoBehaviour));
                DestroyImmediate(tempComponent);
            }*/

            if (component != null)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    infoboxColor = new Color(0, 1, 0);
                }
                else
                {
                    infoboxColor = CognitiveVR_Settings.GreenButton;
                }

                MethodInfo warningInfo = componentType.GetMethod("GetWarning");
                if (warningInfo != null)
                {
                    var v = warningInfo.Invoke(null, null);
                    if (v != null && (bool)v == true)
                    {
                        
                        if (EditorGUIUtility.isProSkin)
                        {
                            infoboxColor = new Color(1, 0.5f, 0);
                        }
                        else
                        {
                            infoboxColor = CognitiveVR_Settings.OrangeButton;
                        }
                    }
                }
            }

            MethodInfo getDescription = componentType.GetMethod("GetDescription");
            if (getDescription == null)
            {
                GUILayout.Box("No description\nAdd a description by implementing 'public static string GetDescription()'", new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            }
            else
            {
                var v = getDescription.Invoke(null, null);

                Color c = GUI.color;
                GUI.color = infoboxColor;
                var guiC = new GUIContent(tex, (string)v);
                GUILayout.Box(guiC);
                GUI.color = c;
            }

            GUILayout.EndHorizontal();

            bool b = GUILayout.Toggle(component != null, "Enable");

            if (b != (component != null))
            {
                if (component == null)
                {
                    manager.gameObject.AddComponent(componentType);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                }
                else
                {
                    DestroyImmediate(component);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                }
            }

            if (component == null) { return; }
            //get all the fields
            foreach (var field in componentType.GetFields())
            {
                //all the attributes per field
                var attr = field.GetCustomAttributes(typeof(Components.DisplaySettingAttribute), false);

                if (attr.Length == 0)
                {
                    //no display settings attribute
                }
                else
                {
                    Type t = field.FieldType;
                    if (t == typeof(bool))
                    {
                        DisplayBoolField(componentType, component, field);
                    }
                    else if (t == typeof(int))
                    {
                        DisplayIntField(componentType, component, field);
                    }
                    else if (t == typeof(float))
                    {
                        DisplayFloatField(componentType, component, field);
                    }
                    else if (t == typeof(string))
                    {
                        DisplayStringField(componentType, component, field);
                    }
                    else if (t == typeof(LayerMask))
                    {
                        DisplayLayerMaskField(componentType, component, field);
                    }
                    else
                    {
                        GUILayout.Label(field.Name + t);
                    }
                }
            }
        }

        private void DisplayIntField(Type componentType, Component instance, FieldInfo field)
        {
            if (instance != null)
            {
                var valueAsInt = (int)field.GetValue(instance);

                var tempValue = 0;

                GUIContent guiContent = new GUIContent(field.Name, "");
                Components.DisplaySettingAttribute display = null;

                for (int i = 0; i<field.GetCustomAttributes(false).Length; i++)
                {
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(TooltipAttribute))
                    {
                        var tooltip = (TooltipAttribute)field.GetCustomAttributes(false)[i];
                        guiContent.tooltip = tooltip.tooltip;
                    }
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(Components.DisplaySettingAttribute))
                    {
                        display = (Components.DisplaySettingAttribute)field.GetCustomAttributes(false)[i];
                    }
                }

                tempValue = EditorGUILayout.IntField(guiContent, valueAsInt);

                int min, max;
                if (display.GetIntLimits(out min, out max))
                {
                    tempValue = Mathf.Clamp(tempValue, min, max);
                }

                if (GUI.changed)
                {
                    field.SetValue(instance, tempValue);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Label(field.Name);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DisplayFloatField(Type componentType, Component instance, FieldInfo field)
        {
            if (instance != null)
            {
                var valueAsFloat = (float)field.GetValue(instance);

                var tempValue = 0f;
                GUIContent guiContent = new GUIContent(field.Name, "");
                Components.DisplaySettingAttribute display = null;

                for (int i = 0; i < field.GetCustomAttributes(false).Length; i++)
                {
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(TooltipAttribute))
                    {
                        var tooltip = (TooltipAttribute)field.GetCustomAttributes(false)[i];
                        guiContent.tooltip = tooltip.tooltip;
                    }
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(Components.DisplaySettingAttribute))
                    {
                        display = (Components.DisplaySettingAttribute)field.GetCustomAttributes(false)[i];
                    }
                }

                tempValue = EditorGUILayout.FloatField(guiContent, valueAsFloat);
                float min, max;
                if (display.GetFloatLimits(out min, out max))
                {
                    tempValue = Mathf.Clamp(tempValue, min, max);
                }

                if (GUI.changed)
                {
                    field.SetValue(instance, tempValue);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Label(field.Name);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DisplayFloatSlider(Type componentType, Component instance, FieldInfo field)
        {
            if (instance != null)
            {
                var valueAsFloat = (float)field.GetValue(instance);

                var tempValue = 0f;
                GUIContent guiContent = new GUIContent(field.Name, "");
                Components.DisplaySettingAttribute display = null;

                for (int i = 0; i < field.GetCustomAttributes(false).Length; i++)
                {
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(TooltipAttribute))
                    {
                        var tooltip = (TooltipAttribute)field.GetCustomAttributes(false)[i];
                        guiContent.tooltip = tooltip.tooltip;
                    }
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(Components.DisplaySettingAttribute))
                    {
                        display = (Components.DisplaySettingAttribute)field.GetCustomAttributes(false)[i];
                    }
                }

                float max = 1;
                float min = 0;

                display.GetFloatLimits(out min, out max);

                tempValue = EditorGUILayout.Slider(guiContent, valueAsFloat,min,max);
                tempValue = Mathf.Clamp(tempValue, min, max);

                if (GUI.changed)
                {
                    field.SetValue(instance, tempValue);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Label(field.Name);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DisplayStringField(Type componentType, Component instance, FieldInfo field)
        {
            if (instance != null)
            {
                var valueAsString = (string)field.GetValue(instance);

                var tempValue = "";
                GUIContent guiContent = new GUIContent(field.Name, "");

                for (int i = 0; i < field.GetCustomAttributes(false).Length; i++)
                {
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(TooltipAttribute))
                    {
                        var tooltip = (TooltipAttribute)field.GetCustomAttributes(false)[i];
                        guiContent.tooltip = tooltip.tooltip;
                    }
                }

                tempValue = EditorGUILayout.TextField(guiContent, valueAsString);

                if (GUI.changed)
                {
                    field.SetValue(instance, tempValue);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Label(field.Name);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DisplayBoolField(Type component, Component instance, FieldInfo field)
        {
            if (instance != null)
            {
                var valueAsBool = (bool)field.GetValue(instance);

                var tempValue = false;
                GUIContent guiContent = new GUIContent(field.Name, "");

                for (int i = 0; i < field.GetCustomAttributes(false).Length; i++)
                {
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(TooltipAttribute))
                    {
                        var tooltip = (TooltipAttribute)field.GetCustomAttributes(false)[i];
                        guiContent.tooltip = tooltip.tooltip;
                    }
                }

                tempValue = EditorGUILayout.Toggle(guiContent, valueAsBool);

                if (GUI.changed)
                {
                    field.SetValue(instance, tempValue);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Label(field.Name);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DisplayLayerMaskField(Type component, Component instance, FieldInfo field)
        {
            if (instance != null)
            {
                
                var valueAsLayerMask = (LayerMask)field.GetValue(instance);

                var tempValue = valueAsLayerMask;
                GUIContent guiContent = new GUIContent(field.Name, "");

                for (int i = 0; i < field.GetCustomAttributes(false).Length; i++)
                {
                    if (field.GetCustomAttributes(false)[i].GetType() == typeof(TooltipAttribute))
                    {
                        var tooltip = (TooltipAttribute)field.GetCustomAttributes(false)[i];
                        guiContent.tooltip = tooltip.tooltip;
                    }
                }

                tempValue = LayerMaskField(guiContent,valueAsLayerMask);

                if (GUI.changed)
                {
                    field.SetValue(instance, tempValue);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Label(field.Name);
                EditorGUI.EndDisabledGroup();
            }
        }

        public static List<int> layerNumbers = new List<int>();

        public static LayerMask LayerMaskField(GUIContent content, LayerMask layerMask)
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;

            layerNumbers.Clear();

            for (int i = 0; i < layers.Length; i++)
                layerNumbers.Add(LayerMask.NameToLayer(layers[i]));

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = UnityEditor.EditorGUILayout.MaskField(content, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;

            return layerMask;
        }
    }
}