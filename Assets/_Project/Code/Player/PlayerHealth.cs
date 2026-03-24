using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    int maxHealth = 100;
    [HideInInspector] public int currentHealth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        Debug.Log($"SHOT! HP:{currentHealth}");

        if(currentHealth < 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        // Player has died, call gameover
        GameManager.Instance.Lose();
    }
}
