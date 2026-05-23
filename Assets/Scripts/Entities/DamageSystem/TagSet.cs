using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

public partial class Attack
{
    public enum Tags
    {
        Player,
        Enemy,
        Wham,
        FriendlyFire,
        Thrown,
        Projectile,
        Environment,
        Pit,
        Boss,
        Egg,
        RagingCharge,
        Explosion,
        Boulder,
        OnPlayerDouble,
        OnPlayerTriple,
        OnPlayerQuadruple,
        OnPlayerNone, 
        Punch,
        Uppercut,
        Kick,
        Headbutt,
        Charge,
        GroundSlam,
        Fire,
        Lazer,
        Peck,
        Lava,
        WeakSpot
    }

    [Serializable]
    public class TagSet : Bitmask
    {
        #region Operators

        /// <summary>
        /// Returns a TagSet where any flags from L OR R are true. Equivalent to | or + operators.
        /// </summary>
        public static TagSet operator |(TagSet L, TagSet R) => Bitmask.OR(L as Bitmask, R as Bitmask) as TagSet;
        /// <summary>
        /// Returns a TagSet where any flags from L OR R are true. Equivalent to | or + operators.
        /// </summary>
        public static TagSet operator +(TagSet L, TagSet R) => Bitmask.OR(L as Bitmask, R as Bitmask) as TagSet;

        /// <summary>
        /// Returns a TagSet where any flags on L AND R are true. 
        /// </summary>
        public static TagSet operator &(TagSet L, TagSet R) => Bitmask.AND(L as Bitmask, R as Bitmask) as TagSet;
        /// <summary>
        /// Returns a TagSet where any flags on L AND R are true. 
        /// </summary>
        public static TagSet operator *(TagSet L, TagSet R) => Bitmask.AND(L as Bitmask, R as Bitmask) as TagSet;

        /// <summary>
        /// Returns a TagSet where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
        /// </summary>
        public static TagSet operator ^(TagSet L, TagSet R) => Bitmask.XOR(L as Bitmask, R as Bitmask) as TagSet;
        /// <summary>
        /// Returns a TagSet where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
        /// </summary>
        public static TagSet operator /(TagSet L, TagSet R) => Bitmask.XOR(L as Bitmask, R as Bitmask) as TagSet;



        /// <summary>
        /// Returns a TagSet where the right index is added to the left TagSet.
        /// </summary>
        public static TagSet operator +(TagSet L, int R) => Bitmask.ADD(L as Bitmask, R) as TagSet;
        /// <summary>
        /// Returns a TagSet where the right indeces are added to the left TagSet.
        /// </summary>
        public static TagSet operator +(TagSet L, int[] R) => Bitmask.ADD(L as Bitmask, R) as TagSet;
        /// <summary>
        /// Returns a TagSet where the right indeces are added to the left TagSet.
        /// </summary>
        public static TagSet operator +(TagSet L, List<int> R) => Bitmask.ADD(L as Bitmask, R) as TagSet;

        /// <summary>
        /// Returns a TagSet where the right index is removed to the left TagSet.
        /// </summary>
        public static TagSet operator -(TagSet L, int R) => Bitmask.REMOVE(L as Bitmask, R) as TagSet;
        /// <summary>
        /// Returns a TagSet where the right indeces are removed to the left TagSet.
        /// </summary>
        public static TagSet operator -(TagSet L, int[] R) => Bitmask.REMOVE(L as Bitmask, R) as TagSet;
        /// <summary>
        /// Returns a TagSet where the right indeces are removed to the left TagSet.
        /// </summary>
        public static TagSet operator -(TagSet L, List<int> R) => Bitmask.REMOVE(L as Bitmask, R) as TagSet;

        /// <summary>
        /// Returns a TagSet where flags true on R are subtracted from L.
        /// </summary>
        public static TagSet operator -(TagSet L, TagSet R) => Bitmask.XAND(L as Bitmask, R as Bitmask) as TagSet;
        /// <summary>
        /// Returns a TagSet that is inverted from the input. 
        /// </summary>
        public static TagSet operator ~(TagSet L) => INVERT(L as Bitmask) as TagSet;


