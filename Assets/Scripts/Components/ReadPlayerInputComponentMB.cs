using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class ReadPlayerInputComponentMB : MonoBehaviour
    {
        
    }
    public struct ReadPlayerInputComponent : IComponentData, IEnableableComponent
    {

    }

    public class ReadPlayerInputBaker : Baker<ReadPlayerInputComponentMB>
    {
        public override void Bake(ReadPlayerInputComponentMB authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ReadPlayerInputComponent>(entity);
        }
    }

}
