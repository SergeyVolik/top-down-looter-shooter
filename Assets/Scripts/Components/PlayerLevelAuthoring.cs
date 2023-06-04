using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class PlayerLevelAuthoring : MonoBehaviour
    {
        public ExpData[] expData;

        [System.Serializable]
        public class ExpData
        {
            public int expToReachLevel;
          
        }


    }


    public struct PlayerLevelComponent : IComponentData
    {
        public int level;
        public int currentExp;

    }

    public struct LevelUpComponent : IComponentData
    {
        public int currentLevel;

    }

    public struct PlayerExpUpgradeComponent : IBufferElementData
    {
        public int expForNextLevel;
        public int addHp;
    }

    public class PlayerLevelComponentBaker : Baker<PlayerLevelAuthoring>
    {
        public override void Bake(PlayerLevelAuthoring authoring)
        {

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerLevelComponent
            {
                currentExp = 0,
                level = 1


            });

            var buffer = AddBuffer<PlayerExpUpgradeComponent>(entity);

            if (authoring.expData != null)
            {
                foreach (var item in authoring.expData)
                {
                    buffer.Add(new PlayerExpUpgradeComponent
                    {
                        expForNextLevel = item.expToReachLevel,
                       
                    });
                }
            }
        }
    }



}
