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
	[SerializeField] private int maxHealth;
	[SerializeField] private UnityEvent<int> damageEvent;
	[SerializeField] private UnityEvent depleteEvent;

	//Data
	private int health;
	private bool damagable = true;

	public int GetCurrentHealth() => health;
	public int GetMaxHealth() => maxHealth;
	public float GetHealthPercentage() => health / maxHealth;


	private void Awake() => health = maxHealth;

	public bool Damage(int amount)
	{
		if (!damagable || amount < 1 || health == 0) return false;

		if (health - amount < 0) amount = health;

		health -= amount;

		OnDamage(amount);
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
