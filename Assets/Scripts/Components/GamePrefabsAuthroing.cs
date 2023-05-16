using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class GamePrefabsAuthroing : MonoBehaviour
    {
        public GameObject enemy;

        public GameObject playerPrefab;

        public GameObject pistol;
        public GameObject shotgun;
        public GameObject minigun;
        public GameObject uzi;
    }

    public struct GamePrefabsComponent : IComponentData
    {
        public Entity enemy;

        public Entity playerPrefab;

        public Entity pistol;
        public Entity shotgun;
        public Entity minigun;
        public Entity uzi;

    }

    public class IndividualRandomBaker1 : Baker<GamePrefabsAuthroing>
    {
        public override void Bake(GamePrefabsAuthroing authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GamePrefabsComponent
            {
                enemy = GetEntity(authoring.enemy, TransformUsageFlags.Dynamic),
                playerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),
                pistol = GetEntity(authoring.pistol, TransformUsageFlags.Dynamic),
                shotgun = GetEntity(authoring.shotgun, TransformUsageFlags.Dynamic),
                minigun = GetEntity(authoring.minigun, TransformUsageFlags.Dynamic),
                uzi = GetEntity(authoring.uzi, TransformUsageFlags.Dynamic),


            }); 
        }
    }

   

}
