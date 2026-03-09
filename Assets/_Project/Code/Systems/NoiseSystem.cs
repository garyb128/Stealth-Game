using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance {  get; private set; }

    EntityManager entityManager;
    World world;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;


        // Get a reference to the ECS world and its EntityManager
        world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
    }

    public void Emit(Vector3 pos, float radius, float loudness01)
    {
        if (world == null || !world.IsCreated) return;

        // Create a new entity to represent this noise event
        // The NoiseListenerSystem will pick this up on the next frame,
        // process it against all listeners, then destroy it
        Entity noiseEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(noiseEntity, new NoiseEventComponent
        {
            Position = new float3(pos.x, pos.y, pos.z),
            Radius = radius,
            Loudness = loudness01
        });
    }
}
