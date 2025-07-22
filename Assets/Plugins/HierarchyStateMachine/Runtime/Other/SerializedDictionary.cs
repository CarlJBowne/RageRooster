using System;
using System.Collections;
using Generics = System.Collections.Generic;
using UnityEngine;

namespace SLS.StateMachineH.SerializedDictionary
{
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : Generics.Dictionary<TKey, TValue>, ISerializationCallbackReceiver, ISerializedDictionaryNonGeneric
    {
        [SerializeField] internal Generics.List<KeyValuePair<TKey, TValue>> serializedList;
        [NonSerialized] private Generics.Dictionary<TKey, Generics.List<int>> occurences;

        public SerializedDictionary() : base()
        {
            serializedList = new();
            occurences = new();
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
                RemoveDuplicates();
#else
            serializedList.Clear();  
            foreach (var kvp in this)  
                serializedList.Add(new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));  
#endif
        }

        public void OnAfterDeserialize()
        {
            base.Clear();
            foreach (var kvp in serializedList)
            {
#if UNITY_EDITOR
                if (!(kvp.Key == null || (kvp.Key is UnityEngine.Object unityObject && unityObject == null)) && !ContainsKey(kvp.Key))
                    base.Add(kvp.Key, kvp.Value);
#else
                Add(kvp.Key, kvp.Value);  
#endif
            }

#if UNITY_EDITOR
            RecalculateOccurences();
#else
            serializedList.Clear();  
#endif
        }

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                bool anyEntryWasFound = false;
                for (int i = 0; i < serializedList.Count; i++)
                {
                    KeyValuePair<TKey, TValue> kvp = serializedList[i];
                    if (!Generics.EqualityComparer<TKey>.Default.Equals(key, kvp.Key)) continue;
                    anyEntryWasFound = true;
                    kvp = new(kvp.Key, value);
                    serializedList[i] = kvp;
                }

                if (!anyEntryWasFound)
                    serializedList.Add(new(key, value));
            }
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            serializedList.Add(new(key, value));
        }

        public void AddNew()
        {
            base.Add(default, default);
            serializedList.Add(new(default, default));
        }

        public new void Clear()
        {
            base.Clear();
            serializedList.Clear();
        }

        public new bool Remove(TKey key)
        {
            if (TryGetValue(key, out var value))
            {
                base.Remove(key);
                serializedList.RemoveAll(kvp => Generics.EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
                RecalculateOccurences();
                return true;
            }

            return false;
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            if (base.TryAdd(key, value))
            {
                serializedList.Add(new(key, value));
                RecalculateOccurences();
                return true;
            }

            return false;
        }

        public void RecalculateOccurences()
        {
            occurences.Clear();
            for (int i = 0; i < serializedList.Count; i++)
            {
                if (!occurences.ContainsKey(serializedList[i].Key)) occurences.Add(serializedList[i].Key, new(i));
                else occurences[serializedList[i].Key].Add(i);
            }
        }

        public void RemoveDuplicates()
        {
            Generics.HashSet<TKey> firstInstances = new();
            for (int i = 0; i < serializedList.Count; i++)
            {
                if (firstInstances.Contains(serializedList[i].Key))
                {
                    serializedList.RemoveAt(i);
                    i--;
                }
                else firstInstances.Add(serializedList[i].Key);
            }
        }

        public bool[] DuplicateValues
        {
            get
            {
                Generics.List<bool> result = new();
                for (int i = 0; i < serializedList.Count; i++) result.Add(false);
                foreach (var item in occurences)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                        result[item.Value[i]] = true;
                }
                return result.ToArray();
            }
        }

        public object this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value; }
        public void Add(object key, object value) => Add((TKey)key, (TValue)value);
        public bool Remove(object key) => Remove((TKey)key);
        public bool TryAdd(object key, object value) => TryAdd((TKey)key, (TValue)value);
    }
    public interface ISerializedDictionaryNonGeneric
    {
        public void OnBeforeSerialize();
        public void OnAfterDeserialize();
        public object this[object key] { get; set; }
        public void Add(object key, object value);
        public void Clear();
        public bool Remove(object key);
        public bool TryAdd(object key, object value);

        public void RecalculateOccurences();
        public bool[] DuplicateValues { get; }
        public void RemoveDuplicates();
    }
    [Serializable]
    public struct KeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public KeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
    [Serializable]
    public class LookupTable<TKey, TValue> : System.Collections.Generic.List<KeyValuePair<TKey, TValue>>, ILookupTableNonGeneric
    {
        public System.Collections.Generic.Dictionary<TKey, System.Collections.Generic.List<int>> occurences = new();

        // Add a method to expose the internal list as an array for SerializedProperty recognition  
        public KeyValuePair<TKey, TValue>[] AsArray => this.ToArray();

        public void RecalculateOccurences()
        {
            occurences.Clear();
            for (int i = 0; i < Count; i++)
            {
                if (!occurences.ContainsKey(this[i].Key)) occurences.Add(this[i].Key, new(i));
                else occurences[this[i].Key].Add(i);
            }
        }

        public void RemoveDuplicates()
        {
            Generics.HashSet<TKey> firstInstances = new();
            for (int i = 0; i < Count; i++)
            {
                if (firstInstances.Contains(this[i].Key))
                {
                    this.RemoveAt(i);
                    i--;
                }
                else firstInstances.Add(this[i].Key);
            }
        }

        public bool[] DuplicateValues
        {
            get
            {
                Generics.List<bool> result = new(Count);
                foreach (var item in occurences)
                {
                    for (int i = 1; i < item.Value.Count; i++)
                        result[item.Value[i]] = true;
                }
                return result.ToArray();
            }
        }
    }
    public interface ILookupTableNonGeneric
    {
        public bool[] DuplicateValues { get; }
        public void RecalculateOccurences();
        public void RemoveDuplicates();
    }
}
