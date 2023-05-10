using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class MoveToPlayerAuthoring : MonoBehaviour
    {
        public float stopDistance;
    }
    public struct MoveToPlayerComponent : IComponentData
    {
        public float stopDistance;

    }


    public class MoveToPlayerBaker : Baker<MoveToPlayerAuthoring>
    {
        public override void Bake(MoveToPlayerAuthoring authoring)
        {
           
           
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<MoveToPlayerComponent>(entity, new MoveToPlayerComponent { 
                 stopDistance = authoring.stopDistance
            });




        }
    }

  
}
