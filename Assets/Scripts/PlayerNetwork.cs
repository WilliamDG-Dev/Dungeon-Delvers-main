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
    
    private Vector3 spherePos;
    private float offset = 1;
    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    private void Start()
    {
        if (!IsOwner) return;

        anim = GetComponent<Animator>();
        playerHealthScript = gameObject.GetComponent<PlayerHealth>();
        cameraTarget = FindFirstObjectByType<CinemachineCamera>();
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        cameraTarget.Target.TrackingTarget = targetTransform;
    }
    private void Update()
    {
        if (!IsOwner) return;
        
        spherePos = new Vector3(transform.position.x, transform.position.y + offset, transform.position.z);

        MouseActions();
        
        if(!playerHealthScript.IsDead())
        {
            PlayerMove();
        }
    }

    private void MouseActions()
    {
        anim.SetBool("AutoAttack", Input.GetMouseButton(0));

        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
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

    private bool Grounded()
    {
        Collider[] colliders = Physics.OverlapSphere(spherePos, 1);

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.tag == "Ground")
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(spherePos, 1);
    }
}
