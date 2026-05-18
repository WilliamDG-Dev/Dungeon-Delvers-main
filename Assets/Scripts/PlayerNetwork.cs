using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform targetTransform;

    private Animator anim;
    private PlayerHealth playerHealthScript;

    private CinemachineCamera cameraTarget;
    private Transform cam;

    private GameObject winScreen;
    private GameObject loseScreen;
    private GameObject loadingScreen;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 8f;
    [SerializeField] private float rayDistance = 2f;

    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 1f;

    private bool canAttack = true;
    private bool canBlock = true;

    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    private void Start()
    {
        anim = GetComponent<Animator>();
        playerHealthScript = GetComponent<PlayerHealth>();

        if (IsOwner)
        {
            winScreen = GameObject.Find("Won");
            loseScreen = GameObject.Find("Lost");
            loadingScreen = GameObject.Find("Loading");

            if (winScreen != null)
                winScreen.SetActive(false);

            if (loseScreen != null)
                loseScreen.SetActive(false);

            if (loadingScreen != null)
                loadingScreen.SetActive(false);

            SoundManager.Instance.PlayMusic(SoundType.BattleMusic);

            cameraTarget = FindFirstObjectByType<CinemachineCamera>();

            if (cameraTarget != null)
            {
                cameraTarget.Target.TrackingTarget = targetTransform;
            }

            cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (!playerHealthScript.IsDead())
        {
            Actions();
            PlayerMove();
        }
    }

    private void Actions()
    {
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            anim.SetBool("AutoAttack", true);

            StartCoroutine(AttackCooldown());
        }
        else
        {
            anim.SetBool("AutoAttack", false);
        }

        if (Input.GetMouseButtonDown(2) && canBlock)
        {
            StartCoroutine(BlockInterval(4f, 5f));
        }

        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }
    public void DamageEnemy()
    {
        if (!IsOwner)
            return;

        DamageEnemyServerRpc();
    }

    [ServerRpc]
    private void DamageEnemyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(senderClientId))
            return;

        GameObject player =
            NetworkManager.Singleton
            .ConnectedClients[senderClientId]
            .PlayerObject
            .gameObject;

        if (player == null)
            return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
            return;

        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
                continue;

            float dist = Vector3.Distance(
                player.transform.position,
                enemy.transform.position
            );

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy == null)
            return;

        if (closestDistance > attackRange)
            return;

        EnemyHealth enemyHP = closestEnemy.GetComponent<EnemyHealth>();

        if (enemyHP == null)
            return;

        int power = Random.Range(8, 14);

        enemyHP.TakeDamage(power);

        PlayEnemyHitSoundClientRpc();

        if (enemyHP.HealthLeft() <= 0)
        {
            PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);

            foreach (PlayerNetwork p in players)
            {
                p.ShowWinClientRpc();
            }
        }
    }

    [ClientRpc]
    private void PlayEnemyHitSoundClientRpc()
    {
        SoundManager.Instance.PlaySound(SoundType.EnemyInjured);
    }

    private void AutoAttackStart()
    {
        if (!IsOwner)
            return;

        SoundManager.Instance.PlaySound(SoundType.PlayerAttack);
    }

    private void PlayerMove()
    {
        float horiz = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horiz, 0, vert).normalized;

        // JUMP
        if (Input.GetKeyDown(KeyCode.Space) && Grounded())
        {
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }

        anim.SetBool("Moving", direction.magnitude >= 0.1f);

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle =
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                + cam.eulerAngles.y;

            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                turnSmoothTime
            );

            rb.MoveRotation(Quaternion.Euler(0, angle, 0));

            Vector3 moveDir =
                Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            rb.MovePosition(
                rb.position
                + moveDir.normalized * moveSpeed * Time.deltaTime
            );
        }
    }

    private IEnumerator BlockInterval(float blockTime, float cooldown)
    {
        canBlock = false;

        anim.SetBool("Blocking", true);

        yield return new WaitForSeconds(blockTime);

        anim.SetBool("Blocking", false);

        yield return new WaitForSeconds(cooldown);

        canBlock = true;
    }

    private bool Grounded()
    {
        return Physics.Raycast(
            transform.position + new Vector3(0, 1, 0),
            Vector3.down,
            rayDistance,
            LayerMask.GetMask("Ground")
        );
    }
    public void AllPlayersDead()
    {
        if (!IsServer)
            return;

        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(
            FindObjectsSortMode.None
        );

        foreach (PlayerNetwork player in players)
        {
            player.ShowLoseClientRpc();
        }
    }

    [ClientRpc]
    public void ShowWinClientRpc()
    {
        StartCoroutine(GameWon(2f));
    }

    [ClientRpc]
    public void ShowLoseClientRpc()
    {
        StartCoroutine(GameLost(2f));
    }

    private IEnumerator GameWon(float seconds)
    {
        Debug.Log("YOU WON");

        yield return new WaitForSeconds(seconds);

        if (IsOwner && winScreen != null)
        {
            winScreen.SetActive(true);
        }
    }

    private IEnumerator GameLost(float seconds)
    {
        Debug.Log("ALL PLAYERS DEAD");

        yield return new WaitForSeconds(seconds);

        if (IsOwner && loseScreen != null)
        {
            loseScreen.SetActive(true);
        }
    }
}