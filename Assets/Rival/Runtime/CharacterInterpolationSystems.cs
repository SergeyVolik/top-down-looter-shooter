using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Rival
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public partial class CharacterInterpolationFixedUpdateSystem : SystemBase
    {
        public double LastFixedUpdateElapsedTime = -1;
        public float LastFixedUpdateTimeStep = 0f;

        private EntityQuery _interpolatedEntitiesQuery;

        [BurstCompile]
        public unsafe struct CharacterInterpolationFixedUpdateJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<LocalTransform> TranslationType;
          
            public ComponentTypeHandle<CharacterInterpolation> CharacterInterpolationType;


            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<LocalTransform> chunkTranslations = chunk.GetNativeArray(TranslationType);

                NativeArray<CharacterInterpolation> chunkCharacterInterpolations = chunk.GetNativeArray(CharacterInterpolationType);

                void* chunkInterpolationsPtr = chunkCharacterInterpolations.GetUnsafePtr();
                int chunkCount = chunk.Count;
                int sizeCharacterInterpolation = UnsafeUtility.SizeOf<CharacterInterpolation>();

                int sizeTranslation = UnsafeUtility.SizeOf<LocalTransform>();

                // Copy all Translation & Rotation to the character interpolation component
                {


                    UnsafeUtility.MemCpyStride(
                        (void*)((long)chunkInterpolationsPtr),
                        sizeCharacterInterpolation,
                        chunkTranslations.GetUnsafeReadOnlyPtr(),
                        sizeTranslation,
                        sizeTranslation,
                        chunkCount
                    );
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _interpolatedEntitiesQuery = GetEntityQuery(typeof(LocalTransform), typeof(CharacterInterpolation));
        }

        protected override void OnUpdate()
        {
            LastFixedUpdateElapsedTime = SystemAPI.Time.ElapsedTime;
            LastFixedUpdateTimeStep = SystemAPI.Time.DeltaTime;

            Dependency = new CharacterInterpolationFixedUpdateJob
            {
                TranslationType = GetComponentTypeHandle<LocalTransform>(true),
              
                CharacterInterpolationType = GetComponentTypeHandle<CharacterInterpolation>(false),
            }.ScheduleParallel(_interpolatedEntitiesQuery, Dependency);
        }
    }

    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(LocalToWorldSystem))]
    public partial class CharacterInterpolationVariableUpdateSystem : SystemBase
    {
        private EntityQuery _interpolatedEntitiesQuery;
        private CharacterInterpolationFixedUpdateSystem _characterInterpolationFixedUpdateSystem;

        [BurstCompile]
        public struct CharacterInterpolationUpdateJob : IJobChunk
        {
            public float NormalizedTimeAhead;

            [ReadOnly]
            public ComponentTypeHandle<LocalTransform> TranslationType;
           
            public ComponentTypeHandle<LocalToWorld> LocalToWorldType;
            public ComponentTypeHandle<CharacterInterpolation> CharacterInterpolationType;


            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<LocalTransform> chunkTranslations = chunk.GetNativeArray(TranslationType);

                NativeArray<LocalToWorld> chunkLocalToWorlds = chunk.GetNativeArray(LocalToWorldType);
                NativeArray<CharacterInterpolation> chunkCharacterInterpolations = chunk.GetNativeArray(CharacterInterpolationType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    LocalTransform translation = chunkTranslations[i];

                    LocalToWorld localToWorld = chunkLocalToWorlds[i];
                    CharacterInterpolation characterInterpolation = chunkCharacterInterpolations[i];

                    RigidTransform targetTransform = new RigidTransform(translation.Rotation, translation.Position);

                    // Interpolation skipping
                    if (characterInterpolation.InterpolationSkipping > 0)
                    {
                        if (characterInterpolation.ShouldSkipNextTranslationInterpolation())
                        {
                            characterInterpolation.PreviousTransform.pos = targetTransform.pos;
                        }
                        if (characterInterpolation.ShouldSkipNextRotationInterpolation())
                        {
                            characterInterpolation.PreviousTransform.rot = targetTransform.rot;
                        }

                        characterInterpolation.InterpolationSkipping = 0;
                        chunkCharacterInterpolations[i] = characterInterpolation;
                    }

                    quaternion interpolatedRot = targetTransform.rot;
                    if (characterInterpolation.InterpolateRotation == 1)
                    {
                        interpolatedRot = math.slerp(characterInterpolation.PreviousTransform.rot, targetTransform.rot, NormalizedTimeAhead);
                    }
                    float3 interpolatedPos = targetTransform.pos;
                    if (characterInterpolation.InterpolateTranslation == 1)
                    {
                        interpolatedPos = math.lerp(characterInterpolation.PreviousTransform.pos, targetTransform.pos, NormalizedTimeAhead);
                    }
                    localToWorld.Value = new float4x4(interpolatedRot, interpolatedPos);

                    chunkLocalToWorlds[i] = localToWorld;
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _interpolatedEntitiesQuery = GetEntityQuery(typeof(LocalTransform), typeof(CharacterInterpolation));
            _characterInterpolationFixedUpdateSystem = World.GetOrCreateSystemManaged<CharacterInterpolationFixedUpdateSystem>();

            RequireForUpdate(_interpolatedEntitiesQuery);
        }

        protected override void OnUpdate()
        {
            if (_characterInterpolationFixedUpdateSystem.LastFixedUpdateElapsedTime <= 0f)
            {
                return;
            }

            float fixedTimeStep = _characterInterpolationFixedUpdateSystem.LastFixedUpdateTimeStep;
            if (fixedTimeStep == 0f)
            {
                return;
            }

            float timeAheadOfLastFixedUpdate = (float)(SystemAPI.Time.ElapsedTime - _characterInterpolationFixedUpdateSystem.LastFixedUpdateElapsedTime);
            float normalizedTimeAhead = math.clamp(timeAheadOfLastFixedUpdate / fixedTimeStep, 0f, 1f);

            Dependency = new CharacterInterpolationUpdateJob
            {
                NormalizedTimeAhead = normalizedTimeAhead,

                TranslationType = GetComponentTypeHandle<LocalTransform>(true),
               
                LocalToWorldType = GetComponentTypeHandle<LocalToWorld>(false),
                CharacterInterpolationType = GetComponentTypeHandle<CharacterInterpolation>(false),
            }.ScheduleParallel(_interpolatedEntitiesQuery, Dependency);
        }
    }
}