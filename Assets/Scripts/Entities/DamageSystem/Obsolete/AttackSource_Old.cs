using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The base form of Attack Source. A singular possible attack.
/// </summary>
[System.Obsolete]
public class AttackSource_Old : MonoBehaviour, IAttacker_Old
{
    //public Attack_Old attack;
    //public Vector3 velocity;
    //
    //void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    //
    //void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    //
    //public virtual void Contact(GameObject target) => (this as IAttacker_Old).BeginAttack(target, attack, velocity);

}

[System.Obsolete]
public interface IAttacker_Old
{
    //public abstract void Contact(GameObject target);
    //
    //public void BeginAttack(GameObject target, Attack_Old attack, Vector3 velocity)
    //{
    //    if (!target.TryGetComponent(out Health targetHealth)) return;
    //
    //    if (targetHealth.Damage(new Attack_Old(attack, this, velocity)))
    //    {
    //
    //    }
    //    else
    //    {
    //
    //    }
    //
    //}
}

/// <summary>
/// The Data of an Individual Attack.
/// </summary>
[Serializable]
public struct Attack_Old
{

    //public int amount;
    //public string name;
    //public bool wham;
    //
    //[HideInInspector] public IAttacker_Old source;
    //[HideInInspector] public Vector3 velocity;
    //
    //public Attack_Old(Attack_Old baseData, IAttacker_Old source, Vector3 velocity)
    //{
    //    amount = baseData.amount;
    //    name = baseData.name;
    //    wham = baseData.wham;
    //    this.source = source;
    //    this.velocity = velocity;
    //}
    //public Attack_Old(Attack_Old baseData)
    //{
    //    amount = baseData.amount;
    //    name = baseData.name;
    //    wham = baseData.wham;
    //    this.source = null;
    //    this.velocity = Vector3.zero;
    //}
    //public Attack_Old(int amount, string name, bool wham)
    //{
    //    this.amount = amount;
    //    this.name = name;
    //    this.wham = wham;
    //    this.source = null;
    //    this.velocity = Vector3.zero;
    //}
    //public Attack_Old(int amount, string name, bool wham, IAttacker_Old source, Vector3 velocity)
    //{
    //    this.amount = amount;
    //    this.name = name;
    //    this.wham = wham;
    //    this.source = source;
    //    this.velocity = velocity;
    //}
}

/*
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
*/