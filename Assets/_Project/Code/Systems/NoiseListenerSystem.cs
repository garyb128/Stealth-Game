using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.ShaderGraph.Internal;

[BurstCompile]
public partial struct NoiseListenerSystem : ISystem
{
    // Called once when the system is created
    public void OnCreate(ref SystemState state)
    {
        // Tell the system only to run if there is at least one NoiseEventComponent
        // No point in running if nothing has made a noise
        state.RequireForUpdate<NoiseEventComponent>();
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Step 1 - collect all noise events into a NativeArray

        // Query all noise event entities
        var eventQuery = SystemAPI.QueryBuilder()
            .WithAll<NoiseEventComponent>()
            .Build();

        // Fetch the noise event data into a temporary array
        var noiseEvents = eventQuery.ToComponentDataArray<NoiseEventComponent>(Allocator.Temp);

        // Step 2 - process every listener against every noise event

        // SystemAPI.Query is the clean way to iterate over entities in a system
        // This loops over every entity that has both components
        foreach (var (listener, entity) in
            SystemAPI.Query<RefRW<NoiseListenerComponent>>()
            .WithEntityAccess())
        {
            // RefRW means read/write access — we need to write results back
            // RefRO would be read only

            for (int i = 0; i < noiseEvents.Length; i++)
            {
                UnityEngine.Debug.Log($"[NoiseListenerSystem] Processing {noiseEvents.Length} noise events");

                {
                    NoiseEventComponent noiseEvent = noiseEvents[i];

                    float dist = math.distance(listener.ValueRO.Position, noiseEvent.Position);

                    // Is listener within range of this noise event
                    float hearingRange = math.min(listener.ValueRO.Radius, noiseEvent.Radius);

                    if (dist > hearingRange) continue;

                    // Calculate loudness falloff - 1 at source, 0 at edge of range
                    float t = 1f - math.saturate(dist / hearingRange);
                    float loudness = t * noiseEvent.Loudness;

                    // Write results to the listener component
                    // The bridge will read these back on the Monobehaviour side
                    listener.ValueRW.WasTriggered = true;
                    listener.ValueRW.Loudness = math.max(listener.ValueRW.Loudness, loudness);
                    listener.ValueRW.LastHeardPosition = noiseEvent.Position;
                    // math.max means if multiple noises hit this listener we keep the loudest
                }
            }
        }

        // Step 3 - Clean up noise events

        // Destroy all noise events entities - they've served their purpose
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach(var (noiseEvent, entity) in 
            SystemAPI.Query<NoiseEventComponent>()
            .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        // Playback executes all queued destroy commands
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        noiseEvents.Dispose();
    }
}
