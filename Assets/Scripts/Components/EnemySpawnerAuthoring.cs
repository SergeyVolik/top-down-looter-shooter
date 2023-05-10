using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{


    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public float spawnDelay;
        public Bounds spawnBound;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            var ls = transform.localScale;
            var size = spawnBound.size;
            Gizmos.DrawWireCube(transform.position + spawnBound.center, new Vector3(size.x * ls.x, size.y * ls.y, size.z * ls.z));
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
    public struct EnemySpawnerComponent : IComponentData
    {
        public Entity enemyPrefab;
        public float spawnDelay;
        public float nextSpawnTime;
    }


    public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {


            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EnemySpawnerComponent>(entity, new EnemySpawnerComponent
            {
                enemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                spawnDelay = authoring.spawnDelay
            });




        }
    }
}
