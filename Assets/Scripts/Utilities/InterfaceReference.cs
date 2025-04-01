using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;

[System.Serializable]
public class IRef<T> : ISerializationCallbackReceiver where T : class
{
    public UnityEngine.Object obj;
    public T I => obj as T;
    void OnValidate()
    {
        if (obj is not T)
        {
            if (obj is GameObject go)
            {
                obj = null;
                foreach (Component c in go.GetComponents<Component>())
                {
                    if (c is T)
                    {
                        obj = c;
                        break;
                    }

                }
            }
        }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() => this.OnValidate();
    void ISerializationCallbackReceiver.OnAfterDeserialize() { }

    //public override bool Equals(object obj) => obj is IRef<T> @ref && EqualityComparer<UnityEngine.Object>.Default.Equals(this.obj, @ref.obj);
    //public override int GetHashCode() => HashCode.Combine(obj);

    public static implicit operator bool(IRef<T> ir) => ir.obj != null;
    public static bool operator ==(IRef<T> L, T R) => L.obj as T == R;
    public static bool operator !=(IRef<T> L, T R) => L.obj as T != R;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(IRef<>), useForChildren: true)]
public class IRefPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);


        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        SerializedProperty Target = property.FindPropertyRelative("obj");

        EditorGUI.ObjectField(position, Target, GUIContent.none);

        if(Target.objectReferenceValue == null)
        {
            Rect subLabelPosition = position;
            subLabelPosition.xMax -= 20;
            GUIStyle subLabelStyle = GUI.skin.label;
            subLabelStyle.alignment = TextAnchor.MiddleRight;
            EditorGUI.DropShadowLabel(subLabelPosition, $"({property.serializedObject.targetObject.GetType().GetField(property.name).FieldType.GetGenericArguments()[0]})", subLabelStyle);
        }

        EditorGUI.EndProperty();
    }
}
#endif