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
        protected override void OnUpdate()
        {



            foreach (var (list, e) in SystemAPI.Query<DynamicBuffer<DamageToApplyComponent>>().WithEntityAccess())
            {

                SystemAPI.SetBufferEnabled<DamageToApplyComponent>(e, false);
                list.Clear();

            }
        }
    }
}