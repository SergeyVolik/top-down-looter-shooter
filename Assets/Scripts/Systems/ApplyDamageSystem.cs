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


    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    [UpdateAfter(typeof(StatefulCollisionEventBufferSystem))]
    public partial class ProjectileVisitorSystem : SystemBase
    {


        [BurstCompile]
        public partial struct UpdateVisitListJob : IJobEntity
        {

            public BufferLookup<VisitProjectileBufferElem> penetrationLookup;
            public ComponentLookup<ClearVisitProjectileBuffer> clearDamageToApply;

            [ReadOnly]
            public ComponentLookup<OwnerComponent> ownerLookup;

            public void Execute(Entity entity, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer, in ProjectileAuthoringComponent projectile)
            {


                foreach (var item in triggerEventBuffer)
                {
                    if (item.State != StatefulEventState.Enter)
                        continue;

                    var target = item.EntityA != entity ? item.EntityA : item.EntityB;

                    if (ownerLookup.TryGetComponent(entity, out var owner) && owner.value == target)
                        continue;

                    if (penetrationLookup.TryGetBuffer(target, out var visitBuffer))
                    {
                        clearDamageToApply.SetComponentEnabled(target, true);

                        visitBuffer.Add(new VisitProjectileBufferElem { value = entity });
                    }
                }

            }

        }



        protected override void OnUpdate()
        {


            Dependency = new UpdateVisitListJob
            {
                penetrationLookup = SystemAPI.GetBufferLookup<VisitProjectileBufferElem>(),
                clearDamageToApply = SystemAPI.GetComponentLookup<ClearVisitProjectileBuffer>(),
                ownerLookup = SystemAPI.GetComponentLookup<OwnerComponent>(true),

            }.Schedule(Dependency);




        }
    }

    [UpdateBefore(typeof(ProjectileVisitorSystem))]
    public partial class ClearVisitedProjectileBUfferSystem : SystemBase
    {


        [BurstCompile]
        public partial struct UpdateVisitListJob : IJobEntity
        {

            public void Execute(Entity entity, ref DynamicBuffer<VisitProjectileBufferElem> triggerEventBuffer, EnabledRefRW<ClearVisitProjectileBuffer> data)
            {


                triggerEventBuffer.Clear();
                data.ValueRW = false;
            }

        }



        protected override void OnUpdate()
        {


            Dependency = new UpdateVisitListJob
            {


            }.Schedule(Dependency);




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

            public void Execute(Entity entity, in DynamicBuffer<VisitProjectileBufferElem> visitedObjects, in DecreaseProjectilePenetrationPowerComponent decrease)
            {
                for (int i = 0; i < visitedObjects.Length; i++)
                {
                    var entityProjectile = visitedObjects[i].value;

                    if (penetrationLookup.HasComponent(entityProjectile))
                    {
                        var refPenPonwer = penetrationLookup.GetRefRW(entityProjectile);

                        var value = refPenPonwer.ValueRW.value;
                        value -= decrease.value;
                        refPenPonwer.ValueRW.value = value;

                        if (value <= 0)
                        {
                            buffer.DestroyEntity(entityProjectile);
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




    [UpdateAfter(typeof(ProjectileVisitorSystem))]
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


            public void Execute(Entity entity, in DynamicBuffer<VisitProjectileBufferElem> visitedProjectile, in DamageableComponent damageable)
            {
                var addDamageEntity = entity;

                if (!damageToApply.TryGetBuffer(addDamageEntity, out var damageToApplyBuffer))
                {
                    return;
                }

                for (int i = 0; i < visitedProjectile.Length; i++)
                {
                    var visitedEntity = visitedProjectile[i].value;



                    var damageEntity = visitedEntity;


                    if (!damageLookUp.TryGetComponent(damageEntity, out var damage))
                        continue;


                    damageToApply.SetBufferEnabled(addDamageEntity, true);



                    var owner = Entity.Null;

                    if (ownerLookUp.TryGetComponent(damageEntity, out var ownerComp))
                    {
                        owner = ownerComp.value;
                    }

                    if (owner == addDamageEntity)
                        continue;

                    damageToApplyBuffer.Add(new DamageToApplyComponent
                    {
                        damage = damage.damage,
                        producer = damageEntity,
                        owner = owner
                    });




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
