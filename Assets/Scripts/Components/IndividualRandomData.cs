using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class IndividualRandomData : MonoBehaviour
    {
        
    }

    public struct IndividualRandomComponent : IComponentData
    {
        public Unity.Mathematics.Random Value;
    }
    public struct IndividualRandomInited : IComponentData
    {
        
    }


    public class IndividualRandomBaker : Baker<IndividualRandomData>
    {
        public override void Bake(IndividualRandomData authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new IndividualRandomComponent
            {
                Value = new Unity.Mathematics.Random(1)
            });
        }
    }

    public partial class IndividualRandomSystem : SystemBase
    {
        protected override void OnStartRunning()
        {
            base.OnStartRunning();


        }
        
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref IndividualRandomComponent randomData) =>
            {
                randomData.Value = Unity.Mathematics.Random.CreateFromIndex((uint)entityInQueryIndex);
                ecb.AddComponent(entity, new IndividualRandomInited());
            }).WithNone<IndividualRandomInited>().Run();

        }
    }

}
