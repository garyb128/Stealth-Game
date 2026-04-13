using UnityEngine;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{
    // Set by the weapon when spawned
    public float damage;
    public GameObject owner;
    public LayerMask hitMask;
    public float range;
    public UnityEvent OnHitEvent;

    private Vector3 startPosition;

    private void Start() { }
    private void Update() { }   // Destroy if traveled beyond range

    private void OnCollisionEnter(Collision collision) { }
    private void OnTriggerEnter(Collider other) { }

    // Applies damage to whatever was hit
    private void ApplyDamage(GameObject target) { }
}