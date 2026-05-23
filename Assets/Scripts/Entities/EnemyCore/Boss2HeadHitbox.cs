using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2HeadHitbox : MonoBehaviour, IDamagable
{
    public Boss2HeadStateMachine Machine;

    public bool Damage(Attack attack) => Machine.Damage(attack);
}
