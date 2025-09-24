using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    [DefaultExecutionOrder(ExecutionOrders.Area)]
    public class AreaRoot : MonoBehaviour
    {
        [field: SerializeField] public AreaAsset asset { get; protected set; }

        private void Awake()
        {
            if (!RoomManager.Active)
            {
                if (EditorState.EditorDestination.IsNull())
                {
                    EditorState.EditorDestinationArea = asset;
                    EditorState.EditorDestination = new()
                    {
                        room = null,
                        spawnID = -1
                    }; Gameplay.BeginEditor();
                }   
                return;
            }

            asset.Connect(this);
        }

#if UNITY_EDITOR
        public class Editor : UnityEditor.Editor
        {


            public static void AttachAsset(AreaRoot This, AreaAsset area)
            {
                This.asset = area;
                UnityEditor.EditorUtility.SetDirty(This);
            }
        }


#endif
    }

}