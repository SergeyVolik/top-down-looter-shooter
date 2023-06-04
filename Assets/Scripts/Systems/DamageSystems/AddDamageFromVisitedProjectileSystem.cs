using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{

    [UpdateInGroup(typeof(GameFixedStepSystemGroup))]
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