using Unity.Entities;

namespace SV.ECS
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(DropExecuterSystem))]
    public partial struct DestroyAfterDeathSystem : ISystem
    {

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


            state.Dependency = new DestroyJob
            {
                ecb = ecb,
            }.Schedule(state.Dependency);
        }

        public partial struct DestroyJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public void Execute(ref DeadComponent dead, Entity e)
            {
                if (dead.frameSkipped)
                    ecb.DestroyEntity(e);

                dead.frameSkipped = true;
            }
        }
    }

}
