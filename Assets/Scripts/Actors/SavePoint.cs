using System.Collections;
using System.Collections.Generic;
using RageRooster.Core.Save;
using RageRooster.World;
using UnityEngine;

namespace RageRooster.Actors.Save
{
    public class SavePoint : MonoBehaviour
    {
        public SpawnPoint spawnPoint;

        public void Save()
        {
            spawnPoint.SetAsReturnLocation();
            SaveData.SaveToSaveFile();
        }
    }
}