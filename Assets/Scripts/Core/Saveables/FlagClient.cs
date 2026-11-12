using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RageRooster.Core.World;
using SLS.SaveFileCore;
using SLS.GeneralUtilities.Events;
using UnityEditor.UIElements;


#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using SLS.EditorUtilities;
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

            if (!SaveData.FlagIDs.Contains(CollectionID)) return false;

            Flag.Collection collection = SaveData.Active.flags[CollectionID];

            if (!collection.ContainsKey(ID)) return false;

            foundFlag = collection[ID] as Flag.Generic<T>;

            return foundFlag != null;
        }
        public EventTicket RegisterCallback(Action<T> input) => !Find() ? null : foundFlag.Subscribe(input);
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(FlagClient<>))]
    public class PropDrawer_FlagClient : PropertyDrawer
    {
        VisualElement root;
        SerializedProperty collectionIDProp;
        DropdownField targetCollectionField;
        List<string> collectionIDs;
        SerializedProperty flagIDProp;
        DropdownField targetFlagField;
        List<string> flagIds;
        Type targetType;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            root = new();
            targetType = fieldInfo.FieldType.GenericTypeArguments[0];
            collectionIDProp = property.FindPropertyRelative("CollectionID");
            flagIDProp = property.FindPropertyRelative("ID");

            collectionIDs = SaveData.FlagIDs.ToList();

            targetCollectionField = new DropdownField("Collection", collectionIDs, collectionIDs.IndexOf("Global")).AddTo(root, c =>
            {
                c.RegisterValueChangedCallback(OnCollectionIDChange);
                c.BindProperty(collectionIDProp);
            });

            return root;
        }

        void OnCollectionIDChange(ChangeEvent<string> v)
        {
            if (string.IsNullOrEmpty(v.newValue)) return;
            flagIds = SaveData.Defs.flags[v.newValue].AllNamesOfType(targetType).ToList();

            if (targetFlagField != null && targetFlagField.parent == root) root.Remove(targetFlagField);
            targetFlagField = new DropdownField("Flag", flagIds, -1).AddTo(root, f =>
            {
                f.BindProperty(flagIDProp);
            });

        }
    }
#endif
}
