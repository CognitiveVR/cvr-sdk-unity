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

            productName = CognitiveVR_SceneExportWindow.GhostTextField("MyProductName", "", productName);
            //productName = EditorGUILayout.TextField(productName);
            productName = MakeProductNameSafe(productName);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Create"))
            {
                RequestNewProduct();
                editorWindow.Close();
            }
            if (GUILayout.Button("Close"))
            {
                editorWindow.Close();                
            }

            GUILayout.EndHorizontal();
        }

        string MakeProductNameSafe(string unsafeName)
        {
            //TODO some regex nightmare to make sure it's all cool
            return unsafeName.Replace(' ', '_');
        }
        
        public void RequestNewProduct()
        {
            CognitiveVR_Settings.Instance.RequestNewProduct(productName);
        }
    }
}