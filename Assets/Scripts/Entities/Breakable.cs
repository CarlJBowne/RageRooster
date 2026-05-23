using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    private bool isBroken = false;

    public GameObject breakVFX;
    public float DespawnTime = 5.0f;
    public LootSpawner lootSpawner;

    public void DestroyMesh()
    { 
        if (isBroken) return;
        isBroken = true;

        if (breakVFX != null)
        {
            Instantiate(breakVFX, transform.position, transform.rotation);
        }

        if (lootSpawner != null)
        {
            lootSpawner.SpawnLoot(transform.position);
        }

        Destroy(gameObject);
    }
}
