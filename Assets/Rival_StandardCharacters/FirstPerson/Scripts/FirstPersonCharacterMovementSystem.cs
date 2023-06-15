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
public partial class FirstPersonCharacterMovementSystem : SystemBase
{


    public EntityQuery CharacterQuery;

    [BurstCompile]
    public struct FirstPersonCharacterMovementJob : IJobChunk
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

        public ComponentTypeHandle<FirstPersonCharacterComponent> FirstPersonCharacterType;
        [ReadOnly]
        public ComponentTypeHandle<FirstPersonCharacterInputs> FirstPersonCharacterInputsType;

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
            NativeArray<FirstPersonCharacterComponent> chunkFirstPersonCharacters = chunk.GetNativeArray(ref FirstPersonCharacterType);
            NativeArray<FirstPersonCharacterInputs> chunkFirstPersonCharacterInputs = chunk.GetNativeArray(ref FirstPersonCharacterInputsType);

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
            FirstPersonCharacterProcessor processor = default;
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
                processor.FirstPersonCharacter = chunkFirstPersonCharacters[i];
                processor.FirstPersonCharacterInputs = chunkFirstPersonCharacterInputs[i];

                // Update character
                processor.OnUpdate();
                var trnas = chunkTranslations[i];

                trnas.Position = processor.Translation;
                // Write back updated data
                // The core character update loop only writes to Translation, Rotation, KinematicCharacterBody, and the various character DynamicBuffers. 
                // You must remember to write back any extra data you modify in your own code
                chunkTranslations[i] = trnas;
                chunkCharacterBodies[i] = processor.CharacterBody;
                chunkPhysicsColliders[i] = processor.PhysicsCollider; // safe to remove if not needed. This would be needed if you resize the character collider, for example
                chunkFirstPersonCharacters[i] = processor.FirstPersonCharacter; // safe to remove if not needed. This would be needed if you changed data in your own character component
            }
        }
    }

    protected override void OnCreate()
    {

        CharacterQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = MiscUtilities.CombineArrays(
                KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                new ComponentType[]
                {
                    typeof(FirstPersonCharacterComponent),
                    typeof(FirstPersonCharacterInputs),
                }),
        });

        RequireForUpdate(CharacterQuery);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();


    }

    protected unsafe override void OnUpdate()
    {


        var CollisionWorld = SystemAPI.GetSingleton<BuildPhysicsWorldData>().PhysicsData.PhysicsWorld.CollisionWorld;

        var PhysicsVelocityFromEntity = SystemAPI.GetComponentLookup<PhysicsVelocity>(true);
        var PhysicsMassFromEntity = SystemAPI.GetComponentLookup<PhysicsMass>(true);
        var StoredKinematicCharacterBodyPropertiesFromEntity = SystemAPI.GetComponentLookup<StoredKinematicCharacterBodyProperties>(true);
        var TrackedTransformFromEntity = SystemAPI.GetComponentLookup<TrackedTransform>(true);
        var entityTypeHandle = GetEntityTypeHandle();
        var TranslationType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false);
        var KinematicCharacterBodyType = GetComponentTypeHandle<KinematicCharacterBody>(false);
        var PhysicsColliderType = GetComponentTypeHandle<PhysicsCollider>(false);
        var CharacterHitsBufferType = GetBufferTypeHandle<KinematicCharacterHit>(false);
        var VelocityProjectionHitsBufferType = GetBufferTypeHandle<KinematicVelocityProjectionHit>(false);
        var StatefulCharacterHitsBufferType = GetBufferTypeHandle<StatefulKinematicCharacterHit>(false);
        var CharacterDeferredImpulsesBufferType = GetBufferTypeHandle<KinematicCharacterDeferredImpulse>(false);
        var componentTypeHandle3 = GetComponentTypeHandle<FirstPersonCharacterComponent>(false);
        var componentTypeHandle4 = GetComponentTypeHandle<FirstPersonCharacterInputs>(true);
      
        var job = new FirstPersonCharacterMovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            CollisionWorld = CollisionWorld,

            PhysicsVelocityFromEntity = PhysicsVelocityFromEntity,
            PhysicsMassFromEntity = PhysicsMassFromEntity,
            StoredKinematicCharacterBodyPropertiesFromEntity = StoredKinematicCharacterBodyPropertiesFromEntity,
            TrackedTransformFromEntity = TrackedTransformFromEntity,

            EntityType = entityTypeHandle,
            TranslationType = TranslationType,

            KinematicCharacterBodyType = KinematicCharacterBodyType,
            PhysicsColliderType = PhysicsColliderType,
            CharacterHitsBufferType = CharacterHitsBufferType,
            VelocityProjectionHitsBufferType = VelocityProjectionHitsBufferType,
            CharacterDeferredImpulsesBufferType = CharacterDeferredImpulsesBufferType,
            StatefulCharacterHitsBufferType = StatefulCharacterHitsBufferType,

            FirstPersonCharacterType = componentTypeHandle3,
            FirstPersonCharacterInputsType = componentTypeHandle4,
        };

        Dependency = job.ScheduleParallel(CharacterQuery, Dependency);

        Dependency = KinematicCharacterUtilities.ScheduleDeferredImpulsesJob(ref this.CheckedStateRef, CharacterQuery, Dependency, false);
    }
}