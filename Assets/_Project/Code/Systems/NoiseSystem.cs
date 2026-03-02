using UnityEngine;

public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance {  get; private set; }

    [Header("Listerner Query")]
    [SerializeField] LayerMask listenerMask; //set this in inspector to the NPC layer
    [SerializeField] int maxHits = 32;

    Collider[] hits;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        hits = new Collider[maxHits];
    }

    public void Emit(Vector3 pos, float radius, float loudness01)
    {
        int count = Physics.OverlapSphereNonAlloc(pos, radius, hits, listenerMask);

        for (int i = 0; i < count; i++)
        {
            var p = hits[i].GetComponentInParent<NPCPerception>();
            if (p == null) continue;

            float dist = Vector3.Distance(hits[i].transform.position,pos);
            float t = 1f - Mathf.Clamp01(dist / radius); //1 at source, 0 at edge
            float strength = t * loudness01; //final impulse strength

            p.HearNoise(pos, strength); 
        } 
    }

}
