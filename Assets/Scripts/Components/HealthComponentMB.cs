using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;

namespace SV.ECS
{
    public class HealthComponentMB : MonoBehaviour
    {
        public int health;

        public bool destroyAfterDeath;
        public bool isDamageable = true;
    }
    public struct HealthComponent : IComponentData
    {
        public int value;
    }
    public struct MaxHealthComponent : IComponentData
    {
        public int value;
    }

    public struct DeadComponent : IComponentData, IEnableableComponent
    {
        public DamageToApplyComponent killDamageIfno;
        public bool destroyAfterDeath;
        public bool frameSkipped;
    }



    [InternalBufferCapacity(3)]
    public struct DamageToApplyComponent : IBufferElementData, IEnableableComponent
    {
        public int damage;
        public Entity producer;
        public Entity owner;

    }

    public struct DamageableComponent : IComponentData, IEnableableComponent
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

            if (!authoring.isDamageable)
            {
                SetComponentEnabled<DamageableComponent>(entity, false);
            }
            AddComponent(entity, new DeadComponent
            {
                destroyAfterDeath = authoring.destroyAfterDeath
            });
            SetComponentEnabled<DeadComponent>(entity, false);
            AddBuffer<DamageToApplyComponent>(entity);
        }
    }

}
