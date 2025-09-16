using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    [DefaultExecutionOrder(-90)]
    public class AreaRoot : MonoBehaviour
    {
        [field: SerializeField] public AreaAsset asset { get; protected set; }

        private void Awake()
        {
            if (!RoomManager.Active)
            {
                if (EditorState.EditorDestination.IsDefault())
                    EditorState.EditorDestination = new()
                    {
                        area = asset,
                        room = null,
                        spawn = null,
                        spawnID = -1
                    }; Gameplay.BeginEditor();
                return;
            }

            asset.Connect(this);
        }

    }

}