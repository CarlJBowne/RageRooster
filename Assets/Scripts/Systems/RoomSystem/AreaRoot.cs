using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    [DefaultExecutionOrder(-200)]
    public class AreaRoot : MonoBehaviour
    {
        [field: SerializeField] public AreaAsset asset { get; protected set; }

        private void Awake()
        {
            asset.Connect(this);
        }

    }

}