using System.Numerics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{

    public partial class DetectAimTargetSystem : SystemBase
    {

        protected override void OnCreate()
        {

            base.OnCreate();
        }

        protected unsafe override void OnUpdate()
        {
            var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var aimTargetLookup = SystemAPI.GetComponentLookup<AimTargetComponent>();
            var detectedTargetLookUp = SystemAPI.GetComponentLookup<DetectedTargetComponent>();

            Entities.ForEach((Entity entity, ref PlayerAimClosestTargetComponent aimTarg, ref LocalTransform lTrans, in LocalToWorld localToWorld) =>
            {
                var targets = new NativeList<DistanceHit>(Allocator.Temp);
                var selfPos = localToWorld.Position;
                var hasObj = physics.CollisionWorld.CalculateDistance(new PointDistanceInput
                {
                    Position = selfPos,
                    MaxDistance = 10,

                    Filter = new CollisionFilter
                    {
                        BelongsTo = aimTarg.belongTo,
                        CollidesWith = aimTarg.collideWith,
                    }
                }, ref targets);


                if (hasObj)
                {
                    DistanceHit hitedTarget = default;

                    bool hasTarget = false;
                    for (int i = 0; i < targets.Length; i++)
                    {
                        hitedTarget = targets[i];
                        if (aimTargetLookup.HasComponent(hitedTarget.Entity))
                        {
                            hasTarget = true;
                            //var vector = hitedTarget.Position - selfPos;

                            //lTrans.Rotation = quaternion.LookRotation(vector, math.up());

                            break;
                        }


                    }

                    detectedTargetLookUp.SetComponentEnabled(entity, hasTarget);
                    var refObj = detectedTargetLookUp.GetRefRW(entity, false);

                    refObj.ValueRW.target = hitedTarget;

                }

                targets.Dispose();
            }).Run();
        }

    }

    public partial class AimToTargetSystem : SystemBase
    {

        protected override void OnCreate()
        {

            base.OnCreate();
        }

        protected unsafe override void OnUpdate()
        {
            var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var aimTargetLookup = SystemAPI.GetComponentLookup<AimTargetComponent>();
            var detectedTargetLookUp = SystemAPI.GetComponentLookup<DetectedTargetComponent>();

            Entities.ForEach((Entity entity, ref PlayerAimClosestTargetComponent aimTarg, ref LocalTransform lTrans, in LocalToWorld localToWorld, in DetectedTargetComponent detected) =>
            {


                var selfPos = localToWorld.Position;
                var vector = detected.target.Position - selfPos;

                lTrans.Rotation = quaternion.LookRotation(vector, math.up());


            }).Run();
        }

    }
}