using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{


    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject enemyPrefab;
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
    public struct EnemySpawnerComponent : IComponentData
    {
        public Entity enemyPrefab;
        public float spawnDelay;
        public float nextSpawnTime;
        public AABB spawnBound;
    }


    public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {

            if (!authoring.enabled)
                return;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemySpawnerComponent
            {
                enemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                spawnDelay = authoring.spawnDelay,
                spawnBound = authoring.spawnBound,
            });




        }
    }
}
