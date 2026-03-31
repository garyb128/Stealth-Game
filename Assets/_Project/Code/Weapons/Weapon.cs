using UnityEngine;
using UnityEngine.Events;

public class Weapon : MonoBehaviour
{
    [Tooltip("Reference WeaponData asset")]
    public WeaponData data;

    [Tooltip("If true, create an instance copy of the WeaponData at runtime (useful for runtime edits)")]
    public bool cloneDataAtRuntime = false;

    // internal reference that points to either the asset or the cloned instance
    protected WeaponData runtimeData;

    [Tooltip("Layer mask for enemy detection")]
    public LayerMask targetLayer;

    public UnityEvent OnUse;
    public UnityEvent OnHit;

    protected float cooldownTimer;

   void Awake()
    {
        cooldownTimer = 0f;
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogWarning($"Weapon on {gameObject.name} has no WeaponData assigned.");
            return;
        }

        runtimeData = cloneDataAtRuntime ? Instantiate(data) : data;
    }

    void Update()
    {
        if(cooldownTimer  > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public virtual bool CanUse()
    {
        return cooldownTimer <= 0f;
    }

    public void Use()
    {
        if (!CanUse()) return;

        OnUse?.Invoke();
        cooldownTimer = runtimeData != null ? runtimeData.coolDown : 1f;
    }

    public virtual void OnEquip() => enabled = true;
    public virtual void OnUnequip() => enabled = false;

    protected void EmitNoise(Vector3 pos)
    {
        if (runtimeData != null && runtimeData.createsNoise && NoiseSystem.Instance != null)
            NoiseSystem.Instance.Emit(pos, runtimeData.noiseLoudness);
    }
}
