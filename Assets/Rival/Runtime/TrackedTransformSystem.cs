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
    public partial class TrackedTransformFixedSimulationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency = Entities
                .ForEach((ref TrackedTransform trackedTransform, in LocalTransform translation) =>
                {
                    trackedTransform.PreviousFixedRateTransform = trackedTransform.CurrentFixedRateTransform;
                    trackedTransform.CurrentFixedRateTransform = new RigidTransform(translation.Rotation, translation.Position);
                }).Schedule(Dependency);
        }
    }
}