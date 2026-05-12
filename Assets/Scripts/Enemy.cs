using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Enemy : NetworkBehaviour
{
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Slider enemyHealth;
    private int startHealth = 5000;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    private float attackRange = 6;
    private float sightRange = 15;

    private int power;

    private GameObject currentTarget;

    private NavMeshAgent thisEnemy;
    private Animator anim;

    private void Start()
    {
        thisEnemy = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        currentHealth.Value = startHealth;
        enemyHealth.value = currentHealth.Value;

        if (!IsServer) return;
    }

    private void Update()
    {
        if (!IsServer) return;

        FindPlayers();

        if (currentTarget == null)
        {
            Patrol();
        }
        else
        {
            float distanceFromPlayer = DistanceToPlayer(currentTarget);

            if (distanceFromPlayer <= sightRange && distanceFromPlayer > attackRange)
            {
                ChasePlayer();
            }

            else if (distanceFromPlayer > sightRange)
            {
                Patrol();
            }

            else if (distanceFromPlayer <= attackRange)
            {
                thisEnemy.isStopped = true;

                anim.SetBool("Walking", false);

                transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));

                anim.SetBool("Attacking", true);
            }
        }
    }

    // ANIMATION EVENT
    private void DamagePlayer()
    {
        if (currentTarget != null && DistanceToPlayer(currentTarget) <= attackRange)
        {
            power = Random.Range(13, 17);
            PlayerHealth playerHP = currentTarget.GetComponent<PlayerHealth>();
            playerHP.TakeDamage(power);
        }
    }

    private void FindPlayers()
    {
        try
        {
            currentTarget = NetworkManager.Singleton.ConnectedClientsList
                .Select(client => client.PlayerObject.gameObject)
                .Where(player =>
                {
                    PlayerHealth health = player.GetComponent<PlayerHealth>();

                    return health != null && !health.IsDead();
                })
                .OrderBy(player => DistanceToPlayer(player))
                .FirstOrDefault();
        }
        catch
        {
            currentTarget = null;
        }
    }

    private float DistanceToPlayer(GameObject player)
    {
        return Vector3.Distance(player.transform.position, this.transform.position);
    }

    public bool AllPlayersDead()
    {
        return currentTarget == null;
    }

    private void Patrol()
    {
        anim.SetBool("Attacking", false);
        thisEnemy.isStopped = false;
        anim.SetBool("Walking", true);

        if (!thisEnemy.pathPending && thisEnemy.remainingDistance < 0.5f)
        {
            int point = Random.Range(0, patrolPoints.Length);
            thisEnemy.SetDestination(patrolPoints[point].position);
        }
    }

    private void ChasePlayer()
    {
        anim.SetBool("Attacking", false);
        anim.SetBool("Walking", true);

        thisEnemy.isStopped = false;
        thisEnemy.SetDestination(currentTarget.transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, attackRange);
    }
}