using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] float walkRadius = 4f;
    [SerializeField] float sprintRadius = 8f;
    [SerializeField] float crouchRadius = 2f;

    [Header("Landing")]
    [SerializeField] float landingRadius = 10f;

    public void EmitFootstep(bool sprinting, bool crouching)
    {
        float radius = sprinting ? sprintRadius : (crouching ? crouchRadius : walkRadius);
        float loudness = sprinting ? 1f : (crouching ? 0.25f : 0.5f);

        NoiseSystem.Instance.Emit(transform.position, radius, loudness);
    }

    public void EmitLanding(float intensity01)
    {
        NoiseSystem.Instance.Emit(transform.position, landingRadius, Mathf.Clamp01(intensity01));
    }
}
