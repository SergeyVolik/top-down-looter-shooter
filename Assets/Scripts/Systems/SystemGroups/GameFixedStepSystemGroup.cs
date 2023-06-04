using Unity.Entities;
using Unity.Physics.Stateful;

namespace SV.ECS
{
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    [UpdateAfter(typeof(StatefulCollisionEventBufferSystem))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class GameFixedStepSystemGroup : ComponentSystemGroup
    {

    }

}
