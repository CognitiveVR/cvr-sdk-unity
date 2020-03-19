using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
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

            Handles.DrawWireArc(menu.transform.position + Vector3.up * menu.Height, Vector3.up, Vector3.forward, menu.ArcSize * Mathf.Rad2Deg, menu.SpawnRadius);
            Handles.DrawWireArc(menu.transform.position + Vector3.up * menu.Height, Vector3.up, Vector3.forward, -menu.ArcSize * Mathf.Rad2Deg, menu.SpawnRadius);

            //Handles.color = new Color(0.5f, 0.5f, 1, 0.1f);
            //Handles.DrawSolidArc(menu.transform.position+Vector3.up * menu.Height, Vector3.up, Vector3.forward, menu.ArcSize * Mathf.Rad2Deg, menu.SpawnRadius);
            //Handles.DrawSolidArc(menu.transform.position+Vector3.up * menu.Height, Vector3.up, Vector3.forward, -menu.ArcSize * Mathf.Rad2Deg, menu.SpawnRadius);
        }

        public override void OnInspectorGUI()
        {
            var ab = target as AssessmentBase;

            //display list of enable/disabled objects
            string controlledComponentList = "These components are enabled only while this Assessment is active:\n";
            int childCount = ab.transform.childCount;
            if (childCount > 0)
                controlledComponentList += "\n<b>Child Transforms</b>";
            for (int i = 0; i < childCount; i++)
            {
                controlledComponentList += "\n   " + ab.transform.GetChild(i).gameObject.name;
            }
            if (ab.ControlledByAssessmentState.Count > 0)
                controlledComponentList += "\n<b>Controlled By Assessment State</b>";
            foreach (var v in ab.ControlledByAssessmentState)
            {
                if (v == null) { continue; }
                controlledComponentList += "\n   " + v.name;
            }

            InitGUIStyle();
            EditorGUILayout.LabelField(new GUIContent(controlledComponentList, "These GameObjects are:\n- Disabled on OnEnable\n- Enabled on BeginAssessment\n- Disabled on CompleteAssessment"), helpboxStyle);



            if (ab.Active == false)
            {
                EditorGUILayout.HelpBox("This assessment is disabled because due to how Ready Room is configured", MessageType.Info);
            }

            EditorGUI.BeginDisabledGroup(true);
            var textComponent = ab.GetComponentInChildren<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                EditorGUILayout.LabelField("Text Display:", textComponent.text, boldWrap);
            }
            EditorGUILayout.IntField(new GUIContent("Order", "The order this assessment will be displayed to the participant. Can be set in the Ready Room Setup window"), ab.Order);
            EditorGUI.EndDisabledGroup();
            
            base.OnInspectorGUI();
        }
    }
}