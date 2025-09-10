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

        [SerializeField, HideInInspector] internal RoomRoot root;


        public void OnTriggerEnter(Collider other)
        {
            if (other != PlayerMovementBody.Get().Collider) return;
            RoomManager.EnterRoom(root.asset);
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
    }

}