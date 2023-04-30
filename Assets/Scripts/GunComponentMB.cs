using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEngine.Rendering;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Unity.Transforms;
using Unity.Physics;

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
           
            AddComponent<GunComponent>(new GunComponent
            {

                bulletPrefab = GetEntity(authoring.Prefab),
                bulletSpawnPos = GetEntity(authoring.bulletSpawnPos),
                shotDelay = authoring.shotDelay,
                nextShotTime = Time.time,
                bulletSpeed = authoring.speed
            });
        }
    }

    [UpdateAfter(typeof(LocalToWorldSystem))]
    public partial class GunSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();
         
        }

        protected override void OnUpdate()
        {
            float time = (float)SystemAPI.Time.ElapsedTime;

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ltw = GetComponentLookup<LocalToWorld>();
            var transLookUp = GetComponentLookup<LocalTransform>();
            Entities.ForEach((ref Entity e, ref GunComponent gun) =>
            {

                if (gun.nextShotTime <= time)
                {
                   

                    if (ltw.TryGetComponent(gun.bulletSpawnPos, out var wordPos) && transLookUp.TryGetComponent(gun.bulletSpawnPos, out var localTrans))
                    {
                        var bullet = ecb.Instantiate(gun.bulletPrefab);
                        gun.nextShotTime = time + gun.shotDelay;


                        ecb.SetComponent(bullet, new LocalTransform
                        {
                            Position = wordPos.Position,
                            Scale = localTrans.Scale,
                             Rotation = quaternion.identity
                             
                        });

                        
                        ecb.SetComponent<PhysicsVelocity>(bullet, new PhysicsVelocity
                        {
                            Linear = wordPos.Forward * gun.bulletSpeed
                        });
                    }

                    
                }
            }).Schedule();

            Dependency.Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

    }

}
