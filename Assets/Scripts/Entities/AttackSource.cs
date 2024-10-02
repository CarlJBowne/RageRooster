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

	/// <summary> A definition for an indidivdual attack. A dictionary of these can be used along with MultiAttackHitBox to define multiple attacks. </summary>
	public struct AttackMove
	{
		public string name;
		public int damage;
	}
	/// <summary> The default attack enacted by this source. </summary>
	public AttackMove defaultAttack;
	/// <summary> A list of possible attacks able to be activated by MultiAttackHitBox-es </summary>
	public AttackMove[] attacks;

	private Dictionary<string, AttackMove> attacksDict;

	private void Awake()
	{
		for (int i = 0; i < attacks.Length; i++) attacksDict.Add(attacks[i].name, attacks[i]);
	}




	void OnTriggerEnter(Collider other) { if (thisCollider) BeginAttack(other.gameObject); }

	void OnCollisionEnter(Collision collision) { if (thisCollider) BeginAttack(collision.gameObject); }


	public void BeginAttack(GameObject target, string attackID = null)
	{
		if (!target.TryGetComponent(out Health targetHealth)) return;

		AttackMove selectedAttack = defaultAttack;
		if (attackID != null) attacksDict.TryGetValue(attackID, out selectedAttack);
		if (targetHealth.Damage(selectedAttack.damage))
		{
			//Succesful Strike Additional Logic here.
		}
		else
		{
			//Unsuccesful Strike Additional Logic here.
		}
		
	}

}