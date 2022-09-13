using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    [CustomEditor(typeof(Cognitive3D.AssessmentManager))]
    public class AssessmentManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Setup Window"))
            {
                ReadyRoomSetupWindow.Init();
            }
            base.OnInspectorGUI();
        }
    }
}