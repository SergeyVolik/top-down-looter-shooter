using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class CollectedVisualMessage : MonoBehaviour
    {
       
    }


    public struct CollectedVisualMessageComponent : IComponentData
    {
        
    }

    public class CollectedVisualMessageBaker : Baker<CollectedVisualMessage>
    {
        public override void Bake(CollectedVisualMessage authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new CollectedVisualMessageComponent
            {
                
            });


        }
    }

}
