using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Sits on the NPC root GameObject alongside NoiseListenerAuthoring and NPCBrain
// Responsible for:
// 1. Creating and destroying the NPC's listener entity
// 2. Keeping the entity's position in sync with the NavMesh agent every frame
// 3. Reading results back from ECS and passing them to NPCPerception
public class NoiseListenerBridge : MonoBehaviour
{
    private Entity entity;
    private EntityManager entityManager;
    private NPCPerception perception;
    private bool isReady;

    private void Start()
    {
        // Get the ECS world — this always exists when Entities package is installed
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[NoiseListenerBridge] No ECS world found.", this);
            return;
        }

        entityManager = world.EntityManager;
        perception = GetComponentInChildren<NPCPerception>();

        if (perception == null)
        {
            Debug.LogError("[NoiseListenerBridge] No NPCPerception found.", this);
            return;
        }

        // Read hearing radius from the authoring component
        float radius = 10f;
        var authoring = GetComponent<NoiseListenerAuthoring>();
        if (authoring != null)
            radius = authoring.hearingRadius;

        // Create the entity directly — no baking, no SubScene needed
        // We own this entity and are responsible for destroying it in OnDestroy
        entity = entityManager.CreateEntity();

        entityManager.AddComponentData(entity, new NoiseListenerComponent
        {
            Position = new float3(transform.position.x, transform.position.y, transform.position.z),
            Radius = radius,
            Loudness = 0f,
            WasTriggered = false
        });

        isReady = true;
        Debug.Log("[NoiseListenerBridge] Entity created successfully.", this);
    }

    private void Update()
    {
        if (!isReady) return;

        // Read the current component data from the entity
        // Remember — GetComponentData returns a COPY not a reference
        var component = entityManager.GetComponentData<NoiseListenerComponent>(entity);

        // Push the NPC's current world position into the entity
        // The system uses this to calculate distances to noise events
        component.Position = new float3(
            transform.position.x,
            transform.position.y,
            transform.position.z
        );

        // Check if the system detected a noise this frame
        if (component.WasTriggered)
        {
            // Convert float3 back to Vector3 for the MonoBehaviour side
            Vector3 noisePos = new Vector3(
                component.LastHeardPosition.x,
                component.LastHeardPosition.y,
                component.LastHeardPosition.z
            );

            // Pass the result to NPCPerception exactly like the old system did
            perception.HearNoise(noisePos, component.Loudness);

            Debug.Log($"[NoiseListenerBridge] Noise triggered! Loudness: {component.Loudness}");

            // Reset flags so we don't fire HearNoise multiple times for the same event
            component.WasTriggered = false;
            component.Loudness = 0f;
        }

        // Write our changes back — mandatory since GetComponentData returns a copy
        entityManager.SetComponentData(entity, component);
    }

    private void OnDestroy()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        if (entityManager.Exists(entity))
            entityManager.DestroyEntity(entity);
    }
}