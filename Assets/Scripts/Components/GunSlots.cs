using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace SV.ECS
{
    public class GunSlots : MonoBehaviour
    {
        public Transform slot1;
        public Transform slot2;
        public Transform slot3;

        public Transform mainGunSlot;

        public WeaponSO mainGun;

        public WeaponSO gun1;
        public WeaponSO gun2;
        public WeaponSO gun3;

    }


    public struct GunSlotsComponent : IComponentData
    {
        public Entity slot1;
        public Entity slot2;
        public Entity slot3;

        public Entity mainSlot;
    }
    public struct SpawnGunSlot1Component : IComponentData, IEnableableComponent
    {
        public Entity gunPrefab;
    }
    public struct SpawnGunSlot2Component : IComponentData, IEnableableComponent
    {
        public Entity gunPrefab;
    }
    public struct SpawnGunSlot3Component : IComponentData, IEnableableComponent
    {
        public Entity gunPrefab;
    }
    public struct SpawnGunMainComponent : IComponentData, IEnableableComponent
    {
        public Entity gunPrefab;
    }





    public class GunSlotsBaker : Baker<GunSlots>
    {
        public override void Bake(GunSlots authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<GunSlotsComponent>(entity, new GunSlotsComponent
            {
                slot1 = GetEntity(authoring.slot1, TransformUsageFlags.Dynamic),
                slot2 = GetEntity(authoring.slot2, TransformUsageFlags.Dynamic),
                slot3 = GetEntity(authoring.slot3, TransformUsageFlags.Dynamic),
               
                mainSlot = GetEntity(authoring.mainGunSlot, TransformUsageFlags.Dynamic),

            });

            var gun1 = default(GameObject);
            var gun2 = default(GameObject);
            var gun3 = default(GameObject);

            var mainGun = default(GameObject);

            if (authoring.gun1 != null)
                gun1 = authoring.gun1.prefab;

            if (authoring.gun2 != null)
                gun2 = authoring.gun2.prefab;

            if (authoring.gun3 != null)
                gun3 = authoring.gun3.prefab;
         

            if (authoring.mainGun != null)
                mainGun = authoring.mainGun.prefab;


            AddComponent(entity, new SpawnGunMainComponent
            {
                 gunPrefab = GetEntity(mainGun, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new SpawnGunSlot1Component
            {
                gunPrefab = GetEntity(gun1, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new SpawnGunSlot2Component
            {
                gunPrefab = GetEntity(gun2, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new SpawnGunSlot3Component
            {
                gunPrefab = GetEntity(gun3, TransformUsageFlags.Dynamic)
            });

           


        }
    }


    public partial struct SpawnGunsSystem : ISystem
    {
        public Entity SpawnGun(ref EntityCommandBuffer ecb, Entity Slot, Entity gunPrefab, Entity owner)
        {
            Entity instance = Entity.Null;
            if (gunPrefab != Entity.Null)
            {
                instance = ecb.Instantiate(gunPrefab);

                ecb.AddComponent(instance, new Parent
                {
                    Value = Slot
                });

                ecb.SetComponent(instance, new LocalTransform
                {
                    Scale = 1f
                });

                ecb.SetComponent(instance, new OwnerComponent
                {
                    value = owner
                });

                ecb.RemoveComponent<Disabled>(Slot);
            }
            else {
                ecb.AddComponent<Disabled>(Slot);
            }

            return instance;
        }
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecbEnd = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var childLookUp = SystemAPI.GetBufferLookup<Child>();
            foreach (var (spawnReq, slots, spawnReqE, e) in SystemAPI.Query<
                RefRO<SpawnGunMainComponent>,
                RefRO<GunSlotsComponent>,
                EnabledRefRW<SpawnGunMainComponent>
                >().WithEntityAccess())
            {
                Debug.Log("SpawnMainGun");
                var spawnData = spawnReq.ValueRO;

                if (childLookUp.HasBuffer(slots.ValueRO.mainSlot))
                {
                    if (childLookUp.TryGetBuffer(slots.ValueRO.mainSlot, out var buffer))
                    {
                        foreach (var item in buffer)
                        {
                            ecb.DestroyEntity(item.Value);
                        }
                    }
                   
                }


                if (spawnData.gunPrefab != Entity.Null)
                {
                    var instance = SpawnGun(ref ecb, slots.ValueRO.mainSlot, spawnData.gunPrefab, e);
                   
                }
                spawnReqE.ValueRW = false;


            }

            //foreach (var (spawnReq, slots, spawnReqE, currentGun, e) in SystemAPI.Query<
            //    RefRO<SpawnGunSlot1Component>,
            //    RefRO<GunSlotsComponent>,
            //    EnabledRefRW<SpawnGunSlot1Component>,
            //    RefRW<CurrentGunSlot1Component>>().WithEntityAccess())
            //{

            //    var spawnData = spawnReq.ValueRO;
            //    var instance = SpawnGun(ref ecb, slots.ValueRO.slot1, spawnData.gunPrefab, e);
            //    spawnReqE.ValueRW = false;
            //    var currentGunE = currentGun.ValueRO.gun;
            //    if (currentGunE != Entity.Null)
            //        ecb.DestroyEntity(currentGun.ValueRO.gun);
            //    currentGun.ValueRW.gun = instance;

            //}

            //foreach (var (spawnReq, slots, spawnReqE, currentGun, e) in SystemAPI.Query<
            //   RefRO<SpawnGunSlot2Component>,
            //   RefRO<GunSlotsComponent>,
            //   EnabledRefRW<SpawnGunSlot2Component>,
            //   RefRW<CurrentGunSlot2Component>>().WithEntityAccess())
            //{

            //    var spawnData = spawnReq.ValueRO;
            //    var instance = SpawnGun(ref ecb, slots.ValueRO.slot2, spawnData.gunPrefab, e);
            //    spawnReqE.ValueRW = false;
            //    var currentGunE = currentGun.ValueRO.gun;
            //    if (currentGunE != Entity.Null)
            //        ecb.DestroyEntity(currentGun.ValueRO.gun);
            //    currentGun.ValueRW.gun = instance;
            //}

            //foreach (var (spawnReq, slots, spawnReqE, currentGun, e) in SystemAPI.Query<
            //  RefRO<SpawnGunSlot3Component>,
            //  RefRO<GunSlotsComponent>,
            //  EnabledRefRW<SpawnGunSlot3Component>,
            //  RefRW<CurrentGunSlot3Component>>().WithEntityAccess())
            //{

            //    var spawnData = spawnReq.ValueRO;
            //    var instance = SpawnGun(ref ecb, slots.ValueRO.slot3, spawnData.gunPrefab, e);
            //    spawnReqE.ValueRW = false;
            //    var currentGunE = currentGun.ValueRO.gun;
            //    if (currentGunE != Entity.Null)
            //        ecb.DestroyEntity(currentGun.ValueRO.gun);
            //    currentGun.ValueRW.gun = instance;
            //}
        }
    }

}
