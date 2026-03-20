using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class RenameDynamicWindow : EditorWindow
    {
        static LegacyDynamicObjectsWindow sourceWindow;
        static DynamicObjectDetailGUI sourceGUI;
        string defaultMeshName;
        static System.Action<string> action;
        public static void Init(LegacyDynamicObjectsWindow dynamicsWindow, string defaultName, System.Action<string> renameAction, string title)
        {
            RenameDynamicWindow window = (RenameDynamicWindow)EditorWindow.GetWindow(typeof(RenameDynamicWindow), true, title);
            window.ShowUtility();
            sourceWindow = dynamicsWindow;
            window.defaultMeshName = defaultName;
            action = renameAction;
        }

        public static void Init(DynamicObjectDetailGUI dynamicsGUI, string defaultName, System.Action<string> renameAction, string title)
        {
            RenameDynamicWindow window = (RenameDynamicWindow)EditorWindow.GetWindow(typeof(RenameDynamicWindow), true, title);
            window.ShowUtility();
            sourceGUI = dynamicsGUI;
            window.defaultMeshName = defaultName;
            action = renameAction;
        }

        bool hasDoneInitialFocus;
        void OnGUI()
        {
            GUI.SetNextControlName("initialFocus");
            defaultMeshName = GUILayout.TextField(defaultMeshName);

            if (!hasDoneInitialFocus)
            {
                hasDoneInitialFocus = true;
                GUI.FocusControl("initialFocus");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rename"))
            {
                action.Invoke(defaultMeshName);
                if (sourceWindow != null) sourceWindow.RefreshList();
                if (sourceGUI != null) sourceGUI.RefreshList();
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                if (sourceWindow != null) sourceWindow.RefreshList();
                if (sourceGUI != null) sourceGUI.RefreshList();
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}
