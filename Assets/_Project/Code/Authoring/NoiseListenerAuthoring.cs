using UnityEngine;

// Sits on the NPC root GameObject alongside NoiseListenerBridge
// Its only job is to hold the hearing radius so the bridge can read it
// No baking, no Baker class — just a simple data container
public class NoiseListenerAuthoring : MonoBehaviour
{
    [SerializeField] public float hearingRadius = 10f;
}