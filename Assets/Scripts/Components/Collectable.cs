using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class Collectable : MonoBehaviour
    {
       
    }
    public struct CollectableComponent : IComponentData
    {
        
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
               
            });

            AddComponent(entity, new CollectedComponent
            {

            });

            SetComponentEnabled<CollectedComponent>(entity, false);
        }
    }

}
