using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;

namespace SV.ECS
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    [UpdateInGroup(typeof(GameFixedStepSystemGroup))]
    public partial struct CollectorTriggerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var collectableLookup = SystemAPI.GetComponentLookup<CollectableComponent>(isReadOnly: true);
            var collectedLookup = SystemAPI.GetComponentLookup<CollectedComponent>(isReadOnly: false);
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


            new CollectorJob
            {
                collectableLookup = collectableLookup,
                collectedLookup = collectedLookup,
                ecb = ecb
            }.Schedule();
        }



        [BurstCompile]
        [WithAll(typeof(CollectorComponent))]

        public partial struct CollectorJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CollectableComponent> collectableLookup;

            public ComponentLookup<CollectedComponent> collectedLookup;

            public EntityCommandBuffer ecb;


            [BurstCompile]
            public void Execute(Entity e, [EntityIndexInQuery] int entityInQueryIndex, DynamicBuffer<StatefulTriggerEvent> triggers)
            {
                foreach (var item in triggers)
                {
                    if (item.State == StatefulEventState.Enter)
                    {
                        var targetEntity = item.EntityA == e ? item.EntityB : item.EntityA;

                        if (collectableLookup.HasComponent(targetEntity))
                        {
                            if (!collectedLookup.IsComponentEnabled(targetEntity))
                            {
                                collectedLookup.SetComponentEnabled(targetEntity, true);
                                ecb.DestroyEntity(targetEntity);

                                var sfx = ecb.CreateEntity();

                                ecb.AddComponent(sfx, new PlaySFX { sfxSettingGuid = collectableLookup.GetRefRO(targetEntity).ValueRO.sfxGuid });
                            }
                        }

                    }
                }
            }
        }
    }


}
