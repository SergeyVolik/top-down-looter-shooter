using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace SV.ECS
{
    public class GunComponentMB : MonoBehaviour
    {
        [Range(0, 45)]
        public float Angle = 20;
        public GameObject Prefab;
        public GameObject bulletSpawnPos;
        public float shotDelay;
        public float speed;
        [Range(1, 10)]
        public int bulletsInShot;
       

        private void OnDrawGizmos()
        {
            var bulletSpawnTrans = bulletSpawnPos.transform;
            var pos = bulletSpawnTrans.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, bulletSpawnTrans.forward * 10);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(pos, Quaternion.AngleAxis(Angle, bulletSpawnTrans.up) * bulletSpawnTrans.forward * 10);
            Gizmos.DrawRay(pos, Quaternion.AngleAxis(-Angle, bulletSpawnTrans.up) * bulletSpawnTrans.forward * 10);

        }

    }
    public struct GunComponent : IComponentData
    {
        public Entity bulletPrefab;
        public Entity bulletSpawnPos;
        public float shotDelay;
        public float nextShotTime;
        public float bulletSpeed;
        public int bulletsInShot;
        public float Angle;
    }

    public class GunBakerBaker : Baker<GunComponentMB>
    {
        public override void Bake(GunComponentMB authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new GunComponent
            {

                bulletPrefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                bulletSpawnPos = GetEntity(authoring.bulletSpawnPos, TransformUsageFlags.Dynamic),
                shotDelay = authoring.shotDelay,
                nextShotTime = Time.time,
                bulletSpeed = authoring.speed,
                Angle = authoring.Angle,
                bulletsInShot = authoring.bulletsInShot
            });
        }
    }



}
