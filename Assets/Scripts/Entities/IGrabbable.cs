using UnityEngine;
using UnityEngine.Animations;

public interface IGrabbable
{
    public bool Grab();
    public void Throw(Vector3 velocity);
    public void Release();



    public IGrabbable This => this;
    public Transform transform { get; }
    public GameObject gameobject { get; }
    public Rigidbody rigidBody { get; }
    public Collider collider { get; }

    public bool IsGrabbable { get; }
    public float AdditionalThrowDistance { get; }
    public Vector3 HeldOffset { get; }

    public void SetVelocity(Vector3 velocity);

}
