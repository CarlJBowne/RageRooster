using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;

namespace RageRooster.Systems.ObjectPooling
{
    public class ObjectPools : ScriptableObject
    {
        public static ObjectPools Instance { private set; get; }
        public static bool initialized { private set; get; }
        public static Transform poolParent;

        static Dictionary<string, Pool> dictionary_string = new();
        static Dictionary<PoolableObject, Pool> dictionary_prefab = new();

        public List<Pool> serializedPools = new();

        private void Awake()
        {
            if (initialized && Instance == this) return;

            Instance = this;
            foreach (var item in serializedPools)
            {
                dictionary_string.Add(item.name, item);
                dictionary_prefab.Add(item.prefab, item);
            }
            Gameplay.onUpdate += Update;
            Gameplay.onDestroy += DeInitialize;

            initialized = true;
        }
        private void OnEnable() { if (!initialized) Awake(); }

        void Update()
        {
            for (int i = 0; i < serializedPools.Count; i++) serializedPools[i].Update();
        }

        void DeInitialize()
        {
            initialized = false;
            Instance = null;
            Gameplay.onUpdate -= Update;
            Gameplay.onDestroy -= DeInitialize;
            foreach (var pool in serializedPools) pool.DeInitialize();
        }


        public static void UnloadAllPools()
        {
            if (!initialized) return;
            foreach (var pool in Instance.serializedPools)
            {
                pool.UnloadAll();
            }
        }

        [System.Serializable, Inspectable]
        public class Pool
        {
            [field: SerializeField] public string name { private set; get; }
            [field: SerializeField] public PoolableObject prefab { private set; get; }
            [field: SerializeField] public int initialSize { private set; get; } = 5;
            [field: SerializeField] public bool canGrow { private set; get; } = true;
            [field: SerializeField] public float autoDisableTime { private set; get; } = -1;

            private readonly List<PoolableObject> poolList = new();
            private int currentActiveObjects = 0;
            private int currentPooledObjects = 0;
            private int currentSelection = 0;
            private bool initialized = false; 

            public Action<PoolableObject> onCreateInstance;
            public Action<PoolableObject> onPump;
            public Action onFailedPump;
            public Action onInstanceDisable;

            public void Initialize()
            {
                if (initialized) return;
                Enum().Begin(Gameplay.Instance);
                IEnumerator Enum()
                {
                    var op = UnityEngine.Object.InstantiateAsync(prefab, initialSize, poolParent);
                    while (!op.isDone) yield return null;
                    for (int i = 0; i < op.Result.Length; i++)
                    {
                        var poolable = op.Result[i];
                        poolable.Initialize(this);
                        poolable.onDeactivate += OnDeActivate;
                        poolList.Add(poolable);
                        currentPooledObjects++;
                        onCreateInstance?.Invoke(poolable);
                    }
                    initialized = true;
                }
            }

            internal void Update()
            {
                if (autoDisableTime > 0)
                    for (int i = 0; i < poolList.Count; i++)
                        if (poolList[i].Active && poolList[i].spawnTime + autoDisableTime <= Time.deltaTime)
                            poolList[i].Active = false;
            }

            public PoolableObject Pump()
            {
                if (!initialized)
                {
                    Initialize();
                    return null;
                }
                if (!FindNextInstance())
                {
                    onFailedPump?.Invoke();
                    return null;
                }

                PoolableObject instance = poolList[currentSelection];
                instance.Active = true;
                currentActiveObjects++;
                instance.onActivate?.Invoke();

                IncrementSelection();
                onPump?.Invoke(instance);
                return instance;
            }
            public bool Pump(out PoolableObject result)
            {
                result = Pump();
                return result != null;
            }

            private bool FindNextInstance()
            {
                if (!poolList[currentSelection].Active) return true;
                if (currentActiveObjects >= currentPooledObjects)
                {
                    if (!canGrow) return false;

                    NewInstance();
                    currentSelection = currentPooledObjects - 1;
                }
                int safetyCounter = 0;
                while (poolList[currentSelection].Active)
                {
                    IncrementSelection();
                    safetyCounter++;
                    if (safetyCounter > initialSize * 1000) return false;
                }
                return true;
            }

            private void IncrementSelection() => currentSelection = (currentSelection == currentPooledObjects - 1) ? 0 : currentSelection + 1;

            private void NewInstance()
            {
                var poolable = GameObject.Instantiate(prefab, poolParent);
                poolable.Initialize(this);
                poolable.onDeactivate += OnDeActivate;
                poolList.Add(poolable);
                currentPooledObjects++;
                onCreateInstance?.Invoke(poolable);
            }

            private void OnDeActivate(PoolableObject obj)
            {
                currentActiveObjects--;
                onInstanceDisable?.Invoke();
            }

            internal void DeInitialize()
            {
                initialized = false;
                currentActiveObjects = 0;
                currentPooledObjects = 0;
                currentSelection = 0;
            }

            public void UnloadAll()
            {
                foreach (var item in poolList) item.Active = false;
                currentActiveObjects = 0;
                currentSelection = 0;
            }
        }

        public static Pool GetPool(string poolName)
        {
            if (!initialized) return null;
            if (dictionary_string.TryGetValue(poolName, out Pool pool)) return pool;
            return null;
        }
        public static Pool GetPool(PoolableObject prefab)
        {
            if (!initialized) return null;
            if (dictionary_prefab.TryGetValue(prefab, out Pool pool)) return pool;
            return null;
























        }



        [System.Serializable, Inspectable]
        public class Client
        {
            [SerializeField, Inspectable] MonoBehaviour owner;
            [SerializeField, Inspectable] PoolableObject prefab;
            [SerializeField, Inspectable] Transform muzzle;
            private bool initialized;
            private Pool pool;
            public Action<PoolableObject> onPumpInstance;

            public void Initialize()
            {
                if (initialized) return;
                pool = GetPool(prefab);
                pool.Initialize();
                initialized = true;
            }

            public PoolableObject Pump(bool autoEnable = true)
            {
                if (!initialized) Initialize();
                var res = pool.Pump();
                if(muzzle != null) res.PlaceAtMuzzle(muzzle);
                onPumpInstance?.Invoke(res);
                if(autoEnable) res.Enable(this);
                return res;
            }
        }

        [MenuItem("File/CreateObjectPool")]
        private static void CREATE()
        {
            var instance = CreateInstance<ObjectPools>();
            AssetDatabase.CreateAsset(instance, "Assets/ObjectPools.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = instance;
        }
    }
}

