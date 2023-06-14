using System;
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
        public float hpRegenInterval;

        public int crit—hance;

    }




    public struct PlayerStatsComponent : IComponentData, IEquatable<PlayerStatsComponent>
    {
        public float speed;
        public int maxHealth;
        public int luck;
        public int damage;
        public int attackSpeed;
        public float hpRegenInterval;
        public int critChance;

        public bool Equals(PlayerStatsComponent other)
        {
            if(other.GetHashCode() == GetHashCode())
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return speed.GetHashCode() 
                ^ maxHealth.GetHashCode() 
                ^ luck.GetHashCode() 
                ^ damage.GetHashCode() 
                ^ attackSpeed.GetHashCode() 
                ^ hpRegenInterval.GetHashCode() 
                ^ critChance.GetHashCode();
        }
    }

    public struct PrevStatsComponent : IComponentData
    {
        public PlayerStatsComponent value;
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
                hpRegenInterval = authoring.hpRegenInterval,
                luck = authoring.luck,
                speed = authoring.speed,
                critChance = authoring.crit—hance

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

            foreach (var regen in SystemAPI.Query<RefRW<HPRegenComponent>>().WithAll<PlayerComponent>())
            {
                regen.ValueRW.regenInterval = stats.ValueRO.hpRegenInterval;
            }
            foreach (var controller in SystemAPI.Query<RefRW<CharacterControllerComponent>>().WithAll<PlayerComponent>())
            {
                controller.ValueRW.speed = stats.ValueRO.speed;
            }

            ecb.DestroyEntity(query);
        }
    }
    public partial struct AutoUpdatePlayerStatsSystem : ISystem
    {
       

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerStatsComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (ps, e) in SystemAPI.Query<RefRO<PlayerStatsComponent>>().WithNone<PrevStatsComponent>().WithEntityAccess())
            {
                ecb.AddComponent<PrevStatsComponent>(e);
                var value = ps.ValueRO;
                ecb.SetComponent(e, value);
            }

            foreach (var (ps, prev) in SystemAPI.Query<RefRO<PlayerStatsComponent>, RefRW<PrevStatsComponent>>())
            {
                if (!ps.ValueRO.Equals(prev.ValueRO.value))
                {
                    prev.ValueRW.value = ps.ValueRO;

                    var e = ecb.CreateEntity();
                    ecb.AddComponent<UpdatePlayerStatsComponent>(e);
                }
            }

        }
    }

}
