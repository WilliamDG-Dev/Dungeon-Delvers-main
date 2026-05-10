using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    private Slider healthBar;
    private int startHealth = 100;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    private NavMeshObstacle navObstacle;
    private Animator anim;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = startHealth;
        }

        anim = GetComponent<Animator>();
        navObstacle = GetComponent<NavMeshObstacle>();

        currentHealth.OnValueChanged += OnHealthChanged;

        if (IsOwner)
        {
            healthBar = GameObject.Find("Health").GetComponentInChildren<Slider>();
            healthBar.maxValue = startHealth;

            healthBar.value = currentHealth.Value;
        }
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (IsOwner && healthBar != null)
        {
            healthBar.value = newValue;
        }

        if (newValue <= 0)
        {
            DeathState();
        }
    }

    void Start()
    {
        if (!IsOwner) return;

    }

    public bool IsDead()
    {
        return currentHealth.Value <= 0;
    }

    private void DeathState()
    {
        anim.SetBool("Died", true);
        navObstacle.enabled = false;        
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        currentHealth.Value -= amount;
    }

    public int ReturnCurrentHealth()
    {
        return currentHealth.Value;
    }
}
