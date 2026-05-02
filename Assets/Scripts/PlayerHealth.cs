using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    private Slider healthBar;
    private int startHealth = 100;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    public bool isDead;


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

    void Update()
    {
        if (!IsOwner) return;

        if (currentHealth.Value <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log("The player has died!");
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        currentHealth.Value -= amount;
    }
}
