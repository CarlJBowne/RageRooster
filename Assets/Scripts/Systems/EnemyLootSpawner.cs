using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLootSpawner : MonoBehaviour
{
    // OLD CODE for spawning loot on destruction of object.
    //public List<GameObject> lootOnDestroyPrefabs;

    // List of prefabs to use when spawning items on damage.
    public List<GameObject> lootOnDamagePrefabs;
    // List of prefabs to use when spawning items on deplete (death).
    public List<GameObject> lootOnDepletePrefabs;

    // OLD CODE value for how many times a prefab gets instantiated.
    // public int lootCount = 1;

    // OLD CODE but still used value for how strong a push force is when applied to object rigidbody.
    public float spawnForce = 5f;

    // Spawn chance for items on deplete. Random.value must roll higher than this.
    public float itemChance = 0.5f;

    // OLD CODE for spawning loot when enemy object is destroyed.
    /*public void SpawnLootOnDestroy(Vector3 position)
    {
        for (int i = 0; i < lootCount; i++)
        {
            int randomIndex = Random.Range(0, lootOnDestroyPrefabs.Count);
            GameObject loot = Instantiate(lootOnDestroyPrefabs[randomIndex], position, Quaternion.identity);
            Rigidbody rb = loot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = (loot.transform.position - position).normalized;
                rb.AddForce(forceDirection * spawnForce, ForceMode.Impulse);
            }
        }
    }*/

    // Function for spawning loot on damage. Set to listen to OnDamage on the EnemyHealth component.
    // Spawns a single random item out of the list in a 2-unit circle's edge around the position of the enemy (with 1 unit off the ground).
    public void SpawnLootOnDamage()
    {
        int randomIndex = Random.Range(0, lootOnDamagePrefabs.Count);
        Vector3 spawnDirection = Random.insideUnitCircle.normalized * 2;
        spawnDirection.z = spawnDirection.y;
        spawnDirection.y = 1;
        Vector3 spawnPoint = this.transform.position + spawnDirection;
        GameObject loot = Instantiate(lootOnDamagePrefabs[randomIndex], spawnPoint, Quaternion.identity);
        Rigidbody rb = loot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(spawnDirection * spawnForce, ForceMode.Impulse);
        }
    }

    // Function for spawning loot on deplete. Set to listen to OnDeplete on the EnemyHealth component.
    // Spawns a single random item out of the list in a 2-unit circle's edge around the position of the enemy (with 1 unit off the ground).
    // Can set spawn odds with 
    public void SpawnLootOnDeplete()
    {
        if (Random.value <= itemChance)
        {
            int randomIndex = Random.Range(0, lootOnDepletePrefabs.Count);
            Vector3 spawnDirection = Random.insideUnitCircle.normalized * 2;
            spawnDirection.z = spawnDirection.y;
            spawnDirection.y = 1;
            Vector3 spawnPoint = this.transform.position + spawnDirection;
            GameObject loot = Instantiate(lootOnDepletePrefabs[randomIndex], spawnPoint, Quaternion.identity);
            Rigidbody rb = loot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(spawnDirection * spawnForce, ForceMode.Impulse);
            }
        }
    }
}
