using QFSW.QC;
using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ConsoleCommands : MonoBehaviour
{
    [Command("set-player's-health")]
    private static void SetPlayerHealth(int health)
    {

        var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerComponent), typeof(HealthComponent), typeof(MaxHealthComponent) });

        if (colorTablesQ.CalculateEntityCount() == 0)
            return;



        var entities = colorTablesQ.ToEntityArray(Allocator.Temp);


        _entityManager.SetComponentData(entities[0], new HealthComponent { value = health });

        entities.Dispose();

    }

    [Command("set-player's-max-health")]
    private static void SetPlayerMaxHealth(int health)
    {

        var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerComponent), typeof(HealthComponent), typeof(MaxHealthComponent) });

        if (colorTablesQ.CalculateEntityCount() == 0)
            return;



        var entities = colorTablesQ.ToEntityArray(Allocator.Temp);


        _entityManager.SetComponentData(entities[0], new HealthComponent { value = health });
        _entityManager.SetComponentData(entities[0], new MaxHealthComponent { value = health });

        entities.Dispose();

    }

    [Command("heal-player-max-hp")]
    private static void PlayerFullHP()
    {

        var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerComponent), typeof(HealthComponent), typeof(MaxHealthComponent) });

        if (colorTablesQ.CalculateEntityCount() == 0)
            return;



        var entities = colorTablesQ.ToEntityArray(Allocator.Temp);
        var maxHealth = colorTablesQ.ToComponentDataArray<MaxHealthComponent>(Allocator.Temp);

        var maxhealth = maxHealth[0];
        _entityManager.SetComponentData(entities[0], new HealthComponent { value = maxhealth.value });
        

        maxHealth.Dispose();
        entities.Dispose();

    }
}
