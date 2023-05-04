using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public partial class LifetimeSystem : SystemBase
    {


        protected override void OnCreate()
        {
            base.OnCreate();

        }

        protected override void OnUpdate()
        {
            var delatTime = SystemAPI.Time.DeltaTime;

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            Entities.ForEach((ref Entity e, ref CurrentLifetimeComponent vel, in LifetimeComponent moveInput) =>
            {

                vel.value += delatTime;

                if (vel.value >= moveInput.value)
                {
                    ecb.DestroyEntity(e);
                }


            }).Schedule();

            Dependency.Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

    }


    public struct DestroyComponent : IComponentData
    {
        
    }

    public partial struct DestroySystem : ISystem
    {

        public partial struct DestroyJob : IJobEntity
        {
            public EntityCommandBuffer buffer;
            public void Execute(Entity entity, in DestroyComponent dc)
            {
                buffer.DestroyEntity(entity);
            }
        }

        public void OnCreate(ref SystemState state)
        {
          

        }

        public void OnUpdate(ref SystemState state)
        {

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var job = new DestroyJob
            {
                buffer = ecb
            };

            state.Dependency = job.Schedule(state.Dependency);
        }
    }
}