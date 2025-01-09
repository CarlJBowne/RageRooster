using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Upgrade : ScriptableObject
{
    public bool value;

    public static implicit operator bool (Upgrade upgrade) => upgrade.value;
}

[CustomPropertyDrawer(typeof(Upgrade), true)]
public class ScriptableCollectionPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        

        EditorGUI.BeginProperty(position, label, property);

        float toggleWidth = 0;
        if (property.objectReferenceValue is Upgrade boolSO)
        {
            toggleWidth = 15;
            bool newValue = EditorGUI.Toggle(new Rect(position.x + EditorGUIUtility.labelWidth + 3, position.y, toggleWidth, position.height), boolSO.value);
            if (newValue != boolSO.value)
            {
                boolSO.value = newValue;
                EditorUtility.SetDirty(boolSO); // Mark the ScriptableObject dirty so the change is saved
            }
        }

        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth + 3, position.height);
        Rect objectFieldRect = new Rect(position.x + EditorGUIUtility.labelWidth + toggleWidth + 3, position.y, 
                                        position.width - toggleWidth - EditorGUIUtility.labelWidth - 4, position.height);

        // Draw the ScriptableObject selector
        EditorGUI.SelectableLabel(labelRect, property.displayName);
        property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, property.objectReferenceValue, typeof(Upgrade), false);

        // Draw the boolean toggle if a ScriptableObject is selected
        

        EditorGUI.EndProperty();
    }
}