using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateAfter(typeof(PresentationSystemGroup))]
public partial class OrbitCameraSystem : SystemBase
{
   

    protected override void OnCreate()
    {
        base.OnCreate();

      
    }

    protected unsafe override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float fixedDeltaTime = World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().RateManager.Timestep;
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<BuildPhysicsWorldData>().PhysicsData.PhysicsWorld.CollisionWorld;


        foreach (var (orbitCameraRW, inputsRO, selfLocalTransformRef, ignoredEntitiesBuffer, entity) in SystemAPI.Query<RefRW<OrbitCamera>, RefRO<OrbitCameraInputs>, RefRW<LocalTransform>, DynamicBuffer<OrbitCameraIgnoredEntityBufferElement>>().WithEntityAccess())
        {
            //var selfLocalTransformRef = SystemAPI.GetComponentRW<LocalTransform>(entity);


            // if there is a followed entity, place the camera relatively to it
            if (orbitCameraRW.ValueRO.FollowedCharacterEntity != Entity.Null)
            {
                //var selfRotation = translation.Rotation;

                // Select the real camera target
                LocalToWorld targetEntityLocalToWorld = default;
                if (SystemAPI.HasComponent<CameraTarget>(orbitCameraRW.ValueRO.FollowedCharacterEntity))
                {
                    CameraTarget cameraTarget = SystemAPI.GetComponent<CameraTarget>(orbitCameraRW.ValueRO.FollowedCharacterEntity);
                    targetEntityLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(cameraTarget.TargetEntity);
                }
                else
                {
                    targetEntityLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(orbitCameraRW.ValueRO.FollowedCharacterEntity);
                }

                // Rotation
                {
                    selfLocalTransformRef.ValueRW.Rotation = quaternion.LookRotationSafe(orbitCameraRW.ValueRO.PlanarForward, targetEntityLocalToWorld.Up);

                    // Handle rotating the camera along with character's parent entity (moving platform)
                    if (orbitCameraRW.ValueRW.RotateWithCharacterParent && SystemAPI.HasComponent<KinematicCharacterBody>(orbitCameraRW.ValueRO.FollowedCharacterEntity))
                    {
                        KinematicCharacterBody characterBody = SystemAPI.GetComponent<KinematicCharacterBody>(orbitCameraRW.ValueRO.FollowedCharacterEntity);
                        KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref selfLocalTransformRef.ValueRW, in characterBody, fixedDeltaTime, deltaTime);
                        orbitCameraRW.ValueRW.PlanarForward = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(selfLocalTransformRef.ValueRO.Rotation), targetEntityLocalToWorld.Up));
                    }

                    // Yaw
                    float yawAngleChange = inputsRO.ValueRO.Look.x * orbitCameraRW.ValueRO.RotationSpeed;
                    quaternion yawRotation = quaternion.Euler(targetEntityLocalToWorld.Up * math.radians(yawAngleChange));
                    orbitCameraRW.ValueRW.PlanarForward = math.rotate(yawRotation, orbitCameraRW.ValueRO.PlanarForward);

                    // Pitch
                    orbitCameraRW.ValueRW.PitchAngle += -inputsRO.ValueRO.Look.y * orbitCameraRW.ValueRO.RotationSpeed;
                    orbitCameraRW.ValueRW.PitchAngle = math.clamp(orbitCameraRW.ValueRO.PitchAngle, orbitCameraRW.ValueRO.MinVAngle, orbitCameraRW.ValueRO.MaxVAngle);
                    quaternion pitchRotation = quaternion.Euler(math.right() * math.radians(orbitCameraRW.ValueRW.PitchAngle));

