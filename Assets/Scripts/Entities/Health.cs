using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A Component managing the Health of an entity, whether that be Player, Enemy, or Destructible Object.
/// </summary>
public class Health : MonoBehaviour
{
	//Config
	[SerializeField] protected int maxHealth;
	[SerializeField] protected UnityEvent<int> damageEvent = new();
	[SerializeField] protected UnityEvent depleteEvent = new();

	//Data
	protected int health;
	protected bool damagable = true;

	//Getters
	public int GetCurrentHealth() => health;
	public int GetMaxHealth() => maxHealth;
	public float GetHealthPercentage() => health / maxHealth;

	protected virtual void Awake() => health = maxHealth;

	public bool Damage(Attack attack)
	{
		if (!damagable || attack.amount < 1 || health == 0) return false;

		if (health - attack.amount < 0) attack.amount = health;

		health -= attack.amount;

		OnDamage(attack.amount);
		if (health == 0) OnDeplete();

		return true;
	}

	public bool Heal(int amount)
	{
		if (amount < 1 || health == maxHealth) return false;

		health += amount;

		if (health > maxHealth) health = maxHealth;

        return true;
	}

	protected virtual void OnDamage(int amount) => damageEvent?.Invoke(amount);
	protected virtual void OnDeplete() => depleteEvent?.Invoke();

}

/// <summary>
/// The Data of an Individual Attack.
/// </summary>
[Serializable]
public struct Attack
{

	public int amount;
	public string name;
	[HideInInspector] public AttackSource source;
	//Additional "Attack Type" data or whatnot.

	public Attack(int amount, string name, AttackSource source)
	{
		this.source = source;
		this.amount = amount;
		this.name = name;
	}
	public Attack(Attack data, AttackSource source)
	{
		this.source = source;
		amount = data.amount;
		name = data.name;
	}

}