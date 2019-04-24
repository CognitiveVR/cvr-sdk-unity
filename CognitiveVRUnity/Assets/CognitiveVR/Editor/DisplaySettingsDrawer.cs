using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR.Components
{
    [CustomPropertyDrawer(typeof(DisplaySettingAttribute))]
    public class DisplaySettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);

            var display = attribute as DisplaySettingAttribute;

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.IntField(position, label, property.intValue);
                int min;
                int max;
                display.GetIntLimits(out min, out max);
                if (max == 0) { max = int.MaxValue; }
                property.intValue = Mathf.Clamp(property.intValue, min, max);
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = EditorGUI.FloatField(position, label, property.floatValue);
                float min;
                float max;
                display.GetFloatLimits(out min, out max);
                if (Mathf.Approximately(max, 0)) { max = float.MaxValue; }
                property.floatValue = Mathf.Clamp(property.floatValue, min, max);
            }
            else if (property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = EditorGUI.Toggle(position, label, property.boolValue);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}