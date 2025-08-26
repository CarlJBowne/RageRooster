using SLS.ISingleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    public class RoomManager : SingletonMonoBasic<RoomManager>
    {
        public static AreaAsset currentArea { get; private set; }
        public static RoomAsset currentRoom { get; private set; }

        private void Update()
        {
            if(currentArea != null)
            {
                for (int i = 0; i < currentArea.rooms.Count; i++)
                {
                    if (currentArea.rooms[i] == currentRoom) continue;
                    currentArea.rooms[i].UpdateDistance();
                }
            }
        }

        public IEnumerator ExitArea()
        {
            yield return null;
        }
        public IEnumerator EnterArea(RoomDestination dest)
        {
            yield return null;

            if (dest.areaAsset == null)
            {
                //Get Area Asset from Area Registry.
                dest.areaAsset = AreaRegistry.GetArea(dest.areaName);
            }

            yield return dest.areaAsset.LoadArea();
            AreaRoot areaRoot = dest.areaAsset.root;

            if (dest.roomAsset == null)
            {
                //Get Room Asset from intended Area.
                dest.roomAsset = dest.areaAsset.rooms[dest.roomID];
            }

            yield return dest.roomAsset.LoadInto();
            RoomRoot roomRoot = dest.roomAsset.root;

            if (dest.spawnPoint == null)
            {
                //Get Spawn Point from ID list.
                dest.spawnPoint = roomRoot.spawns[dest.spawnID];
            }
            dest.spawnPoint.SpawnPlayerAt();
            yield return ForceLoadNearbyAreas();

        }

        public IEnumerator ForceLoadNearbyAreas()
        {
            yield return null;
        }

    }
}