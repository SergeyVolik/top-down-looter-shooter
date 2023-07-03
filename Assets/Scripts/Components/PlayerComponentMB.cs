using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class PlayerComponentMB : MonoBehaviour
    {
        public TextMesh nickName;

        private void OnEnable()
        {
            
        }
    }
    public struct PlayerComponent : IComponentData
    {

    }
    public class PlayerNickName : IComponentData
    {
        public Entity nickName;
    }
    public class PlayerBaker : Baker<PlayerComponentMB>
    {
        public override void Bake(PlayerComponentMB authoring)
        {
            if (!authoring.enabled)
                return;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PlayerComponent>(entity);

           
            if (authoring.nickName)
            {

                var tickNaemEntity = GetEntity(authoring.nickName, TransformUsageFlags.Dynamic);

               
                AddComponentObject<PlayerNickName>(entity, new PlayerNickName
                {
                    nickName = tickNaemEntity
                });
            }
        }
    }

}
