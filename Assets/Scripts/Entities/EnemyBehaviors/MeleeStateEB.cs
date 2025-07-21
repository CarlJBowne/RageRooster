using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;

public class MeleeStateEB : StateBehavior
{

    #region Config
    [SerializeField] float attackRate;
    public Attack attack;
    public UltEvents.UltEvent attackEvent;
    #endregion
    #region Data

    TrackerEB tracker;
    private float attackTimer;
    #endregion 

    protected override void OnFixedUpdate()
    {
        attackTimer += Time.fixedDeltaTime;
        if(attackTimer > attackRate)
        {
            attackTimer -= attackRate;
            BeginAttack();
        }
    }

    public void BeginAttack()
    {
        DebugAttack();
    }

    public void DebugAttack()
    {
        attackEvent?.Invoke();

        Vector3 pos = transform.position + Vector3.up + transform.forward;

        var res = Physics.OverlapSphere(pos, 0.5f, Physics.AllLayers);
        foreach (var item in res)
        {
            if(item.TryGetComponent(out PlayerHealth hp))
            {
                hp.Damage(attack);
                break;
            }
        }
    }

}
