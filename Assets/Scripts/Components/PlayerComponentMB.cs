using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class PlayerComponentMB : MonoBehaviour
    {
        private void OnEnable()
        {
            
        }
    }
    public struct PlayerComponent : IComponentData
    {

    }

    public class PlayerBaker : Baker<PlayerComponentMB>
    {
        public override void Bake(PlayerComponentMB authoring)
        {
            if (!authoring.enabled)
                return;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PlayerComponent>(entity);
        }
    }

}
