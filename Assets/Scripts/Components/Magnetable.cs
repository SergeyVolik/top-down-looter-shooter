using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class Magnetable : MonoBehaviour
    {
        
    }
    public struct MagnetableComponent : IComponentData
    {
        
    }

    public struct MagnetTargetComponent : IComponentData, IEnableableComponent
    {
        public Entity value;
    }

    public class MagnetableBaker : Baker<Magnetable>
    {
        public override void Bake(Magnetable authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MagnetableComponent
            {
               
            });
            AddComponent(entity, new MagnetTargetComponent
            {
                
            });

            SetComponentEnabled<MagnetTargetComponent>(entity, false);
        }
    }

}
