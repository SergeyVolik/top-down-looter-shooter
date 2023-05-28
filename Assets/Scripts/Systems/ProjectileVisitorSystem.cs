using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;

namespace SV.ECS
{


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
}