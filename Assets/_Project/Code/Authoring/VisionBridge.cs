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
            RayOrigin = float3.zero,
            RayDirection = math.forward(),
            RayDistance = 0f,
            ObstructionMask = obstructionMask,
            HitSomething = false,
            IsValid = false,
        });

        isReady = true;
        Debug.Log("[VisionRaycastBridge] Entity Created successfully", this);
    }

    void Update()
    {
        if (!isReady) return;

        var component = entityManager.GetComponentData<VisionComponent>(entity);

        // Write FOV and raycast query data from NPCPerception
        if (perception.HasVisionQuery(
            out Vector3 origin,
            out Vector3 direction,
            out float distance,
            out Vector3 npcPosition,
            out Vector3 npcForward,
            out Vector3 targetPosition,
            out float viewDistance,
            out float fovDegrees))
        {
            // FOV inputs
            component.NPCPosition = new float3(npcPosition.x, npcPosition.y, npcPosition.z);
            component.NPCForward = new float3(npcForward.x, npcForward.y, npcForward.z);
            component.TargetPosition = new float3(targetPosition.x, targetPosition.y, targetPosition.z);
            component.ViewDistance = viewDistance;
            component.FOVDegrees = fovDegrees;
            component.VerticalLimit = 3.0f;

            // Raycast inputs
            component.RayOrigin = new float3(origin.x, origin.y, origin.z);
            component.RayDirection = new float3(direction.x, direction.y, direction.z);
            component.RayDistance = distance;
            component.IsValid = true;
        }
        else
        {
            component.IsValid = false;
        }

        // Read FOV results back from last frame's job
        perception.SetFOVResult(component.InRange, component.InFOV);

        // Read raycast result back from last frame
        perception.SetRaycastResult(component.HitSomething);

        entityManager.SetComponentData(entity, component);
    }

    void OnDestroy()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        if (entityManager.Exists(entity))
            entityManager.DestroyEntity(entity);
    }
}