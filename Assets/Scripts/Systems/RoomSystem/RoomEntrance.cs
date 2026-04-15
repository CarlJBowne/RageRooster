using RageRooster.Systems.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.OpenVR;
using UnityEditor.Rendering;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    [ExecuteInEditMode]
    /// <summary>
    /// An entrance to a Room. MonoBehavior that triggers entering the Room when colliding with the Player.
    /// <br/>A pure-data representation of this entrance, <see cref="RoomEntrance.Data"/> is stored in a <see cref="RoomAsset"/> for runtime loading.
    /// </summary>
    public class RoomEntrance : RoomActor
    {


        /// <summary>
        /// The distance radius at which the room will begin loading.
        /// </summary>
        public float loadRadius = 20f;
        /// <summary>
        /// The distance radius at which the room will unload.
        /// </summary>
        public float unloadRadius = 30f;
        /// <summary>
        /// The distance radius at which the room's LOD will be loaded.
        /// </summary>
        public float lodRadius = 50f;
        /// <summary>
        /// The direction of the entrance inward into the room. <br/>
        /// If the player is on the near side of this transition it will not trigger loading nor entering the room.
        /// </summary>
        public Vector3 direction = Vector3.forward;

        /// <summary>
        /// An optional <see cref="SpawnPoint"/> this entrance can set the player's respawn location to when entered.
        /// </summary>
        public SpawnPoint spawnPoint;
        /// <summary>
        /// Whether the <see cref="spawnPoint"/> should only be set on death reloads, and not normal transitions."/>
        /// </summary>
        public bool forDeathOnly = false;




#if UNITY_EDITOR
        public override void OnRegister()
        {
            Root.EntranceActors.Add(this);
            Root.asset.entrances.Add(GetData());
        }
        public override void OnDeregister()
        {
            Root.asset.entrances.RemoveAt(Root.EntranceActors.IndexOf(this));
            Root.EntranceActors.Remove(this);
        }
        public override void OnSave() => Root.asset.entrances[Root.EntranceActors.IndexOf(this)] = GetData();
#endif

        public void OnTriggerEnter(Collider other)
        {
            if (other != Player.Collider) return;
            RoomManager.EnterRoom(Root.asset);
            if (spawnPoint != null)
                (forDeathOnly ? SaveData.DeathReloadData : SaveData.Current).location = spawnPoint.GetDestination();
        }






        public Data GetData() => new()
        {
            point = transform.position,
            direction = transform.TransformDirection(direction),
            loadRadiusSQR = loadRadius * loadRadius,
            unloadRadiusSQR = unloadRadius * unloadRadius,
            lodRadiusSQR = lodRadius * lodRadius
        };

        /// <summary>
        /// Packaged data about this entrance to be saved into a <see cref="RoomAsset"/>.
        /// </summary>
        [System.Serializable]
        public struct Data
        {
            public Vector3 point;
            public Vector3 direction;
            public float loadRadiusSQR;
            public float unloadRadiusSQR;
            public float lodRadiusSQR;
            [System.NonSerialized] public float distanceSquared;


            public void UpdateDistance(out int closestStrip)
            {
                distanceSquared = direction != Vector3.zero && Vector3.Dot(point - Player.Position, direction) < 0
                    ? distanceSquared = -1
                    : Vector3.SqrMagnitude(Player.Position - point);

                closestStrip = distanceSquared < loadRadiusSQR ? 3
                    : distanceSquared < unloadRadiusSQR ? 2
                    : distanceSquared < lodRadiusSQR ? 1
                    : 0;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Add Spawn Point")]
        private void AddSpawnPoint()
        {
            GameObject G = new("SpawnPoint");
            G.transform.SetParent(transform);
            G.transform.localPosition = Vector3.zero;
            G.transform.localRotation = Quaternion.identity;
            spawnPoint = G.AddComponent<SpawnPoint>();
            UnityEditor.Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Room Entrance");
        }

        [UnityEditor.MenuItem("GameObject/Create Room Entrance", false, 0)]
        public static void CreateRoomEntrance()
        {
            GameObject newObject = new("Room Entrance");
            UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Create Room Entrance");
            RoomEntrance entrance = newObject.AddComponent<RoomEntrance>();
            var parent = UnityEditor.Selection.activeTransform;
            if (parent != null)
            {
                UnityEditor.Undo.SetTransformParent(newObject.transform, parent.transform, "Create Room Entrance");
                newObject.transform.localPosition = Vector3.zero;
                newObject.transform.localRotation = Quaternion.identity;
            }
            UnityEditor.Selection.activeGameObject = newObject;
            newObject.AddComponent<BoxCollider>().isTrigger = true;
            entrance.AddSpawnPoint();
        }
#endif

    }

}