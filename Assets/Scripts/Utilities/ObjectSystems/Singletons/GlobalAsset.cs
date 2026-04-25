#if UNITY_EDITOR
#endif

using UnityEngine;

namespace Utilities.Singletons
{
    /// <summary>
    /// A base class for globally accessible "Singleton" type Scriptable Object Assets.
    /// Inherit from `GlobalAsset&lt;YourType&gt;` to gain automatic registration and availability.
    /// </summary>
    /// <remarks>
    /// <see cref="GlobalAsset{T}"/>s are automatically registered with the <see cref="GlobalAssetRegistry"/>, which is itself automatically registered with Preloaded Assets. 
    /// </remarks>
    /// <typeparam name="T">The concrete type inheriting this base class (the singleton type).</typeparam>
    public abstract class GlobalAsset<T> : GlobalAssetBase where T : class
    {
        /// <summary>
        /// Backing field for the singleton asset instance.
        /// </summary>
        private static T instance;

        /// <summary>
        /// Gets the registered asset singleton instance.
        /// </summary>
        public static T Get => Singleton.Get(ref instance);

        /// <summary>
        /// Whether an instance of this Singleton Type is Active.
        /// </summary>
        public static bool Active => instance != null;

        /// <summary>
        /// Attempts to get the currently registered asset singleton.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance) => Singleton.TryGet(Get, out instance);

        /// <summary>
        /// Unity OnEnable callback override - registers this ScriptableObject as the singleton instance.
        /// </summary>
        public sealed override void OnEnable()
        {
            Singleton.OperationMessage res = Singleton.Register(ref instance, this as T);
            if(res != Singleton.OperationMessage.Success) return;
            OnInit();
        }

        /// <summary>
        /// Unity OnDisable callback - unregisters this ScriptableObject if it is registered.
        /// </summary>
        private void OnDisable()
        {
            Singleton.OperationMessage res = Singleton.Unregister(ref instance, this as T);
            if (res != Singleton.OperationMessage.Success) return;
            OnDeInit();
        }

        public virtual void OnInit() { }
        public virtual void OnDeInit() { }
    }


    /// <summary>
    /// Lightweight ScriptableObject base to allow `GlobalAsset` to inherit.
    /// </summary>
    public abstract class GlobalAssetBase : ScriptableObject
    {
        /// <summary>
        /// Called by Unity when this ScriptableObject becomes enabled. Override in derived classes as needed.
        /// </summary>
        public virtual void OnEnable() { }
    }
}