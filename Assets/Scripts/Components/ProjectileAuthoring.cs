using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        
    }
    public struct ProjectileAuthoringComponent : IComponentData
    {
      
    }

    public class ProjectileBaker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new ProjectileAuthoringComponent
            {
               
            });
        }
    }

}
