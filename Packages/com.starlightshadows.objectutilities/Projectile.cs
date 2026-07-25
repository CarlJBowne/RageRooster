using SLS.EditorUtilities.ComponentHeaders;
using UnityEngine;

namespace SLS.ObjectUtilities
{
    [RequireComponent(typeof(Spawnable))]
    public abstract class Projectile : MonoBehaviour
    {
        [field: SerializeField, HeaderItem(true)] public Spawnable spawnable { get; private set; }

        public Vector3 initVelocity { get; protected set; }
        public Vector3 initPosition { get; protected set; }
        public Placement target { get; protected set; }
        public abstract Vector3 velocity { get; protected set; }

        private void Reset()
        {
            HeaderItemAttribute.Reset(this);
        }

        public virtual Projectile Place(Placement placement)
        {
            transform.SetPositionAndRotation(placement.Position, placement.Rotation);
            return this;
        }
        public virtual Projectile Send(Vector3 velocity)
        {
            initVelocity = velocity;
            this.velocity = initVelocity;
            return this;
        }
        public virtual Projectile SendAt(Placement target, float force)
        {
            this.target = target;
            initVelocity = (target.Position - transform.position).normalized * force;
            this.velocity = initVelocity;
            return this;
        }
    }
}
