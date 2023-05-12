using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class SpawnPointAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        public float spawnDelay;
    }
    public struct SpawnPointComponent : IComponentData
    {
        public Entity prefab;
        public float spawnDelay;
    }
    public struct SpawnTimeComponent : IComponentData
    {       
        public float spawnTime;
    }


    public class SpawnPointBaker : Baker<SpawnPointAuthoring>
    {
        public override void Bake(SpawnPointAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<SpawnPointComponent>(entity, new SpawnPointComponent
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                spawnDelay = authoring.spawnDelay,
            });
        }
    }

}
