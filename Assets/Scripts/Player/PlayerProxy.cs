using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RageRooster.Player.Services;

public class PlayerProxy : MonoBehaviour
{
    Transform realPlayer;

    private void Start()
    {
        realPlayer = Player.Transform;
    }

    private void FixedUpdate()
    {
        transform.position = realPlayer.position;
        transform.rotation = realPlayer.rotation;
    }
}
