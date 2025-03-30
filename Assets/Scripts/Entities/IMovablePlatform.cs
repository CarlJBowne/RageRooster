using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovablePlatform
{
    private static PlayerMovementBody body;
    protected static void DoAnchorMove(IMovablePlatform This, Vector3 offset)
    {
        if (body == null) body = PlayerMovementBody.Get();
        if (PlayerMovementBody.currentAnchor == This) body.position += offset;
    }
}
