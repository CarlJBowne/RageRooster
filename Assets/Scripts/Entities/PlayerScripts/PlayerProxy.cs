using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProxy : MonoBehaviour
{
    Transform realPlayer;

    private void Start()
    {
        realPlayer = Gameplay.Player.transform;
    }

    private void FixedUpdate()
    {
        transform.position = realPlayer.position;
        transform.rotation = realPlayer.rotation;
    }
}
