using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Collections.ObjectModel;
using SLS.EditorUtilities.Editor;

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
    public TagSet tags = new();

    public Attack() { }
    public Attack(int damage, Vector3 velocity = default, TagSet tags = default)
    {
        this.amount = damage;
        this.velocity = velocity;
        this.tags = tags;
    }
    public Attack(Attack source)
    {
        this.amount = source.amount;
        this.velocity = source.velocity;
        this.tags = new(source.tags);
    }
    public Attack Clone() => new(this);

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
            displayNameField = new(property.FindPropertyRelative(nameof(_displayName)));
            amountField = new(property.FindPropertyRelative(nameof(amount)), string.Empty);
            velocityField = new(property.FindPropertyRelative(nameof(velocity)));
            tagsField = new(property.FindPropertyRelative(nameof(tags)));

            Foldout foldout = new();
            foldout.Bind(property.serializedObject);
            foldout.Clear();
            foldout.style.marginTop = 0;
            foldout.style.marginBottom = 0;
            foldout.value = false;

            foldout.contentContainer.Add(velocityField);
            foldout.contentContainer.Add(tagsField);

            foldout.DelayedBuild(() =>
            {
                Label label = foldout.Q<Label>(className: Foldout.textUssClassName);
                //VisualElement header = label.parent;

                //if (label.text.StartsWith("Element "))
                //    label.text = label.text.Replace("Element ", "Attack ");
                //label.ShrinkToTextWidth();
                //label.parent.style.flexDirection = FlexDirection.Row;

                //header.Add(displayNameField);
                //header.Add(amountField);

                amountField.style.maxWidth = Length.Percent(.15f);

                //header.Add(amountField);
            });

            //displayNameField.RegisterValueChangeCallback(DisplayNameChanged);
            //void DisplayNameChanged(SerializedPropertyChangeEvent ev) => foldout.text = !string.IsNullOrEmpty(ev.changedProperty.stringValue) 
            //        ? ev.changedProperty.stringValue 
            //        : property.displayName;

            return foldout; 
        }
    }
#endif

}
