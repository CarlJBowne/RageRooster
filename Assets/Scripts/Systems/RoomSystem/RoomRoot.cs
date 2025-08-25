using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    [DefaultExecutionOrder(-200)]
    public class RoomRoot : MonoBehaviour
    {
        [field: SerializeField] public RoomAsset asset { get; protected set; }
        [field: SerializeField] public SpawnPoint[] spawns { get; protected set; }

        private void Awake()
        {
            asset.Connect(this);
        }

    }

}