using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//IMPROVEMENT custom editor to drag/drop scene assets onto and auto-fill the list of sceneinfos

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
            Handles.DrawWireCube(Vector3.zero + Vector3.up*2, new Vector3(1, 1, 0));
            Handles.Label(Vector3.up*2, menu.SceneInfos[i].DisplayName);
        }

        Handles.matrix = startMatrix;
    }

    public override void OnInspectorGUI()
    {
        var ab = target as AssessmentBase;
        if (ab.Active == false)
        {
            EditorGUILayout.HelpBox("This assessment is disabled because due to how Ready Room is configured", MessageType.Info);
        }
        base.OnInspectorGUI();
    }
}