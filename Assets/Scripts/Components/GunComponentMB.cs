using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class GunComponentMB : MonoBehaviour
    {
        public GameObject Prefab;
        public GameObject bulletSpawnPos;
        public float shotDelay;
        public float speed;

    }
    public struct GunComponent : IComponentData
    {
        public Entity bulletPrefab;
        public Entity bulletSpawnPos;
        public float shotDelay;
        public float nextShotTime;
        public float bulletSpeed;
    }

    public class GunBakerBaker : Baker<GunComponentMB>
    {
        public override void Bake(GunComponentMB authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
           
            AddComponent<GunComponent>(entity, new GunComponent
            {

                bulletPrefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                bulletSpawnPos = GetEntity(authoring.bulletSpawnPos, TransformUsageFlags.Dynamic),
                shotDelay = authoring.shotDelay,
                nextShotTime = Time.time,
                bulletSpeed = authoring.speed
            });
        }
    }



}
