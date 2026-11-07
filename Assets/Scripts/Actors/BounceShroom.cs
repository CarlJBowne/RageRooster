using System;
using System.Collections.Generic;
using UnityEngine;

public class BounceShroom : MonoBehaviour
{
    public float bouncePower;
    public float bounceHeight;
    public float bounceMinHeight;

    private Animator anim;

    public void BounceReaction()
    {
        if (anim || transform.parent.TryGetComponent(out anim)) anim.Play("Bounce");
    }
}

