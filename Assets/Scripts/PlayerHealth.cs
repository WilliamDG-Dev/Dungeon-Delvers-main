using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    private Slider healthBar;
    private int startHealth = 100;
    private NetworkVariable<int> playerCurrentHealth = new NetworkVariable<int>();
    private NavMeshObstacle navObstacle;
    private Animator anim;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerCurrentHealth.Value = startHealth;
        }

        anim = GetComponent<Animator>();
        navObstacle = GetComponent<NavMeshObstacle>();

        playerCurrentHealth.OnValueChanged += OnHealthChanged;

        if (IsOwner)
        {
            healthBar = GameObject.Find("Health").GetComponentInChildren<Slider>();
            healthBar.maxValue = startHealth;

            healthBar.value = playerCurrentHealth.Value;
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

    public bool IsDead()
    {
        return playerCurrentHealth.Value <= 0;
    }

    private void DeathState()
    {
        anim.SetBool("Died", true);
        navObstacle.enabled = false;        
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        if (!anim.GetBool("Blocking"))
        {
            playerCurrentHealth.Value -= amount;
        }
    }
}
