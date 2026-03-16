using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.LightTransport;

// Sits on the NPC root GameObject alongside VisionRaycastAuthoring
// Responsible for:
// 1. Creating and destroying the NPC's vision raycast entity
// 2. Writing query data to the entity every frame (origin, direction, distance)
// 3. Reading the raycast result back and passing it to NPCPerception
public class VisionBridge: MonoBehaviour
{
    Entity entity;
    EntityManager entityManager;
    NPCPerception perception;
    bool isReady;

    private void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[VisionRaycastBridge] No ECS world found.", this);
            return;
        }

        entityManager = world.EntityManager;
        perception = GetComponentInChildren<NPCPerception>();

        if (perception == null)
        {
            Debug.LogError("[VisionRaycastBridge] No NPCPerception found.", this);
            return;
        }

        // Read obstruction mask from authoring component
        int obstructionMask = 0;
        var authoring = GetComponent<VisionAuthoring>();
        if (authoring != null)
            obstructionMask = authoring.obstructionMask.value;

        // Create the entity directly
        entity = entityManager.CreateEntity();

        entityManager.AddComponentData(entity, new VisionComponent
        {
            Origin = float3.zero,
            Direction = math.forward(),
            Distance = 0f,
            ObstructionMask = obstructionMask,
            HitSomething = false,
            IsValid = false,
        });

        isReady = true;
        Debug.Log("[VisionRaycastBridge] Entity Created successfully", this);
    }

 private void Update()
    {
        if (!isReady) return;

        var component = entityManager.GetComponentData<VisionComponent>(entity);

        // Ask NPCPerception for the current vision query data
        // If it has a valid target and eyes, write the query — otherwise mark invalid
        if (perception.HasVisionQuery(out Vector3 origin, out Vector3 direction, out float distance))
        {
            component.Origin = new float3(origin.x, origin.y, origin.z);
            component.Direction = new float3(direction.x, direction.y, direction.z);
            component.Distance = distance;
            component.IsValid = true;
        }
        else
        {
            component.IsValid = false;
        }

        // Read result back from last frame's raycast
        // HitSomething is written by VisionRaycastSystem
        perception.SetRaycastResult(component.HitSomething);

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