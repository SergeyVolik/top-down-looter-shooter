using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{


    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject spawnerPrefab;

        [System.Serializable]
        public class EnemyData
        {
            public GameObject prefab;
            public float probability;
        }
        public EnemyData[] enemies;


        public float spawnDelay;
        public AABB spawnBound;

        private void OnEnable()
        {

        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            var ls = transform.localScale;
            var size = spawnBound.Size;
            Gizmos.DrawWireCube(transform.position + (Vector3)spawnBound.Center, new Vector3(size.x * ls.x, size.y * ls.y, size.z * ls.z));
            Gizmos.DrawWireSphere(transform.position + (Vector3)spawnBound.Center, 1f);
        }
    }
    public struct EnemiesListComponent : IBufferElementData
    {
        public Entity enemyPrefab;
        public float probability;

    }
    public struct EnemySpawnerComponent : IComponentData
    {
        
        public float spawnDelay;
        public float nextSpawnTime;
        public Entity spawnPointVisual;
        public AABB spawnBound;
        
    }


        public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {

            if (!authoring.enabled)
                return;

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var buffer = AddBuffer<EnemiesListComponent>(entity);

            foreach (var item in authoring.enemies)
            {
                buffer.Add(new EnemiesListComponent
                {
                    enemyPrefab = GetEntity(item.prefab, TransformUsageFlags.Dynamic),
                    probability = item.probability,

                });
            }
            AddComponent(entity, new EnemySpawnerComponent
            {
                spawnDelay = authoring.spawnDelay,
                spawnBound = authoring.spawnBound,
                 spawnPointVisual = GetEntity(authoring.spawnerPrefab, TransformUsageFlags.Dynamic)
            });



        }
    }
}
