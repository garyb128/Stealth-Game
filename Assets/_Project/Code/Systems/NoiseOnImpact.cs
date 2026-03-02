using Unity.VisualScripting;
using UnityEngine;

public class NoiseOnImpact : MonoBehaviour
{
    [SerializeField] float impactRadius = 12f;
    [SerializeField] float impactStrength = 0.30f;
    [SerializeField] float minImpactSpeed = 2f;


    private void OnCollisionEnter(Collision collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed) return;

        Vector3 pos = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        NoiseSystem.Instance.Emit(pos, impactRadius, impactStrength);
    }
}
