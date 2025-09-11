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
                if (!EditorState.EditorDestination.IsValid()) EditorState.EditorDestination = new(this);
                Gameplay.BeginEditor(EditorState.EditorDestination);
                return;
            }

            asset.Connect(this);
        }

    }

}