using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class GamePrefabsAuthroing : MonoBehaviour
    {

        public GameObject playerPrefab;
       
    }

   
   
    public struct GamePrefabsComponent : IComponentData
    {
        public Entity playerPrefab;

    }



   


    public class GamePrefabsComponentBaker : Baker<GamePrefabsAuthroing>
    {
        public override void Bake(GamePrefabsAuthroing authoring)
        {
         
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GamePrefabsComponent
            {
               
                playerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),


            }); 
        }
    }

   

}
