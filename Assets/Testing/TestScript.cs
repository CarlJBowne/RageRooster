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
        Enum().Begin(Gameplay.Instance);
        return true;
        IEnumerator Enum()
        {
            yield return Overlay.OverMenus.BasicFadeOutWait(1f);
            OverlayLoading.SetVisible(true);
            yield return RoomManager.Transition(new Destination()
            {
                area = area,
                room = room,
                spawnID = 0
            }, true);
            yield return Overlay.OverMenus.BasicFadeInWait(1f);
        }
    }
}
