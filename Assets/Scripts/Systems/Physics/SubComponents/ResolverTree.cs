using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.Physics
{
    [System.Serializable]
    public class ResolverTree : PhysicsSubComponent
    {

        [field: SerializeField] public PhysicsResolver groundedResolver { get; private set; }
        [field: SerializeField] public PhysicsResolver airborneResolver { get; private set; }

        public PhysicsResolver Active { get; private set; }

        public override void Init(PhysicsBody owner)
        {
            base.Init(owner);
            PhysicsResolver[] resolvers = owner.GetComponents<PhysicsResolver>();
            for (int i = 0; i < resolvers.Length; i++) resolvers[i].OnStart();
        }

        public void Update()
        {
            if (body.Ground) Update(groundedResolver);
            else Update(airborneResolver);
        }
        public void Update(PhysicsResolver resolver)
        {
            if (resolver == Active) return;
            Active?.Exit();
            Active = resolver;
            Active.Enter();
        }
    }
}