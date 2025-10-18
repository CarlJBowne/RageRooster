using RageRooster.Systems.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CheckPoint that updates the player's spawn location upon contact.
/// </summary>
public class CheckPoint : MonoBehaviour
{
    /// <summary>
    /// The target <see cref="SpawnPoint"/> to set as the player's new spawn location."/>
    /// </summary>
    public SpawnPoint spawnPoint;
    /// <summary>
    /// Whether this CheckPoint only updates the spawn location for death respawns.
    /// </summary>
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

        if(!forDeathOnly) SaveData.Current.location = spawnPoint.GetDestination();
        SaveData.DeathReloadData.location = spawnPoint.GetDestination();
    }
}
