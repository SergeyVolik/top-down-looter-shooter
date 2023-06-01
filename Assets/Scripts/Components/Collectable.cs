using System;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class Collectable : MonoBehaviour
    {
        public AudioSFX sfx;
    }
    public struct CollectableComponent : IComponentData
    {
        public Guid sfxGuid;
    }

    public struct CollectedComponent : IComponentData, IEnableableComponent
    {

    }

    public class CollectableBaker : Baker<Collectable>
    {
        public override void Bake(Collectable authoring)
        {
           
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CollectableComponent
            {
                sfxGuid = authoring.sfx != null ? authoring.sfx.GetGuid() : Guid.Empty,
            }); 

            AddComponent(entity, new CollectedComponent
            {

            });

            SetComponentEnabled<CollectedComponent>(entity, false);
        }
    }

}
