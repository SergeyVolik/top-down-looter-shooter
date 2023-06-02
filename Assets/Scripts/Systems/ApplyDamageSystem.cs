
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{


    [UpdateAfter(typeof(AddDamageFromVisitedProjectileSystem))]
    public partial class ApplyDamageSystem : SystemBase
    {

        EntityQuery query;

        [BurstCompile]
        public partial struct ApplayDamageJob : IJobEntity
        {

           
           
            public EntityCommandBuffer buffer;
            public void Execute(Entity entity, ref HealthComponent healthComp, ref DynamicBuffer<DamageToApplyComponent> damageList)
            {
                var health = healthComp.value;

                for (int i = 0; i < damageList.Length; i++)
                {
                    var damage = damageList[i];

                    health -= damage.damage;

                    if (health <= 0)
                    {
                        health = 0;


                        buffer.SetComponentEnabled<DeadComponent>(entity, true);
                        //buffer.DisableWithChilds(entity, ref childs);
                        buffer.SetComponent(entity, new DeadComponent
                        {
                            killDamageIfno = damage
                        });


                        break;
                    }
                }
                //damageList.Clear();
                healthComp.value = health;

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


            var ecbSys = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSys.CreateCommandBuffer(World.Unmanaged);


            Dependency = new ApplayDamageJob
            {

                buffer = ecb,              
              
            }.Schedule(query, Dependency);




        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ClearDamageListSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSys = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSys.CreateCommandBuffer(World.Unmanaged);

            foreach (var (list, e) in SystemAPI.Query<DynamicBuffer<DamageToApplyComponent>>().WithEntityAccess())
            {

                list.Clear();
                ecb.SetComponentEnabled<DamageToApplyComponent>(e, false);
            }
        }
    }



    







    [UpdateAfter(typeof(ProjectileVisitorSystem))]
    public partial struct AddDamageFromVisitedProjectileSystem : ISystem
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
