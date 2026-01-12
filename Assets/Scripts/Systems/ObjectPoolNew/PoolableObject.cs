using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.Systems.ObjectPool
{
    /// <summary>
    /// A component that marks a GameObject as being poolable.
    /// </summary>
    public class PoolableObject : MonoBehaviour
    {

        public ObjectPools.Pool pool { private set; get; }
        public bool isPrefab { private set; get; } = true;
        public bool Active
        {
            set
            {
                if (value == _active) return;
                _active = value;
                gameObject.SetActive(value);
                if (_active)
                {
                    spawnTime = Time.time;
                    onActivate?.Invoke();
                }
                else
                {
                    onDeactivate?.Invoke(this);
                }
            }
            get => _active;
        }
        private bool _active;
        public float spawnTime { private set; get; }


        public Action onActivate;
        /// <summary>
        /// If nothing calls this action when this object instance is done the object will never be available for reuse. (Extremely unintuitive, fix in V3.)
        /// </summary>
        public Action<PoolableObject> onDeactivate;


        private void OnEnable() => Active = true;
        private void OnDisable() => Active = false;


        public void SetPosition(Vector3 position) => transform.position = position;
        public void SetRotation(Vector3 rotation) => transform.eulerAngles = rotation;
        public void PlaceAtMuzzle(Transform muzzle)
        {
            transform.position = muzzle.position;
            transform.rotation = muzzle.rotation;
        }


        public void Initialize(ObjectPools.Pool pool)
        {
            this.pool = pool;
            isPrefab = false;
            Active = false;
        }

        public static PoolableObject Is(GameObject subject)
        {
            PoolableObject poolable = subject.GetComponent<PoolableObject>();
            if (!poolable) return null;
            if (poolable.pool == null) return null;
            return poolable;
        }
        public static bool Is(GameObject subject, out PoolableObject result)
        {
            result = subject.GetComponent<PoolableObject>();
            return result && result.pool != null;
        }
        public static bool DisableOrDestroy(GameObject subject)
        {
            if (subject.TryGetComponent(out PoolableObject poolable) && poolable.pool != null)
            {
                poolable.Active = false;
                return true;
            }
            else
            {
                Destroy(subject);
                return false;
            }
        }
    }
}

