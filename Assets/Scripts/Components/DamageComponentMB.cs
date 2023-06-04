using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class DamageComponentMB : MonoBehaviour
    {
        public int damage;


        public bool isPereodicDamage;

        [HideIf("@this.isPereodicDamage == false")]
        public float interval;
    }
    public struct DamageComponent : IComponentData
    {
        public int damage;
    }

    public struct PereodicDamageComponent : IComponentData
    {
        public float inteval;

    }
    public struct PereodicDamageNextDamageTimeComponent : IComponentData
    {
        public float value;

    }
    public class DamageComponentMBBaker : Baker<DamageComponentMB>
    {
        public override void Bake(DamageComponentMB authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new DamageComponent
            {
                damage = authoring.damage,

            });

            if (authoring.isPereodicDamage)
            {
                AddComponent(e, new PereodicDamageComponent
                {
                    inteval = authoring.interval

                });

                AddComponent(e, new PereodicDamageNextDamageTimeComponent
                {


                });
            }
        }
    }

}
