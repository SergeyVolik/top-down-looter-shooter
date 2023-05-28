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

            var ecsSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();


            EntityCommandBuffer ecb = ecsSystem.CreateCommandBuffer(World.Unmanaged);
            var ltw = GetComponentLookup<LocalToWorld>();

            var transLookUp = GetComponentLookup<LocalTransform>();
            var ownerlookup = GetComponentLookup<OwnerComponent>();
            Entities.ForEach((Entity e, ref GunComponent gun, ref IndividualRandomComponent rnd) =>
            {

                if (gun.nextShotTime <= time)
                {
                    var gunOwnerEntity = Entity.Null;

                    if (ownerlookup.TryGetComponent(e, out var gunOwner))
                        gunOwnerEntity = gunOwner.value;

                    if (ltw.TryGetComponent(gun.bulletSpawnPos, out var wordPos) && transLookUp.TryGetComponent(gun.bulletSpawnPos, out var localTrans))
                    {

                        for (int i = 0; i < gun.bulletsInShot; i++)
                        {
                            var bullet = ecb.Instantiate(gun.bulletPrefab);
                            gun.nextShotTime = time + gun.shotDelay;

                            var vector = wordPos.Forward;

                            var rndValue = rnd.Value.NextFloat(-gun.Angle, gun.Angle);


                            var rot = quaternion.AxisAngle(wordPos.Up, math.radians(rndValue));
                            var nexVector = math.mul(rot, vector);


                            vector = nexVector;

                            ecb.SetComponent(bullet, new LocalTransform
                            {
                                Position = wordPos.Position,
                                Scale = localTrans.Scale,
                                Rotation = quaternion.LookRotation(vector, math.up())
                            });


                            ecb.SetComponent(bullet, new OwnerComponent
                            {
                                value = gunOwnerEntity
                            });

                            ecb.SetComponent(bullet, new PhysicsVelocity
                            {
                                Linear = vector * gun.bulletSpeed
                            });
                        }

                    }


                }
            }).Schedule();


        }

    }




}