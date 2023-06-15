using System.Collections.Generic;
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

        public struct SortDistaceHit : IComparer<DistanceHit>
        {
            public int Compare(DistanceHit x, DistanceHit y)
            {
                if (x.Distance > y.Distance)
                {
                    return 1;
                }
                if (x.Distance < y.Distance)
                {
                    return -1;
                }
                return 0;
            }
        }

        protected unsafe override void OnUpdate()
        {

          
            var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var aimTargetLookup = SystemAPI.GetComponentLookup<AimTargetComponent>();
            var detectedTargetLookUp = SystemAPI.GetComponentLookup<DetectedTargetComponent>();
            var gunActivatedLookUp = SystemAPI.GetComponentLookup<GunActivated>();

            Entities.WithNone<Disabled>().ForEach((Entity entity, ref PlayerAimClosestTargetComponent aimTarg, ref LocalTransform lTrans, in LocalToWorld localToWorld) =>
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

                targets.Sort(new SortDistaceHit());


                if (hasObj)
                {
                    DistanceHit hitedTarget = default;
                    Entity detectedEntity = default;
                    bool hasTarget = false;
                    for (int i = 0; i < targets.Length; i++)
                    {
                        hitedTarget = targets[i];
                        if (aimTargetLookup.TryGetComponent(hitedTarget.Entity, out var aimTarg1))
                        {
                            hasTarget = true;
                            
                            detectedEntity = aimTarg1.AimPointEntity;
                           

                            break;
                        }


                    }

                    detectedTargetLookUp.SetComponentEnabled(entity, hasTarget);

                    if(gunActivatedLookUp.HasComponent(entity))
                        gunActivatedLookUp.SetComponentEnabled(entity, hasTarget);

                    var refObj = detectedTargetLookUp.GetRefRW(entity);

                    refObj.ValueRW.target = detectedEntity;

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




        protected unsafe override void OnUpdate()
        {


            var aimTargetLookup = SystemAPI.GetComponentLookup<AimTargetComponent>();
            var detectedTargetLookUp = SystemAPI.GetComponentLookup<DetectedTargetComponent>();

            ComponentLookup<LocalToWorld> localTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            ComponentLookup<Parent> parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
            ComponentLookup<GunComponent> gunLookup = SystemAPI.GetComponentLookup<GunComponent>(true);
            ComponentLookup<GunActivated> gunActivatedLookup = SystemAPI.GetComponentLookup<GunActivated>(false);

            BufferLookup<Child> chilLookup = SystemAPI.GetBufferLookup<Child>(true);

            Entities.ForEach((Entity e, ref LocalTransform transform, in LocalToWorld localToWorld, in DetectedTargetComponent detected) =>
            {

                float3 target = default;
                if (localTransformLookup.TryGetComponent(detected.target, out var ltwTarget))
                {
                    target = ltwTarget.Position;
                }

                target.y = localToWorld.Position.y;

               

                transform.LookAt(e, target, ref parentLookup, ref localTransformLookup);

            }).WithAll<PlayerAimClosestTargetComponent>().Run();

            Entities.ForEach((Entity e, in GunSlotsComponent slot) =>
            {
                var detected = detectedTargetLookUp.IsComponentEnabled(e);
              

                if (chilLookup.TryGetBuffer(slot.mainSlot, out var buffer))
                {
                    foreach (var item in buffer)
                    {
                        if (gunLookup.HasComponent(item.Value))
                        {
                            gunActivatedLookup.SetComponentEnabled(item.Value, detected);
                        }

                    }
                }
              

            }).WithAll<PlayerAimClosestTargetComponent>().Run();
        }

    }
}