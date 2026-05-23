using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SLS.ISingleton
{
    /// <summary>
    /// Global non-generic base interface for ISingleton types.
    /// </summary>
    public interface ISingleton
    {

        public static Dictionary<Type, ISingleton> allActives;

        public static T Get<T>() where T : ISingleton => (T)allActives[typeof(T)];

        public void Awake() { }
    }

    public static class _TypeHelpers
    {
        public static Type[] GetAllChildTypes(this Type T, bool noAbstracts = false)
        => Assembly.GetAssembly(T).GetTypes().Where(i =>
        i.ImplementsOrDerives(T) &&
        (!noAbstracts || !i.IsAbstract)
        ).ToArray();

        public static bool ImplementsOrDerives(this Type @this, Type from)
        {
            if (from is null)
                return false;

            if (!from.IsGenericType || !from.IsGenericTypeDefinition)
                return from.IsAssignableFrom(@this);

            if (from.IsInterface)
                foreach (Type @interface in @this.GetInterfaces())
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == from)
                        return true;

            if (@this.IsGenericType && @this.GetGenericTypeDefinition() == from)
                return true;

            return @this.BaseType?.ImplementsOrDerives(from) ?? false;
        }

        public static bool IsPrefab(this Transform This) => !This.gameObject.scene.IsValid();
        public static bool IsPrefab(this GameObject This) => !This.scene.IsValid();
    }

#if UNITY_EDITOR
    class SingletonScriptableAPP : UnityEditor.AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            UnityEngine.Object[] objs = UnityEditor.PlayerSettings.GetPreloadedAssets();
            foreach (UnityEngine.Object item in objs)
                if (item is ISingleton and ScriptableObject)
                    (item as ISingleton).Awake();
        }
    }
#endif

}