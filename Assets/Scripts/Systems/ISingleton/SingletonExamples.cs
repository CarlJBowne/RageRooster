using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLS.ISingleton
{
    /// <summary>
    /// A basic Example of a Singleton MonoBehavior.
    /// </summary>
    /// <typeparam name="T">The intended type, ensure that this slot is filled with the actual name of the class you're creating.</typeparam>
    public abstract class SingletonMonoBasic<T> : MonoBehaviour, ISingleton<T> where T : MonoBehaviour, ISingleton<T>, new()
    {
        protected static T Instance;
        protected ISingleton<T> Interface => this;
        public static T Get() => ISingleton<T>.Get(ref Instance);
        public static bool TryGet(out T result) => ISingleton<T>.TryGet(Get, out result);

        public void Awake() => Interface.Initialize(ref Instance);
        private void OnDestroy() => Interface.DeInitialize(ref Instance);

        //This redirection isn't necessary if creating the coding from scratch.
        //Just use the first two methods to override Initialization and DeInitialization functionality.
        void ISingleton<T>.OnInitialize() => OnInitialize();
        void ISingleton<T>.OnDeInitialize() => OnDeInitialize();
        protected virtual void OnInitialize() { }
        protected virtual void OnDeInitialize() { }
    }
    /// <summary>
    /// A basic example of a Singleton ScriptableObject that is saved as an asset.
    /// </summary>
    /// <typeparam name="T">The intended type, ensure that this slot is filled with the actual name of the class you're creating.</typeparam>
    public abstract class SingletonAsset<T> : ScriptableObject, ISingleton<T> where T : ScriptableObject, ISingleton<T>, new()
    {
        protected static T Instance;
        protected ISingleton<T> Interface => this;
        public static T Get() => ISingleton<T>.Get(ref Instance);
        public static bool TryGet(out T result) => ISingleton<T>.TryGet(Get, out result);


        protected void OnEnable() => Interface.Initialize(ref Instance);
        public void Awake() => Interface.Initialize(ref Instance);
        protected virtual void OnDestroy() => Interface.DeInitialize(ref Instance);

        //This redirection isn't necessary if creating the coding from scratch.
        //Just use the first two methods to override Initialization and DeInitialization functionality.
        void ISingleton<T>.OnInitialize() => OnInitialize();
        void ISingleton<T>.OnDeInitialize() => OnDeInitialize();
        protected virtual void OnInitialize() { }
        protected virtual void OnDeInitialize() { }
    }
    /// <summary>
    /// A basic example of a Singleton ScriptableObject that is not saved as an asset.
    /// </summary>
    /// <typeparam name="T">The intended type, ensure that this slot is filled with the actual name of the class you're creating.</typeparam>
    public abstract class SingletonPlain<T> : ScriptableObject, ISingleton<T> where T : ScriptableObject, ISingleton<T>, new()
    {
        protected static T Instance;
        protected ISingleton<T> Interface => this;
        public static T Get() => ISingleton<T>.Get(ref Instance, ISingleton<T>.Create);
        public static bool TryGet(out T result) => ISingleton<T>.TryGet(Get, out result);

        //This redirection isn't necessary if creating the coding from scratch.
        //Just use the first two methods to override Initialization and DeInitialization functionality.
        void ISingleton<T>.OnInitialize() => OnInitialize();
        void ISingleton<T>.OnDeInitialize() => OnDeInitialize();
        protected virtual void OnInitialize() { }
        protected virtual void OnDeInitialize() { }
    }
}