using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class ProjectilePenetrationPowerAuthoring : MonoBehaviour
    {
        public int value;
    }
    public struct ProjectilePenetrationPowerComponent : IComponentData
    {
        public int value;
    }

    public class ProjectilePenetrationPowerBaker : Baker<ProjectilePenetrationPowerAuthoring>
    {
        public override void Bake(ProjectilePenetrationPowerAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new ProjectilePenetrationPowerComponent
            {
                value = authoring.value,
            });
        }
    }

}
