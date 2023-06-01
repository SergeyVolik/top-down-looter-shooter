using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class Collector : MonoBehaviour
    {
       
    }
    public struct CollectorComponent : IComponentData
    {
        
    }

    public class CollectorBaker : Baker<Collector>
    {
        public override void Bake(Collector authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CollectorComponent
            {
               
            });
        }
    }

}
