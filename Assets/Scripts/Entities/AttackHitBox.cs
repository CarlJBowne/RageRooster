using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An individual Attack Hitbox to be used when multiple colliders are hitboxes.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AttackHitBox : MonoBehaviour
{
	/// <summary>
	/// The AttackSource this Hitbox is tied too.
	/// </summary>
	[SerializeField] AttackSource source;
	/// <summary>
	/// The ID string used to determine which attack in the AttackSource's repetior is meant to be used. Leave null for default.
	/// </summary>
	public string currentAttackID = null;



	void OnTriggerEnter(Collider other) => source.BeginAttack(other.gameObject, currentAttackID);

	void OnCollisionEnter(Collision collision) => source.BeginAttack(collision.gameObject, currentAttackID);

	/// <summary>
	/// Use via Animations or otherwise to set specific attacks to be used at a given point in time.
	/// </summary>
	/// <param name="attackID"></param>
	public void SetAttackID(string attackID) => currentAttackID = attackID;
}