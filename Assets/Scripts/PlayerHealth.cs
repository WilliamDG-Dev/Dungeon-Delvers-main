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

        if (IsOwner)
        {
            healthBar = GameObject.Find("Health").GetComponentInChildren<Slider>();
            healthBar.maxValue = startHealth;

            currentHealth.OnValueChanged += OnHealthChanged;
            healthBar.value = currentHealth.Value;
        }
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (IsOwner && healthBar != null)
        {
            healthBar.value = newValue;
        }
    }

    void Start()
    {
        if (!IsOwner) return;

        anim = GetComponent<Animator>();
        anim.SetBool("Died", false);

        navObstacle = GetComponent<NavMeshObstacle>();
        navObstacle.enabled = true;
    }

    public bool IsDead()
    {
        return currentHealth.Value <= 0;
    }

    public void DeathState()
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
