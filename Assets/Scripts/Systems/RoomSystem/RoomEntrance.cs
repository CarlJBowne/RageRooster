using RageRooster.Systems.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    public class RoomEntrance : MonoBehaviour
    {
        public float loadRadius = 20f;
        public float unloadRadius = 30f;
        public float lodRadius = 50f;
        public Vector3 direction = Vector3.forward;

        public SpawnPoint spawnPoint;
        public bool forDeathOnly = false;

        [SerializeField, HideInInspector] internal RoomRoot root;


        public void OnTriggerEnter(Collider other)
        {
            if (other != Player.Collider) return;
            RoomManager.EnterRoom(root.asset);
            if(spawnPoint != null)
                (forDeathOnly ? SaveData.DeathReloadData : SaveData.Current).location = spawnPoint.GetDestination();
        }






        public Data GetData() => new Data()
        {
            point = transform.position,
            direction = transform.TransformDirection(direction),
            loadRadius = loadRadius,
            unloadRadius = unloadRadius,
            lodRadius = lodRadius
        };

        [System.Serializable]
        public struct Data
        {
            public Vector3 point;
            public Vector3 direction;
            public float loadRadius;
            public float unloadRadius;
            public float lodRadius;
        }

#if UNITY_EDITOR
        [ContextMenu("Add Spawn Point")]
        private void AddSpawnPoint()
        {
            GameObject G = new("SpawnPoint");
            G.transform.SetParent(transform);
            G.transform.localPosition = Vector3.zero;
            G.transform.localRotation = Quaternion.identity;
            spawnPoint = G.AddComponent<SpawnPoint>();
        }
#endif

    }

}