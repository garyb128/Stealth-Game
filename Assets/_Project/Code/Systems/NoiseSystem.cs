using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance { get; private set; }

    EntityManager entityManager;
    World world;

    [Header("Noise Settings")]
    public float MaxNoiseRadius = 20f; // Radius at 1 loudness

    // These store the last emitted noise for debugging only
    Vector3 debugLastPos;
    float debugLastRadius;
    float debugLastLoudness;

    public float CurrentNoiseLoudness { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;


        // Get a reference to the ECS world and its EntityManager
        world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
    }

    private void Update()
    {
        // Decay back to zero over time
        CurrentNoiseLoudness = Mathf.MoveTowards(CurrentNoiseLoudness, 0f, Time.deltaTime);
    }

    public void Emit(Vector3 pos, float loudness01)
    {
        // Set loudness when noise fires - UI reads this
        CurrentNoiseLoudness = loudness01;

        if (world == null || !world.IsCreated) return;

        float radius = loudness01 * MaxNoiseRadius;

        // Debug info
        debugLastPos = pos;
        debugLastRadius = radius;
        debugLastLoudness = loudness01;

        // Create a new entity to represent this noise event
        // The NoiseListenerSystem will pick this up on the next frame,
        // process it against all listeners, then destroy it
        Entity noiseEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(noiseEntity, new NoiseEventComponent
        {
            Position = new float3(pos.x, pos.y, pos.z),
            Radius = radius,
            Loudness = loudness01
        });
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (debugLastLoudness <= 0f)
            return;

        // Outer hearing radius
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(debugLastPos, debugLastRadius);

        // Inner sphere scaled by loudness
        float scaled = Mathf.Lerp(0.1f, debugLastRadius, debugLastLoudness);
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawSphere(debugLastPos, scaled);

        // Floating label
#if UNITY_EDITOR
        Handles.Label(
            debugLastPos + Vector3.up * 1.5f,
            $"Noise: {debugLastLoudness:F2}\nRadius: {debugLastRadius:F1}"
        );
#endif
    }
#endif
}
