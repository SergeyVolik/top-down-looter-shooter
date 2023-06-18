using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class MoveToPlayerAuthoring : MonoBehaviour
    {
        public float stopDistance;

        public void OnEnable()
        {
            
        }
        public class Baker : Baker<MoveToPlayerAuthoring>
        {
            public override void Bake(MoveToPlayerAuthoring authoring)
            {

                if (!authoring.enabled)
                    return;

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MoveToPlayerComponent>(entity, new MoveToPlayerComponent
                {
                    stopDistance = authoring.stopDistance
                });




            }
        }
    }

    public struct MoveToPlayerComponent : IComponentData
    {
        public float stopDistance;

    }


   

  
}
