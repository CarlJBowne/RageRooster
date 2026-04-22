using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Collections.ObjectModel;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

[System.Serializable]
public partial class Attack
{
    public int amount;

    public Vector3 velocity = Vector3.zero;
    public TagSet tags;

    public Attack(int damage, Vector3 velocity = default, TagSet tags = default)
    {
        this.amount = damage;
        this.velocity = velocity;
        this.tags = tags;
    }

    public float x => velocity.x;
    public float y => velocity.y;
    public float z => velocity.z;







#if UNITY_EDITOR
    [SerializeField] private string _displayName = "";
    [CustomPropertyDrawer(typeof(Attack))]
    public class PropertyDrawer : UnityEditor.PropertyDrawer
    {
        PropertyField displayNameField;
        PropertyField amountField;
        PropertyField velocityField;
        PropertyField tagsField;
        bool oldTagsVisible = true;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement();

            displayNameField = new(property.FindPropertyRelative(nameof(_displayName)), "Attack Name");
            amountField = new(property.FindPropertyRelative(nameof(amount)), string.Empty);
            velocityField = new(property.FindPropertyRelative(nameof(velocity)));
            tagsField = new(property.FindPropertyRelative(nameof(tags)));

            FoldoutPlus foldout = new();
            foldout.value = false;
            root.Add(foldout);

            foldout.headerSide.Add(amountField);
            foldout.contentContainer.Add(displayNameField);
            foldout.contentContainer.Add(velocityField);
            foldout.contentContainer.Add(tagsField);

            displayNameField.RegisterValueChangeCallback(DisplayNameChanged);
            void DisplayNameChanged(SerializedPropertyChangeEvent ev) => foldout.text = !string.IsNullOrEmpty(ev.changedProperty.stringValue) 
                    ? ev.changedProperty.stringValue 
                    : property.displayName;

            root.Bind(property.serializedObject);

            return root; 
        }
    }
#endif

}
