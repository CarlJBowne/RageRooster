using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.Systems.SaveSystem
{
    public class SavePoint : MonoBehaviour, IInteractable
    {
        public SpawnPoint spawnPoint;

        Vector3 IInteractable.PopupPosition => transform.position + Vector3.up * 2f;

        bool IInteractable.canInteract => true;

        bool IInteractable.Interaction()
        {
            Save();
            return true;
        }

        public void Save() => SaveFile.SaveFileToDisk(spawnPoint.GetDestination());
    }
}