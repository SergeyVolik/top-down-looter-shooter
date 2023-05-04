using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class OwnerComponentAuthoring : MonoBehaviour
    {
        public GameObject owner;
    }
    public struct OwnerComponent : IComponentData
    {
        public Entity value;
    }

    public class OwnerBaker : Baker<OwnerComponentAuthoring>
    {
        public override void Bake(OwnerComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new OwnerComponent { 
                 value = GetEntity(authoring.owner, TransformUsageFlags.Dynamic)
            });
        }
    }

}
