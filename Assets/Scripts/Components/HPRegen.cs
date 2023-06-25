using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class HPRegen : MonoBehaviour
    {
        public float regenInterval;
        private void OnEnable()
        {
            
        }
    }


    public struct HPRegenComponent : IComponentData
    {
        public float regenInterval;
    }
    public struct PrevHPRegenTimeComponent : IComponentData
    {
        public float value;
    }

    public class HPRegenBaker : Baker<HPRegen>
    {
        public override void Bake(HPRegen authoring)
        {
            if (!authoring.enabled)
                return;
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<HPRegenComponent>(entity, new HPRegenComponent
            {
                regenInterval = authoring.regenInterval,
            });

        }
    }

    public partial struct HPRegenSystem : ISystem
    {

        public void OnCreate(ref SystemState state)
        {

        }
        public void OnUpdate(ref SystemState state)
        {
            var rcb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            float time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (currentHealth, e) in SystemAPI.Query<RefRO<HealthComponent>>().WithNone<PrevHPRegenTimeComponent>().WithEntityAccess())
            {
                rcb.AddComponent<PrevHPRegenTimeComponent>(e);
            }

            foreach (var (currentHealth, maxHealth, regen, prefRegentTime) in SystemAPI.Query<RefRW<HealthComponent>, RefRO<MaxHealthComponent>, RefRO<HPRegenComponent>, RefRW<PrevHPRegenTimeComponent>>())
            {
                var currentHealthValue = currentHealth.ValueRO.value;

                var maxHealthValue = maxHealth.ValueRO.value;

                if (maxHealthValue == currentHealthValue)
                    continue;

                if (currentHealthValue == 0)
                    continue;

              
                if (prefRegentTime.ValueRO.value + regen.ValueRO.regenInterval < time)
                {
                    currentHealth.ValueRW.value += 1;
                    prefRegentTime.ValueRW.value = time;
                }

            }
        }
    }

}
