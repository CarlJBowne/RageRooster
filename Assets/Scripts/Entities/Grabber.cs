using EditorAttributes;
using UnityEngine;

[System.Serializable]
public class Grabber
{
    public Grabbable currentGrabbed;
    public UltEvents.UltEvent<bool> GrabStateEvent;

    [HideInEditMode, HideInPlayMode] public Collider collider;

    public virtual bool AttemptGrab(GameObject target, out Grabbable result, bool doGrab = true)
    {
        if (IGrabbable.Grab(target, out result))
        {
            if(doGrab) BeginGrab(result);
            return true;
        }
        else return false;
    }

    public virtual void BeginGrab(Grabbable target)
    {
        currentGrabbed = target;
        currentGrabbed.Grab(this);
        this.OnGrab();
        GrabStateEvent?.Invoke(true);
    }

    public virtual void Release(Vector3 velocity)
    {
        currentGrabbed.Throw(velocity);
        this.OnRelease();
        currentGrabbed = null;
        GrabStateEvent?.Invoke(false);
    }

    protected virtual void OnGrab() { }
    protected virtual void OnRelease() { }
}