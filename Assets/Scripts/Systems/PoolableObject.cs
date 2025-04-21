using System;
using UnityEngine;

public class PoolableObject : MonoBehaviour
{

    [HideInInspector] public ObjectPool pool;
    [HideInInspector] public bool Active;
    [HideInInspector] public float timeExisting;


    public Action onActivate;
    /// <summary>
    /// If nothing calls this action when this object instance is done the object will never be available for reuse. (Extremely unintuitive, fix in V3.)
    /// </summary>
    public Action<PoolableObject> onDeactivate;

    /// <summary>
    /// This method is used for Setup of the Pooled Object Instance after it is Activated. In the default base of this script this method does nothing, if not overridden Setup is the responsibility of the script calling Pump();
    /// </summary>
    public virtual void Prepare() { }

    public virtual void Prepare_Basic(Vector3 position, Vector3 direction, Vector3 velocity, bool relative = true)
    {
        transform.position = position;
        transform.eulerAngles = direction;

        Rigidbody rigid = rb;
        if (!rigid) return;

        rigid.velocity = relative ? transform.TransformDirection(velocity) : velocity;
        rigid.angularVelocity = Vector3.zero;

    }

    /// <summary>
    /// An accesible function for another script to disable this Poolable Object without necessarily Deactivating the Game Object.
    /// </summary>
    public void Disable(bool deactivateGameObject = true)
    {
        if (!gameObject.scene.isLoaded || pool == null) return;
        bool wasActive = Active;
        Active = false;
        if (onDeactivate.GetInvocationList().Length > 0 && wasActive) onDeactivate(this);
        if (deactivateGameObject) gameObject.SetActive(false);

        if (pool == null) Destroy(gameObject);
    }

    private void OnDisable() { if (Active) Disable(); }
    public Rigidbody rb => GetComponent<Rigidbody>();

    public static PoolableObject Is(GameObject subject)
    {
        PoolableObject poolable = subject.GetComponent<PoolableObject>(); 
        if (!poolable) return null;
        if (poolable.pool == null) return null;
        return poolable;
    }
    public static bool Is(GameObject subject, out PoolableObject result)
    {
        result = subject.GetComponent<PoolableObject>();
        return result && result.pool != null;
    }
    public static bool DisableOrDestroy(GameObject subject)
    {
        if (subject.TryGetComponent(out PoolableObject poolable) && poolable.pool != null)
        {
            poolable.Disable();
            return true;
        }
        else
        {
            Destroy(subject);
            return false;
        }
    }

    public void SetPosition(Vector3 position) => transform.position = position;
    public void SetRotation(Vector3 rotation) => transform.eulerAngles = rotation;
    public void PlaceAtMuzzle(Transform muzzle)
    {
        transform.position = muzzle.position;
        transform.rotation = muzzle.rotation;
    }

}