        /// <summary>
        /// Equality operator. True if both are the same reference or both non-null with equal integer masks.
        /// </summary>
        public static bool operator ==(TagSet L, TagSet R)
        {
            if (ReferenceEquals(L, R)) return true;
            if (L is null || R is null) return false;
            return L.intValue == R.intValue;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(TagSet L, TagSet R) => !(L == R);

        /// <summary>
        /// Inclusion operator. True if both are the same reference or both non-null with equal integer masks.
        /// </summary>
        public static bool operator ==(TagSet L, int R)
        {
            if (L == null) return false;
            if (R is < 0 or > 31) throw new ArgumentOutOfRangeException("Index outside of 0..31 Range");
            return L[R] == true;
        }

        /// <summary>
        /// Uninclusion operator.
        /// </summary>
        public static bool operator !=(TagSet L, int R) => !(L == R);

        #endregion



        public bool this[Tags i]
        {
            get => this[(int)i];
            set => this[(int)i] = value;
        }

        public static bool operator ==(TagSet L, Tags R) => L[R];
        public static bool operator !=(TagSet L, Tags R) => !L[R];




        public override bool Equals(object obj) => obj is TagSet set && base.Equals(obj) && intValue == set.intValue;
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), intValue);

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(TagSet))]
        public class AttackTagBitEnumDrawer : UnityEditor.PropertyDrawer
        {
            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                var container = new VisualElement();

                SerializedProperty intProp = property.FindPropertyRelative(nameof(intValue));
                if (intProp == null)
                {
                    // Fallback: show a label if property layout is unexpected
                    container.Add(new Label("Error: intValue not found on AttackTagBitEnum"));
                    return container;
                }

                var imgui = new IMGUIContainer(() =>
                {
                    // Retrieve dynamic names from GlobalPrefabs; ensure null-safety
                    string[] options;
                    try
                    {
                        options = TagNames != null && TagNames.Count > 0 ? TagNames.ToArray() : (new string[] { "None" });
                    }
                    catch
                    {
                        options = new string[] { "None" };
                    }

                    EditorGUI.BeginChangeCheck();

                    // Render mask field using GUILayout so it integrates into the IMGUIContainer
                    int currentMask = intProp.intValue;
                    int newMask = EditorGUILayout.MaskField(property.displayName, currentMask, options);

                    if (EditorGUI.EndChangeCheck())
                    {
                        intProp.intValue = newMask;
                        // Apply changes immediately to the serialized object
                        property.serializedObject.ApplyModifiedProperties();
                    }
                });

                container.Add(imgui);
                return container;
            }
        }
#endif
    }

    public bool this[int i]
    {
        get => tags[i];
        set => tags[i] = value;
    }
    public bool this[Tags i]
    {
        get => tags[(int)i];
        set => tags[(int)i] = value;
    }

    public static bool operator ==(Attack L, Tags R) => L.tags[R];
    public static bool operator !=(Attack L, Tags R) => !L.tags[R];

    public static implicit operator TagSet(Attack O) => O.tags;
    

    public static void InitGlobalData(List<string> namesInput)
    {
        TagNames = namesInput;

        Dictionary<string, int> dictionary = new();
        for (int i = 0; i < TagNames.Count; i++) dictionary[TagNames[i]] = i;
        TagNameToID = new(dictionary);
    }

    public override bool Equals(object obj) => obj is Attack attack && amount == attack.amount && velocity.Equals(attack.velocity) && EqualityComparer<TagSet>.Default.Equals(tags, attack.tags) && x == attack.x && y == attack.y && z == attack.z && _displayName == attack._displayName;
    public override int GetHashCode() => HashCode.Combine(amount, velocity, tags, x, y, z, _displayName);

    public static ReadOnlyDictionary<string, int> TagNameToID { get; private set; }
    public static IReadOnlyList<string> TagNames { get; private set; }

}
