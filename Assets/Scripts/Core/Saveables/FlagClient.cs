using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RageRooster.World;
using SLS.SaveData;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using SLS.EditorUtilities.Editor;
#endif

namespace RageRooster.Core.Save
{
    [System.Serializable]
    public class FlagClient<T> where T : struct
    {
        public string ID;
        public string CollectionID;
        private Flag.Generic<T> foundFlag;

        public bool TryGet(out T res)
        {
            res = default;
            if (Find())
            {
                res = foundFlag.Value;
                return true;
            }
            return false;
        }
        public bool TrySet(T value)
        {
            if (!Find()) return false;
            foundFlag.Value = value;
            return true;
        }
        public bool Find()
        {
            if (foundFlag != null) return true;





            return (foundFlag != null);
        }
        public void RegisterCallback(Action<T> input)
        {
            if (!Find()) return;
            foundFlag.OnValueChanged += input;
        }
        public void UnregisterCallback(Action<T> input)
        {
            if (!Find()) return;
            foundFlag.OnValueChanged -= input;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(FlagClient<>))]
    public class PropDrawer_FlagClient : PropertyDrawer
    {
        VisualElement root;
        SerializedProperty collectionIDProp;
        DynamicEnumField targetCollectionField;
        List<string> collectionIDs;
        SerializedProperty flagIDProp;
        DynamicEnumField targetFlagField;
        List<string> flagIds;
        Type targetType;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            root = new();
            targetType = fieldInfo.FieldType.GenericTypeArguments[0];
            collectionIDProp = property.FindPropertyRelative("CollectionID");
            flagIDProp = property.FindPropertyRelative("ID");

            collectionIDs = new(IDestination.AllAreas);
            collectionIDs.Insert(0, "Global");
            collectionIDs.Insert(1, "Story Flags");
            int collInitID = collectionIDs.Contains(collectionIDProp.stringValue)
                ? collectionIDs.IndexOf(collectionIDProp.stringValue)
                : 0;

            targetCollectionField = new DynamicEnumField(collectionIDs, collInitID, OnCollectionIDChange);
            

            OnCollectionIDChange(collInitID);

            return root;
        }

        void OnCollectionIDChange(int v)
        {
            flagIds = new();

            Polymorph.Dictionary<Flag> target;
            if (v == 1) target = SaveData.Default.progress.storyFlags as Polymorph.Dictionary<Flag>;
            else target = SaveData.Default.flags[collectionIDs[v]];

        }
    }
#endif
}
