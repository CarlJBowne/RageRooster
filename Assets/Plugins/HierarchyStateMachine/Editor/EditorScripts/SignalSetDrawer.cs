using UnityEditor;
using UnityEngine;

namespace SLS.StateMachineH.SerializedDictionary
{
    [CustomPropertyDrawer(typeof(SignalSet), true)]
    public class SignalSetDrawer : SerializedDictionaryDrawer
    {
        protected override void KeyValuePairDrawer(SerializedProperty item, Instance drawerInstance, Rect position, int id, bool isDupe)
        {
            SerializedProperty keyProperty = item.FindPropertyRelative("Key");
            SerializedProperty valueProperty = item.FindPropertyRelative("Value");

            if (keyProperty == null || valueProperty == null) return;

            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, true);
            float valueHeight = EditorGUI.GetPropertyHeight(valueProperty, true);
            float totalHeight = keyHeight + valueHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect keyRect = new Rect(position.x + 10, position.y, position.width - 30, keyHeight);
            Rect valueRect = new Rect(position.x - 15, position.y + keyHeight + EditorGUIUtility.standardVerticalSpacing, position.width + 17, valueHeight);

            var prevColor = GUI.color;
            if (isDupe) GUI.color = new Color(1.5f, 1, 1);

            try
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
            }
            finally { GUI.color = prevColor; }
            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                drawerInstance.property.serializedObject.ApplyModifiedProperties();
                drawerInstance.Update(updateList: true);
            }
        }

        protected override float KeyValuePairHeight(SerializedProperty serializedListProperty, Instance drawerInstance, int index)
        {
            SerializedProperty element = serializedListProperty.GetArrayElementAtIndex(index);
            SerializedProperty keyProperty = element.FindPropertyRelative("Key");
            SerializedProperty valueProperty = element.FindPropertyRelative("Value");
            return EditorGUI.GetPropertyHeight(keyProperty, true) +
                   EditorGUI.GetPropertyHeight(valueProperty, true) +
                   EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
