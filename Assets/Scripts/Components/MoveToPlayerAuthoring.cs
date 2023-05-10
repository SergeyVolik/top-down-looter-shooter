using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class MoveToPlayerAuthoring : MonoBehaviour
    {
        
    }
    public struct MoveToPlayerComponent : IComponentData, IEnableableComponent
    {
      
    }


    public class MoveToPlayerBaker : Baker<AimTarget>
    {
        public override void Bake(AimTarget authoring)
        {
           
           
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<MoveToPlayerComponent>(entity);




        }
    }

  
}
