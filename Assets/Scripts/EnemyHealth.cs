using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyHealth : NetworkBehaviour
{
    private Slider healthBar;
    private int startHealth = 500;
    private NetworkVariable<int> enemyCurrentHealth = new NetworkVariable<int>();
    private Animator anim;

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
            healthBar = GameObject.Find("EnemyHealth").GetComponentInChildren<Slider>();
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

        if (newValue <= 0)
        {
            Debug.Log("Enemy Died");
            //DeathState();
        }
    }

    public bool IsDead()
    {
        return enemyCurrentHealth.Value <= 0;
    }

    private void DeathState()
    {
        anim.SetBool("Died", true);
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        enemyCurrentHealth.Value -= amount;
    }
}
