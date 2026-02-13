using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerLassoProjectile : PlayerProjectile
{
    //config
    public float pullSpeed;
    public float reachplayerDistance = 1f;

    //data
    private bool pullingPhase;
    private Grabbable grabbable;


    protected override void FixedUpdate()
    {
        if (!pullingPhase) ProjectileUpdate();
        else PullUpdate();
    }

    private void PullUpdate()
    {
        transform.position += pullSpeed * Time.fixedDeltaTime * (Player.Position - transform.position).normalized;

        if (grabbable != null) grabbable.transform.position = this.transform.position;

        if (Vector3.Distance(transform.position, Player.Position) <= reachplayerDistance) ReachPlayer();
    }

    public override void Contact(GameObject target)
    {
        if (target == Player.GameObject || pullingPhase) return;
        pullingPhase = true;

        Grabbable.Attempt(target, success => { grabbable = success; grabbable.Grab(); }, null, null);

        Player.SignalManager.FireSignalBasic("LassoPull");
    }

    private void ReachPlayer()
    {
        grabbable = null;
        pullingPhase = false;
        gameObject.SetActive(false);
        Player.SignalManager.FireSignalBasic("LassoReach");
        if (grabbable != null) Player.Grabber.Grab(grabbable);
    }
}