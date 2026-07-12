using System.Collections.Generic;
using Services;
using UnityEngine;
using Utilities.ObjectPooling;

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
        bool actionDelayed = false;
        private ObjectPool pool;

        private void Awake() 
        {
            if (Gameplay.GameState != Gameplay.GameStates.Active) return;
            UpdateDelayer.QueueUpdate(() =>
            {
                if (usePool)
                {
                    if (!activePools.TryGetValue(prefab.gameObject, out pool))
                    {
                        pool = ObjectPool<Spawnable>.NEW(prefab, 3, true, false);
                        activePools.Add(prefab.gameObject, pool);
                        pool.Initialize();
                    }
                }
                if (Distance < loadDistance) AttemptLoad();
            }, "EntitySpawn", true);
        }

        private void FixedUpdate()
        {
            float distance = Distance;

            if (active == null || active.State is Spawnable.States.Inactive)
            {
                if(distance < loadDistance)
                {
                    Debug.Log("Entered Range");
                    AttemptLoad();
                }
            }
            else if (active.State is Spawnable.States.Active)
            {
                if (distance > unloadDistance)
                {
                    Debug.Log("Exited Range");
                    active.Deactivate();
                }
                else if (distance > offScreenDistance)
                {
                    Debug.Log("Went Off Screen");
                    active.LeaveScreen();
                }
            }
            else if (active.State is Spawnable.States.Offscreen)
            {
                if (distance < loadDistance)
                {
                    Debug.Log("Entered From Off Screen");
                    active.Activate();
                }
                else if (distance > unloadDistance)
                {
                    Debug.Log("Exited Range");
                    active.Deactivate();
                }
            }
        }

        float Distance => Vector3.Distance(PlayerPosition.position,
                (measureFromSpawn || active == null) ? transform.position : active.transform.position); 

        void AttemptLoad()
        {
            UpdateDelayer.QueueUpdate(() =>
            {
                if (active == null || active.State is Spawnable.States.Inactive)
                {
                    if (usePool)
                    {
                        active = pool.Pump();
                        active.SendMessage("OnSpawn");
                        active.transform.SetPositionAndRotation(transform.position, transform.rotation);
                    }
                    else
                    {
                        active = Spawnable.Instantiate(prefab.gameObject, this);
                        active.SendMessage("OnSpawn");
                        active.transform.SetPositionAndRotation(transform.position, transform.rotation);
                    }
                    active.Activate();
                }
            }, "EntitySpawn", true);
        }


        public static Transform PlayerPosition;
        public static Dictionary<GameObject, ObjectPool> activePools = new();

    }
}