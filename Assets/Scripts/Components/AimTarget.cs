using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class AimTarget : MonoBehaviour
    {
        public Transform aimPoint;
        private void OnEnable()
        {
            
        }
    }
    public struct AimTargetComponent : IComponentData, IEnableableComponent
    {
        public Entity AimPointEntity;
    }

    public class AimTargetComponentMBBaker : Baker<AimTarget>
    {
        public override void Bake(AimTarget authoring)
        {
            if (!authoring.enabled)
                return;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AimTargetComponent
            {
                AimPointEntity = GetEntity(authoring.aimPoint, TransformUsageFlags.Dynamic)
            });
        }
    }

}
