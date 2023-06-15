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

        ComponentLookup<PhysicsVelocity> componentLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true);
        ComponentLookup<PhysicsMass> componentLookup1 = SystemAPI.GetComponentLookup<PhysicsMass>(true);
        ComponentLookup<StoredKinematicCharacterBodyProperties> componentLookup2 = SystemAPI.GetComponentLookup<StoredKinematicCharacterBodyProperties>(true);
        ComponentLookup<TrackedTransform> componentLookup3 = SystemAPI.GetComponentLookup<TrackedTransform>(true);
        EntityTypeHandle entityTypeHandle = GetEntityTypeHandle();
        ComponentTypeHandle<LocalTransform> componentTypeHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(false);
        ComponentTypeHandle<KinematicCharacterBody> componentTypeHandle1 = GetComponentTypeHandle<KinematicCharacterBody>(false);
        ComponentTypeHandle<PhysicsCollider> componentTypeHandle2 = GetComponentTypeHandle<PhysicsCollider>(false);
        BufferTypeHandle<KinematicCharacterHit> bufferTypeHandle = GetBufferTypeHandle<KinematicCharacterHit>(false);
        BufferTypeHandle<KinematicVelocityProjectionHit> bufferTypeHandle1 = GetBufferTypeHandle<KinematicVelocityProjectionHit>(false);
        BufferTypeHandle<StatefulKinematicCharacterHit> bufferTypeHandle2 = GetBufferTypeHandle<StatefulKinematicCharacterHit>(false);
        BufferTypeHandle<KinematicCharacterDeferredImpulse> bufferTypeHandle3 = GetBufferTypeHandle<KinematicCharacterDeferredImpulse>(false);
        ComponentTypeHandle<FirstPersonCharacterComponent> componentTypeHandle3 = GetComponentTypeHandle<FirstPersonCharacterComponent>(false);
        ComponentTypeHandle<FirstPersonCharacterInputs> componentTypeHandle4 = GetComponentTypeHandle<FirstPersonCharacterInputs>(true);
      
        var job = new FirstPersonCharacterMovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            CollisionWorld = CollisionWorld,

            PhysicsVelocityFromEntity = componentLookup,
            PhysicsMassFromEntity = componentLookup1,
            StoredKinematicCharacterBodyPropertiesFromEntity = componentLookup2,
            TrackedTransformFromEntity = componentLookup3,

            EntityType = entityTypeHandle,
            TranslationType = componentTypeHandle,

            KinematicCharacterBodyType = componentTypeHandle1,
            PhysicsColliderType = componentTypeHandle2,
            CharacterHitsBufferType = bufferTypeHandle,
            VelocityProjectionHitsBufferType = bufferTypeHandle1,
            CharacterDeferredImpulsesBufferType = bufferTypeHandle3,
            StatefulCharacterHitsBufferType = bufferTypeHandle2,

            FirstPersonCharacterType = componentTypeHandle3,
            FirstPersonCharacterInputsType = componentTypeHandle4,
        };

        Dependency = job.ScheduleParallel(CharacterQuery, Dependency);

        Dependency = KinematicCharacterUtilities.ScheduleDeferredImpulsesJob(ref this.CheckedStateRef, CharacterQuery, Dependency, false);
    }
}