                    // Final rotation
                    selfLocalTransformRef.ValueRW.Rotation = quaternion.LookRotationSafe(orbitCameraRW.ValueRO.PlanarForward, targetEntityLocalToWorld.Up);
                    selfLocalTransformRef.ValueRW.Rotation = math.mul(selfLocalTransformRef.ValueRO.Rotation, pitchRotation);
                }

                float3 cameraForward = MathUtilities.GetForwardFromRotation(selfLocalTransformRef.ValueRO.Rotation);

                // Distance input
                float desiredDistanceMovementFromInput = inputsRO.ValueRO.Zoom * orbitCameraRW.ValueRO.DistanceMovementSpeed * deltaTime;
                orbitCameraRW.ValueRW.TargetDistance = math.clamp(orbitCameraRW.ValueRO.TargetDistance + desiredDistanceMovementFromInput, orbitCameraRW.ValueRO.MinDistance, orbitCameraRW.ValueRO.MaxDistance);
                orbitCameraRW.ValueRW.CurrentDistanceFromMovement = math.lerp(orbitCameraRW.ValueRO.CurrentDistanceFromMovement, orbitCameraRW.ValueRO.TargetDistance, MathUtilities.GetSharpnessInterpolant(orbitCameraRW.ValueRO.DistanceMovementSharpness, deltaTime));

                // Obstructions
                if (orbitCameraRW.ValueRO.ObstructionRadius > 0f)
                {
                    float obstructionCheckDistance = orbitCameraRW.ValueRO.CurrentDistanceFromMovement;

                    CameraObstructionHitsCollector collector = new CameraObstructionHitsCollector(ignoredEntitiesBuffer, cameraForward);
                    collisionWorld.SphereCastCustom<CameraObstructionHitsCollector>(
                        targetEntityLocalToWorld.Position,
                        orbitCameraRW.ValueRO.ObstructionRadius,
                        -cameraForward,
                        obstructionCheckDistance,
                        ref collector,
                        CollisionFilter.Default,
                        QueryInteraction.IgnoreTriggers);

                    float newObstructedDistance = obstructionCheckDistance;
                    if (collector.NumHits > 0)
                    {
                        newObstructedDistance = obstructionCheckDistance * collector.ClosestHit.Fraction;

                        // Redo cast with the interpolated body transform to prevent FixedUpdate jitter in obstruction detection
                        if (orbitCameraRW.ValueRO.PreventFixedUpdateJitter)
                        {
                            RigidBody hitBody = collisionWorld.Bodies[collector.ClosestHit.RigidBodyIndex];
                            LocalToWorld hitBodyLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(hitBody.Entity);

                            hitBody.WorldFromBody = new RigidTransform(quaternion.LookRotationSafe(hitBodyLocalToWorld.Forward, hitBodyLocalToWorld.Up), hitBodyLocalToWorld.Position);

                            collector = new CameraObstructionHitsCollector(ignoredEntitiesBuffer, cameraForward);
                            hitBody.SphereCastCustom<CameraObstructionHitsCollector>(
                                targetEntityLocalToWorld.Position,
                                orbitCameraRW.ValueRO.ObstructionRadius,
                                -cameraForward,
                                obstructionCheckDistance,
                                ref collector,
                                CollisionFilter.Default,
                                QueryInteraction.IgnoreTriggers);

                            if (collector.NumHits > 0)
                            {
                                newObstructedDistance = obstructionCheckDistance * collector.ClosestHit.Fraction;
                            }
                        }
                    }

                    // Update current distance based on obstructed distance
                    if (orbitCameraRW.ValueRO.CurrentDistanceFromObstruction < newObstructedDistance)
                    {
                        // Move outer
                        orbitCameraRW.ValueRW.CurrentDistanceFromObstruction = math.lerp(orbitCameraRW.ValueRO.CurrentDistanceFromObstruction,
                            newObstructedDistance, MathUtilities.GetSharpnessInterpolant(orbitCameraRW.ValueRO.ObstructionOuterSmoothingSharpness, deltaTime));
                    }
                    else if (orbitCameraRW.ValueRO.CurrentDistanceFromObstruction > newObstructedDistance)
                    {
                        // Move inner
                        orbitCameraRW.ValueRW.CurrentDistanceFromObstruction = math.lerp(orbitCameraRW.ValueRO.CurrentDistanceFromObstruction,
                            newObstructedDistance, MathUtilities.GetSharpnessInterpolant(orbitCameraRW.ValueRO.ObstructionInnerSmoothingSharpness, deltaTime));
                    }
                }
                else
                {
                    orbitCameraRW.ValueRW.CurrentDistanceFromObstruction = orbitCameraRW.ValueRO.CurrentDistanceFromMovement;
                }

                // Calculate final camera position from targetposition + rotation + distance
                selfLocalTransformRef.ValueRW.Position = targetEntityLocalToWorld.Position + (-cameraForward * orbitCameraRW.ValueRO.CurrentDistanceFromObstruction);

                // Manually calculate the LocalToWorld since this is updating after the Transform systems, and the LtW is what rendering uses
                LocalToWorld cameraLocalToWorld = new LocalToWorld();
                cameraLocalToWorld.Value = new float4x4(selfLocalTransformRef.ValueRO.Rotation, selfLocalTransformRef.ValueRO.Position);
                SystemAPI.SetComponent(entity, cameraLocalToWorld);
            }
        }
    }
}

public struct CameraObstructionHitsCollector : ICollector<ColliderCastHit>
{
    public bool EarlyOutOnFirstHit => false;
    public float MaxFraction => 1f;
    public int NumHits { get; private set; }

    public ColliderCastHit ClosestHit;

    private float _closestHitFraction;
    private float3 _cameraDirection;
    private DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> _ignoredEntitiesBuffer;

    public CameraObstructionHitsCollector(DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> ignoredEntitiesBuffer, float3 cameraDirection)
    {
        NumHits = 0;
        ClosestHit = default;

        _closestHitFraction = float.MaxValue;
        _cameraDirection = cameraDirection;
        _ignoredEntitiesBuffer = ignoredEntitiesBuffer;
    }

    public bool AddHit(ColliderCastHit hit)
    {
        if (math.dot(hit.SurfaceNormal, _cameraDirection) < 0f || !PhysicsUtilities.IsCollidable(hit.Material))
        {
            return false;
        }

        for (int i = 0; i < _ignoredEntitiesBuffer.Length; i++)
        {
            if (_ignoredEntitiesBuffer[i].Entity == hit.Entity)
            {
                return false;
            }
        }

        // Process valid hit
        if (hit.Fraction < _closestHitFraction)
        {
            _closestHitFraction = hit.Fraction;
            ClosestHit = hit;
        }
        NumHits++;

        return true;
    }
}