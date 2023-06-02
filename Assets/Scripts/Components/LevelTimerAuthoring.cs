using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class LevelTimerAuthoring : MonoBehaviour
    {
        public float duration;
    }

    public struct LevelTimerComponent : IComponentData
    {
        public float duration;
        public float currentTime;

    }

    public partial struct LevelTimerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTIme = state.WorldUnmanaged.Time.DeltaTime;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (lt, e) in SystemAPI.Query<RefRW<LevelTimerComponent>>().WithEntityAccess())
            {
                lt.ValueRW.currentTime += deltaTIme;

                if (lt.ValueRO.currentTime > lt.ValueRO.duration)
                {
                    ecb.DestroyEntity(e);
                }
            }
        }
    }

    public class LevelTimerComponentBaker : Baker<LevelTimerAuthoring>
    {
        public override void Bake(LevelTimerAuthoring authoring)
        {

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LevelTimerComponent
            {
                duration = authoring.duration


            });
        }
    }



}
