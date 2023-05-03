using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class PlayerComponentMB : MonoBehaviour
    {
        
    }
    public struct PlayerComponent : IComponentData
    {

    }

    public class PlayerBaker : Baker<PlayerComponentMB>
    {
        public override void Bake(PlayerComponentMB authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PlayerComponent>(entity);
        }
    }

}
