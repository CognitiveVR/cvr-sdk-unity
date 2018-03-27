using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace CognitiveVR
{
    public class CognitiveVR_SelectSDKPopup : PopupWindowContent
    {
        public override Vector2 GetWindowSize()
        {
            return new Vector2(292, 310);
        }

        public override void OnOpen()
        {
#if CVR_STEAMVR
            option.Add("CVR_STEAMVR");
#endif
#if CVR_OCULUS
            option.Add("CVR_OCULUS");
#endif
#if CVR_GOOGLEVR
            option.Add("CVR_GOOGLEVR");
#endif
#if CVR_DEFAULT
            option.Add("CVR_DEFAULT");
#endif
#if CVR_FOVE
            option.Add("CVR_FOVE");
#endif
#if CVR_PUPIL
            option.Add("CVR_PUPIL");
#endif
#if CVR_ARKIT //apple
            option.Add("CVR_ARKIT");
#endif
#if CVR_ARCORE //google
            option.Add("CVR_ARCORE");
#endif
#if CVR_META 
            option.Add("CVR_META");
#endif
        }

        public override void OnClose()
        {
            EditorCore.SetPlayerDefine(option);
        }

        List<string> option = new List<string>();
        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<b>Please Select your VR SDK</b>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (option.Contains("CVR_DEFAULT")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Unity Default"))
            {
                if (option.Contains("CVR_DEFAULT"))
                    option.Remove("CVR_DEFAULT");
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_DEFAULT");
                }
            }
            GUI.color = Color.white;

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Label("VR", GUILayout.Width(20));
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.EndHorizontal();

            if (option.Contains("CVR_STEAMVR")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Steam VR 1.2.0"))
            {
                if (option.Contains("CVR_STEAMVR"))
                    option.Remove("CVR_STEAMVR");
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_STEAMVR");
                }
            }
            GUI.color = Color.white;

            if (option.Contains("CVR_OCULUS")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Oculus Utilities 1.9.0"))
            {
                if (option.Contains("CVR_OCULUS"))
                    option.Remove("CVR_OCULUS");
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_OCULUS");
                }
            }
            GUI.color = Color.white;

            if (option.Contains("CVR_FOVE")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Fove VR 1.3.1"))
            {
                if (option.Contains("CVR_FOVE"))
                {
                    option.Remove("CVR_FOVE");
                    option.Remove("CVR_GAZETRACK");
                }
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_FOVE");
                    option.Add("CVR_GAZETRACK");
                }
            }
            GUI.color = Color.white;

            if (option.Contains("CVR_PUPIL")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Pupil Labs"))
            {
                if (option.Contains("CVR_PUPIL"))
                {
                    option.Remove("CVR_PUPIL");
                    option.Remove("CVR_GAZETRACK");
                }
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_PUPIL");
                    option.Add("CVR_GAZETRACK");
                }
            }
            GUI.color = Color.white;

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Label("AR",GUILayout.Width(20));
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.EndHorizontal();

            if (option.Contains("CVR_ARKIT")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Apple ARKit"))
            {
                if (option.Contains("CVR_ARKIT"))
                {
                    option.Remove("CVR_ARKIT");
                }
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_ARKIT");
                }
            }
            GUI.color = Color.white;

            if (option.Contains("CVR_ARCORE")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Google ARCore"))
            {
                if (option.Contains("CVR_ARCORE"))
                {
                    option.Remove("CVR_ARCORE");
                }
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_ARCORE");
                }
            }
            GUI.color = Color.white;

            if (option.Contains("CVR_META")) { GUI.color = EditorCore.GreenButton; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Meta SDK v2.4.1.49"))
            {
                if (option.Contains("CVR_META"))
                {
                    option.Remove("CVR_META");
                }
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_META");
                }
            }
            GUI.color = Color.white;

            GUILayout.Space(5);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(5);

            GUI.color = EditorCore.GreenButton;
            GUI.contentColor = Color.white;
            if (GUILayout.Button("Save and Close"))
            {
                editorWindow.Close();
            }
            GUI.color = Color.white;
        }
    }
}