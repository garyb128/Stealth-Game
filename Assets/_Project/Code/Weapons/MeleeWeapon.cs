// name=Assets/_Project/Code/Player/Weapons/MeleeWeapon.cs
using UnityEngine;

public class MeleeWeapon : Weapon
{
    [Header("Optional Overrides")]
    public float rangeOverride = -1f;
    public float radiusOverride = -1f;
    public LayerMask overrideTargetLayer;

    public Vector3 originOffset = new Vector3(0, 0.9f, 0);

    void UseInternal()
    {
        if (runtimeData == null)
        {
            Debug.LogWarning("No WeaponData available for MeleeWeapon.");
            return;
        }

        float range = (rangeOverride > 0f) ? rangeOverride : runtimeData.meleeRange;
        float radius = (radiusOverride > 0f) ? radiusOverride : runtimeData.meleeRadius;
        LayerMask mask = (overrideTargetLayer != 0) ? overrideTargetLayer : targetLayer;

        Vector3 origin = transform.position + originOffset;
        Vector3 sphereCenter = origin + transform.forward * Mathf.Clamp(range * 0.5f, 0f, range);

        Collider[] hits = Physics.OverlapSphere(sphereCenter, radius, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        Collider chosen = null;
        float bestDist = float.MaxValue;
        foreach (var c in hits)
        {
            float d = Vector3.Distance(origin, c.transform.position);
            if (d < bestDist) { bestDist = d; chosen = c; }
        }
        if (chosen == null) return;

        var npc = chosen.GetComponentInParent<NPCBrain>();
        if (npc != null)
        {
            Vector3 toPlayer = (transform.position - npc.transform.position).normalized;
            float dot = Vector3.Dot(npc.transform.forward, toPlayer);

            bool isBackstab = dot < runtimeData.backstabDotThreshold;

            if (isBackstab)
            {
                if (runtimeData.meleeKnockOutDuration > 0f)
                    npc.Knockout(runtimeData.meleeKnockOutDuration);
            }
            else
            {
                npc.Knockout(Mathf.Min(2f, runtimeData.meleeKnockOutDuration));
                EmitNoise(transform.position);
            }
        }
        else
        {
            EmitNoise(transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;
        float range = (rangeOverride > 0f) ? rangeOverride : data.meleeRange;
        float radius = (radiusOverride > 0f) ? radiusOverride : data.meleeRadius;
        Vector3 origin = transform.position + originOffset;
        Vector3 c = origin + transform.forward * Mathf.Clamp(range * 0.5f, 0f, range);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(c, radius);
    }
}