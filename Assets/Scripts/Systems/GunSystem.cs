using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
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
                            Rotation = quaternion.LookRotation(wordPos.Forward, math.up())
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