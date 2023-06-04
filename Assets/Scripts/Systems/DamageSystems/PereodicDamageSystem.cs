using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;

namespace SV.ECS
{
    [UpdateInGroup(typeof(GameFixedStepSystemGroup))]
    [UpdateBefore(typeof(ApplyDamageSystem))]
    public partial class PereodicDamageSystem : SystemBase
    {


        [BurstCompile]
        public partial struct PereodicDamageJob : IJobEntity
        {



            [ReadOnly]
            public ComponentLookup<OwnerComponent> ownerLookup;

            [ReadOnly]
            public ComponentLookup<DamageableComponent> damageableLookup;


            public BufferLookup<DamageToApplyComponent> damageToApply;

            public float time;



            public void Execute(Entity entity, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer, in DamageComponent damage, in PereodicDamageComponent preodic, ref PereodicDamageNextDamageTimeComponent nextTime)
            {
                if (nextTime.value < time)
                {
                    nextTime.value = time + preodic.inteval;

                    foreach (var item in triggerEventBuffer)
                    {



                        var target = item.EntityA != entity ? item.EntityA : item.EntityB;

                        if (ownerLookup.TryGetComponent(entity, out var ownerData) && ownerData.value == target)
                            continue;

                        if (!damageableLookup.HasComponent(target))
                            continue;

                        damageToApply.SetBufferEnabled(target, true);



                        if (damageToApply.TryGetBuffer(target, out var buffer))
                        {
                            buffer.Add(new DamageToApplyComponent
                            {
                                damage = damage.damage,
                                owner = ownerData.value
                            });
                        }




                    }
                }

            }

        }



        protected override void OnUpdate()
        {


            Dependency = new PereodicDamageJob
            {
                ownerLookup = SystemAPI.GetComponentLookup<OwnerComponent>(true),
                damageableLookup = SystemAPI.GetComponentLookup<DamageableComponent>(true),
                damageToApply = SystemAPI.GetBufferLookup<DamageToApplyComponent>(),
                time = (float)SystemAPI.Time.ElapsedTime


            }.Schedule(Dependency);




        }
    }
}