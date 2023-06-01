using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{

    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct MagnetTriggerSystem : ISystem
    {
        public void OnUpdate(ref SystemState handle)
        {
            var magnetableLookup = SystemAPI.GetComponentLookup<MagnetableComponent>();
            var magnetableTargLookup = SystemAPI.GetComponentLookup<MagnetTargetComponent>();
            foreach (var (triggers, e) in SystemAPI.Query<DynamicBuffer<StatefulTriggerEvent>>().WithAll<MagnetComponent>().WithEntityAccess())
            {
                foreach (var item in triggers)
                {
                    if (item.State == StatefulEventState.Enter)
                    {
                        var targetEntity = item.EntityA == e ? item.EntityB : item.EntityA;

                        if (magnetableLookup.HasComponent(targetEntity))
                        {
                            magnetableTargLookup.SetComponentEnabled(targetEntity, true);
                            magnetableTargLookup.GetRefRW(targetEntity).ValueRW.value = e;
                        }
                    }
                }
            }
        }
    }

    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
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
                    if (item.State == StatefulEventState.Enter)
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

    public partial struct MagnetMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState handle)
        {
            var magnetableLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
            var magnetLookup = SystemAPI.GetComponentLookup<MagnetComponent>();
            foreach (var (pv, mt, magSetting, e) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<MagnetTargetComponent>, RefRO<MagnetableComponent>>().WithEntityAccess())
            {
                var selfPos = magnetableLookup.GetRefRO(e).ValueRO.Position;

                var target = mt.ValueRO.value;
                var targPos = magnetableLookup.GetRefRO(target).ValueRO.Position;

                var vecotr = targPos - selfPos;
                var force = magnetLookup.GetRefRO(target).ValueRO.force;
                pv.ValueRW.Linear = math.normalize(vecotr) * force;
            }
        }
    }
}