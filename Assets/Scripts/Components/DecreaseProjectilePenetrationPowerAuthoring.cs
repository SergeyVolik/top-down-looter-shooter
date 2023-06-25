using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class DecreaseProjectilePenetrationPowerAuthoring : MonoBehaviour
    {
        public int value;

        private void OnEnable()
        {
            
        }
    }
    public struct DecreaseProjectilePenetrationPowerComponent : IComponentData
    {
        public int value;
    }

    public class DecreaseProjectilePenetrationPowerBaker : Baker<DecreaseProjectilePenetrationPowerAuthoring>
    {
        public override void Bake(DecreaseProjectilePenetrationPowerAuthoring authoring)
        {
            if (!authoring.enabled)
                return;

            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new DecreaseProjectilePenetrationPowerComponent
            {
                value = authoring.value,
            });
        }
    }

}
