using UnityEngine;

public class MovingPlatform : MonoBehaviour, IMovablePlatform
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2.0f;

    private Vector3 target;
    private Transform player;
    private Transform originalParent;

    void Start()
    {
        target = pointB.position;
    }

    void FixedUpdate()
    {
        Vector3 offset = speed * Time.fixedDeltaTime * (target - transform.position).normalized;

        transform.position = transform.position + offset;
        IMovablePlatform.DoAnchorMove(this, offset);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            target = target == pointA.position ? pointB.position : pointA.position;
        }
    }
}