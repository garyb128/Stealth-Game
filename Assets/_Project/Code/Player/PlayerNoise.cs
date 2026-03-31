using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    public void EmitFootstep(bool sprinting, bool crouching)
    {
        // For now use 1 as the base. Change into a more sophisticated loudness calculation
        // (Checking movement speed, if jumping or falling, material type of ground being walked on)
        float loudness = sprinting ? 1f : (crouching ? 0.25f : 0.5f); 

        NoiseSystem.Instance.Emit(transform.position, loudness);
    }

    // Emits a noise with intensity
    public void EmitNoise(float intensity01)
    {
        NoiseSystem.Instance.Emit(transform.position, Mathf.Clamp01(intensity01));
    }

}
