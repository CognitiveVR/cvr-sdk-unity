using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SceneSelectMenu))]
public class SceneSelectMenuEditor : Editor
{
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
        if (ab.Active == false)
        {
            EditorGUILayout.HelpBox("This assessment is disabled because due to how Ready Room is configured", MessageType.Info);
        }

        EditorGUILayout.LabelField("The user is presented with the SceneInfos below and asked to choose which scene they want to load. This should always be the final assessment!", EditorStyles.boldLabel);

        GUILayout.Space(15);
        base.OnInspectorGUI();
    }
}