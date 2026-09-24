using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        TextField displayNameField;
        IntegerField amountField;
        PropertyField velocityField;
        PropertyField tagsField;
        bool oldTagsVisible = true;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            displayNameField = new TextField("");
            displayNameField.BindProperty(property.FindPropertyRelative(nameof(_displayName)));
            amountField = new IntegerField("");
            amountField.BindProperty(property.FindPropertyRelative(nameof(amount)));
            velocityField = new(property.FindPropertyRelative(nameof(velocity)));
            tagsField = new(property.FindPropertyRelative(nameof(tags)));

            Foldout foldout = new();
            foldout.text = property.displayName;

            foldout.Bind(property.serializedObject);
            foldout.Clear();
            foldout.style.marginTop = 0;
            foldout.style.marginBottom = 0;
            foldout.value = false;

            foldout.contentContainer.Add(velocityField);
            foldout.contentContainer.Add(tagsField);

            if(foldout.SetupHeader(out VisualElement header, out Label label, out Toggle toggle))
            {
                header.Add(displayNameField);
                header.Add(amountField);

                if (label.text.StartsWith("Element "))
                    label.text = label.text.Replace("Element ", "Attack ");

                amountField.style.width = Length.Percent(20f);
                amountField.ClampToOneLine();
                amountField.SetTextElementAlign(TextAnchor.MiddleCenter);
                displayNameField.style.maxWidth = Length.Percent(60f);
                displayNameField.style.flexGrow = 1;
                displayNameField.SetTextElementAlign(TextAnchor.UpperRight);
                displayNameField.MakeTextFieldBackgroundInvisible();
                if (displayNameField.text == "")
                    displayNameField.SetValueWithoutNotify("...");

                return foldout;
            }
            else
            {
                foldout.Add(amountField);
                foldout.Add(displayNameField);
                foldout.Add(velocityField);
                foldout.Add(tagsField);
                return foldout;
            }
        }
    }
#endif

}
