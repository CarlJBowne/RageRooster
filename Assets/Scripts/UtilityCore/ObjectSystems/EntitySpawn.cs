using System.Collections.Generic;
using Services;
using UnityEngine;
using Utilities.ObjectPooling;
using Utilities.Xtensions.Unity;

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
        public bool hidden = false;
        ObjectPool pool;

        private void Awake() 
        {
            if (Gameplay.GameState != Gameplay.GameStates.Active) return;
            actionDelayed = true;
            UpdateDelayer.QueueUpdate(() =>
            {
                actionDelayed = false;
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
            if (actionDelayed) return;
            float distance = Distance;

            if (active == null || active.Ready)
            {
                if(distance < loadDistance)
                {
                    Debug.Log("Entered Range");
                    AttemptLoad();
                }
            }
            else if (active.Active)
            {
                if (distance > unloadDistance)
                {
                    Debug.Log("Exited Range");
                    PlaceAndActivate();
                }
                else if (distance > offScreenDistance)
                {
                    Debug.Log("Went Off Screen");
                    active.SetActive(false);
                    hidden = true;
                }
            }
            else if (hidden)
            {
                if (distance < loadDistance)
                {
                    Debug.Log("Entered From Off Screen");
                    PlaceAndActivate();
                    hidden = false;
                }
                else if (distance > unloadDistance)
                {
                    Debug.Log("Exited Range");
                    hidden = false;
                }
            }
        }

        public void PlaceAndActivate()
        {
            active.SetActive(true);
            active.transform.CopyFrom(transform);
        }

        float Distance => Vector3.Distance(PlayerPosition.position,
                (measureFromSpawn || active == null) ? transform.position : active.transform.position); 

        void AttemptLoad()
        {
            actionDelayed = true;
            UpdateDelayer.QueueUpdate(() =>
            {
                actionDelayed = false;
                if (active == null || active.Active)
                {
                    active = usePool ? pool.Pump() : Spawnable.Instantiate(prefab.gameObject); 
                    PlaceAndActivate();
                    active.SendMessage("OnSpawn");
                }
            }, "EntitySpawn", true);
        }


        public static Transform PlayerPosition;
        public static Dictionary<GameObject, ObjectPool> activePools = new();

    }
}