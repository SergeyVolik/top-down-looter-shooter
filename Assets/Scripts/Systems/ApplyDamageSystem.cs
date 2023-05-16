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
                if (damageToApply.TryGetBuffer(entity, out var damageList))
                {
                    for (int i = 0; i < damageList.Length; i++)
                    {
                        var damage = damageList[i];

                        health -= damage.damage;

                        if (health <= 0)
                        {
                            health = 0;
                            buffer.DestroyEntity(entity);
                            break;
                        }
                    }

                    damageToApply.SetBufferEnabled(entity, false);
                    damageList.Clear();
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

    [UpdateBefore(typeof(ApplyDamageSystem))]
    public partial class DecreaseProjectilePowerPenetrationSystem : SystemBase
    {

       
        [BurstCompile]
        public partial struct DecreasePenetrationJob : IJobEntity
        {
            public EntityCommandBuffer buffer;
            public ComponentLookup<ProjectilePenetrationPowerComponent> penetrationLookup;

            public void Execute(Entity entity, in DynamicBuffer<DamageToApplyComponent> damageList, in DecreaseProjectilePenetrationPowerComponent decrease)
            {
                for (int i = 0; i < damageList.Length; i++)
                {
                    var damageInfo = damageList[i];

                    if (penetrationLookup.HasComponent(damageInfo.producer))
                    {
                        var refPenPonwer = penetrationLookup.GetRefRW(damageInfo.producer);

                        var value = refPenPonwer.ValueRW.value;
                        value -= decrease.value;
                        refPenPonwer.ValueRW.value = value;
                        if (value <= 0)
                        {
                            buffer.DestroyEntity(damageInfo.producer);
                        }
                    }
                    

                }
            }

        }



        protected override void OnUpdate()
        {
           
            var ecbSys = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSys.CreateCommandBuffer(World.Unmanaged);


            Dependency = new DecreasePenetrationJob
            {
                penetrationLookup = SystemAPI.GetComponentLookup<ProjectilePenetrationPowerComponent>(),
                buffer = ecb
            }.Schedule(Dependency);




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
            [ReadOnly]
            public ComponentLookup<OwnerComponent> ownerLookUp;

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

                            var owner = Entity.Null;

                            if (ownerLookUp.TryGetComponent(damageEntity, out var ownerComp))
                            {
                                owner = ownerComp.value;
                            }

                            damageToApplyBuffer.Add(new DamageToApplyComponent
                            {
                                damage = damage,
                                producer = damageEntity,
                                owner = owner
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
                damageLookUp = SystemAPI.GetComponentLookup<DamageComponent>(isReadOnly: true),
                ownerLookUp = SystemAPI.GetComponentLookup<OwnerComponent>(isReadOnly: true)

            };

            state.Dependency = job.Schedule(state.Dependency);

        }
    }
}
