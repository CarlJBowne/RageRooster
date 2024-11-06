using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OBSOLETE - A source from which Attacks are inflicted upon Health components, specifically for Hazard Objects.
/// </summary>
[System.Obsolete]
public class AttackConstant : MonoBehaviour
{
    public Attack attack;

    PlayerHealth player;

    public void BeginAttack(GameObject target)
    {
        if (!target.TryGetComponent(out Health targetHealth)) return;

        if (targetHealth.Damage(attack))
        {
            //Succesful Strike Additional Logic here.
        }
        else
        {
            //Unsuccesful Strike Additional Logic here.
        }
        if (target.TryGetComponent(out player)) player.queuedAttack = this;
    }
    public void BeginAttack(PlayerHealth play) => player.Damage(attack);

    private void OnCollisionEnter(Collision collision) => BeginAttack(collision.gameObject);
    private void OnCollisionExit(Collision collision)
    {
        if (player == null || collision.gameObject != player.gameObject || player.queuedAttack != this) return;
        player.queuedAttack = null;
        player = null;
    }
    private void OnTriggerEnter(Collider other) => BeginAttack(other.gameObject);
    private void OnTriggerExit(Collider other)
    {
        if (player == null || other.gameObject != player.gameObject || player.queuedAttack != this) return;
        player.queuedAttack = null;
        player = null;
    }
}
