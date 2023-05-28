using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class EnemyComponentAuthoring : MonoBehaviour
    {
        
    }
    public struct EnemyComponent : IComponentData
    {

    }

    public class EnemyComponentAuthoringBaker : Baker<EnemyComponentAuthoring>
    {
        public override void Bake(EnemyComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<EnemyComponent>(entity);
        }
    }

}
