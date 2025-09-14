using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamageable
{
    // Components
    private NavMeshAgent agent;

    // Enemy stats
    [SerializeField] private float enemyHealth = 100f;

    // Patrol settigns
    [SerializeField] private Transform[] wayPoints;
    [SerializeField] private float waitTimeMin = 1f;
    [SerializeField] private float waitTimeMax = 3f;
    [SerializeField] private float randomOffsetRadius = 1f;

    private int waypointIndex = 0;
    private int direction = 1;
    private Vector3 target;
    private bool isWaiting = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Instellingen voor vloeiende beweging
        agent.autoBraking = false;
        agent.stoppingDistance = 0.2f;

        // Begin bij eerste waypoint
        waypointIndex = 0;
        UpdateDestination();
    }

    private void Update()
    {
        agent.speed = 2f + Mathf.Sin(Time.time * 0.5f) * 0.3f;

        if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartCoroutine(WaitAtWaypoint());
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        agent.isStopped = true;

        float waitTime = Random.Range(waitTimeMin, waitTimeMax);
        yield return new WaitForSeconds(waitTime);

        IterateWaypointIndex();

        if (waypointIndex < wayPoints.Length)
        {
            UpdateDestination();
            agent.isStopped = false;
            isWaiting = false;
        }
    }
    private void UpdateDestination()
    {
        Vector3 offset = new Vector3(
            Random.Range(-randomOffsetRadius, randomOffsetRadius),
            0,
            Random.Range(-randomOffsetRadius, randomOffsetRadius)
        );

        target = wayPoints[waypointIndex].position + offset;
        agent.SetDestination(target);
    }

    private void IterateWaypointIndex()
    {
        waypointIndex++;

        if (waypointIndex >= wayPoints.Length)
        {
            agent.isStopped = true;
            Debug.Log("Enemy heeft laatste waypoint bereikt en stopt.");
            StopAllCoroutines();
            enabled = false;
            return;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        enemyHealth -= damageAmount;
        if (enemyHealth <= 0)
        {
            Debug.Log("Enemy is dood");
            /// logica toevoegen wanneer enemy doodgaat
        }
    }
}
