using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Stores all data needed for the vision raycast job
// The bridge writes the query data every frame
// The system writes the result back
public struct VisionComponent: IComponentData
{
    // FOV inputs - written by the bridge every frame
    public float3 NPCPosition; // world position of the NPC
    public float3 NPCForward; // NPC forward direction
    public float3 TargetPosition; // world position of target
    public float ViewDistance; // max vision range
    public float FOVDegrees; // full FOV angle in degrees
    public float VerticalLimit; // max vertical delta to still be in FOV

    // Raycast inputs - written by bridge every frame
    public float3 RayOrigin; // where the ray starts (eye position)
    public float3 RayDirection; // normalised direction toward the target
    public float RayDistance; // how far to cast the ray
    public int ObstructionMask; // which layers count as obstructions

    // FOV results - written by the job
    public bool InRange; // is target within view distance
    public bool InFOV; // is target within FOV angle
    public bool CanPotentiallySee; // InRange && InFOV

    // Raycast result - written by the system
    public bool HitSomething; // true if ray hit an obstruction
    public bool IsValid; // false if the NPC has no target to check against
}
