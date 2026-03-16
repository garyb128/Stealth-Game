using UnityEngine;

// Sits on NPC root alongside bridge
// Just holds layermask so bridge can read it when creating entity
public class VisionAuthoring : MonoBehaviour
{
    [SerializeField] public LayerMask obstructionMask;
}
