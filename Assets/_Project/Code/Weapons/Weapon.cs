using UnityEngine;
using UnityEngine.Events;

public class Weapon : MonoBehaviour
{
    public WeaponData data;
    public bool cloneDataAtRuntime = false;
    protected WeaponData runtimeData;

    public LayerMask targetLayer;        // Used by melee; ranged uses data.hitMask

    public UnityEvent OnUse;
    public UnityEvent OnHit;

    protected float cooldownTimer;

    protected virtual void Awake()
    {
        cooldownTimer = 0f;
    }

    protected virtual void Start()
    {
        if (data == null)
        {
            Debug.LogWarning($"Weapon on {gameObject.name} has no WeaponData assigned.");
            return;
        }
        runtimeData = cloneDataAtRuntime ? Instantiate(data) : data;
    }

    protected virtual void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public virtual bool CanUse()
    {
        return cooldownTimer <= 0f;
    }

    public virtual void Use()
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