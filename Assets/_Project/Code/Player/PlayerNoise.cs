using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] float walkRadius = 4f;
    [SerializeField] float sprintRadius = 8f;
    [SerializeField] float crouchRadius = 2f;

    public void EmitFootstep(bool sprinting, bool crouching)
    {
        float radius = sprinting ? sprintRadius : (crouching ? crouchRadius : walkRadius);
        float loudness = sprinting ? 1f : (crouching ? 0.25f : 0.5f);

        NoiseSystem.Instance.Emit(transform.position, radius, loudness);
    }

    // Emits a noise with intensity and how far the sound can be heard from
    public void EmitNoise(float intensity01, float hearingRadius)
    {
        NoiseSystem.Instance.Emit(transform.position, hearingRadius, Mathf.Clamp01(intensity01));
    }

}
