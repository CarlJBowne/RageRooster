using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An external Attack Source type that outsources the attack inflicted onto an AttackMulti
/// </summary>
[RequireComponent(typeof(Collider))]
public class AttackHitBox : MonoBehaviour, IAttacker
{
    /// <summary>
    /// The AttackSource this Hitbox is tied too.
    /// </summary>
    [SerializeField] AttackMulti source;
    /// <summary>
    /// The ID string used to determine which attack in the AttackSource's repetior is meant to be used. Leave null for default.
    /// </summary>
    public string currentAttackID = null;


    void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public void SetActive(bool value) => gameObject.SetActive(value);
    public void Enable() => gameObject.SetActive(true);
    public void Disable() => gameObject.SetActive(false);

    public void Contact(GameObject target) => BeginAttack(target);
    public void BeginAttack(GameObject target) => source.BeginAttack(target, currentAttackID);

    /// <summary>
    /// Use via Animations or otherwise to set specific attacks to be used at a given point in time.
    /// </summary>
    /// <param name="attackID"></param>
    public void SetAttackID(string attackID) => currentAttackID = attackID;
    
}