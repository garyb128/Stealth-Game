using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
    public float maxHealth;
    public float currentHealth { get; private set; }

    public UnityEvent OnDamaged;
    public UnityEvent OnHealed;
    public UnityEvent OnDeath;

    private void Start() { }

    public void TakeDamage(float amount, GameObject source = null) { }
    public void Heal(float amount) { }

    private void Die() { }
}