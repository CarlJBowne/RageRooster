using System;
using System.Collections.Generic;
using UnityEngine;

public class BounceShroom : MonoBehaviour
{
    [SerializeField] float bouncePower;
    [SerializeField] float bounceHeight;
    [SerializeField] float bounceMinHeight;

    public static void AttemptBounce(GameObject G, PlayerAirborn bouncingState)
    {
        if (!G.TryGetComponent(out BounceShroom I)) return;
        bouncingState.BeginJump(I.bouncePower, I.bounceHeight, I.bounceMinHeight != 0 ? I.bounceMinHeight : I.bounceHeight);
    }


}

