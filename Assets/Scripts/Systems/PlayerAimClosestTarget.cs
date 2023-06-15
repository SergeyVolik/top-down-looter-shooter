using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SV.ECS
{
    public class PlayerAimClosestTarget : MonoBehaviour
    {
        public float aimDistance;

        public PhysicsCategoryTags belongTo;
        public PhysicsCategoryTags collideWith;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aimDistance);
        }
    }


    public struct DetectedTargetComponent : IComponentData, IEnableableComponent
    {
        public Entity target;
    }

    public struct PlayerAimClosestTargetComponent : IComponentData
    {
        public float aimDistance;

        public uint belongTo;
        public uint collideWith;
    }



    public class PlayerAimClosestTargetComponentComponentBaker : Baker<PlayerAimClosestTarget>
    {
        public override void Bake(PlayerAimClosestTarget authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerAimClosestTargetComponent
            {
                aimDistance = authoring.aimDistance,
                belongTo = authoring.belongTo.Value,
                collideWith = authoring.collideWith.Value,

            });

            AddComponent(entity, new DetectedTargetComponent());
            SetComponentEnabled<DetectedTargetComponent>(entity, false);

        }
    }

}
