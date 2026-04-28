using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovablePlatform
{
    public List<CharacterMovementBody> bodies { get; }
    public bool MovePlayer { get; set; }

    public void AddBody(CharacterMovementBody body) => bodies.Add(body);
    public void RemoveBody(CharacterMovementBody body) => bodies.Remove(body);
    public void SetPlayerInfluence(bool value) => MovePlayer = value;

    protected static void DoAnchorMove(IMovablePlatform This, Vector3 offset)
    {
        for (int i = 0; i < This.bodies.Count; i++) 
            This.bodies[i].Position += offset;
        if(This.MovePlayer) Player.MovementBody.Position += offset;
    }
}
