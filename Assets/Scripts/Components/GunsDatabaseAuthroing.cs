using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class GunsDatabaseAuthroing : MonoBehaviour
    {
        public WeaponSO[] weapons;

    }

    public struct GunsDatabase : ICleanupComponentData
    {
        public NativeHashMap<Guid, Entity> guns;
    }
    public struct GunsBuffer : IBufferElementData
    {
        public Guid guid;
        public Entity gun;
    }





    public partial struct ClenupGunsDatabaseSystem : ISystem
    {


        public void OnCreate(ref SystemState state)
        {
            state.RequireAnyForUpdate(
                SystemAPI.QueryBuilder().WithAll<GunsDatabase>().WithNone<GunsBuffer>().Build(),
                 SystemAPI.QueryBuilder().WithNone<GunsDatabase>().WithAll<GunsBuffer>().Build()
                );
        }
        public void OnUpdate(ref SystemState state)
        {


            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (item, e) in SystemAPI.Query<GunsDatabase>().WithNone<GunsBuffer>().WithEntityAccess())
            {
                Debug.Log("GunsDatabase Removed");
                ecb.RemoveComponent<GunsDatabase>(e);
            }

            foreach (var (buffer, e) in SystemAPI.Query<DynamicBuffer<GunsBuffer>>().WithNone<GunsDatabase>().WithEntityAccess())
            {
                var hasmap = new NativeHashMap<Guid, Entity>(buffer.Length, Allocator.Persistent);

                var gunsDatabase = new GunsDatabase
                {
                    guns = hasmap
                };

                foreach (var item in buffer)
                {

                    gunsDatabase.guns.Add(item.guid, item.gun);
                }

                ecb.AddComponent(e, gunsDatabase);
                Debug.Log("GunsDatabase Added");

            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }



    public class GunsDatabaseBaker : Baker<GunsDatabaseAuthroing>
    {
        public override void Bake(GunsDatabaseAuthroing authoring)
        {

            var entity = GetEntity(TransformUsageFlags.None);



            if (authoring.weapons != null)
            {

                var buffer = AddBuffer<GunsBuffer>(entity);

                foreach (var item in authoring.weapons)
                {
                    buffer.Add(new GunsBuffer
                    {
                        gun = GetEntity(item.prefab, TransformUsageFlags.Dynamic),
                        guid = item.GetGuid()
                    });

                }



            }



        }
    }



}
