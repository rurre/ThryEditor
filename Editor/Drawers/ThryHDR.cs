// Borrowed from liltoon https://github.com/lilxyzw/lilToon

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class ThryHDRDrawer : MaterialPropertyDrawer
    {
        // Gamma HDR
        // [ThryHDR]
        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
#if UNITY_2019_1_OR_NEWER
            float xMax = position.xMax;
            position.width = string.IsNullOrEmpty(label)
                ? Mathf.Min(50.0f, position.width)
                : EditorGUIUtility.labelWidth + 50.0f;
            var value = prop.colorValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            value = EditorGUI.ColorField(position, new GUIContent(label), value, true, true, true);
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                prop.colorValue = value;
            }

            // Hex
            EditorGUI.BeginChangeCheck();
            float intensity = value.maxColorComponent > 1.0f ? value.maxColorComponent : 1.0f;
            var value2 = new Color(value.r / intensity, value.g / intensity, value.b / intensity, 1.0f);
            string hex = ColorUtility.ToHtmlStringRGB(value2);
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            position.x += position.width + 4.0f;
#if UNITY_2021_2_OR_NEWER
            position.width = Mathf.Min(50.0f, xMax - position.x);
            if(position.width > 10.0f)
            {
                EditorGUI.showMixedValue = prop.hasMixedValue;
                hex = "#" + EditorGUI.TextField(position, GUIContent.none, hex);
                EditorGUI.showMixedValue = false;
            }
#else
            position.width = 50.0f;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            hex = "#" + EditorGUI.TextField(position, GUIContent.none, hex);
            EditorGUI.showMixedValue = false;
#endif
            EditorGUI.indentLevel = indentLevel;
            if(EditorGUI.EndChangeCheck())
            {
                if(!ColorUtility.TryParseHtmlString(hex, out value2)) return;
                value.r = value2.r * intensity;
                value.g = value2.g * intensity;
                value.b = value2.b * intensity;
                prop.colorValue = value;
            }
#else
            var value = prop.colorValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            value = EditorGUI.ColorField(position, new GUIContent(label), value, true, true, true);
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                prop.colorValue = value;
            }
#endif
        }
    }
}