using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public partial class CastAimTargetSystem : SystemBase
    {

        protected override void OnCreate()
        {

            base.OnCreate();
        }

        protected unsafe override void OnUpdate()
        {
            var physics = GetSingleton<PhysicsWorldSingleton>();

            //physics.CollisionWorld.CalculateDistance(new PointDistanceInput { 
            //     Position = Vector3.zero,
            //      MaxDistance = 10
            //});
        }

    }
}