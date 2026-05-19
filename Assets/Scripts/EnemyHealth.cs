using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyHealth : NetworkBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private NavMeshAgent navAgent;
    private int startHealth = 500;
    private NetworkVariable<int> enemyCurrentHealth = new NetworkVariable<int>();
    private Animator anim;

    private int fallAmount = 2;

    private bool died = false;

    public override void OnNetworkSpawn()
    {
        anim = GetComponent<Animator>();

        if (IsServer)
        {
            enemyCurrentHealth.Value = startHealth;
        }

        enemyCurrentHealth.OnValueChanged += OnHealthChanged;

        if (IsClient)
        {
            healthBar.maxValue = startHealth;

            healthBar.value = enemyCurrentHealth.Value;
        }
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (healthBar != null)
        {
            healthBar.value = newValue;
        }

        if (newValue <= 0 && !died)
        {
            died = true;
            DeathState();
        }
    }

    public bool IsDead()
    {
        return enemyCurrentHealth.Value <= 0;
    }

    public int HealthLeft()
    {
        return enemyCurrentHealth.Value;
    }

    private void DeathState()
    {
        navAgent.enabled = false;
        transform.position = transform.position - new Vector3(0, fallAmount, 0);
        anim.SetBool("Died", true);
        SoundManager.Instance.PlaySound(SoundType.EnemyDead);
    }

    public void TakeDamage(int amount)
    {
        DamageEnemyServerRPC(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void DamageEnemyServerRPC(int damage)
    {
        enemyCurrentHealth.Value -= damage;
    }
}
