using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public partial struct VisionSystem: ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Only run if there is at least one VisionComponent in the world
        state.RequireForUpdate<VisionComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Step 1 - collect all vision queries into a native array

        // Query for all entities with VisionComponent
        var query = SystemAPI.QueryBuilder().WithAll<VisionComponent>().Build();

        int count = query.CalculateEntityCount();

        if (count == 0) return;

        // Allocate arrays for commands and results
        // One command and one result per NPC
        var commands = new NativeArray<RaycastCommand>(count, Allocator.TempJob);
        var results = new NativeArray<RaycastHit>(count, Allocator.TempJob);

        // Step 2 - populate commands from component data

        // We need to read component data to build the commands
        // This runs on the main thread but is just reading data, not doing physics
        var visionComponents = query.ToComponentDataArray<VisionComponent>(Allocator.TempJob);

        for(int i = 0; i < count; i++)
        {
            var vision = visionComponents[i];

            if (!vision.IsValid)
            {
                // Invalid query - use a zero length ray that hits nothing
                commands[i] = new RaycastCommand(
                    Vector3.zero,
                    Vector3.forward,
                    new QueryParameters(vision.ObstructionMask),
                    0f
                );
                continue;
            }

            commands[i] = new RaycastCommand(
                vision.Origin,
                vision.Direction,
                new QueryParameters(vision.ObstructionMask),
                vision.Distance
                );
        }

        visionComponents.Dispose();

        // Step 3 - schedule the job

        // ScheduleBatch takes all commands and fires them in parallel
        // Second parameter (1) is minimum batch size per job
        // Lower = more parallelism, higher = less job scheduling overhead
        // 1 is fine since raycasts are expensive enough to warrant individual jobs
        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(commands, results,1,state.Dependency);

        // Wait for raycasts to complete before writing results back
        raycastHandle.Complete();

        // Step 4 - write results back to components

        // Now update each VisionComponent with raycast result
        var entities = query.ToEntityArray(Allocator.Temp);
        var updatedComponents = query.ToComponentDataArray<VisionComponent>(Allocator.Temp);

        for(int i = 0; i < count; i++)
        {
            var vision = updatedComponents[i];

            // A hit is registered if the collider is not null
            vision.HitSomething = results[i].collider != null;

            state.EntityManager.SetComponentData(entities[i], vision);
        }

        entities.Dispose();
        updatedComponents.Dispose();
        commands.Dispose();
        results.Dispose();
    }
}