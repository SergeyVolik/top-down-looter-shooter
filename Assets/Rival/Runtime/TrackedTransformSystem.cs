using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Rival
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct TrackedTransformFixedSimulationSystem : ISystem
    {

        [BurstCompile]
        public partial struct TrackedTransformFixedSimulationJob : IJobEntity
        {

            [BurstCompile]
            public void Execute(ref TrackedTransform trackedTransform, in LocalTransform translation)
            {
                trackedTransform.PreviousFixedRateTransform = trackedTransform.CurrentFixedRateTransform;
                trackedTransform.CurrentFixedRateTransform = new RigidTransform(translation.Rotation, translation.Position);
            }
        }

        public void OnUpdate(ref SystemState state)
        {

            var job = new TrackedTransformFixedSimulationJob { 

            };

            job.ScheduleParallel();

           
        }
    }
}