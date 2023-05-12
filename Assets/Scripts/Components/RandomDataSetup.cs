using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class RandomDataSetup : MonoBehaviour
    {
        public uint seed;
    }
    public struct RandomDataComponent : IComponentData
    {
        public Unity.Mathematics.Random Value;
    }

    public class RandomDataBaker : Baker<RandomDataSetup>
    {
        public override void Bake(RandomDataSetup authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RandomDataComponent
            {
                Value = new Unity.Mathematics.Random(authoring.seed)
            });
        }
    }

}
