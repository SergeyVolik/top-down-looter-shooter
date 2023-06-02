using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class DamageVisualMessage : MonoBehaviour
    {
       
    }


    public struct DamageVisualMessageComponent : IComponentData
    {
        
    }

    public class DamageVisualMessageBaker : Baker<DamageVisualMessage>
    {
        public override void Bake(DamageVisualMessage authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new DamageVisualMessageComponent
            {
                
            });


        }
    }

}
