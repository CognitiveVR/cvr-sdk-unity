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
            return new Vector2(292, 150);
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
        }

        public override void OnClose()
        {
            CognitiveVR_Settings.Instance.SetPlayerDefine(option);
        }

        List<string> option = new List<string>();
        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Please Select your VR SDK");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (option.Contains("CVR_STEAMVR")) { GUI.color = CognitiveVR_Settings.Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Steam VR 1.1.1+"))
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

            if (option.Contains("CVR_OCULUS")) { GUI.color = CognitiveVR_Settings.Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Oculus Utilities 1.9.0+"))
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

            if (option.Contains("CVR_FOVE")) { GUI.color = CognitiveVR_Settings.Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Fove VR Plugin"))
            {
                if (option.Contains("CVR_FOVE"))
                    option.Remove("CVR_FOVE");
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_FOVE");
                }
            }
            GUI.color = Color.white;

            if (option.Contains("CVR_DEFAULT")) { GUI.color = CognitiveVR_Settings.Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Unity Default VR Settings"))
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

            if (GUILayout.Button("Save and Close"))
            {
                editorWindow.Close();                
            }
        }
    }
}