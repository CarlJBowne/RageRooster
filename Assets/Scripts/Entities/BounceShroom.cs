using System;
using System.Collections.Generic;
using UnityEngine;

public class BounceShroom : MonoBehaviour
{
    [SerializeField] float bouncePower;
    [SerializeField] float bounceHeight;
    [SerializeField] float bounceMinHeight;

    private Animator anim;

    public static void AttemptBounce(GameObject G, PlayerAirborn bouncingState)
    {if (G.TryGetComponent(out BounceShroom I)) I.Bounce(bouncingState);}

    public void Bounce(PlayerAirborn bouncingState)
    {
        bouncingState.BeginJump(bouncePower, bounceHeight, bounceMinHeight != 0 ? bounceMinHeight : bounceHeight);
        if (anim || transform.parent.TryGetComponent(out anim)) anim.Play("Bounce");
    }
}

