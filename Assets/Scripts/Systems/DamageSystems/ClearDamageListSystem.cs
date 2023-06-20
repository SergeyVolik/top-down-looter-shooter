using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(AddDamageFromVisitedProjectileSystem))]
    public partial class ClearDamageListSystem : SystemBase
    {


        [WithAll(typeof(DamageToApplyComponent))]
        public partial struct ClearJob : IJobEntity
        {
            public BufferLookup<DamageToApplyComponent> buffer;
            public void Execute(Entity e)
            {

                buffer.SetBufferEnabled(e, false);
                if (buffer.TryGetBuffer(e, out var bufferData))
                {
                    bufferData.Clear();
                    buffer.SetBufferEnabled(e, false);
                }
                 
              
            }
        }

        protected override void OnUpdate()
        {

            var job = new ClearJob {
                buffer = SystemAPI.GetBufferLookup<DamageToApplyComponent>()
            };

            Dependency = job.Schedule(Dependency);
            //foreach (var (list, e) in SystemAPI.Query<DynamicBuffer<DamageToApplyComponent>>().WithEntityAccess())
            //{

            //    SystemAPI.SetBufferEnabled<DamageToApplyComponent>(e, false);
            //    list.Clear();

            //}
        }
    }
}