using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;

namespace SV.ECS
{
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    [UpdateInGroup(typeof(GameFixedStepSystemGroup))]
    public partial struct CollectorTriggerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var collectableLookup = SystemAPI.GetComponentLookup<CollectableComponent>();
            var collectedLookup = SystemAPI.GetComponentLookup<CollectedComponent>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (triggers, e) in SystemAPI.Query<DynamicBuffer<StatefulTriggerEvent>>().WithAll<CollectorComponent>().WithEntityAccess())
            {
                foreach (var item in triggers)
                {
                    if (item.State == StatefulEventState.Enter )
                    {
                        var targetEntity = item.EntityA == e ? item.EntityB : item.EntityA;

                        if (collectableLookup.HasComponent(targetEntity))
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
