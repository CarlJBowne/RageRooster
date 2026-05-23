using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Simple script that can spawn a prefab while disabling the object this is attached to.
// Good for things like explosion attacks, where you want to get rid of the thing that
// exploded, while enabling the explosion prefab.

public class SpawnAndDelete : MonoBehaviour
{
    // Object that will be spawned.
    public GameObject prefab;
    public EnemyHealth health;

    private void Awake()
    {
        if (health == null) TryGetComponent(out health);
    }

    // First, instantiate the object to be spawned the the position of the original object.
    // Then, disable the original object.
    public void PerformSpawnAndDelete()
    {
        Instantiate(prefab, this.transform.position, Quaternion.identity);
        if (health) health.Destroy();
        else Destroy(gameObject);
        //this.transform.gameObject.SetActive(false);
    }
}
