using UnityEngine;
using UnityEngine.Events;

public class ThrowableProjectile : MonoBehaviour
{
    public float damage;
    public GameObject owner;
    public float fuseTime;
    public bool explodesOnImpact;
    public GameObject explosionEffect;
    public float explosionRadius;
    public UnityEvent OnHitEvent;

    private bool hasExploded;

    private void Start() { }
    private void OnCollisionEnter(Collision collision) { }

    public void Explode() { }
}