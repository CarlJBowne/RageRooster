using RageRooster.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RageRooster.Services;

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
    /// Whether this CheckPoint updates the spawn location for death respawns.
    /// </summary>
    public bool deathCheckpoint = false;

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
        if (Player.Owns(other)) return;

        SaveSystem.CurrentDestination.Set(spawnPoint.GetDestination());
        if (deathCheckpoint) SaveSystem.SaveToDeathData();
    }
}
