using QFSW.QC;
using SV.ECS;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class ConsoleCommands : MonoBehaviour
{
    [Command("set-current-health")]
    public static void SetPlayerCurrentHealth(int health)
    {

        var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerComponent), typeof(HealthComponent), typeof(MaxHealthComponent) });

        if (colorTablesQ.CalculateEntityCount() == 0)
            return;



        var entities = colorTablesQ.ToEntityArray(Allocator.Temp);

        var maxComp = _entityManager.GetComponentData<MaxHealthComponent>(entities[0]);
        var healthComp = new HealthComponent { value = math.clamp(health, 0, maxComp.value) };

        _entityManager.SetComponentData(entities[0], healthComp);

        entities.Dispose();

    }

    [Command("set-health-regen")]
    public static void SetPlayerRegen(float interval)
    {

        var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerComponent), typeof(HealthComponent), typeof(HPRegenComponent) });

        if (colorTablesQ.CalculateEntityCount() == 0)
            return;



        var entities = colorTablesQ.ToEntityArray(Allocator.Temp);

        _entityManager.SetComponentData(entities[0], new HPRegenComponent
        {
            regenInterval = interval
        });


        entities.Dispose();

    }

    [Command("stats-update")]
    public static void UpdateStats()
    {
        PlayerStatsUtils.UpdateStats(World.DefaultGameObjectInjectionWorld.EntityManager);
    }

    [Command("set-max-heath")]
    public static void SetPlayerMaxHealth(int health)
    {

        var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerStatsComponent) });

        if (colorTablesQ.CalculateEntityCount() == 0)
            return;



        var entities = colorTablesQ.ToEntityArray(Allocator.Temp);


        var stast = _entityManager.GetComponentData<PlayerStatsComponent>(entities[0]);

        stast.maxHealth = health;

        _entityManager.SetComponentData(entities[0], stast);

        entities.Dispose();

        UpdateStats();
    }

    [Command("heal-player-max-hp")]
    public static void PlayerFullHP()
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
