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
    [SerializeField] ParticleSystem partical;

    [SerializeField] private bool isBoss;

    [SerializeField] private float attackRange = 6.5f;
    [SerializeField] private float sightRange = 16;

    private bool attackCooldownActive = false;

    private int power;

    private GameObject currentTarget;
    private List<GameObject> targets = new List<GameObject>();

    private void Update()
    {
        if (!IsServer) return;

        FindPlayers();

        if (!anim.GetBool("Died"))
        {
            if (currentTarget != null)
            {
                float distanceFromPlayer = DistanceToPlayer(currentTarget);

                if (distanceFromPlayer < sightRange)
                {
                    transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
                }

                if (distanceFromPlayer > sightRange)
                {
                    Patrol();
                }

                else if (distanceFromPlayer <= sightRange && distanceFromPlayer > attackRange)
                {
                    ChasePlayer();
                }

                else if (distanceFromPlayer <= attackRange && !attackCooldownActive)
                {
                    thisEnemy.isStopped = true;
                    int random = Random.Range(0, 3);

                    Debug.Log(random);

                    if (random == 2 && isBoss)
                    {
                        LeapAttack();
                    }
                    else
                    {
                        anim.SetBool("Walking", false);

                        anim.SetTrigger("Attacking");
                        StartCoroutine(AttackCooldown(2.5f));
                    }
                }
            }

        }
    }

    private void LeapAttack()
    {
        anim.SetBool("Walking", false);

        anim.SetTrigger("JumpAttack");
        StartCoroutine(AttackCooldown(4));
    }

    private IEnumerator AttackCooldown(float seconds)
    {
        attackCooldownActive = true;
        yield return new WaitForSeconds(seconds);
        attackCooldownActive = false;
    }

    private void DamagePlayer()
    {
        DamageDeal(13, 17);
    }

    private void JumpAttack()
    {
        DamageDeal(20, 30);
        partical.Emit(350);
    }

    private void DamageDeal(int minDamage, int maxDamage)
    {
        if (currentTarget != null && DistanceToPlayer(currentTarget) <= attackRange)
        {
            power = Random.Range(minDamage, maxDamage);
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

            if (DistanceToPlayer(targets[0]) > attackRange)
            {
                currentTarget = targets[0];
            }
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