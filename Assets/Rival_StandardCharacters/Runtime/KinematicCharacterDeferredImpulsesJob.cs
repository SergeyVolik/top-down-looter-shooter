using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Profiling;
using Unity.Transforms;

namespace Rival
{
    [BurstCompile]
    public struct KinematicCharacterDeferredImpulsesJob : IJobChunk
    {
        public int InitialHashMapCapacity;
        public BufferTypeHandle<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBufferType;

        public ComponentLookup<KinematicCharacterBody> CharacterBodyFromEntity;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityFromEntity;
        public ComponentLookup<LocalTransform> TranslationFromEntity;


        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            BufferAccessor<KinematicCharacterDeferredImpulse> chunkCharacterrDeferredImpulsesBuffers = chunk.GetBufferAccessor(ref CharacterDeferredImpulsesBufferType);

            for (int i = 0; i < chunk.Count; i++)
            {
                DynamicBuffer<KinematicCharacterDeferredImpulse> characterDeferredImpulsesBuffer = chunkCharacterrDeferredImpulsesBuffers[i];

                KinematicCharacterUtilities.ProcessDeferredImpulses(
                    ref TranslationFromEntity,
                    ref PhysicsVelocityFromEntity,
                    ref CharacterBodyFromEntity,
                    in characterDeferredImpulsesBuffer);
            }
        }
    }
}