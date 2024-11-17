using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A special Attack Source that contains a list of Attacks and can be activated by external AttackHitBox-es
/// </summary>
public class AttackMulti : AttackSource
{
	/// <summary> Whether this source recieves contacts from the collider on its own mesh. (Leave on unless using the MultiAttackHitBox script on another Collider.) </summary>
	[SerializeField] bool thisCollider = true;
	public bool IsSelfCollider() => thisCollider;

	/// <summary> A list of possible attacks able to be activated by MultiAttackHitBox-es </summary>
	public Attack[] attacks;

	private Attack currentAttack;
	private Dictionary<string, Attack> attacksDict = new Dictionary<string, Attack>();

	private void Awake()
	{
		attacksDict = new Dictionary<string, Attack>();
		foreach (Attack item in attacks) attacksDict.Add(item.name, new(item));
		currentAttack = attack;
	}

    public override void Contact(GameObject target)
	{
		if (thisCollider) (this as IAttacker).BeginAttack(target, currentAttack, velocity);
	}

	public void BeginAttack(GameObject target, string ID)
	{
		SetAttackID(ID);
        (this as IAttacker).BeginAttack(target, currentAttack, velocity);
	}

    /// <summary>
    /// Use via Animations or otherwise to set specific attacks to be used at a given point in time.
    /// </summary>
    /// <param name="attackID"></param>
    public void SetAttackID(string attackID)
	{
		currentAttack = attackID != null && attackID != "" && attacksDict.ContainsKey(attackID) 
			? attacksDict[attackID] 
			: currentAttack;
    } 

}