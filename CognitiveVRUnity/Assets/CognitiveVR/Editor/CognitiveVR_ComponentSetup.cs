using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;
using System.Globalization;

/// <summary>
/// this window is simply for adding and removing analytics components from the cognitiveVR manager gameobject. this can also be done in the inspector
/// </summary>

namespace CognitiveVR
{
    public class CognitiveVR_ComponentSetup : EditorWindow
    {
        static Color Green = new Color(0.6f, 1f, 0.6f);
        static Color Orange = new Color(1f, 0.6f, 0.3f);

        System.Collections.Generic.IEnumerable<Type> childTypes;
        Vector2 canvasPos;

        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            CognitiveVR_ComponentSetup window = (CognitiveVR_ComponentSetup)EditorWindow.GetWindow(typeof(CognitiveVR_ComponentSetup));
            window.minSize = new Vector2(500,500);
            window.Show();
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
            GUI.skin.label.wordWrap = true;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            CognitiveVR_Manager manager = FindObjectOfType<CognitiveVR_Manager>();

            //==============
            //component list
            //==============

            GetAnalyticsComponentTypes();

            canvasPos = GUILayout.BeginScrollView(canvasPos,false,true);
            EditorGUI.BeginDisabledGroup(manager == null);

            if (manager == null)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUI.Box(new Rect(canvasPos.x, canvasPos.y, position.width, position.height),"");
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
            }

            bool displayedFirstComponent = false;
            foreach (var v in childTypes)
            {
                GUILayout.Space(10);
                if (displayedFirstComponent)
                {
                    GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                }
                else{displayedFirstComponent = true;}
                TogglableComponent(manager, v);
            }
            GUI.color = Color.white;
            EditorGUI.EndDisabledGroup();
            GUILayout.EndScrollView();





            //==============
            //footer
            //==============

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            
            //add/select manager
            if (manager == null)
            {
                if (GUILayout.Button(new GUIContent("Add CognitiveVR Manager", "Does not Destroy on Load\nInitializes analytics system with basic device info"),GUILayout.Height(40)))
                {
                    string sampleResourcePath = GetSamplesResourcePath();
                    UnityEngine.Object basicInit = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sampleResourcePath + "CognitiveVR/Resources/CognitiveVR_Manager.prefab");
                    if (basicInit)
                    {
                        Selection.activeGameObject = PrefabUtility.InstantiatePrefab(basicInit) as GameObject;
                    }
                    else
                    {
                        Debug.LogWarning("Couldn't find CognitiveVR_Manager.prefab");
                        GameObject go = new GameObject("CognitiveVR_Manager");
                        manager = go.AddComponent<CognitiveVR_Manager>();
                        Selection.activeGameObject = go;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Select CognitiveVR Manager", GUILayout.Height(40)))
                {
                    Selection.activeGameObject = manager.gameObject;
                }
            }
            
            //add/remove all
            EditorGUI.BeginDisabledGroup(manager == null);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add All", GUILayout.MaxWidth(position.width/2)))
            {
                foreach (var component in childTypes)
                {
                    if (!manager.GetComponent(component))
                    {
                        manager.gameObject.AddComponent(component);
                    }
                }
            }

            if (GUILayout.Button("Remove All", GUILayout.MaxWidth(position.width / 2)))
            {
                foreach (var component in childTypes)
                {
                    if (manager.GetComponent(component))
                    {
                        DestroyImmediate(manager.GetComponent(component));
                    }
                }
            }

            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            //close

            GUILayout.Space(20);

            if (GUILayout.Button("Save and Close"))
            {
                Close();
            }
        }

        void TogglableComponent(CognitiveVR_Manager manager, System.Type componentType)
        {
            Component component = null;
            if (manager != null)
                component = manager.GetComponent(componentType);

            GUILayout.BeginHorizontal();

            GUI.skin.toggle.richText = true;

            bool b = GUILayout.Toggle(component != null, "<size=14><b>" + componentType.Name + "</b></size>");

            if (b != (component != null))
            {
                if (component == null)
                {
                    manager.gameObject.AddComponent(componentType);
                }
                else
                {
                    DestroyImmediate(component);
                }
            }

            if (GUILayout.Button("Open Script",GUILayout.Width(100)))
            {
                //temporarily add script, open from component, remove component
                var tempComponent = manager.gameObject.AddComponent(componentType);
                AssetDatabase.OpenAsset(MonoScript.FromMonoBehaviour(tempComponent as MonoBehaviour));
                DestroyImmediate(tempComponent);
            }

            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();

            if (component != null)
            {
                GUI.backgroundColor = Green;
                MethodInfo warningInfo = componentType.GetMethod("GetWarning");
                if (warningInfo != null)
                {
                    var v = warningInfo.Invoke(null, null);
                    if (v != null && (bool)v == true)
                    {
                        GUI.backgroundColor = Orange;
                    }
                }
            }

            MethodInfo info = componentType.GetMethod("GetDescription");
            if (info == null)
            {
                GUILayout.Box("No description\nAdd a description by implementing 'public static string GetDescription()'", new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            }
            else
            {
                var v = info.Invoke(null, null);
                GUILayout.Box((string)v, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            }

            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
        }
    }
}