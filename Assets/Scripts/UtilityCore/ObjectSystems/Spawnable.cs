using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// A component that marks a GameObject as being poolable.
    /// </summary>
    public class Spawnable : MonoBehaviour
    {
        public enum States
        {
            Not = -2,
            Prefab = -1,
            Active = 0,
            Inactive = 1,
            Offscreen = 2,
        }

        public enum Types
        {
            Not = -1,
            Entity = 0,
            Projectile = 1,
            HordeEntity = 2,
            Particle = 3,
        }

        public States State
        { get; private set; } = States.Prefab;
        [field: SerializeField] public Types Type { get; private set; } = Types.Entity;

        public object currentClient
        { set; get; }

        //May stay here, may end up being tied to SpawnerClients or a different system. This is for the purpose of having a single update handler for all spawnables, so that they can be updated in a single loop instead of each having their own update loop.
        //public class UpdateHandler
        //{
        //    public float maxExistenceTime = -1;
        //    public float loadDistance = 30;
        //    public float offScreenDistance = 50;
        //    public float unloadDistance = 70;
        //    public bool stateChanged = false;
        //    public int handlerPriority = 0;
        //}

        /// <summary>
        /// This is only to be used within Spawner Clients (Object Pools, Entity Spawns, etc.)
        /// </summary>
        /// <returns></returns>
        public static Spawnable Instantiate(GameObject prefab, object client, Types intendedType = Types.Entity)
        {
            GameObject instance = Instantiate(prefab);
            if (!instance.TryGetComponent(out Spawnable result))
                result = instance.AddComponent<Spawnable>();
            result.currentClient = client;
            result.State = States.Inactive;
            return result;
        }


        public void Activate()
        {
            if (State is States.Active) return;
            State = States.Active;

            spawnTime = Time.time;
            gameObject.SetActive(true);
            onActivate?.Invoke();
        }
        public event Action onActivate;
        public void Deactivate()
        {
            if (State is States.Inactive) return;
            State = States.Inactive;
            gameObject.SetActive(false);
            onDeactivate?.Invoke();
        }
        public event Action onDeactivate;
        public void LeaveScreen()
        {
            if (State is States.Offscreen) return;
            State = States.Offscreen;
            gameObject.SetActive(false);
            onLeaveScreen?.Invoke();
        }
        public event Action onLeaveScreen;

        public float spawnTime { private set; get; }


        //Above is used functionality.

        /// <summary>
        /// Simple function for if this <see cref="GameObject"/> is a <see cref="Spawnable"/>. <br/>
        /// Not to be confused with <see cref="IsSpawnable(GameObject)"/>, which also checks if the object instance is available for reuse.
        /// </summary>
        public static bool IsASpawnable(GameObject subject) => subject.TryGetComponent(out Spawnable _);
        /// <summary>
        /// Simple function for if this <see cref="GameObject"/> is a <see cref="Spawnable"/>. <br/>
        /// Not to be confused with <see cref="IsSpawnable(GameObject, out Spawnable)"/>, which also checks if the object instance is available for reuse.
        /// </summary>
        public static bool IsASpawnable(GameObject subject, out Spawnable result) => subject.TryGetComponent(out result);

        /// <summary>
        /// Function that checks if this <see cref="GameObject"/> is a <see cref="Spawnable"/> and if it is available for reuse. <br/>
        /// Use <see cref="IsASpawnable(GameObject)"/> if you only want to check if the object is a <see cref="Spawnable"/>.
        /// </summary>
        public static bool IsSpawnable(GameObject subject) =>
            subject.TryGetComponent(out Spawnable spawnable) && spawnable.State is States.Inactive;
        /// <summary>
        /// Function that checks if this <see cref="GameObject"/> is a <see cref="Spawnable"/> and if it is available for reuse. <br/>
        /// Use <see cref="IsASpawnable(GameObject, out Spawnable)"/> if you only want to check if the object is a <see cref="Spawnable"/>.
        /// </summary>
        public static bool IsSpawnable(GameObject subject, out Spawnable spawnable) =>
            subject.TryGetComponent(out spawnable) && spawnable.State is States.Inactive;

        /// <summary>
        /// Returns if this object is a prefab with a <see cref="Spawnable"/> component attached. <br/>
        /// Cannot properly detect prefabs that add the component after instantiation.
        /// </summary>
        public static bool IsSpawnablePrefab(GameObject subject) =>
            subject.TryGetComponent(out Spawnable spawnable) && spawnable.State is States.Prefab;

        public static void DestroyOrDisable(GameObject subject)
        {
            if (!IsASpawnable(subject, out Spawnable spawnable)) Destroy(subject);
            else spawnable.State = States.Inactive;
        }
    }

    public static class Xtensions_Spawnables
    {
        public static void DestroyOrDisable(this GameObject subject) => Spawnable.DestroyOrDisable(subject);
        public static bool IsSpawnable(this GameObject subject) => Spawnable.IsSpawnable(subject);
        public static bool IsSpawnable(this GameObject subject, out Spawnable spawnable) => Spawnable.IsSpawnable(subject, out spawnable);
    }
}