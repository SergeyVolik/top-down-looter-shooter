using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class Experience : MonoBehaviour
    {
        public int value;
    }
    public struct ExperienceComponent : IComponentData
    {
        public int value;
    }


    public class ExperienceBaker : Baker<Experience>
    {
        public override void Bake(Experience authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExperienceComponent
            {
                value = authoring.value

            });

        }
    }

}
