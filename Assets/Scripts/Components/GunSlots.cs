using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
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
           
            AddComponent<GunSlotsComponent>(entity, new GunSlotsComponent { 
                slot1 = GetEntity(authoring.slot1, TransformUsageFlags.Dynamic),
                slot2 = GetEntity(authoring.slot2, TransformUsageFlags.Dynamic),
                slot3 = GetEntity(authoring.slot3, TransformUsageFlags.Dynamic),
                slot4 = GetEntity(authoring.slot4, TransformUsageFlags.Dynamic),
                slot5 = GetEntity(authoring.slot5, TransformUsageFlags.Dynamic),
                slot6 = GetEntity(authoring.slot6, TransformUsageFlags.Dynamic),

            });
            AddComponent(entity, new SpawnGunSlotsComponent { });
            SetComponentEnabled<SpawnGunSlotsComponent>(entity, false);
        }
    }

}
