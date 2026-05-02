using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NetworkBehaviour
{
    [SerializeField] private Transform[] patrolPoints;

    private float attackRange = 6;
    private float sightRange = 17.5f;

    private int power;

    private NavMeshAgent thisEnemy;
    private Animator anim;
    private List<GameObject> playerPos = new List<GameObject>();

    private void Start()
    {
        thisEnemy = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!IsServer) return;

        try
        {
            playerPos = NetworkManager.Singleton.ConnectedClientsList
                .Select(client => client.PlayerObject.gameObject)
                .OrderBy(player => DistanceToPlayer(player))
                .ToList();
        }
        catch
        {
            playerPos.Clear();
        }

        if (playerPos.Count == 0)
            return;

        Debug.Log(playerPos.Count);
        Debug.Log(playerPos[0].name);

        float distanceFromPlayer = DistanceToPlayer(playerPos[0]);

        if (PlayerDead())
        {
            thisEnemy.isStopped = true;
            return;
        }

        // CHASE
        if (distanceFromPlayer <= sightRange && distanceFromPlayer > attackRange)
        {
            anim.SetBool("Attacking", false);

            ChasePlayer();
        }

        // PATROL
        else if (distanceFromPlayer > sightRange)
        {
            anim.SetBool("Attacking", false);

            Patrol();
        }

        // ATTACK
        else if (distanceFromPlayer <= attackRange && !PlayerDead())
        {
            thisEnemy.isStopped = true;
            anim.SetBool("Walking", false);
            anim.SetBool("Attacking", true);
        }
    }

    // ANIMATION EVENT
    private void DamagePlayer()
    {
        if (playerPos.Count > 0 && DistanceToPlayer(playerPos[0]) <= attackRange)
        {
            power = Random.Range(13, 17);
            playerPos[0].GetComponent<PlayerHealth>().TakeDamage(power);
        }
    }

    private bool PlayerDead()
    {
        PlayerHealth health = playerPos[0].GetComponent<PlayerHealth>();
        return health != null && health.isDead;
    }

    private float DistanceToPlayer(GameObject player)
    {
        return Vector3.Distance(player.transform.position, this.transform.position);
    }

    private void Patrol()
    {
        if (!thisEnemy.pathPending && thisEnemy.remainingDistance < 0.5f)
        {
            thisEnemy.isStopped = false;
            anim.SetBool("Walking", true);

            int point = Random.Range(0, patrolPoints.Length);
            thisEnemy.SetDestination(patrolPoints[point].position);
        }
    }

    private void ChasePlayer()
    {
        anim.SetBool("Walking", true);

        thisEnemy.isStopped = false;
        thisEnemy.SetDestination(playerPos[0].transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, attackRange);
    }
}