using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A source from which Attacks are inflicted upon Health components. Call BeginAttack and pass in a Health component. Also detects collisions by default.
/// </summary>
public class AttackSource : MonoBehaviour
{
	/// <summary> Whether this source recieves contacts from the collider on its own mesh. (Leave on unless using the MultiAttackHitBox script on another Collider.) </summary>
	[SerializeField] bool thisCollider = true;

	/// <summary> The default attack enacted by this source. </summary>
	public Attack defaultAttack;
	/// <summary> A list of possible attacks able to be activated by MultiAttackHitBox-es </summary>
	public Attack[] attacks;

	private Dictionary<string, Attack> attacksDict;
	/// <summary>
	/// The ID string used to determine which attack in the AttackSource's repetior is meant to be used. Leave null for default.
	/// </summary>
	public string currentAttackID = null;

	private void Awake()
	{
		defaultAttack.source = this;
		foreach (var attack in attacks) attacksDict.Add(attack.name, new(attack, this));
		
	}




	void OnTriggerEnter(Collider other) { if (thisCollider) BeginAttack(other.gameObject); }

	void OnCollisionEnter(Collision collision) { if (thisCollider) BeginAttack(collision.gameObject); }


	public void BeginAttack(GameObject target, string attackID = null)
	{
		if (!target.TryGetComponent(out Health targetHealth)) return;

		if (targetHealth.Damage(GetAttack(attackID)))
		{
			//Succesful Strike Additional Logic here.
		}
		else
		{
			//Unsuccesful Strike Additional Logic here.
		}

	}

	/// <summary>
	/// Use via Animations or otherwise to set specific attacks to be used at a given point in time.
	/// </summary>
	/// <param name="attackID"></param>
	public void SetAttackID(string attackID) => currentAttackID = attackID;

	public Attack GetAttack(string ID = null)
	{
		ID ??= currentAttackID;
		if (ID != null && attacksDict.TryGetValue(ID, out Attack result)) return result;
		return defaultAttack;
	}
}