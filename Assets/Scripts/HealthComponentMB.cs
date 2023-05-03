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
        public int health;
    }

    public class HealthBaker : Baker<HealthComponentMB>
    {
        public override void Bake(HealthComponentMB authoring)
        {
            AddComponent<HealthComponent>(new HealthComponent { 
                 health = authoring.health,
            });
        }
    }

}
