using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    public GameObject spawnPoint;
    public GameObject playerPrefab;

    public class Baker : Baker<PlayerSpawnPoint>
    {
        public override void Bake(PlayerSpawnPoint authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerSpawnP
            {
                playerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),
                spawnPoint = GetEntity(authoring.spawnPoint, TransformUsageFlags.Dynamic)
            });
        }
    }




}


public struct PlayerSpawnP : IComponentData
{
    public Entity spawnPoint;
    public Entity playerPrefab;
}

