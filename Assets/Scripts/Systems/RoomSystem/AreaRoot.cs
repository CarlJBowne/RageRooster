using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    /// <summary>
    /// The Root component for an Area. Attached to the root <see cref="GameObject"/> of a <see cref="AreaAsset.shellScene"/>
    /// <br/> If an Area is created via the File/CreateRoom button, this component is automatically setup.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrders.Area)]
    public class AreaRoot : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="AreaAsset"/> associated with this instance.
        /// </summary>
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