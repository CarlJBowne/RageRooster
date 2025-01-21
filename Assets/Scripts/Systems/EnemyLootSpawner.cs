using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLootSpawner : MonoBehaviour
{
    public List<GameObject> lootPrefabs;
    public int lootCount = 1;
    public float spawnForce = 5f;

    public void SpawnLoot(Vector3 position)
    {
        for (int i = 0; i < lootCount; i++)
        {
            int randomIndex = Random.Range(0, lootPrefabs.Count);
            GameObject loot = Instantiate(lootPrefabs[randomIndex], position, Quaternion.identity);
            Rigidbody rb = loot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = (loot.transform.position - position).normalized;
                rb.AddForce(forceDirection * spawnForce, ForceMode.Impulse);
            }
        }
    }
}
