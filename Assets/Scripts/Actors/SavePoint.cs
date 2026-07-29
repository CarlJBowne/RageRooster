using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.SaveSystem
{
    public class SavePoint : MonoBehaviour
    {
        public SpawnPoint spawnPoint;

        public void Save() => SaveData.SaveFileToDisk(spawnPoint.GetDestination());
    }
}