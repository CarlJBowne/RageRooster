using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public struct Attack
{
    public int amount;

    public Vector3 velocity;
    public Tag[] tags;

    public Attack(int damage, params Tag[] tags)
    {
        this.amount = damage;
        velocity = Vector3.zero;
        this.tags = tags;
    }
    public Attack(int damage, Vector3 velocity, params Tag[] tags)
    {
        this.amount = damage;
        this.velocity = velocity;
        this.tags = tags;
    }



    public static Attack operator +(Attack a, Tag[] tags) => new(a.amount, a.velocity, a.tags.Concat(tags).ToArray());

    [System.Serializable]
    public struct Tag
    {
        public string name;

        public Tag(string name) => this.name = name;

        public override bool Equals(object obj) => obj is Tag tag && name == tag.name;
        public override int GetHashCode() => HashCode.Combine(name);

        public static bool operator ==(Tag a, Tag b) => a.name == b.name;
        public static bool operator !=(Tag a, Tag b) => a.name != b.name;

        public static implicit operator string(Tag a) => a.name;
        public static implicit operator Tag(string a) => new(a);

        public static Tag FromPlayer => new("FromPlayer");
        public static Tag FromEnemy => new("FromEnemy");
        public static Tag Wham => new("Wham");
        public static Tag FriendlyFire => new("FriendlyFire");
        public static Tag Punch => new("Punch");
        public static Tag Egg => new("Egg");
        public static Tag Thrown => new("Thrown");
        public static Tag ThrownEnemy => new("ThrownEnemy");
        public static Tag Pit => new("Pit");

    }

    public readonly bool HasTag(Tag tag)
    {
        for (int i = 0; i < tags.Length; i++)
            if (tags[i].name == tag.name) return true;
        return false;
    }

    /// <summary>
    /// Works the same as HasTag
    /// </summary>
    public static bool operator ==(Attack A, string S)
    {
        for (int i = 0; i < A.tags.Length; i++)
            if (A.tags[i].name == S) return true;
        return false;
    }
    /// <summary>
    /// Works the same as !HasTag
    /// </summary>
    public static bool operator !=(Attack A, string S)
    {
        for (int i = 0; i < A.tags.Length; i++)
            if (A.tags[i].name == S) return false;
        return true;
    }
    /// <summary>
    /// Works the same as HasTag
    /// </summary>
    public static bool operator ==(Attack A, Tag T)
    {
        for (int i = 0; i < A.tags.Length; i++)
            if (A.tags[i].name == T.name) return true;
        return false;
    }
    /// <summary>
    /// Works the same as !HasTag
    /// </summary>
    public static bool operator !=(Attack A, Tag T)
    {
        for (int i = 0; i < A.tags.Length; i++)
            if (A.tags[i].name == T.name) return false;
        return true;
    }
}
public static class _AttackTagOverrides
{
    public static bool Includes(this Attack.Tag[] List, Attack.Tag Tag) => List.Contains(Tag);
    public static bool IncludedBy(this Attack.Tag Tag, Attack.Tag[] List) => List.Contains(Tag);
    public static bool IncludesAny(this Attack.Tag[] destList, Attack.Tag[] checkList) => destList.Intersect(checkList).Count() > 0;
    public static bool IncludedAny(this Attack.Tag[] checkList, Attack.Tag[] destList) => destList.Intersect(checkList).Count() > 0;

}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Attack.Tag))]
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