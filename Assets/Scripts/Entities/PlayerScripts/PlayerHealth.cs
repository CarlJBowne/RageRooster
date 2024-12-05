using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHealth : Health
{
    public float invincibilityTime;

    private Coroutine invincibility;
    private new Collider collider;
    private UIHUDSystem UI;
    private Rigidbody rb;

    private Vector3 respawnPoint;

    protected override void Awake()
    {
        base.Awake();
        collider = GetComponent<Collider>();
        UIHUDSystem.TryGet(out UI);
        TryGetComponent(out rb);
        UpdateHealth();
        //Respawn();
    }

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);
        if(health != 0)
        {
            invincibility = StartCoroutine(InvinceEnum(invincibilityTime));
            damagable = false;
        }
        UpdateHealth();
        if(attack.name == "Pit") Respawn();
    }

    protected override void OnDeplete(Attack attack) => UI.StartCoroutine(DeathEnum());

    private IEnumerator InvinceEnum(float time)
    {
        yield return new WaitForSeconds(time);
        InvincEnd();
    }

    private void InvincEnd()
    {
        damagable = true;
        collider.enabled = false;
        collider.enabled = true;
        //if(queuedAttack != null) queuedAttack.BeginAttack(this);
    }

    public void UpdateHealth() => UI.UpdateHealth(health, maxHealth);

    public void AddMaxHealth(int value = 1)
    {
        maxHealth += value;
        UpdateHealth();
    }

    /*
   private void InvincEndDontWork()
   {
       CapsuleCollider caps = GetComponent<CapsuleCollider>();
       float radius = caps.radius + 0.05f;
       Vector3 point1 = transform.position + caps.center - (Vector3.up * ((caps.height / 2) - caps.radius));
       Vector3 point2 = transform.position + caps.center + (Vector3.up * ((caps.height / 2) - caps.radius));

       Collider[] colls = Physics.OverlapCapsule(point1, point2, radius, GetComponent<Rigidbody>().includeLayers, QueryTriggerInteraction.Collide); //Fix Getting Rigidbody's useful layers

       for (int i = 0; i < colls.Length; i++)
       {
           if(colls[i].TryGetComponent(out AttackMulti soc))
           {
               if (soc.IsSelfCollider()) soc.BeginAttack(gameObject);
               break;
           }
           else if(colls[i].TryGetComponent(out AttackHitBox box))
           {
               box.ManualBeginAttack(gameObject);
               break;
           }
       }
   }*/


    private IEnumerator DeathEnum()
    {
        gameObject.SetActive(false);

        yield return new WaitForSeconds(2);

        Respawn();
        gameObject.SetActive(true);
        health = maxHealth;
        UpdateHealth();
    }

    public void SetRespawnPoint(Vector3 respawnPoint) => this.respawnPoint = respawnPoint;

    public void Respawn() => rb.MovePosition(respawnPoint);

}
