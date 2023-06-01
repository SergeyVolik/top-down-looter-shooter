using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;


namespace SV.ECS
{
    public class MoveWithEntity : MonoBehaviour
    {
        public GameObject target;
    }

    public struct MoveWithEntityComponent : IComponentData
    {
        public Entity target;
    }

    public class MoveWithEntityBaker : Baker<MoveWithEntity>
    {
        public override void Bake(MoveWithEntity authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MoveWithEntityComponent
            {
                target = GetEntity(authoring.target, TransformUsageFlags.Dynamic)
            });
        }
    }

    public partial struct MoveWithEntitySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
            foreach (var (trans, mwe) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveWithEntityComponent>>())
            {
                var pos = localToWorldLookup.GetRefRO(mwe.ValueRO.target).ValueRO.Position;
                trans.ValueRW.Position = pos;
            }
        }
    }

}
