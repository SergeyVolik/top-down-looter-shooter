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
        public Transform slot4;
        public Transform slot5;
        public Transform slot6;

        public GameObject gun1;
        public GameObject gun2;
        public GameObject gun3;
        public GameObject gun4;
        public GameObject gun5;
        public GameObject gun6;
    }


    public struct GunSlotsComponent : IComponentData
    {
        public Entity slot1;
        public Entity slot2;
        public Entity slot3;
        public Entity slot4;
        public Entity slot5;
        public Entity slot6;
    }

    public struct SpawnGunSlotsComponent : IComponentData, IEnableableComponent
    {
        public Entity slot1Gun;
        public Entity slot2Gun;
        public Entity slot3Gun;
        public Entity slot4Gun;
        public Entity slot5Gun;
        public Entity slot6Gun;
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
                slot4 = GetEntity(authoring.slot4, TransformUsageFlags.Dynamic),
                slot5 = GetEntity(authoring.slot5, TransformUsageFlags.Dynamic),
                slot6 = GetEntity(authoring.slot6, TransformUsageFlags.Dynamic),

            });
            AddComponent(entity, new SpawnGunSlotsComponent
            {
                slot1Gun = GetEntity(authoring.gun1, TransformUsageFlags.Dynamic),
                slot2Gun = GetEntity(authoring.gun2, TransformUsageFlags.Dynamic),
                slot3Gun = GetEntity(authoring.gun3, TransformUsageFlags.Dynamic),
                slot4Gun = GetEntity(authoring.gun4, TransformUsageFlags.Dynamic),
                slot5Gun = GetEntity(authoring.gun5, TransformUsageFlags.Dynamic),
                slot6Gun = GetEntity(authoring.gun6, TransformUsageFlags.Dynamic),
            });

            SetComponentEnabled<SpawnGunSlotsComponent>(entity, true);
        }
    }


    public partial struct SpawnGunsSystem : ISystem
    {
        public void SpawnGun(ref EntityCommandBuffer ecb, Entity Slot, Entity gunPrefab, Entity owner)
        {
            if (gunPrefab != Entity.Null)
            {
                var gunEntity = ecb.Instantiate(gunPrefab);

                ecb.AddComponent(gunEntity, new Parent
                {
                    Value = Slot
                });

                ecb.SetComponent(gunEntity, new LocalTransform
                {
                    Scale = 1f
                });

                ecb.SetComponent(gunEntity, new OwnerComponent
                {
                    value = owner
                });
            }
        }
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
           
            foreach (var (spawnReq, slots, spawnReqE, e) in SystemAPI.Query<RefRO<SpawnGunSlotsComponent>, RefRO<GunSlotsComponent>, EnabledRefRW<SpawnGunSlotsComponent>>().WithEntityAccess())
            {
                var spawnData = spawnReq.ValueRO;

                SpawnGun(ref ecb, slots.ValueRO.slot1, spawnData.slot1Gun, e);
                SpawnGun(ref ecb, slots.ValueRO.slot2, spawnData.slot2Gun, e);
                SpawnGun(ref ecb, slots.ValueRO.slot3, spawnData.slot3Gun, e);
                SpawnGun(ref ecb, slots.ValueRO.slot4, spawnData.slot4Gun, e);
                SpawnGun(ref ecb, slots.ValueRO.slot5, spawnData.slot5Gun, e);
                SpawnGun(ref ecb, slots.ValueRO.slot6, spawnData.slot6Gun, e);
                spawnReqE.ValueRW = false;
            }
        }
    }

}
