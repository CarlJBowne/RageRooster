using UnityEngine;

public interface IAttackSource
{
    public Attack GetAttack();

    public void Contact(GameObject target);
}