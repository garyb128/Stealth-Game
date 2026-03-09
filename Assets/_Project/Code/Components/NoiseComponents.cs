using Unity.Entities;
using Unity.Mathematics;

// Data attached to each NPC entity representing its ability to hear
// The bridge writes Position every frame, the system writes Loudness and WasTriggered
public struct NoiseListenerComponent : IComponentData
{
    public float3 Position;     // kept in sync with the NPC's world position by the bridge
    public float Radius;       // max hearing range, set once in Start and never changes
    public float Loudness;     // written by the system — how loud the noise was this frame
    public float3 LastHeardPosition; // where the noise came from
    public bool WasTriggered; // written by the system — did we hear something this frame?

}

// Created as a temporary entity when a noise fires, destroyed by the system after processing
public struct NoiseEventComponent : IComponentData
{
    public float3 Position; // where the noise happened in world space
    public float Loudness; // how loud it was, 0-1
    public float Radius;   // how far the noise travels
}