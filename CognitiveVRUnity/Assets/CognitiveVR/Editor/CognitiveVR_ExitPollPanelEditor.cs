using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CognitiveVR
{
    [CustomEditor(typeof(ExitPollPanel))]
    public class CognitiveVR_ExitPollPanelEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Set Buttons to Gradient"))
            {
                ApplyGradient((ExitPollPanel)target);
            }
        }

        void ApplyGradient(ExitPollPanel panel)
        {
            var grad = panel.IntegerGradient;
            var horizontal = panel.GetComponentInChildren<UnityEngine.UI.HorizontalLayoutGroup>();
            if (horizontal == null) { return; }
            Transform horizontalGroup = horizontal.transform;
            int gazeButtonCount = horizontalGroup.childCount;

            for (int i = 0; i<gazeButtonCount; i++)
            {
                var imagechild = horizontalGroup.GetChild(i).Find("Image");
                if (imagechild == null) { continue; }
                var image = imagechild.GetComponent<UnityEngine.UI.Image>();
                if (image == null) { continue; }
                image.color = grad.Evaluate(i / (float)gazeButtonCount);
            }
            Debug.Log("Set " + gazeButtonCount + " colours on gaze buttons");
        }
    }
}
