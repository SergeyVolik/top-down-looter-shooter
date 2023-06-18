using System.Collections.Generic;
using System.Numerics;
using Unity.Burst;
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

        [BurstCompile]
        protected unsafe override void OnUpdate()
        {


            var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var aimTargetLookup = SystemAPI.GetComponentLookup<AimTargetComponent>();
            var detectedTargetLookUp = SystemAPI.GetComponentLookup<DetectedTargetComponent>();
            var gunActivatedLookUp = SystemAPI.GetComponentLookup<GunActivated>();

            Dependency = Entities.WithNone<Disabled>().ForEach((Entity entity, ref PlayerAimClosestTargetComponent aimTarg, ref LocalTransform lTrans, in LocalToWorld localToWorld) =>
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

                    if (gunActivatedLookUp.HasComponent(entity))
                        gunActivatedLookUp.SetComponentEnabled(entity, hasTarget);

                    var refObj = detectedTargetLookUp.GetRefRW(entity);

                    refObj.ValueRW.target = detectedEntity;

                }

                targets.Dispose();
            }).WithBurst().Schedule(Dependency);
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



        [WithAll(typeof(PlayerAimClosestTargetComponent))]

        public partial struct ActivateGunJobJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<DetectedTargetComponent> detectedTargetLookUp;

            [ReadOnly]
            public BufferLookup<Child> chilLookup;

            [ReadOnly]
            public ComponentLookup<GunComponent> gunLookup;
            public ComponentLookup<GunActivated> gunActivatedLookup;

            [BurstCompile]
            public void Execute(Entity e, in GunSlotsComponent slot)
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
            }
        }

        [WithAll(typeof(PlayerAimClosestTargetComponent), typeof(LocalToWorld), typeof(LocalTransform))]
        public partial struct LookAtTargetJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalToWorld> localToWorldLookup;
            public ComponentLookup<LocalTransform> localTransformLookup1;

            [ReadOnly]
            public ComponentLookup<Parent> parentLookup;
            [BurstCompile]
            public void Execute(Entity e, in DetectedTargetComponent detected)
            {
                float3 target = default;
                if (localToWorldLookup.TryGetComponent(detected.target, out var ltwTarget))
                {
                    target = ltwTarget.Position;
                }

                if (localToWorldLookup.TryGetComponent(e, out ltwTarget))
                {
                    target.y = ltwTarget.Position.y;
                }




                var transRef = localTransformLookup1.GetRefRW(e);

                transRef.ValueRW.LookAt(e, target, ref parentLookup, ref localToWorldLookup);
            }
        }

        protected override void OnUpdate()
        {



            var detectedTargetLookUp = SystemAPI.GetComponentLookup<DetectedTargetComponent>(isReadOnly: true);

            ComponentLookup<LocalToWorld> localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true);
            ComponentLookup<LocalTransform> localTransformLookup1 = SystemAPI.GetComponentLookup<LocalTransform>(false);

            ComponentLookup<Parent> parentLookup = SystemAPI.GetComponentLookup<Parent>(isReadOnly: true);
            ComponentLookup<GunComponent> gunLookup = SystemAPI.GetComponentLookup<GunComponent>(isReadOnly: true);
            ComponentLookup<GunActivated> gunActivatedLookup = SystemAPI.GetComponentLookup<GunActivated>(false);

            BufferLookup<Child> chilLookup = SystemAPI.GetBufferLookup<Child>(isReadOnly: true);


            Dependency = new LookAtTargetJob
            {
                localToWorldLookup = localToWorldLookup,
                localTransformLookup1 = localTransformLookup1,
                parentLookup = parentLookup,
            }.Schedule(Dependency);

            Dependency = new ActivateGunJobJob
            {
                chilLookup = chilLookup,
                detectedTargetLookUp = detectedTargetLookUp,
                gunActivatedLookup = gunActivatedLookup,
                gunLookup = gunLookup,
            }.Schedule(Dependency);


        }

    }
}