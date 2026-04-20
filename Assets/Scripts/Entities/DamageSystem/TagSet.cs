using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

public partial struct Attack
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
    }

    [Serializable]
    public class TagSet : BitwiseEnum
    {

        public bool this[Tags i]
        {
            get => this[(int)i];
            set => this[(int)i] = value;
        }

        public static bool operator ==(TagSet L, Tags R) => L[R];
        public static bool operator !=(TagSet L, Tags R) => !L[R];


        public static void TransferFromOldTags(Attack.Tag_OLD[] oldTags, TagSet newTags)
        {
            bool newTagAdded = false;
            for (int i = 0; i < oldTags.Length; i++)
            {
                string iName = oldTags[i];
                if (!TagNameToID.TryGetValue(iName, out int ID) && !HandleSpecificNames(iName))
                {
                    List<string> serializedList = GlobalPrefabs.Get().attackTagNames;
                    ID = serializedList.Count;
                    serializedList.Add(iName);
                    newTagAdded = true;
                    newTags[ID] = true;
                }
                else newTags[ID] = true;

                bool HandleSpecificNames(string input)
                {
                    if (input is "FromPlayer")
                    {
                        newTags[Tags.Player] = true;
                        return true;
                    }
                    if (input is "FromEnemy")
                    {
                        newTags[Tags.Enemy] = true;
                        return true;
                    }
                    if (input is "ThrownRock" or "ThrownEnemy")
                    {
                        newTags[Tags.Thrown] = true;
                        return true;
                    }
                    if (input is "FromBoss" or "Boss1Slam" or "Boss1Charge" or "FromPecky" or "FromStumpy" or "FromSlasher")
                    {
                        newTags[Tags.Boss] = true;
                        newTags[Tags.Enemy] = true;
                        return true;
                    }
                    if (input is "PlayerPoint=2" or "PlayerPoints=2")
                    {
                        newTags[Tags.OnPlayerDouble] = true;
                        return true;
                    }
                    if (input is "LAVA")
                    {
                        newTags[Tags.Lava] = true;
                        return true;
                    }

                    return false;
                }
                
            }
            if (newTagAdded)
            {
                EditorUtility.SetDirty(GlobalPrefabs.Get());
                InitGlobalData(GlobalPrefabs.Get().attackTagNames);
            }
        }

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
    }
#endif

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

    public void TransferTags()
    {
        if (tags == null) tags = new();
        TagSet.TransferFromOldTags(oldTags, tags);
    }


    public static void InitGlobalData(List<string> namesInput)
    {
        TagNames = namesInput;

        Dictionary<string, int> dictionary = new();
        for (int i = 0; i < TagNames.Count; i++) dictionary[TagNames[i]] = i;
        TagNameToID = new(dictionary);
    }
    public static ReadOnlyDictionary<string, int> TagNameToID { get; private set; }
    public static IReadOnlyList<string> TagNames { get; private set; }

}
