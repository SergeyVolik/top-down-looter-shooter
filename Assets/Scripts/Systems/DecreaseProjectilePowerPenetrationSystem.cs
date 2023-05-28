using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
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
}