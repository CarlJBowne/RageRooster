using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float detectionRadius = 10f;
    public float chaseSpeed = 5f;
    public float patrolSpeed = 2f;
    public LayerMask playerLayer;

    private int currentPatrolIndex;
    private NavMeshAgent agent;
    private Transform player;
    private bool isChasing;
    Animator _animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        _animator = GetComponent<Animator>();

        Patrol();
    }

    void Update()
    {
        DetectPlayer();

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {

        _animator.SetBool("isWalking", true);
        _animator.SetBool("isRunning", false);
        // Patrol to the next point
        if (agent.remainingDistance < 0.5f && !agent.pathPending)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.destination = patrolPoints[currentPatrolIndex].position;
            agent.speed = patrolSpeed;
            Debug.Log("Patrolling to point: " + currentPatrolIndex);
        }
    }

    void ChasePlayer()
    {

        _animator.SetBool("isWalking", false);
        _animator.SetBool("isRunning", true);
        // Chase the player when detected
        agent.destination = player.position;
        agent.speed = chaseSpeed;
        Debug.Log("Chasing player");
    }

    void DetectPlayer()
    {
        // Check if the player is within the detection radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // Check if there is a clear line of sight to the player
                Vector3 directionToPlayer = (hitCollider.transform.position - transform.position).normalized;
                if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, detectionRadius))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        isChasing = true;
                        Debug.Log("Player detected, starting chase");
                        return;
                    }
                }
            }
        }
        // If no player detected, stop chasing
        isChasing = false;
        Debug.Log("Player not detected, continue patrolling");
    }

    // Optional: Debug visualization for detection radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}