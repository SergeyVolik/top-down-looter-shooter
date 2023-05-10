using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;


namespace SV.ECS
{


    [UpdateAfter(typeof(AddDamageFromTriggerSystem))]
    public partial class ApplyDamageSystem : SystemBase
    {

        EntityQuery query;

        [BurstCompile]
        public partial struct ApplayDamageJob : IJobEntity
        {
            public BufferLookup<DamageToApplyComponent> damageToApply;
            public EntityCommandBuffer buffer;
            public void Execute(Entity entity, ref HealthComponent healthComp)
            {
                var health = healthComp.value;
                if (damageToApply.TryGetBuffer(entity, out var triggerEventBuffer))
                {
                    for (int i = 0; i < triggerEventBuffer.Length; i++)
                    {
                        var damage = triggerEventBuffer[i];

                        health -= damage.damage;

                        if (health <= 0)
                        {
                            health = 0;
                            buffer.DestroyEntity(entity);
                            break;
                        }
                    }

                    damageToApply.SetBufferEnabled(entity, false);
                    triggerEventBuffer.Clear();
                    healthComp.value = health;
                }
            }

        }

        protected override void OnCreate()
        {
            // Get respective queries, that includes components required by `CopyPositionsJob` described earlier.


            query = GetEntityQuery(typeof(HealthComponent), typeof(DamageToApplyComponent));
            RequireForUpdate(query);


        }


        protected override void OnUpdate()
        {
            if (query.CalculateEntityCount() == 0)
                return;


            var ecbSys = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSys.CreateCommandBuffer(World.Unmanaged);


            Dependency = new ApplayDamageJob
            {
                damageToApply = SystemAPI.GetBufferLookup<DamageToApplyComponent>(),
                buffer = ecb
            }.Schedule(query, Dependency);




        }
    }

    public partial struct DamageValidationSystem : ISystem
    {

    }



    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct AddDamageFromTriggerSystem : ISystem
    {


        [BurstCompile]
        public partial struct AddDamageToEntityJob : IJobEntity
        {

            [ReadOnly]
            public ComponentLookup<DamageComponent> damageLookUp;

            public BufferLookup<DamageToApplyComponent> damageToApply;

            public void Execute(Entity entity, in DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer, in DamageableComponent damageable)
            {
                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];

                    if (triggerEvent.State != StatefulEventState.Enter)
                        continue;

                    var addDamageEntity = triggerEvent.EntityB;
                    var damageEntity = triggerEvent.EntityA;



                    if (!damageToApply.HasBuffer(addDamageEntity))
                    {
                        var buffer = addDamageEntity;
                        addDamageEntity = damageEntity;
                        damageEntity = buffer;

                    }

                    if (damageToApply.HasBuffer(addDamageEntity) && damageLookUp.HasComponent(damageEntity))
                    {
                        var damageCom = damageLookUp.GetRefRO(damageEntity);
                        var damage = damageCom.ValueRO.damage;
                        damageToApply.SetBufferEnabled(addDamageEntity, true);
                        if (damageToApply.TryGetBuffer(addDamageEntity, out var damageToApplyBuffer))
                        {

                            damageToApplyBuffer.Add(new DamageToApplyComponent
                            {
                                damage = damage
                            });
                        }


                    }
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new AddDamageToEntityJob
            {
                damageToApply = SystemAPI.GetBufferLookup<DamageToApplyComponent>(),
                damageLookUp = SystemAPI.GetComponentLookup<DamageComponent>(isReadOnly: true)

            };

            state.Dependency = job.Schedule(state.Dependency);

        }
    }
}
