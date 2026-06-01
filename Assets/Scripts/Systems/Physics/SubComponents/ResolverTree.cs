using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.Physics
{
    [System.Serializable]
    public class ResolverTree : PhysicsSubComponent
    {
        [field: SerializeField] public List<PhysicsResolver> resolvers { get; private set; } = new();
        [field: SerializeField] public PhysicsResolver defaultGroundedIndex { get; private set; }
        [field: SerializeField] public PhysicsResolver defaultAirIndex { get; private set; }

        public PhysicsResolver Active { get; private set; }

        public PhysicsResolver this[int i] => resolvers[i];
        public T GetResolver<T>() where T : PhysicsResolver
        {
            for (int i = 0; i < resolvers.Count; i++)
                if (resolvers[i].GetType() == typeof(T))
                    return resolvers[i] as T;
            return null;
        }
        public bool TryGetResolver<T>(out T result) where T : PhysicsResolver
        {
            for (int i = 0; i < resolvers.Count; i++)
                if (resolvers[i].GetType() == typeof(T))
                {
                    result = resolvers[i] as T;
                    return true;
                }
            result = null;
            return false;
        }
        public int ResolverCount => resolvers.Count;
        public int IndexOf(PhysicsResolver resolver) => resolvers.IndexOf(resolver);

        public void Update()
        {
            if (body.Ground) Update(defaultGroundedIndex);
            else Update(defaultAirIndex);
        }
        public void Update(int target) => Update(resolvers[target]);
        public void Update(PhysicsResolver resolver)
        {
            if (resolver == Active) return;
            Active?.Exit();
            Active = resolver;
            Active.Enter();
        }
    }
}