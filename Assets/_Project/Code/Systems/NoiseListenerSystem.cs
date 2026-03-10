using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
        var noiseEvents = eventQuery.ToComponentDataArray<NoiseEventComponent>(Allocator.TempJob);

        // Step 2 - create and schedule the job
        var job = new ProcessNoiseListenersJob 
        {
            // Pass the noise events array to the job as read only
            // [ReadOnly] means all threads can read it simultaneously
            NoiseEvents = noiseEvents
        };

        // Schedule means run on background threads in parallel
        // The JobHandle represents the in-progress job
        // state.Dependency chains this job after any previously scheduled jobs
        // that touch the same data — prevents data races automatically
        var handle = job.ScheduleParallel(state.Dependency);

        // Tell the system this job must complete before the next system runs
        state.Dependency = handle;

        // Complete ensures the job finishes before we move on to destroying events
        // We need to wait because we're about to use EntityManager which requires
        // all jobs touching entity data to be finished first
        handle.Complete();

        // Step 3 - Dispose of array
        noiseEvents.Dispose();

        // Step 4 - Destroy all noise event entities
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach(var (noiseEvent, entity)in
            SystemAPI.Query<NoiseEventComponent>()
            .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    // The job itself — runs once per entity that has NoiseListenerComponent
    // partial is required for IJobEntity — the compiler generates boilerplate code for us
    [BurstCompile]
    public partial struct ProcessNoiseListenersJob: IJobEntity
    {
        // ReadOnly - all parallel threads can read this at the same time without conflict
        [ReadOnly] public NativeArray<NoiseEventComponent> NoiseEvents;

        // Execute is called once per matching entity
        // Unity automatically passes the components we declare as parameters
        // ref means read/write access — we need to write results back to the listener
        public void Execute(ref NoiseListenerComponent listener)
        {
            // Reset from last frame first
            listener.WasTriggered = false;
            listener.Loudness = 0f;

            for (int i = 0; i < NoiseEvents.Length; i++)
            {
                NoiseEventComponent noiseEvent = NoiseEvents[i];

                float dist = math.distance(listener.Position, noiseEvent.Position);
                float hearingRange = math.min(listener.Radius, noiseEvent.Radius);

                if (dist > hearingRange) continue;

                float t = 1f - math.saturate(dist / hearingRange);
                float loudness = t * noiseEvent.Loudness;

                if(loudness > listener.Loudness)
                {
                    listener.WasTriggered = true;
                    listener.Loudness = loudness;
                    listener.LastHeardPosition = noiseEvent.Position;
                }
            }
        }
    }
}
