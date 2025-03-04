using UnityEngine;

[System.Serializable]
public class Grabber
{
    public Grabbable currentGrabbed;
    public UltEvents.UltEvent<bool> GrabStateEvent;

    public bool AttemptGrab(GameObject target, out Grabbable result, bool doGrab = true)
    {
        if (Grabbable.Grab(target, out  result))
        {
            if(doGrab) BeginGrab(result);
            return true;
        }
        else return false;
    }

    public void BeginGrab(Grabbable target)
    {
        currentGrabbed = target;
        currentGrabbed.Grab(this);
        this.OnGrab();
        GrabStateEvent?.Invoke(true);
    }

    public void Release()
    {
        currentGrabbed.Release();
        this.OnRelease();
        currentGrabbed = null;
        GrabStateEvent?.Invoke(false);
    }

    protected virtual void OnGrab() { }
    protected virtual void OnRelease() { }
}