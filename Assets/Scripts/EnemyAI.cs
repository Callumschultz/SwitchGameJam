using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple two-state enemy AI:
/// - Patrol: walk to random NavMesh points within a radius around its spawn location
/// - Chase: if player is within detection range AND enemy is facing the player, chase until player leaves range
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Chase
    }

    [Header("Targeting")]
    [SerializeField] public float detectionRange = 8f;
    [Tooltip("Facing cone for chase. 90 means +/-90 degrees from forward.")]
    [SerializeField] public float facingAngle = 90f;

    [Header("Patrol")]
    [SerializeField] public float patrolRadius = 5f;
    [SerializeField] public float waitMinSeconds = 1f;
    [SerializeField] public float waitMaxSeconds = 2f;
    [SerializeField] public float navMeshSampleMaxDistance = 2f;

    [Header("Agent Thresholds")]
    [SerializeField] public float arriveVelocitySqrThreshold = 0.01f;

    private State state = State.Patrol;

    private NavMeshAgent agent;
    private Transform player;
    private Vector3 spawnPoint;

    private Vector3 patrolTarget;
    private float waitUntilTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnPoint = transform.position;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        // Start patrolling immediately.
        PickNewPatrolPoint();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // State transitions.
        if (state == State.Patrol)
        {
            if (distanceToPlayer <= detectionRange && IsFacingPlayer())
            {
                SwitchToChase();
            }
            else
            {
                HandlePatrol();
            }
        }
        else if (state == State.Chase)
        {
            if (distanceToPlayer > detectionRange)
            {
                SwitchToPatrol();
            }
            else
            {
                HandleChase();
            }
        }
    }

    private void SwitchToChase()
    {
        if (state == State.Chase)
        {
            return;
        }

        state = State.Chase;
        agent.isStopped = false;
    }

    private void SwitchToPatrol()
    {
        if (state == State.Patrol)
        {
            return;
        }

        state = State.Patrol;
        PickNewPatrolPoint();
    }

    private void HandlePatrol()
    {
        // Wait at patrol point.
        if (Time.time < waitUntilTime)
        {
            return;
        }

        // If agent is effectively at destination, pick next.
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && agent.velocity.sqrMagnitude <= arriveVelocitySqrThreshold)
        {
            PickNewPatrolPoint();
        }
    }

    private void HandleChase()
    {
        agent.SetDestination(player.position);
    }

    private bool IsFacingPlayer()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toPlayer);
        return angle <= facingAngle; // 90 degree cone
    }

    private void PickNewPatrolPoint()
    {
        state = State.Patrol;

        // Random point within radius around spawn position.
        Vector3 randomPoint = spawnPoint + Random.insideUnitSphere * patrolRadius;
        randomPoint.y = spawnPoint.y;

        // Sample on NavMesh to avoid falling off map.
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleMaxDistance, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);

            // Wait 1-2 seconds after arriving (implemented by setting waitUntilTime now).
            waitUntilTime = Time.time + Random.Range(waitMinSeconds, waitMaxSeconds);
        }
        else
        {
            // If sampling fails, try again next frame.
            agent.SetDestination(transform.position);
            waitUntilTime = Time.time + 0.25f;
        }
    }

    /// <summary>
    /// Called by PlayerAttack when this enemy is hit.
    /// </summary>
    public void TakeDamage()
    {
        Destroy(gameObject);
    }
}

