using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateAfter(typeof(TransformSystemGroup))]
[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
public partial class OrbitCameraSystem : SystemBase
{


    protected override void OnCreate()
    {
        base.OnCreate();

        //Enabled = false;

    }

    public partial struct CameraJob : IJobEntity
    {

        [ReadOnly]
        public ComponentLookup<CameraTarget> camTarglookup;

        [ReadOnly]
        public ComponentLookup<LocalToWorld> ltwLookup;

        [ReadOnly]
        public ComponentLookup<KinematicCharacterBody> kinLookup;
        public CollisionWorld collisionWorld;
        public float fixedDeltaTime;

        public float deltaTime;
        public void Execute(Entity entity, ref OrbitCamera orbitCameraRW, in OrbitCameraInputs inputsRO, ref LocalTransform selfLocalTransformRef, DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> ignoredEntitiesBuffer)
        {
            //var selfLocalTransformRef = SystemAPI.GetComponentRW<LocalTransform>(entity);


            // if there is a followed entity, place the camera relatively to it
            if (orbitCameraRW.FollowedCharacterEntity != Entity.Null)
            {
                //var selfRotation = translation.Rotation;

                // Select the real camera target
                LocalToWorld targetEntityLocalToWorld = default;
                if (camTarglookup.HasComponent(orbitCameraRW.FollowedCharacterEntity))
                {
                    CameraTarget cameraTarget = camTarglookup.GetRefRO(orbitCameraRW.FollowedCharacterEntity).ValueRO;
                    targetEntityLocalToWorld = ltwLookup.GetRefRO(cameraTarget.TargetEntity).ValueRO;
                }
                else
                {
                    targetEntityLocalToWorld = ltwLookup.GetRefRO(orbitCameraRW.FollowedCharacterEntity).ValueRO;
                }

                // Rotation
                {
                    selfLocalTransformRef.Rotation = quaternion.LookRotationSafe(orbitCameraRW.PlanarForward, targetEntityLocalToWorld.Up);

                    // Handle rotating the camera along with character's parent entity (moving platform)
                    if (orbitCameraRW.RotateWithCharacterParent && kinLookup.HasComponent(orbitCameraRW.FollowedCharacterEntity))
                    {
                        KinematicCharacterBody characterBody = kinLookup.GetRefRO(orbitCameraRW.FollowedCharacterEntity).ValueRO;
                        KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref selfLocalTransformRef, in characterBody, fixedDeltaTime, deltaTime);
                        orbitCameraRW.PlanarForward = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(selfLocalTransformRef.Rotation), targetEntityLocalToWorld.Up));
                    }

                    // Yaw
                    float yawAngleChange = inputsRO.Look.x * orbitCameraRW.RotationSpeed;
                    quaternion yawRotation = quaternion.Euler(targetEntityLocalToWorld.Up * math.radians(yawAngleChange));
                    orbitCameraRW.PlanarForward = math.rotate(yawRotation, orbitCameraRW.PlanarForward);

                    // Pitch
                    orbitCameraRW.PitchAngle += -inputsRO.Look.y * orbitCameraRW.RotationSpeed;
                    orbitCameraRW.PitchAngle = math.clamp(orbitCameraRW.PitchAngle, orbitCameraRW.MinVAngle, orbitCameraRW.MaxVAngle);
                    quaternion pitchRotation = quaternion.Euler(math.right() * math.radians(orbitCameraRW.PitchAngle));

                    // Final rotation
                    selfLocalTransformRef.Rotation = quaternion.LookRotationSafe(orbitCameraRW.PlanarForward, targetEntityLocalToWorld.Up);
                    selfLocalTransformRef.Rotation = math.mul(selfLocalTransformRef.Rotation, pitchRotation);
                }

                float3 cameraForward = MathUtilities.GetForwardFromRotation(selfLocalTransformRef.Rotation);

                // Distance input
                float desiredDistanceMovementFromInput = inputsRO.Zoom * orbitCameraRW.DistanceMovementSpeed * deltaTime;
                orbitCameraRW.TargetDistance = math.clamp(orbitCameraRW.TargetDistance + desiredDistanceMovementFromInput, orbitCameraRW.MinDistance, orbitCameraRW.MaxDistance);
                orbitCameraRW.CurrentDistanceFromMovement = math.lerp(orbitCameraRW.CurrentDistanceFromMovement, orbitCameraRW.TargetDistance, MathUtilities.GetSharpnessInterpolant(orbitCameraRW.DistanceMovementSharpness, deltaTime));

                // Obstructions
                if (orbitCameraRW.ObstructionRadius > 0f)
                {
                    float obstructionCheckDistance = orbitCameraRW.CurrentDistanceFromMovement;

                    CameraObstructionHitsCollector collector = new CameraObstructionHitsCollector(ignoredEntitiesBuffer, cameraForward);
                    collisionWorld.SphereCastCustom<CameraObstructionHitsCollector>(
                        targetEntityLocalToWorld.Position,
                        orbitCameraRW.ObstructionRadius,
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
                        if (orbitCameraRW.PreventFixedUpdateJitter)
                        {
                            RigidBody hitBody = collisionWorld.Bodies[collector.ClosestHit.RigidBodyIndex];
                            LocalToWorld hitBodyLocalToWorld = ltwLookup.GetRefRO(hitBody.Entity).ValueRO;

                            hitBody.WorldFromBody = new RigidTransform(quaternion.LookRotationSafe(hitBodyLocalToWorld.Forward, hitBodyLocalToWorld.Up), hitBodyLocalToWorld.Position);

                            collector = new CameraObstructionHitsCollector(ignoredEntitiesBuffer, cameraForward);
                            hitBody.SphereCastCustom<CameraObstructionHitsCollector>(
                                targetEntityLocalToWorld.Position,
                                orbitCameraRW.ObstructionRadius,
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
                    if (orbitCameraRW.CurrentDistanceFromObstruction < newObstructedDistance)
                    {
                        // Move outer
                        orbitCameraRW.CurrentDistanceFromObstruction = math.lerp(orbitCameraRW.CurrentDistanceFromObstruction,
                            newObstructedDistance, MathUtilities.GetSharpnessInterpolant(orbitCameraRW.ObstructionOuterSmoothingSharpness, deltaTime));
                    }
                    else if (orbitCameraRW.CurrentDistanceFromObstruction > newObstructedDistance)
                    {
                        // Move inner
                        orbitCameraRW.CurrentDistanceFromObstruction = math.lerp(orbitCameraRW.CurrentDistanceFromObstruction,
                            newObstructedDistance, MathUtilities.GetSharpnessInterpolant(orbitCameraRW.ObstructionInnerSmoothingSharpness, deltaTime));
                    }
                }
                else
                {
                    orbitCameraRW.CurrentDistanceFromObstruction = orbitCameraRW.CurrentDistanceFromMovement;
                }

                // Calculate final camera position from targetposition + rotation + distance
                selfLocalTransformRef.Position = targetEntityLocalToWorld.Position + (-cameraForward * orbitCameraRW.CurrentDistanceFromObstruction); //math.lerp(selfLocalTransformRef.Position, targetEntityLocalToWorld.Position + (-cameraForward * orbitCameraRW.CurrentDistanceFromObstruction), fixedDeltaTime * 5);
                //selfLocalTransformRef.Rotation = 
                // Manually calculate the LocalToWorld since this is updating after the Transform systems, and the LtW is what rendering uses
                //LocalToWorld cameraLocalToWorld = new LocalToWorld();
                //cameraLocalToWorld.Value = new float4x4(selfLocalTransformRef.Rotation, selfLocalTransformRef.Position);

                //ltwLookup.GetRefRW(entity).ValueRW = cameraLocalToWorld;

            }
        }
    }
    protected unsafe override void OnUpdate()
    {

        float fixedDeltaTime = World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().RateManager.Timestep;

        float deltaTime = SystemAPI.Time.DeltaTime;
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<BuildPhysicsWorldData>().PhysicsData.PhysicsWorld.CollisionWorld;


        Dependency = new CameraJob   
        {
            deltaTime = deltaTime,
            fixedDeltaTime = fixedDeltaTime,
            ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            camTarglookup = SystemAPI.GetComponentLookup<CameraTarget>(isReadOnly: true),
            collisionWorld = collisionWorld,
            kinLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(isReadOnly: true)
        }.Schedule(Dependency);


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