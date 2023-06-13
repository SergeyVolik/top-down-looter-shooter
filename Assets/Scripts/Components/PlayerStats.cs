using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class PlayerStats : MonoBehaviour
    {
        public float speed;
        public int maxHealth;
        public int luck;
        public int damage;
        public int attackSpeed;
        public int hpRegen;

    }




    public struct PlayerStatsComponent : IComponentData
    {
        public float speed;
        public int maxHealth;
        public int luck;
        public int damage;
        public int attackSpeed;
        public int hpRegen;
    }
    public struct UpdatePlayerStatsComponent : IComponentData
    {

    }


    public static class PlayerStatsUtils
    {
        public static void UpdateStats(EntityManager manager)
        {
            var e = manager.CreateEntity(typeof(UpdatePlayerStatsComponent));
            manager.SetName(e, "UpdatePlayerStatsComponent");


        }
        public static void UpdateStats(ref EntityCommandBuffer ecb)
        {
            var e = ecb.CreateEntity();
            ecb.AddComponent<UpdatePlayerStatsComponent>(e);
        }
    }

    public class PlayerStatsComponentBaker : Baker<PlayerStats>
    {
        public override void Bake(PlayerStats authoring)
        {

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerStatsComponent
            {
                attackSpeed = authoring.attackSpeed,
                damage = authoring.damage,
                maxHealth = authoring.maxHealth,
                hpRegen = authoring.hpRegen,
                luck = authoring.luck,
                speed = authoring.speed,


            });

           
        }
    }


    public partial struct UpdatePlayerStatsSystem : ISystem
    {
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerStatsComponent>();
            state.RequireForUpdate<UpdatePlayerStatsComponent>();

            query = SystemAPI.QueryBuilder().WithAll<UpdatePlayerStatsComponent>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {

            Debug.Log("Update Stats");
            var statsEntity = SystemAPI.GetSingletonEntity<PlayerStatsComponent>();
            var stats = SystemAPI.GetComponentRO<PlayerStatsComponent>(statsEntity);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (max, current) in SystemAPI.Query<RefRW<MaxHealthComponent>, RefRW<HealthComponent>>().WithAll<PlayerComponent>())
            {
                Debug.Log("Max Health Stats");

                var diff = stats.ValueRO.maxHealth - max.ValueRW.value;
                max.ValueRW.value = stats.ValueRO.maxHealth;
                current.ValueRW.value += diff;
            }


            ecb.DestroyEntity(query);
        }
    }


}
