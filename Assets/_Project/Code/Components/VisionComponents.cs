using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Stores all data needed for the vision raycast job
// The bridge writes the query data every frame
// The system writes the result back
public struct VisionComponent: IComponentData
{
    // Query inputs - written by bridge every frame
    public float3 Origin; // where the ray starts (eye position)
    public float3 Direction; // normalised direction toward the target
    public float Distance; // how far to cast the ray
    public int ObstructionMask; // which layers count as obstructions

    // Query result - written by the system
    public bool HitSomething; // true if ray hit an obstruction
    public bool IsValid; // false if the NPC has no target to check against
}
