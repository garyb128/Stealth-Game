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
        // Run FOV check job for all NPCs in parallel
        var fovJob = new FOVCheckJob();
        var fovHandle = fovJob.ScheduleParallel(state.Dependency);
        fovHandle.Complete();

        // Collect all vision queries into a native array
        var query = SystemAPI.QueryBuilder().WithAll<VisionComponent>().Build(); // Query for all entities with VisionComponent

        int count = query.CalculateEntityCount();

        if (count == 0) return;

        // Allocate arrays for commands and results
        // One command and one result per NPC
        var commands = new NativeArray<RaycastCommand>(count, Allocator.TempJob);
        var results = new NativeArray<RaycastHit>(count, Allocator.TempJob);

        // Populate commands from component data

        // We need to read component data to build the commands
        // This runs on the main thread but is just reading data, not doing physics
        var visionComponents = query.ToComponentDataArray<VisionComponent>(Allocator.TempJob);

        for(int i = 0; i < count; i++)
        {
            var vision = visionComponents[i];

            if (!vision.IsValid || !vision.CanPotentiallySee)
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
                vision.RayOrigin,
                vision.RayDirection,
                new QueryParameters(vision.ObstructionMask),
                vision.RayDistance
                );
        }

        visionComponents.Dispose();

        // Schedule the job

        // ScheduleBatch takes all commands and fires them in parallel
        // Second parameter (1) is minimum batch size per job
        // Lower = more parallelism, higher = less job scheduling overhead
        // 1 is fine since raycasts are expensive enough to warrant individual jobs
        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(commands, results,1,state.Dependency);

        // Wait for raycasts to complete before writing results back
        raycastHandle.Complete();

        // Write results back to components

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

    [BurstCompile]
    public partial struct FOVCheckJob: IJobEntity
    {
        public void Execute(ref VisionComponent vision)
        {
            if (!vision.IsValid)
            {
                vision.InRange = false;
                vision.InFOV = false;
                vision.CanPotentiallySee = false;
                return;
            }

            // Range check
            float3 toTarget = vision.TargetPosition - vision.NPCPosition;
            float distSqr = math.lengthsq(toTarget);
            vision.InRange = distSqr <= (vision.ViewDistance * vision.ViewDistance);

            // Flatten for horizontal angle check
            float3 toTargetFlat = new float3(toTarget.x, 0f, toTarget.z);
            float3 forwardFlat = new float3(vision.NPCForward.x, 0f, vision.NPCForward.z);

            if(math.lengthsq(toTargetFlat) < 0.01f)
            {
                // Target is directly above or below - consider in FOV
                vision.InFOV = true;
            }
            else
            {
                toTargetFlat = math.normalize(toTargetFlat);
                forwardFlat = math.normalize(forwardFlat);

                // Dot product gives us cos(angle) - convert to degrees
                float dot = math.clamp(math.dot(forwardFlat, toTargetFlat), -1f,1f);
                float angle = math.degrees(math.acos(dot));

                // Vertical check
                float verticalDelta = math.abs(vision.TargetPosition.y - vision.NPCPosition.y);
                bool withinVertical = verticalDelta <= vision.VerticalLimit;

                vision.InFOV = withinVertical && (angle <= (vision.FOVDegrees * 0.5f));
            }

            vision.CanPotentiallySee = vision.InRange && vision.InFOV;
        }
    }
}