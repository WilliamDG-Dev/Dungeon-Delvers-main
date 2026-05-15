using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform targetTransform;
    private Animator anim;
    private PlayerHealth playerHealthScript;
    private CinemachineCamera cameraTarget;
    private Transform cam;
    private float moveSpeed = 5;
    private float jumpHeight = 8;
    private float rayDistance = 2;
    
    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    private float attackRange = 6;
    private bool canBlock = true;

    private int power;

    private void Start()
    {
        if (!IsOwner) return;

        SoundManager.Instance.PlayMusic(SoundType.BattleMusic);
        anim = GetComponent<Animator>();
        playerHealthScript = gameObject.GetComponent<PlayerHealth>();
        cameraTarget = FindFirstObjectByType<CinemachineCamera>();
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        cameraTarget.Target.TrackingTarget = targetTransform;
    }
    private void Update()
    {
        if (!IsOwner) return;
        
        if(!playerHealthScript.IsDead())
        {
            Actions();
            PlayerMove();
        }
    }

    private void Actions()
    {
        anim.SetBool("AutoAttack", Input.GetMouseButtonDown(0));
            
        if (Input.GetMouseButtonDown(2) && canBlock)
        {
            StartCoroutine(BlockInterval(4, 5));
        }
        else if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void GameWon()
    {
        Debug.Log("You Won");
    }

    private void DamageEnemy()
    {
        if (DistanceCheck(FindClosestEnemy()) <= attackRange)
        {
            power = Random.Range(8, 14);
            EnemyHealth enemyHP = FindClosestEnemy().GetComponent<EnemyHealth>();
            if (enemyHP.HealthLeft() <= power)
            {
                GameWon();
            }
            enemyHP.TakeDamage(power);
            SoundManager.Instance.PlaySound(SoundType.EnemyInjured);
        }
    }

    private void AutoAttackStart()
    {
        SoundManager.Instance.PlaySound(SoundType.PlayerAttack);
    }

    private GameObject FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.OrderBy(enemy => DistanceCheck(enemy)).FirstOrDefault();
    }

    private float DistanceCheck(GameObject otherTarget)
    {
        return Vector3.Distance(transform.position, otherTarget.transform.position);
    }

    private void PlayerMove()
    {
        float horiz = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horiz, 0, vert).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && Grounded()) rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);


        anim.SetBool("Moving", direction.magnitude >= 0.1f);

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            rb.MoveRotation(Quaternion.Euler(0, angle, 0));

            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            rb.MovePosition(rb.position + moveDir.normalized * moveSpeed * Time.deltaTime);
        }
    }

    private IEnumerator BlockInterval(int blockTime, int cooldown)
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
        return Physics.Raycast(transform.position + new Vector3(0,1,0), Vector3.down, rayDistance, LayerMask.GetMask("Ground"));
    }
}
