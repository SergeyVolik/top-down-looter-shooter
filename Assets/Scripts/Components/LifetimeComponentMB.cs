using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class LifetimeComponentMB : MonoBehaviour
    {
        public float value;

        private void OnEnable()
        {
            
        }
    }

    public struct CurrentLifetimeComponent : IComponentData
    {
        public float value;
    }
    public struct LifetimeComponent : IComponentData
    {
        public float value;
    }


    public class ReadPlayerInputBaker1 : Baker<LifetimeComponentMB>
    {
        public override void Bake(LifetimeComponentMB authoring)
        {
            if (!authoring.enabled)
                return;

            AddComponent(new LifetimeComponent { value = authoring.value });
            AddComponent(new CurrentLifetimeComponent { value = 0 });

        }
    }


}
