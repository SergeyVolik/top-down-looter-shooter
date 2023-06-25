using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class VisitProjectile : MonoBehaviour
    {
        private void OnEnable()
        {
            
        }
    }

    [InternalBufferCapacity(3)]
    public struct VisitProjectileBufferElem : IBufferElementData
    {
        public Entity value;
    }
  
    public struct ClearVisitProjectileBuffer: IEnableableComponent, IComponentData
    {
        public Entity value;
    }
    public class VisitProjectileBaker : Baker<VisitProjectile>
    {
        public override void Bake(VisitProjectile authoring)
        {
            if (!authoring.enabled)
                return;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<VisitProjectileBufferElem>(entity);
            AddComponent<ClearVisitProjectileBuffer>(entity);
            SetComponentEnabled<ClearVisitProjectileBuffer>(entity, false);
        }
    }

}
