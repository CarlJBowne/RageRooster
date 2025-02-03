using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A Component managing the Health of an entity, whether that be Player, Enemy, or Destructible Object.
/// </summary>

public class Health : MonoBehaviour
{
	//Config
	[SerializeField] protected int maxHealth;
	[SerializeField] protected UnityEvent<int> damageEvent = new();
	[SerializeField] protected UnityEvent depleteEvent = new();
	[SerializeField] protected Attack.Tag[] immuneTags;

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
        if (!damagable || attack.amount < 1 || immuneTags.IncludesAny(attack.tags) || OverrideDamage(attack)) return false;

        health -= attack.amount;

		if(health < 0) health = 0;

        OnDamage(attack);
        if (health == 0) OnDeplete(attack);

        return true;
    }

    protected virtual void OnDamage(Attack attack) => damageEvent?.Invoke(attack.amount);
    protected virtual void OnDeplete(Attack attack) => depleteEvent?.Invoke();

	protected virtual bool OverrideDamage(Attack attack) { return false; }

    public bool Heal(int amount)
	{
		if (amount < 1 || health == maxHealth) return false;

		health += amount;

		if (health > maxHealth) health = maxHealth;

        return true;
	}


    public bool Damage(Attack_Old attack)
    {
        if (!damagable || attack.amount < 1) return false;

        if (health - attack.amount < 0) attack.amount = health;

        health -= attack.amount;

        OnDamage(attack);
        if (health == 0) OnDeplete(attack);

        return true;
    }

    protected virtual void OnDamage(Attack_Old attack) => damageEvent?.Invoke(attack.amount);
	protected virtual void OnDeplete(Attack_Old attack) => depleteEvent?.Invoke();

}

/// <summary>
/// The Data of an Individual Attack.
/// </summary>
[Serializable]
public struct Attack_Old
{

	public int amount;
	public string name;
	public bool wham;
	
	[HideInInspector] public IAttacker_Old source;
    [HideInInspector] public Vector3 velocity;

	public Attack_Old(Attack_Old baseData, IAttacker_Old source, Vector3 velocity)
	{
		amount = baseData.amount;
		name = baseData.name;
		wham = baseData.wham;
		this.source = source;
		this.velocity = velocity;
	}	
	public Attack_Old(Attack_Old baseData)
	{
		amount = baseData.amount;
		name = baseData.name;
		wham = baseData.wham;
		this.source = null;
		this.velocity = Vector3.zero;
	}
	public Attack_Old(int amount, string name, bool wham)
	{
		this.amount = amount;
		this.name = name;
		this.wham = wham;
		this.source = null;
		this.velocity = Vector3.zero;
	}	
	public Attack_Old(int amount, string name, bool wham, IAttacker_Old source, Vector3 velocity)
	{
		this.amount = amount;
		this.name = name;
		this.wham = wham;
		this.source = source;
		this.velocity = velocity;
	}
}

#if UNITY_EDITOR
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
#endif