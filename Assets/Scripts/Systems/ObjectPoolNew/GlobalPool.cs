using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;

namespace RageRooster.Systems.ObjectPooling
{
    /// <summary>
    /// A global pool for pooled objects shared between multiple entities. Use a <see cref="GlobalPool.Client"/> to interface with this.
    /// </summary>
    public class GlobalPool : ScriptableObject
    {
        public static GlobalPool Instance { private set; get; }
        public static bool initialized { private set; get; }
        public static Transform poolParent;

        static Dictionary<string, ObjectPool> dictionary_string = new();
        static Dictionary<PoolableObject, ObjectPool> dictionary_prefab = new();

        public List<ObjectPool> serializedPools = new();

        public ObjectPool<AttackProjectile> basicEnemyBullet;
        public static ObjectPool<AttackProjectile> BasicEnemyBullet;

        private void OnEnable()
        {
            if (Instance == this) return;
            Instance = this;
        }

        public void Initialize()
        {
            if (initialized && Instance == null) return;

            InitPoolGlobally(basicEnemyBullet);
            BasicEnemyBullet = basicEnemyBullet;

            foreach (var item in serializedPools) InitPoolGlobally(item);

            Gameplay.onUpdate += Update;
            Gameplay.onDestroy += DeInitialize;

            initialized = true;
        }

        void Update()
        {
            basicEnemyBullet.Update(Time.deltaTime);
            for (int i = 0; i < serializedPools.Count; i++) serializedPools[i].Update(Time.deltaTime);
        }

        void DeInitialize()
        {
            initialized = false;
            Instance = null;
            Gameplay.onUpdate -= Update;
            Gameplay.onDestroy -= DeInitialize;
            foreach (var pool in serializedPools) pool.Cleanup();
        }


        private void InitPoolGlobally(ObjectPool item)
        {
            if (item.prefab == null) return;
            item.Initialize();
            if(!string.IsNullOrEmpty(item.name)) dictionary_string.Add(item.name, item);
            dictionary_prefab.Add(item.prefab, item);
        }

        public static void UnloadAllPools()
        {
            if (!initialized) return;
            foreach (var pool in Instance.serializedPools) pool.DisableAll();
        }

        public static ObjectPool GetPool(string poolName)
        {
            if (!initialized) return null;
            if (dictionary_string.TryGetValue(poolName, out ObjectPool pool)) return pool;
            return null;
        }
        public static ObjectPool GetPool(PoolableObject prefab)
        {
            if (!initialized) return null;
            if (dictionary_prefab.TryGetValue(prefab, out ObjectPool pool)) return pool;
            return null;

        }


        /// <summary>
        /// A <see cref="Client"/> of the <see cref="GlobalPool"/> system.
        /// </summary>
        [System.Serializable, Inspectable]
        public class Client
        {
            [SerializeField, Inspectable] MonoBehaviour owner;
            [SerializeField, Inspectable] PoolableObject prefab;
            [SerializeField, Inspectable] Transform muzzle;
            private bool initialized;
            private ObjectPool pool;
            public Action<PoolableObject> onPumpInstance;

            public Client(MonoBehaviour Owner = null, PoolableObject Prefab = null, ObjectPool Pool = null)
            {
                if (Pool != null)
                {
                    pool = Pool;
                    prefab = Pool.prefab;
                }
                else if (Prefab != null) prefab = Prefab;
                if (Owner != null) owner = Owner;
            }

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
                if (muzzle != null) res.PlaceAtMuzzle(muzzle);
                onPumpInstance?.Invoke(res);
                res.currentClient = owner;
                if (autoEnable) res.Active = true;
                return res;
            }
        }

        /*
        [MenuItem("File/CreateObjectPool")]
        private static void CREATE()
        {
            var instance = CreateInstance<GlobalPool>();
            AssetDatabase.CreateAsset(instance, "Assets/ObjectPools.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = instance;
        }*/
    }
}

