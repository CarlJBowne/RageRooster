using UnityEngine;

public abstract class AttackSourceBase : MonoBehaviour
{
    public abstract Attack GetAttack();

    void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public abstract void Contact(GameObject target);
}