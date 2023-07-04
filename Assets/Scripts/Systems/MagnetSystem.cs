using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{

    [WorldSystemFilter( WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct MagnetTriggerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState handle)
        {
            var magnetableLookup = SystemAPI.GetComponentLookup<MagnetableComponent>(isReadOnly: true);
            var magnetableTargLookup = SystemAPI.GetComponentLookup<MagnetTargetComponent>();

            foreach (var (triggers, e) in SystemAPI.Query<DynamicBuffer<StatefulTriggerEvent>>().WithAll<MagnetComponent>().WithEntityAccess())
            {
                foreach (var item in triggers)
                {
                    
                    var targetEntity = item.EntityA == e ? item.EntityB : item.EntityA;

                    if (magnetableLookup.HasComponent(targetEntity) && !magnetableTargLookup.IsComponentEnabled(targetEntity))
                    {
                        magnetableTargLookup.SetComponentEnabled(targetEntity, true);
                        magnetableTargLookup.GetRefRW(targetEntity).ValueRW.value = e;
                    }
                    
                }
            }
        }
    }



    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct MagnetMoveSystem : ISystem
    {
        [BurstCompile]
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