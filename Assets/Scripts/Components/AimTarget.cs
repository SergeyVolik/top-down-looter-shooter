using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class AimTarget : MonoBehaviour
    {
        
    }
    public struct AimTargetComponent : IComponentData, IEnableableComponent
    {

    }

    public class AimTargetComponentMBBaker : Baker<AimTarget>
    {
        public override void Bake(AimTarget authoring)
        {
            var entity =GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<AimTargetComponent>(entity);
        }
    }

}
