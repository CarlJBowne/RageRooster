using RageRooster.Systems.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public SpawnPoint spawnPoint;
    public bool forDeathOnly = false;

    private void Reset()
    {
        if (spawnPoint != null) return;
        if (TryGetComponent(out spawnPoint)) return;
        if (transform.GetChild(0).TryGetComponent(out spawnPoint)) return;

        GameObject G = new("SpawnPoint");
        G.transform.SetParent(transform);
        G.transform.localPosition = Vector3.zero;
        G.transform.localRotation = Quaternion.identity;
        spawnPoint = G.AddComponent<SpawnPoint>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != Player.Collider) return;

        if(!forDeathOnly) SaveFile.Current.location = spawnPoint.GetDestination();
        SaveFile.DeathReloadData.location = spawnPoint.GetDestination();
    }
}
