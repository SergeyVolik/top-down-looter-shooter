using System;
using System.Linq;
using Unity.Entities;

namespace SV.ECS
{
    [UpdateAfter(typeof(CollectorTriggerSystem))]
    [UpdateInGroup(typeof(GameFixedStepSystemGroup))]
    public partial struct ApplyExperienceSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerLevelComponent>();
            state.RequireForUpdate<PlayerExpUpgradeComponent>();

        }

        public void OnUpdate(ref SystemState state)
        {


            var expEntity = SystemAPI.GetSingletonEntity<PlayerLevelComponent>();
            var currentExp = SystemAPI.GetSingleton<PlayerLevelComponent>();
            var playerExpUpgradeData = SystemAPI.GetBuffer<PlayerExpUpgradeComponent>(expEntity);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (exp, e) in SystemAPI.Query<RefRO<ExperienceComponent>>().WithAll<CollectedComponent>().WithEntityAccess())
            {
                if (currentExp.level >= playerExpUpgradeData.Length)
                    break;

                var nextLevel = playerExpUpgradeData[currentExp.level - 1];

                currentExp.currentExp += exp.ValueRO.value;


                if (currentExp.currentExp >= nextLevel.expForNextLevel)
                {

                    var entity = ecb.CreateEntity();

                   
                    currentExp.currentExp = currentExp.currentExp - nextLevel.expForNextLevel;
                    currentExp.level += 1;

                    ecb.AddComponent(entity, new LevelUpComponent
                    {
                        currentLevel = currentExp.level,
                    });
                }
            }

            SystemAPI.SetComponent(expEntity, currentExp);


        }
    }

    [UpdateAfter(typeof(CollectorTriggerSystem))]
    [UpdateInGroup(typeof(GameFixedStepSystemGroup))]
    public partial class LevelUpSystemSystem : SystemBase
    {
        protected override void OnCreate()
        { 
            base.OnCreate();
            RequireForUpdate<PlayerLevelComponent>();
            RequireForUpdate<PlayerExpUpgradeComponent>();
            var query = SystemAPI.QueryBuilder().Build();

           
        }
        public static event Action OnLevelUp = delegate { };

        protected override void OnUpdate()
        {


           



            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

            var hasItems = false;
            foreach (var (exp, e) in SystemAPI.Query<RefRO<LevelUpComponent>>().WithEntityAccess())
            {
                hasItems = true;

                //foreach (var (health, maxHealth) in SystemAPI.Query<RefRW<HealthComponent>, RefRW<MaxHealthComponent>>().WithAll<PlayerComponent>())
                //{
                //    maxHealth.ValueRW.value += 1;
                //    health.ValueRW.value = maxHealth.ValueRO.value;
                //}

                ecb.DestroyEntity(e);
            }

            if (hasItems)
            {
                OnLevelUp.Invoke();
            }

           


        }
    }
}
