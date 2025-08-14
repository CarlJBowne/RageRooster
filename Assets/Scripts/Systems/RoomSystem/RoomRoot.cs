using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomRoot : MonoBehaviour
{
    [field: SerializeField] public RoomAsset Asset { get;  protected set; }

    private void Awake()
    {
        
    }
}
