using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using Rival;
using Unity.Collections.LowLevel.Unsafe;
using Unity.NetCode;
using UnityEngine;
using System;
using Unity.Burst.Intrinsics;

namespace Rival.Samples.OnlineFPS
{
    [Serializable]
    public struct RPCCharacterDeathVFX : IRpcCommand
    {
        public float3 Position;
    }

    [UpdateInGroup(typeof(KinematicCharacterUpdateGroup))]
    public partial class OnlineFPSCharacterMovementSystem : SystemBase
    {
      
        public PredictedSimulationSystemGroup GhostPredictionSystemGroup;
        public EntityQuery PredictedCharacterQuery;

        [BurstCompile]
        public struct OnlineFPSCharacterJob : IJobChunk
        {
            public NetworkTick Tick;
            public float DeltaTime;
            [ReadOnly]
            public CollisionWorld CollisionWorld;

            [ReadOnly]
            public ComponentLookup<PhysicsVelocity> PhysicsVelocityFromEntity;
            [ReadOnly]
            public ComponentLookup<PhysicsMass> PhysicsMassFromEntity;
            [ReadOnly]
            public ComponentLookup<StoredKinematicCharacterBodyProperties> StoredKinematicCharacterBodyPropertiesFromEntity;
            [ReadOnly]
            public ComponentLookup<TrackedTransform> TrackedTransformFromEntity;

            [ReadOnly]
            public EntityTypeHandle EntityType;
            public ComponentTypeHandle<LocalTransform> TranslationType;

            public ComponentTypeHandle<KinematicCharacterBody> KinematicCharacterBodyType;
            [ReadOnly]
            public ComponentTypeHandle<PhysicsCollider> PhysicsColliderType;
            public BufferTypeHandle<KinematicCharacterHit> CharacterHitsBufferType;
            public BufferTypeHandle<KinematicVelocityProjectionHit> VelocityProjectionHitsBufferType;
            public BufferTypeHandle<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBufferType;
            public BufferTypeHandle<StatefulKinematicCharacterHit> StatefulCharacterHitsBufferType;

            [ReadOnly]
            public ComponentTypeHandle<PredictedGhost> PredictedGhostType;
            public ComponentTypeHandle<OnlineFPSCharacterComponent> OnlineFPSCharacterType;
            [ReadOnly]
            public ComponentTypeHandle<OnlineFPSCharacterInputs> OnlineFPSCharacterInputsType;

            [NativeDisableContainerSafetyRestriction]
            public NativeList<int> TmpRigidbodyIndexesProcessed;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<Unity.Physics.RaycastHit> TmpRaycastHits;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<ColliderCastHit> TmpColliderCastHits;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<DistanceHit> TmpDistanceHits;



            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> chunkEntities = chunk.GetNativeArray(EntityType);
                NativeArray<LocalTransform> chunkTranslations = chunk.GetNativeArray(ref TranslationType);

                NativeArray<KinematicCharacterBody> chunkCharacterBodies = chunk.GetNativeArray(ref KinematicCharacterBodyType);
                NativeArray<PhysicsCollider> chunkPhysicsColliders = chunk.GetNativeArray(ref PhysicsColliderType);
                BufferAccessor<KinematicCharacterHit> chunkCharacterHitBuffers = chunk.GetBufferAccessor(ref CharacterHitsBufferType);
                BufferAccessor<KinematicVelocityProjectionHit> chunkVelocityProjectionHitBuffers = chunk.GetBufferAccessor(ref VelocityProjectionHitsBufferType);
                BufferAccessor<KinematicCharacterDeferredImpulse> chunkCharacterDeferredImpulsesBuffers = chunk.GetBufferAccessor(ref CharacterDeferredImpulsesBufferType);
                BufferAccessor<StatefulKinematicCharacterHit> chunkStatefulCharacterHitsBuffers = chunk.GetBufferAccessor(ref StatefulCharacterHitsBufferType);
                NativeArray<PredictedGhost> chunkPredictedGhosts = chunk.GetNativeArray(ref PredictedGhostType);
                NativeArray<OnlineFPSCharacterComponent> chunkOnlineFPSCharacters = chunk.GetNativeArray(ref OnlineFPSCharacterType);
                NativeArray<OnlineFPSCharacterInputs> chunkOnlineFPSCharacterInputs = chunk.GetNativeArray(ref OnlineFPSCharacterInputsType);

