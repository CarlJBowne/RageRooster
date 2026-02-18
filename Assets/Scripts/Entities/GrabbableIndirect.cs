using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableIndirect : Indirecter<Grabbable>
{
    public override Grabbable Get() => target;

    public Grabbable target;
}

public abstract class Indirecter<T> : MonoBehaviour where T : MonoBehaviour
{
    public abstract T Get();
}