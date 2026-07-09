using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    public class EntitySpawn : MonoBehaviour
    {
        public Spawnable prefab;
        public bool usePool = false;
        public float loadDistance = 30;
        public float offScreenDistance = 40;
        public float unloadDistance = 70;
        public bool measureFromSpawn = false;

        Spawnable active;

        private void FixedUpdate()
        {
            float distance = Vector3.Distance(PlayerPosition.position,
                (measureFromSpawn || active == null) ? transform.position : active.transform.position);

            if (active == null || active.State is Spawnable.States.Inactive)
            {
                if(distance < loadDistance)
                {
                    if (active == null) active = Spawnable.Instantiate(prefab.gameObject, this);
                    active.Activate();
                }
            }
            else if (active.State is Spawnable.States.Active)
            {
                if (distance > unloadDistance) active.Deactivate();
                else if (distance > offScreenDistance) active.LeaveScreen();
            }
            else if (active.State is Spawnable.States.Offscreen)
            {
                if (distance < loadDistance) active.Activate();
                else if (distance > unloadDistance) active.Deactivate();
            }
        }

        public static Transform PlayerPosition;
    }
}