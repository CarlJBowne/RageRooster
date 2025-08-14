using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaRoot : MonoBehaviour
{
    [field: SerializeField] public AreaAsset Asset { get; protected set; }

    private void Awake()
    {
        
    }
}
