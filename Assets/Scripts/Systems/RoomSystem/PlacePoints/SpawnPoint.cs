using EditorAttributes;
using JetBrains.Annotations;
using RageRooster.RoomSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set point in the world where the player can spawn. <br/>
/// When activated it moves the player to the exact transform position.
/// </summary>
public class SpawnPoint : RoomActor
{

    /// <summary>
    /// The ID of this SpawnPoint within the Room it belongs to.
    /// </summary>
    public int ID => Root.Spawns.IndexOf(this);
    /// <summary>
    /// Whether the player should rotate to the forward direction of the <see cref="SpawnPoint"/>.
    /// </summary>
    public bool rotate = true;
    /// <summary>
    /// Whether the player should snap downwards to the nearest floor when spawned.
    /// </summary>
    public bool snapToFloor = true;



    /// <summary>
    /// Places the player at this <see cref="SpawnPoint"/>'s position.
    /// </summary>
    public void SpawnPlayerAt()
    {
        //Cast downwards and get point.
        Vector3 target = transform.position;
        if (snapToFloor && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit)) target = hit.point;

        Player.InstantMove(target, rotate ? transform.eulerAngles.y : null);
        //Player.MovementBody.InstantSnapToFloor();
    }

    /// <returns>The <see cref="Destination"/> this <see cref="SpawnPoint"/> goes to.</returns>
    public Destination GetDestination() => new()
    {
        room = Root.asset,
        spawnID = ID
    };


#if UNITY_EDITOR
    public override void OnRegister()
    {
        Root.Spawns.AddUnique(this);
        Root.asset.spawnPointNames.Add(gameObject.name);
    }
    public override void OnDeregister()
    {
        Root.asset.spawnPointNames.RemoveAt(Root.Spawns.IndexOf(this));
        Root.Spawns.Remove(this);
    }
    public override void OnSave() => Root.asset.spawnPointNames[Root.Spawns.IndexOf(this)] = gameObject.name;
#endif

    [Button]
    void AAAAAAAA() => Reset();

#if UNITY_EDITOR
    [Button("Play from here.")]
    private void BeginFromHere()
    {
        EditorState.EditorDestination = new()
        {
            room = Root.asset,
            spawnID = ID
        };
        UnityEditor.EditorApplication.isPlaying = true;
    }

    [UnityEditor.MenuItem("GameObject/Create Spawn Point", false, 0)]
    public static void CreateSpawnPoint()
    {
        GameObject newObject = new("SpawnPoint");
        UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Create Spawn Point");
        SpawnPoint spawnPoint = newObject.AddComponent<SpawnPoint>();
        if (UnityEditor.Selection.activeTransform != null)
            newObject.transform.SetParent(UnityEditor.Selection.activeTransform);
    }


#endif
}