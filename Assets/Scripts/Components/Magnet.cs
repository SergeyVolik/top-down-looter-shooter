using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class Magnet : MonoBehaviour
    {
        public float force;
    }
    public struct MagnetComponent : IComponentData, IEnableableComponent
    {
        public float force;
    }

    public class MagnetBaker : Baker<Magnet>
    {
        public override void Bake(Magnet authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MagnetComponent
            {
                force = authoring.force,
            });
        }
    }

}
