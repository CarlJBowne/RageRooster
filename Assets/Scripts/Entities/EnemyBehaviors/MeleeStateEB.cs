using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV2;

public class MeleeStateEB : StateBehavior
{

    #region Config
    [SerializeField] float attackRate;
    #endregion
    #region Data

    TrackerEB tracker;
    private float attackTimer;
    #endregion 

    public override void OnFixedUpdate()
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
        Vector3 pos = transform.position + Vector3.up + transform.forward;

        var res = Physics.OverlapSphere(pos, 0.5f, Physics.AllLayers);
        foreach (var item in res)
        {
            if(item.TryGetComponent(out PlayerHealth hp))
            {
                hp.Damage(new Attack(1, "no", false));
                break;
            }
        }
    }

}
