using UnityEngine;
using UnityEngine.Animations;

public interface IGrabbable
{
    public bool Grab(IGrabber grabber);

    public void Throw(Vector3 velocity);
    public void Release();

    public static bool Test(Collider target, out IGrabbable result)
    {
        if(target.TryGetComponent(out IGrabbable I) && I.This.IsGrabbable)
        {
            result = I.This;
            return true;
        }
        result = null;
        return false;
    }

    public static bool Test(Collider[] possibleTargets, out IGrabbable result)
    {
        for (int i = 0; i < possibleTargets.Length; i++) 
            if (possibleTargets[i].TryGetComponent(out IGrabbable I) && I.This.IsGrabbable)
            {
                result = I.This;
                return true;
            }     
        result = null;
        return false;
    }

    public IGrabbable This => this;
    public IGrabber Grabber { get; } 
    public Transform transform { get; }

    public bool IsGrabbable { get; }

    public float AdditionalThrowDistance { get; }
    public Vector3 HeldOffset { get; }

    public void SetVelocity(Vector3 velocity);

    public Rigidbody rigidBody { get; }
}
public interface IGrabber
{
    public Collider ownerCollider { get; }
}