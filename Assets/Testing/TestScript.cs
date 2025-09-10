using EditorAttributes;
using FMOD.Studio;
using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScript : MonoBehaviour, IInteractable
{
    public AreaAsset area;
    public RoomAsset room;

    Vector3 IInteractable.PopupPosition => Vector3.zero;
    bool IInteractable.canInteract => true;


    private void Awake()
    {

    }

    bool IInteractable.Interaction()
    {
        Enum().Begin(Gameplay.Get());
        return true;
        IEnumerator Enum()
        {
            yield return Overlay.OverMenus.BasicFadeOutWait(1f);
            OverlayLoading.SetVisible(true);
            yield return RoomManager.TransitionOut();
            RoomManager.transitionDestination = new TransitionDestination()
            {
                area = area,
                room = room,
                spawnID = 0
            };
            yield return RoomManager.TransitionIn();
            yield return Overlay.OverMenus.BasicFadeInWait(1f);
        }
    }
}
