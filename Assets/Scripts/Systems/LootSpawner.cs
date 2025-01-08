using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    public List<GameObject> lootPrefabs; // Changed from single prefab to list of prefabs
    public int lootCount = 1;
    public float spawnForce = 5f;

    public void SpawnLoot(Vector3 position)
    {
        for (int i = 0; i < lootCount; i++)
        {
            int randomIndex = Random.Range(0, lootPrefabs.Count); // Get a random index
            GameObject loot = Instantiate(lootPrefabs[randomIndex], position, Quaternion.identity); // Instantiate random prefab
            Rigidbody rb = loot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = (loot.transform.position - position).normalized;
                rb.AddForce(forceDirection * spawnForce, ForceMode.Impulse);
            }
        }
    }
}
