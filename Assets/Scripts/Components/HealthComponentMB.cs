using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class HealthComponentMB : MonoBehaviour
    {
        public int health;
    }
    public struct HealthComponent : IComponentData
    {
        public int value;
    }
    public struct MaxHealthComponent : IComponentData
    {
        public int value;
    }

    [InternalBufferCapacity(3)]
    public struct DamageToApplyComponent : IBufferElementData, IEnableableComponent
    {
        public int damage;
        public Entity producer;
        public Entity owner;

    }

    public struct DamageableComponent : IComponentData
    {

    }

    public class HealthBaker : Baker<HealthComponentMB>
    {
        public override void Bake(HealthComponentMB authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new HealthComponent
            {
                value = authoring.health,
            });
            AddComponent(entity, new MaxHealthComponent
            {
                value = authoring.health,
            });

            AddComponent(entity, new DamageableComponent());

            AddBuffer<DamageToApplyComponent>(entity);
        }
    }

}
