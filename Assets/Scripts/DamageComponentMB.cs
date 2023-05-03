using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class DamageComponentMB : MonoBehaviour
    {
        public int damage;
    }
    public struct DamageComponent : IComponentData
    {
        public int damage;
    }

    public class DamageComponentMBBaker : Baker<DamageComponentMB>
    {
        public override void Bake(DamageComponentMB authoring)
        {
            AddComponent<DamageComponent>(new DamageComponent { 
                 damage = authoring.damage,
            });
        }
    }

}
