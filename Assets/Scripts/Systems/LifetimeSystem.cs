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

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            Entities.ForEach((ref Entity e, ref CurrentLifetimeComponent vel, in LifetimeComponent moveInput) =>
            {

                vel.value += delatTime;

                if (vel.value >= moveInput.value)
                {
                    ecb.DestroyEntity(e);
                }


            }).Schedule();


        }

    }

}