using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D.ReadyRoom
{
    [CustomEditor(typeof(SceneSelectMenu))]
    public class SceneSelectMenuEditor : Editor
    {
        GUIStyle helpboxStyle;
        GUIStyle boldWrap;
        public void InitGUIStyle()
        {
            if (helpboxStyle != null) { return; }
            helpboxStyle = new GUIStyle(EditorStyles.helpBox);
            helpboxStyle.richText = true;

            boldWrap = new GUIStyle(EditorStyles.boldLabel);
            boldWrap.wordWrap = true;
        }

        public void OnSceneGUI()
        {
            var menu = target as SceneSelectMenu;
            var positions = menu.GetPositions(menu.SceneInfos.Count);

            var startMatrix = Handles.matrix;
            var menuPos = menu.transform.position;
            menuPos.y = 0;
            Handles.color = Color.white;

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 horizontalPosition = positions[i] + menuPos;
                horizontalPosition.y = 0;

                Handles.matrix = Matrix4x4.LookAt(horizontalPosition, menuPos, Vector3.up);
                Handles.DrawWireCube(Vector3.zero + Vector3.up * menu.Height, new Vector3(1, 1, 0));
                Handles.color = Color.white;
                Handles.Label(Vector3.up * menu.Height, menu.SceneInfos[i].DisplayName, EditorStyles.whiteLargeLabel);
            }

            Handles.matrix = startMatrix;

            Handles.DrawWireArc(menu.transform.position + Vector3.up * menu.Height, Vector3.up, Vector3.forward, menu.ArcSize, menu.SpawnRadius);
            Handles.DrawWireArc(menu.transform.position + Vector3.up * menu.Height, Vector3.up, Vector3.forward, -menu.ArcSize, menu.SpawnRadius);
        }

        public override void OnInspectorGUI()
        {
            var ab = target as AssessmentBase;

            InitGUIStyle();
            
            base.OnInspectorGUI();
        }
    }
}