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
using Unity.Burst.Intrinsics;

[UpdateInGroup(typeof(KinematicCharacterUpdateGroup))]
public partial struct TopDownCharacterMovementSystem : ISystem, ISystemStartStop
{
    public BuildPhysicsWorld BuildPhysicsWorldSystem;

    public EntityQuery CharacterQuery;


    [BurstCompile]
    public struct ThirdPersonCharacterMovementJob : IJobChunk
    {
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
        public ComponentTypeHandle<PhysicsCollider> PhysicsColliderType;

        public BufferTypeHandle<KinematicCharacterHit> CharacterHitsBufferType;
        public BufferTypeHandle<KinematicVelocityProjectionHit> VelocityProjectionHitsBufferType;
        public BufferTypeHandle<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBufferType;
        public BufferTypeHandle<StatefulKinematicCharacterHit> StatefulCharacterHitsBufferType;

        public ComponentTypeHandle<TopDownCharacterComponent> ThirdPersonCharacterType;
        [ReadOnly]
        public ComponentTypeHandle<TopDownCharacterInputs> ThirdPersonCharacterInputsType;

        [NativeDisableContainerSafetyRestriction]
        public NativeList<int> TmpRigidbodyIndexesProcessed;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<Unity.Physics.RaycastHit> TmpRaycastHits;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<ColliderCastHit> TmpColliderCastHits;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<DistanceHit> TmpDistanceHits;


        [BurstCompile]
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
            NativeArray<TopDownCharacterComponent> chunkThirdPersonCharacters = chunk.GetNativeArray(ref ThirdPersonCharacterType);
            NativeArray<TopDownCharacterInputs> chunkThirdPersonCharacterInputs = chunk.GetNativeArray(ref ThirdPersonCharacterInputsType);

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
            TopDownCharacterProcessor processor = default;
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

            // Iterate on individual characters
            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = chunkEntities[i];

                // Assign the per-character data of the processor
                processor.Entity = entity;
                processor.Translation = chunkTranslations[i].Position;
                processor.Rotation = chunkTranslations[i].Rotation;
                processor.PhysicsCollider = chunkPhysicsColliders[i];
                processor.CharacterBody = chunkCharacterBodies[i];
                processor.CharacterHitsBuffer = chunkCharacterHitBuffers[i];
                processor.CharacterDeferredImpulsesBuffer = chunkCharacterDeferredImpulsesBuffers[i];
                processor.VelocityProjectionHitsBuffer = chunkVelocityProjectionHitBuffers[i];
                processor.StatefulCharacterHitsBuffer = chunkStatefulCharacterHitsBuffers[i];
                processor.ThirdPersonCharacter = chunkThirdPersonCharacters[i];
                processor.ThirdPersonCharacterInputs = chunkThirdPersonCharacterInputs[i];

                // Update character
                processor.OnUpdate();

                // Write back updated data
                // The core character update loop only writes to Translation, Rotation, KinematicCharacterBody, and the various character DynamicBuffers. 
                // You must remember to write back any extra data you modify in your own code

                var localTransfrom = chunkTranslations[i];
                localTransfrom.Position = processor.Translation;

                chunkTranslations[i] = localTransfrom;
                chunkCharacterBodies[i] = processor.CharacterBody;
                chunkPhysicsColliders[i] = processor.PhysicsCollider; // safe to remove if not needed. This would be needed if you resize the character collider, for example
                chunkThirdPersonCharacters[i] = processor.ThirdPersonCharacter; // safe to remove if not needed. This would be needed if you changed data in your own character component
            }
        }
    }


    public void OnCreate(ref SystemState state)
    {

       
        CharacterQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = MiscUtilities.CombineArrays(
                KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                new ComponentType[]
                {
                    typeof(TopDownCharacterComponent),
                    typeof(TopDownCharacterInputs),
                }),
        });

        state.RequireForUpdate(CharacterQuery);
    }


    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        var buildPhysWorld = SystemAPI.GetSingletonRW<BuildPhysicsWorldData>();

        state.Dependency = new ThirdPersonCharacterMovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,


            CollisionWorld = buildPhysWorld.ValueRW.PhysicsData.PhysicsWorld.CollisionWorld,

            PhysicsVelocityFromEntity = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
            PhysicsMassFromEntity = SystemAPI.GetComponentLookup<PhysicsMass>(true),
            StoredKinematicCharacterBodyPropertiesFromEntity = SystemAPI.GetComponentLookup<StoredKinematicCharacterBodyProperties>(true),
            TrackedTransformFromEntity = SystemAPI.GetComponentLookup<TrackedTransform>(true),

            EntityType = SystemAPI.GetEntityTypeHandle(),
            TranslationType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
            KinematicCharacterBodyType = SystemAPI.GetComponentTypeHandle<KinematicCharacterBody>(false),
            PhysicsColliderType = SystemAPI.GetComponentTypeHandle<PhysicsCollider>(false),
            CharacterHitsBufferType = SystemAPI.GetBufferTypeHandle<KinematicCharacterHit>(false),
            VelocityProjectionHitsBufferType = SystemAPI.GetBufferTypeHandle<KinematicVelocityProjectionHit>(false),
            CharacterDeferredImpulsesBufferType = SystemAPI.GetBufferTypeHandle<KinematicCharacterDeferredImpulse>(false),
            StatefulCharacterHitsBufferType = SystemAPI.GetBufferTypeHandle<StatefulKinematicCharacterHit>(false),

            ThirdPersonCharacterType = SystemAPI.GetComponentTypeHandle<TopDownCharacterComponent>(false),
            ThirdPersonCharacterInputsType = SystemAPI.GetComponentTypeHandle<TopDownCharacterInputs>(true),
        }.ScheduleParallel(CharacterQuery, state.Dependency);


        state.Dependency = KinematicCharacterUtilities.ScheduleDeferredImpulsesJob(ref state, CharacterQuery, state.Dependency);
    }

    public void OnStartRunning(ref SystemState state)
    {

    }

    public void OnStopRunning(ref SystemState state)
    {

    }
}
