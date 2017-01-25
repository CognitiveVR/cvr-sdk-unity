using UnityEngine;
using UnityEditor;
using System.Collections;

namespace CognitiveVR
{
    public class CognitiveVR_NewProductPopup : PopupWindowContent
    {
        string productName = "";

        public override Vector2 GetWindowSize()
        {
            return new Vector2(292, 150);
        }

        public override void OnGUI(Rect rect)
        {           
            GUILayout.Label("New Product", EditorStyles.boldLabel);

            if (CognitiveVR_Preferences.Instance.UserData.organizations.Length > 1)
            {
                GUILayout.Label("Current Organization: " + CognitiveVR_Preferences.Instance.SelectedOrganization.name);
            }

            productName = CognitiveVR_Settings.GhostTextField("MyProductName", "", productName);
            GUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(productName));

            if (!string.IsNullOrEmpty(productName))
            {
                GUI.color = CognitiveVR_Settings.GreenButton;
                GUI.contentColor = Color.white;
            }

            if (GUILayout.Button("Create"))
            {
                RequestNewProduct();
                editorWindow.Close();
            }
            EditorGUI.EndDisabledGroup();

            GUI.color = Color.white;
            GUI.contentColor = Color.white;

            if (GUILayout.Button("Close"))
            {
                editorWindow.Close();                
            }

            GUILayout.EndHorizontal();
        }
        
        public void RequestNewProduct()
        {
            if (CognitiveVR_Settings.Instance == null)
            {
                Debug.Log("Instance of cognitiveVR_Settings window is null"); //when recompiling with the window open, instance loses it's reference
                editorWindow.Close();
                return;
            }
            CognitiveVR_Settings.Instance.RequestNewProduct(productName);
        }
    }
}