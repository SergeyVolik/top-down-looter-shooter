using System.Numerics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

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
                    var refObj = detectedTargetLookUp.GetRefRW(entity);

                    refObj.ValueRW.target = hitedTarget;

                }

                targets.Dispose();
            }).Run();
        }

    }

    public static class LocalTransformExtentions
    {
        public static void LookAt(this ref LocalTransform transform, Entity selfEntity, float3 tragetWorldPosition, ref ComponentLookup<Parent> parentLookup, ref ComponentLookup<LocalToWorld> localTransformLookup)
        {

           

       
            if (parentLookup.TryGetComponent(selfEntity, out var parent) && localTransformLookup.TryGetComponent(parent.Value, out var parentL2W))
            {

                tragetWorldPosition = math.inverse(parentL2W.Value).TransformPoint(tragetWorldPosition);
            }

            tragetWorldPosition -= transform.Position;
            quaternion rotation = quaternion.LookRotationSafe(tragetWorldPosition, math.up());
            transform.Rotation = rotation;

        }
            
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class AimToTargetSystem : SystemBase
    {

        protected override void OnCreate()
        {

            base.OnCreate();
        }
        public static float3 GetScale(float4x4 matrix) => new float3(
            math.length(matrix.c0.xyz),
            math.length(matrix.c1.xyz),
            math.length(matrix.c2.xyz));

        void LookAt(Entity e, float3 target, float3 worldUp)
        {
            if (SystemAPI.HasComponent<Parent>(e))
            {
                Entity parent = SystemAPI.GetComponent<Parent>(e).Value;
                float4x4 parentL2W = SystemAPI.GetComponent<LocalToWorld>(parent).Value;
                target = math.inverse(parentL2W).TransformPoint(target);
            }

            LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(e);
            quaternion rotation = quaternion.LookRotationSafe(target, worldUp);
            SystemAPI.SetComponent(e, transform.WithRotation(rotation));
        }


        protected unsafe override void OnUpdate()
        {


            var aimTargetLookup = SystemAPI.GetComponentLookup<AimTargetComponent>();
            var detectedTargetLookUp = SystemAPI.GetComponentLookup<DetectedTargetComponent>();

            ComponentLookup<LocalToWorld> localTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            ComponentLookup<Parent> parentLookup = SystemAPI.GetComponentLookup<Parent>(true);


            Entities.ForEach((Entity e, ref PlayerAimClosestTargetComponent aimTarg, ref LocalTransform transform, ref LocalToWorld localToWorld, in DetectedTargetComponent detected) =>
            {

                float3 target = default;
                if (localTransformLookup.TryGetComponent(detected.target.Entity, out var ltwTarget))
                {
                    target = ltwTarget.Position;
                }

                target.y = localToWorld.Position.y;

                //if (parentLookup.TryGetComponent(e, out var parent) && localTransformLookup.TryGetComponent(parent.Value, out var parentL2W))
                //{



                //    target = math.inverse(parentL2W.Value).TransformPoint(target);
                //}

                //target -= transform.Position;


                //quaternion rotation = quaternion.LookRotationSafe(target, math.up());

                //transform.Rotation = rotation;

                transform.LookAt(e, target, ref parentLookup, ref localTransformLookup);

            }).Run();
        }

    }
}