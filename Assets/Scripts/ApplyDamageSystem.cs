using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;

namespace SV.ECS
{
    public partial struct ApplyDamageSystem : ISystem
    {


        [BurstCompile]
        public partial struct ApplayDamageJob : IJobEntity
        {

            public ComponentLookup<HealthComponent> healthLookUp;

            [ReadOnly]
            public ComponentLookup<DamageComponent> damageLookUp;

            public void Execute(Entity entity, in DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer, in DamageableComponent damageable)
            {
                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];

                    if (triggerEvent.State != StatefulEventState.Enter)
                        continue;

                    var healthEntity = triggerEvent.EntityB;
                    var damageEntity = triggerEvent.EntityA;



                    if (!healthLookUp.HasComponent(healthEntity))
                    {
                        var buffer = healthEntity;
                        healthEntity = damageEntity;
                        damageEntity = buffer;


                    }

                    if (healthLookUp.HasComponent(healthEntity) && damageLookUp.HasComponent(damageEntity))
                    {
                        var damageCom = damageLookUp.GetRefRO(damageEntity);
                        var healthComp = healthLookUp.GetRefRW(healthEntity, false);

                        var damage = damageCom.ValueRO.damage;
                        healthComp.ValueRW.value -= damage;

                    }
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ApplayDamageJob
            {
                healthLookUp = SystemAPI.GetComponentLookup<HealthComponent>(),
                damageLookUp = SystemAPI.GetComponentLookup<DamageComponent>(isReadOnly: true)

            };

            state.Dependency = job.Schedule(state.Dependency);

        }
    }
}
