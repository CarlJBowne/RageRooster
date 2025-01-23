using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
	[SerializeField, HideInInspector] protected int health;
	protected bool damagable = true;

	//Getters
	public int GetCurrentHealth() => health;
	public int GetMaxHealth() => maxHealth;
	public float GetHealthPercentage() => health / maxHealth;

	protected virtual void Awake() => health = maxHealth;

	public bool Damage(Attack attack)
	{
		if (!damagable || attack.amount < 1) return false;

		if (health - attack.amount < 0) attack.amount = health;

		health -= attack.amount;

		OnDamage(attack);
		if (health == 0) OnDeplete(attack);

		return true;
	}

	public bool Heal(int amount)
	{
		if (amount < 1 || health == maxHealth) return false;

		health += amount;

		if (health > maxHealth) health = maxHealth;

        return true;
	}

	protected virtual void OnDamage(Attack attack) => damageEvent?.Invoke(attack.amount);
	protected virtual void OnDeplete(Attack attack) => depleteEvent?.Invoke();

}

/// <summary>
/// The Data of an Individual Attack.
/// </summary>
[Serializable]
public struct Attack
{

	public int amount;
	public string name;
	public bool wham;
	
	[HideInInspector] public IAttacker source;
    [HideInInspector] public Vector3 velocity;

	public Attack(Attack baseData, IAttacker source, Vector3 velocity)
	{
		amount = baseData.amount;
		name = baseData.name;
		wham = baseData.wham;
		this.source = source;
		this.velocity = velocity;
	}	
	public Attack(Attack baseData)
	{
		amount = baseData.amount;
		name = baseData.name;
		wham = baseData.wham;
		this.source = null;
		this.velocity = Vector3.zero;
	}
	public Attack(int amount, string name, bool wham)
	{
		this.amount = amount;
		this.name = name;
		this.wham = wham;
		this.source = null;
		this.velocity = Vector3.zero;
	}	
	public Attack(int amount, string name, bool wham, IAttacker source, Vector3 velocity)
	{
		this.amount = amount;
		this.name = name;
		this.wham = wham;
		this.source = source;
		this.velocity = velocity;
	}
}

[CustomEditor(typeof(Health), true)]
public class HealthEditor : Editor
{
    public override void OnInspectorGUI()
	{
        serializedObject.Update();

		if (EditorApplication.isPlaying)
		{
            SerializedObject serializedObject = new UnityEditor.SerializedObject(target);
            SerializedProperty maxHealth = serializedObject.FindProperty("maxHealth");
            SerializedProperty health = serializedObject.FindProperty("health");

            Rect fullRect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUILayout.BeginVertical();
			EditorGUI.ProgressBar(fullRect, (float)health.intValue / (float)maxHealth.intValue, $"{health.intValue} / {maxHealth.intValue}");
			EditorGUILayout.EndVertical();
		}

        base.OnInspectorGUI();
    }
}
