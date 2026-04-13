using UnityEngine;

public class Hitbox : MonoBehaviour, IDamageable
{
    [Tooltip("Reference to the root Health component (usually on parent)")]
    public Health rootHealth;

    [Tooltip("Damage multiplier for this body part (e.g., headshot = 2.0)")]
    public float damageMultiplier = 1f;

    // Passes damage to the root health with multiplier applied
    public void TakeDamage(float amount, GameObject source = null) { }
}