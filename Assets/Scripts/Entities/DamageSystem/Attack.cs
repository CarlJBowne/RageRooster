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
public partial struct Attack
{
    public int amount;

    public Vector3 velocity;
    public TagSet tags;

    public Attack(int damage, TagSet tags)
    {
        this.amount = damage;
        velocity = Vector3.zero;
        this.oldTags = null;
        this.tags = default;

    }
    public Attack(int damage, Vector3 velocity, TagSet tags)
    {
        this.amount = damage;
        this.velocity = velocity;
        this.oldTags = null;
        this.tags = default;
    }

    public readonly float x => velocity.x;
    public readonly float y => velocity.y;
    public readonly float z => velocity.z;






    #region Uses Old Tag System

    [FormerlySerializedAs("tags")] public Tag_OLD[] oldTags;

    public Attack(int damage, params Tag_OLD[] tags)
    {
        this.amount = damage;
        velocity = Vector3.zero;
        this.oldTags = tags;
        this.tags = default;
    }
    public Attack(int damage, Vector3 velocity, params Tag_OLD[] tags)
    {
        this.amount = damage;
        this.velocity = velocity;
        this.oldTags = tags;
        this.tags = default;
    }


    public static Attack operator +(Attack a, Tag_OLD[] tags) => new(a.amount, a.velocity, a.oldTags.Concat(tags).ToArray());

    [System.Serializable, System.Obsolete]
    public struct Tag_OLD
    {
        public string name;

        public Tag_OLD(string name) => this.name = name;

        public override bool Equals(object obj) => obj is Tag_OLD tag && name == tag.name;
        public override int GetHashCode() => HashCode.Combine(name);

        public static bool operator ==(Tag_OLD a, Tag_OLD b) => a.name == b.name;
        public static bool operator !=(Tag_OLD a, Tag_OLD b) => a.name != b.name;

        public static implicit operator string(Tag_OLD a) => a.name;
        public static implicit operator Tag_OLD(string a) => new(a);

        public static Tag_OLD FromPlayer => new("FromPlayer");
        public static Tag_OLD FromEnemy => new("FromEnemy");
        public static Tag_OLD Wham => new("Wham");
        public static Tag_OLD FriendlyFire => new("FriendlyFire");
        public static Tag_OLD Punch => new("Punch");
        public static Tag_OLD Egg => new("Egg");
        public static Tag_OLD Thrown => new("Thrown");
        public static Tag_OLD ThrownEnemy => new("ThrownEnemy");
        public static Tag_OLD Pit => new("Pit");

    }

    public readonly bool HasTag(Tag_OLD tag)
    {
        for (int i = 0; i < oldTags.Length; i++)
            if (oldTags[i].name == tag.name) return true;
        return false;
    }

    public override bool Equals(object obj) => obj is Attack attack && amount == attack.amount && velocity.Equals(attack.velocity) && EqualityComparer<Tag_OLD[]>.Default.Equals(oldTags, attack.oldTags);
    public override int GetHashCode() => HashCode.Combine(amount, velocity, oldTags);

    /// <summary>
    /// Works the same as HasTag
    /// </summary>
    public static bool operator ==(Attack A, string S)
    {
        for (int i = 0; i < A.oldTags.Length; i++)
            if (A.oldTags[i].name == S) return true;
        return false;
    }
    /// <summary>
    /// Works the same as !HasTag
    /// </summary>
    public static bool operator !=(Attack A, string S)
    {
        for (int i = 0; i < A.oldTags.Length; i++)
            if (A.oldTags[i].name == S) return false;
        return true;
    }
    /// <summary>
    /// Works the same as HasTag
    /// </summary>
    public static bool operator ==(Attack A, Tag_OLD T)
    {
        for (int i = 0; i < A.oldTags.Length; i++)
            if (A.oldTags[i].name == T.name) return true;
        return false;
    }
    /// <summary>
    /// Works the same as !HasTag
    /// </summary>
    public static bool operator !=(Attack A, Tag_OLD T)
    {
        for (int i = 0; i < A.oldTags.Length; i++)
            if (A.oldTags[i].name == T.name) return false;
        return true;
    }

    #endregion

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Attack))]
    public class PropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement();

            SerializedProperty amountProp = property.FindPropertyRelative(nameof(amount));
            SerializedProperty velocityProp = property.FindPropertyRelative(nameof(velocity));
            SerializedProperty tagsProp = property.FindPropertyRelative(nameof(oldTags));
            SerializedProperty tagsProp2 = property.FindPropertyRelative(nameof(tags));

            FoldoutPlus foldout = new();
            root.Add(foldout);
            foldout.text = property.displayName;
            foldout.value = false; // collapsed by default

            PropertyField amountField = new(amountProp, string.Empty);
            foldout.headerSide.Add(amountField);

            PropertyField velocityField = new(velocityProp);
            foldout.contentContainer.Add(velocityField);
            PropertyField tagsField = new(tagsProp);
            foldout.contentContainer.Add(tagsField);

            PropertyField tags2Field = new(tagsProp2);
            foldout.contentContainer.Add(tags2Field);

            root.Bind(property.serializedObject);

            return root;
        }
    }
#endif

}
public static class _AttackTagOverrides
{
    public static bool Includes(this Attack.Tag_OLD[] List, Attack.Tag_OLD Tag) => List.Contains(Tag);
    public static bool IncludedBy(this Attack.Tag_OLD Tag, Attack.Tag_OLD[] List) => List.Contains(Tag);
    public static bool IncludesAny(this Attack.Tag_OLD[] destList, Attack.Tag_OLD[] checkList) => destList.Intersect(checkList).Count() > 0;
    public static bool IncludedAny(this Attack.Tag_OLD[] checkList, Attack.Tag_OLD[] destList) => destList.Intersect(checkList).Count() > 0;

}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Attack.Tag_OLD))]
public class _AttackTagPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position.xMin -= 10;

        // Retrieve the serialized fields
        SerializedProperty nameProperty = property.FindPropertyRelative("name");
        EditorGUI.PropertyField(position, nameProperty, GUIContent.none);

        EditorGUI.EndProperty();
    }
}

#endif
