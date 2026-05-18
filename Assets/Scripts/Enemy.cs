using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Enemy : NetworkBehaviour
{
    [SerializeField] private Transform[] patrolPoints;

    [SerializeField] private NavMeshAgent thisEnemy;
    [SerializeField] private Animator anim;

    private float attackRange = 8;
    private float sightRange = 16;

    private int power;

    private GameObject currentTarget;
    private List<GameObject> targets = new List<GameObject>();

    private void Update()
    {
        if (!IsServer) return;

        FindPlayers();

        if (!anim.GetBool("Died"))
        {
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
    }

    // ANIMATION EVENT
    private void DamagePlayer()
    {
        if (currentTarget != null && DistanceToPlayer(currentTarget) <= attackRange)
        {
            power = Random.Range(13, 17);
            PlayerHealth playerHP = currentTarget.GetComponent<PlayerHealth>();
            PlayerNetwork player = currentTarget.GetComponent<PlayerNetwork>();
            if (playerHP.HealthLeft() <= power && targets.Count == 1)
            {
                player.AllPlayersDead();
            }
            playerHP.TakeDamage(power);
        }
    }

    private void FindPlayers()
    {
        try
        {
            targets = NetworkManager.Singleton.ConnectedClientsList
                .Select(client => client.PlayerObject.gameObject)
                .Where(player =>
                {
                    PlayerHealth health = player.GetComponent<PlayerHealth>();

                    return health != null && !health.IsDead();
                })
                .OrderBy(player => DistanceToPlayer(player))
                .ToList();

            currentTarget = targets[0];
        }
        catch
        {
            currentTarget = null;
            targets.Clear();
        }
    }

    private float DistanceToPlayer(GameObject player)
    {
        return Vector3.Distance(player.transform.position, this.transform.position);
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