using UnityEngine;

public class MovingPlatform : MonoBehaviour
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
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.fixedDeltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            target = target == pointA.position ? pointB.position : pointA.position;
        }
    }

    /*
    void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            originalParent = player.parent;
            player.SetParent(transform);

            var playerController = player.GetComponentInChildren<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
        
    }

    void OnTriggerExit(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            player.SetParent(originalParent);
            player = null;

            var playerController = other.GetComponentInChildren<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
        
    }
    */
}