                // Initialize the Temp collections
                if (!TmpRigidbodyIndexesProcessed.IsCreated)
                {
                    TmpRigidbodyIndexesProcessed = new NativeList<int>(24, Allocator.Temp);
                }
                if (!TmpRaycastHits.IsCreated)
                {
                    TmpRaycastHits = new NativeList<Unity.Physics.RaycastHit>(24, Allocator.Temp);
                }
                if (!TmpColliderCastHits.IsCreated)
                {
                    TmpColliderCastHits = new NativeList<ColliderCastHit>(24, Allocator.Temp);
                }
                if (!TmpDistanceHits.IsCreated)
                {
                    TmpDistanceHits = new NativeList<DistanceHit>(24, Allocator.Temp);
                }

                // Assign the global data of the processor
                OnlineFPSCharacterProcessor processor = default;
                processor.DeltaTime = DeltaTime;
                processor.CollisionWorld = CollisionWorld;
                processor.StoredKinematicCharacterBodyPropertiesFromEntity = StoredKinematicCharacterBodyPropertiesFromEntity;
                processor.PhysicsMassFromEntity = PhysicsMassFromEntity;
                processor.PhysicsVelocityFromEntity = PhysicsVelocityFromEntity;
                processor.TrackedTransformFromEntity = TrackedTransformFromEntity;
                processor.TmpRigidbodyIndexesProcessed = TmpRigidbodyIndexesProcessed;
                processor.TmpRaycastHits = TmpRaycastHits;
                processor.TmpColliderCastHits = TmpColliderCastHits;
                processor.TmpDistanceHits = TmpDistanceHits;

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (chunkPredictedGhosts[i].ShouldPredict(Tick))
                    {

                        Entity entity = chunkEntities[i];
                        // Assign the per-character data of the processor

                        processor.Entity = entity;

                        var localTransform = chunkTranslations[i];
                        processor.Translation = localTransform.Position;

                        processor.PhysicsCollider = chunkPhysicsColliders[i];
                        processor.CharacterBody = chunkCharacterBodies[i];
                        processor.CharacterHitsBuffer = chunkCharacterHitBuffers[i];
                        processor.CharacterDeferredImpulsesBuffer = chunkCharacterDeferredImpulsesBuffers[i];
                        processor.VelocityProjectionHitsBuffer = chunkVelocityProjectionHitBuffers[i];
                        processor.StatefulCharacterHitsBuffer = chunkStatefulCharacterHitsBuffers[i];
                        processor.OnlineFPSCharacter = chunkOnlineFPSCharacters[i];
                        processor.CharacterIputs = chunkOnlineFPSCharacterInputs[i];

                        processor.OnUpdate();

                        // Write back updated data
                        localTransform.Position = processor.Translation;
                        chunkTranslations[i] = localTransform;
                        chunkCharacterBodies[i] = processor.CharacterBody;
                        chunkOnlineFPSCharacters[i] = processor.OnlineFPSCharacter;
                    }
                }
            }
        }

        protected override void OnCreate()
        {
         
            GhostPredictionSystemGroup = World.GetExistingSystemManaged<PredictedSimulationSystemGroup>();

            PredictedCharacterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = MiscUtilities.CombineArrays(
                    KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                    new ComponentType[]
                    {
                        typeof(OnlineFPSCharacterComponent),
                        typeof(OnlineFPSCharacterInputs),
                        typeof(PredictedGhost),
                    }),
            });

            RequireForUpdate(PredictedCharacterQuery);
        }

        protected unsafe override void OnUpdate()
        {
            var networkTick = SystemAPI.GetSingleton<NetworkTime>();
            var tick = networkTick.ServerTick; //GhostPredictionSystemGroup.PredictingTick;
            CollisionWorld collisionWorld = SystemAPI.GetSingletonRW<BuildPhysicsWorldData>().ValueRW.PhysicsData.PhysicsWorld.CollisionWorld; //BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld;

            Dependency = new OnlineFPSCharacterJob
            {
                Tick = tick,
                DeltaTime = SystemAPI.Time.DeltaTime,
                CollisionWorld = collisionWorld,

                PhysicsVelocityFromEntity = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
                PhysicsMassFromEntity = SystemAPI.GetComponentLookup<PhysicsMass>(true),
                StoredKinematicCharacterBodyPropertiesFromEntity = SystemAPI.GetComponentLookup<StoredKinematicCharacterBodyProperties>(true),
                TrackedTransformFromEntity = SystemAPI.GetComponentLookup<TrackedTransform>(true),

                EntityType = GetEntityTypeHandle(),
                TranslationType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),

                KinematicCharacterBodyType = GetComponentTypeHandle<KinematicCharacterBody>(false),
                PhysicsColliderType = GetComponentTypeHandle<PhysicsCollider>(true),
                CharacterHitsBufferType = GetBufferTypeHandle<KinematicCharacterHit>(false),
                VelocityProjectionHitsBufferType = GetBufferTypeHandle<KinematicVelocityProjectionHit>(false),
                CharacterDeferredImpulsesBufferType = GetBufferTypeHandle<KinematicCharacterDeferredImpulse>(false),
                StatefulCharacterHitsBufferType = GetBufferTypeHandle<StatefulKinematicCharacterHit>(false),

                PredictedGhostType = GetComponentTypeHandle<PredictedGhost>(true),
                OnlineFPSCharacterType = GetComponentTypeHandle<OnlineFPSCharacterComponent>(false),
                OnlineFPSCharacterInputsType = GetComponentTypeHandle<OnlineFPSCharacterInputs>(true),
            }.Schedule(PredictedCharacterQuery, Dependency);

            
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //[UpdateBefore(typeof(PredictedPhysicsSystemGroup))]
    [UpdateAfter(typeof(OnlineFPSPlayerControlSystem))]
    public partial class OnlineFPSCharacterRotationSystem : SystemBase
    {
        public PredictedSimulationSystemGroup GhostPredictionSystemGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            GhostPredictionSystemGroup = World.GetExistingSystemManaged<PredictedSimulationSystemGroup>();
            RequireForUpdate<NetworkId>();
        }

        protected override void OnUpdate()
        {
            var networkTick = SystemAPI.GetSingleton<NetworkTime>();
            var tick = networkTick.ServerTick;//GhostPredictionSystemGroup.PredictingTick;
            float deltaTime = SystemAPI.Time.DeltaTime;

           
            Entities.ForEach((
                Entity entity,
                ref OnlineFPSCharacterComponent character,
                in OnlineFPSCharacterInputs inputs,
                in KinematicCharacterBody characterBody,
                in PredictedGhost predictedGhost) =>
            {
                if (!predictedGhost.ShouldPredict(tick))
                    return;

                LocalTransform characterRotation = GetComponent<LocalTransform>(entity);

                // Camera tilt
                {
                    float3 characterRight = MathUtilities.GetRightFromRotation(characterRotation.Rotation);
                    float characterMaxSpeed = characterBody.IsGrounded ? character.GroundMaxSpeed : character.AirMaxSpeed;
                    float3 characterLateralVelocity = math.projectsafe(characterBody.RelativeVelocity, characterRight);
                    float characterLateralVelocityRatio = math.clamp(math.length(characterLateralVelocity) / characterMaxSpeed, 0f, 1f);
                    bool velocityIsRight = math.dot(characterBody.RelativeVelocity, characterRight) > 0f;
                    float targetTiltAngle = math.lerp(0f, character.TiltAmount, characterLateralVelocityRatio);
                    targetTiltAngle = velocityIsRight ? -targetTiltAngle : targetTiltAngle;
                    character.CameraTiltAngle = math.lerp(character.CameraTiltAngle, targetTiltAngle, math.saturate(character.TiltSharpness * deltaTime));
                }

                // Compute character & view rotations from rotation input
                OnlineFPSCharacterUtilities.ComputeFinalRotationsFromRotationDelta(
                    ref characterRotation.Rotation,
                    ref character.ViewPitchDegrees,
                    inputs.LookYawPitchDegrees,
                    character.CameraTiltAngle,
                    -89f,
                    89f,
                    out quaternion localViewRotation,
                    out float canceledPitchDegrees);

                SetComponent(entity, characterRotation);
                //SetComponent(character.ViewEntity, new Rotation { Value = localViewRotation });
            }).Schedule();
        }
    }
}
