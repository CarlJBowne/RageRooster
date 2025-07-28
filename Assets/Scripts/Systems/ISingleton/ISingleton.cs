using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SLS.ISingleton
{
    /// <summary>
    /// A type of Behavior that can only exist once in a scene.
    /// <br/> Simple base scripts included:
    /// <br/> <see cref="SLS.ISingleton.SingletonMonoBasic{T}"/>
    /// <br/> <see cref="SLS.ISingleton.SingletonAsset{T}"/>
    /// <br/> <see cref="SLS.ISingleton.SingletonPlain{T}"/>
    /// </summary>
    /// <typeparam name="T">The Behavior's Type</typeparam>
    public interface ISingleton<T> : ISingleton where T : UnityEngine.Object, ISingleton<T>, new()
    {
        public delegate T Delegate();
        public delegate T DelegatePath(string path);

        /// <summary>
        /// Initialization method. Should be called in Awake or some equivilent method.
        /// </summary>
        /// <param name="instanceSlot">A reference instance slot. Must be created in script for anything to work.</param>
        /// <param name="dontDestroyOnLoad">Automatically tell the item to DontDestroyOnLoad. (Only works if inheriting from MonoBehavior.)</param>
        public sealed void Initialize(ref T instanceSlot, bool dontDestroyOnLoad = false)
        {
            if (instanceSlot == this) return;
            else if (instanceSlot != null && instanceSlot != this)
            {
#if UNITY_EDITOR
                Debug.Log($"Second {typeof(T)} found, Destroying...");
#endif

                DeInitialize(ref instanceSlot);
            }
            else
            {
                instanceSlot = this as T;
                if (dontDestroyOnLoad && typeof(T).ImplementsOrDerives(typeof(Component))) MonoBehaviour.DontDestroyOnLoad(instanceSlot);
                OnInitialize();
            }
        }
        protected void OnInitialize() { }

        /// <summary>
        /// DeInitialization method. Should be called in OnDestroy or some equivilent function.
        /// </summary>
        /// <param name="instanceSlot"></param>
        public sealed void DeInitialize(ref T instanceSlot)
        {
            if (instanceSlot == this) instanceSlot = null;
            OnDeInitialize();
            if (typeof(T).ImplementsOrDerives(typeof(UnityEngine.Object)))
                UnityEngine.Object.Destroy(this as UnityEngine.Object);
        }
        protected void OnDeInitialize() { }




        /// <summary>
        /// Attempts to acquire the Singleton. If already initialized will just return the active instance.
        /// </summary>
        /// <param name="instanceSlot">A reference instance slot. Must be created in script for anything to work.</param>
        /// <returns>The Singleton Instance.</returns>
        public static T Get(ref T instanceSlot)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif
            if (instanceSlot != null) return instanceSlot;

            if (TryGet(FindObject, out T attempt1))
                attempt1.Initialize(ref instanceSlot);
            else throw new Exception($"No Singleton of type {typeof(T)} could be found.");

            return instanceSlot;
        }
        /// <summary>
        /// Attempts to acquire the Singleton. If already initialized will just return the active instance.
        /// </summary>
        /// <param name="instanceSlot">A reference instance slot. Must be created in script for anything to work.</param>
        /// <param name="secondMethod">A second method provided by the Singleton Class to get/create an instance. (Only accepts one without a path.)</param>
        /// <returns>The Singleton Instance.</returns>
        public static T Get(ref T instanceSlot, Delegate secondMethod)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif
            if (instanceSlot != null) return instanceSlot;

            if (TryGet(FindObject, out T attempt1))
                attempt1.Initialize(ref instanceSlot);
            else if (secondMethod != null && TryGet(secondMethod as Delegate, out T attempt2))
                attempt2.Initialize(ref instanceSlot);
            else throw new Exception($"No Singleton of type {typeof(T)} could be found.");

            return instanceSlot;
        }
        /// <summary>
        /// Attempts to acquire the Singleton. If already initialized will just return the active instance.
        /// </summary>
        /// <param name="instanceSlot">A reference instance slot. Must be created in script for anything to work.</param>
        /// <param name="secondMethod">A second method provided by the Singleton Class to get/create an instance. (Only accepts one with a path.)</param>
        /// <param name="path">A provided path for searching amongst assets.</param>
        /// <returns>The Singleton Instance.</returns>
        public static T Get(ref T instanceSlot, DelegatePath secondMethod, string path)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif
            if (instanceSlot != null) return instanceSlot;

            if (TryGet(FindObject, out T attempt1))
                attempt1.Initialize(ref instanceSlot);
            else if (secondMethod != null && TryGet(secondMethod as DelegatePath, out T attempt2, path))
                attempt2.Initialize(ref instanceSlot);
            else throw new Exception($"No Singleton of type {typeof(T)} could be found.");

            return instanceSlot;
        }

        /// <summary>
        /// A TryGetter that outputs bool if successful and provides the Singleton as an Out.
        /// </summary>
        /// <param name="Getter">Just place the identifier for your Get function here.</param>
        /// <param name="singleton">The resulting singleton if a valid one is able to be found. </param>
        /// <returns>Whether an active Singleton was able to be found/made.</returns>
        public static bool TryGet(Delegate Getter, out T singleton)
        {
            singleton = Getter();
            return singleton != null;
        }
        /// <summary>
        /// A TryGetter that outputs bool if successful and provides the Singleton as an Out.
        /// </summary>
        /// <param name="Getter">Just place the identifier for your Get function here.</param>
        /// <param name="singleton">The resulting singleton if a valid one is able to be found. </param>
        /// <param name="path">The path provided. </param>
        /// <returns>Whether an active Singleton was able to be found/made.</returns>
        public static bool TryGet(DelegatePath Getter, out T singleton, string path)
        {
            singleton = Getter(path);
            return singleton != null;
        }

        /// <summary>
        /// Most Basic Initialization Method. Simply attempts to find it in loaded scene or among loaded Scriptable Objects. <br />
        /// Automatically runs first before any other Initial Methods. <br />
        /// </summary>
        /// <param name="result"></param>
        /// <returns>Success Value</returns>
        public static T FindObject() => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) as T;

        /// <summary>
        /// Creates an instance of this type of Singleton and returns it. <br />
        /// Will create a new object with the component on it if the type inherits from Component. <br />
        /// Will simply create a new Scriptable Object if inheriting from that.
        /// </summary>
        /// <returns>The created Singleton Object.</returns>
        public static T Create()
        {
            if (typeof(T).ImplementsOrDerives(typeof(Component)))
            {
                GameObject GO = new(typeof(T).ToString());
                return GO.AddComponent(typeof(T)) as T;
            }
            else if (typeof(T).ImplementsOrDerives(typeof(ScriptableObject)))
                return ScriptableObject.CreateInstance(typeof(T)) as T;
            else return new T();
            throw new Exception($"Unable to create instance of Singleton: {typeof(T)} for some unclear reason.");
        }

        /// <summary>
        /// Attempts to load a prefab/asset of the Singleton by its path in Resources. (Doesn't work unless in the project's Resources Folder.)
        /// </summary>
        /// <param name="path">The Path to search in.</param>
        /// <returns>The Singleton prefab/asset loaded.</returns>
        public static T FromResource(string path)
        {
            if (path == null)
            {

                if (typeof(T).ImplementsOrDerives(typeof(Component)))
                {
                    GameObject loadAttempt = Resources.Load<GameObject>(path);
                    return loadAttempt ? loadAttempt as T
                        : throw new Exception("Prefab not found, make sure the path is correct.");
                }
                else if (typeof(T).ImplementsOrDerives(typeof(ScriptableObject)))
                {
                    ScriptableObject loadAttempt = Resources.Load<ScriptableObject>(path);
                    return loadAttempt ? loadAttempt as T
                        : throw new Exception("Prefab not found, make sure the path is correct.");
                }
            }
            else throw new Exception("This Singleton type doesn't have a path attached. Add SingletonPath Interface.");
            throw new Exception($"Unable to create instance of Singleton: {typeof(T)} for some unclear reason.");
        }

        public static T FromPreloaded() => UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<T>()[0]);

        #if UNITY_ADDRESSABLES_EXIST
        /// <summary>
        /// Instantiates a Prefab using the Addressables System. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T FromAddressable(string path)
        {
            //NOTE, NEEDS MORE ERROR PROOFING. ADD LATER.

            if (path != null)
            {
                if (typeof(T).ImplementsOrDerives(typeof(Component)))
                {
                    GameObject loadAttempt = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();
                    return loadAttempt ? loadAttempt as T
                        : throw new Exception("Prefab not found, make sure the path is correct.");
                }
                else if (typeof(T).ImplementsOrDerives(typeof(ScriptableObject)))
                {
                    ScriptableObject loadAttempt = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<ScriptableObject>(path).WaitForCompletion();
                    return loadAttempt ? loadAttempt as T
                        : throw new Exception("Prefab not found, make sure the path is correct.");
                }
            }
            else throw new Exception("This Singleton type doesn't have a path attached. Add SingletonPath Interface.");
            throw new Exception($"Unable to create instance of Singleton: {typeof(T)} for some unclear reason.");
        }
        #endif



    }


}