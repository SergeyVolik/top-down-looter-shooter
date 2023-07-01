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
using UnityEngine.SceneManagement;

[Serializable]
public class WeaponsDictionary : UnitySerializedDictionary<string, WeaponSO> { }
public class ConsoleCommands : MonoBehaviour
{
    public WeaponsDictionary weapons = new WeaponsDictionary();

    private static ConsoleCommands Instance;
    private void Awake()
    {
        Instance = this;
    }


    [Command("change-gun")]
    public static void ChangeGun(string gunName)
    {
        if (Instance.weapons.TryGetValue(gunName, out var gunData))
        {
            var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var gunDatabaseQuery = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(GunsDatabase) });

            if (gunDatabaseQuery.CalculateEntityCount() == 0)
            {

                Debug.LogError("GunsDatabase not exist");
                return;
            }

            var damageEntities = gunDatabaseQuery.ToEntityArray(Allocator.Temp);
            var gunsDatabase = _entityManager.GetComponentData<GunsDatabase>(damageEntities[0]);

            if (gunsDatabase.guns.TryGetValue(gunData.guid, out var gunPrefabEntity))
            {
                var playerQuery = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(GunSlotsComponent) });

                if (playerQuery.CalculateEntityCount() == 0)
                {
                    Debug.LogError("player not exist");
                    return;
                }

                var playerEntities = playerQuery.ToEntityArray(Allocator.Temp);

                _entityManager.SetComponentData(playerEntities[0], new SpawnGunMainComponent
                {
                    gunPrefab = gunPrefabEntity
                });
                _entityManager.SetComponentEnabled<SpawnGunMainComponent>(playerEntities[0], true);

                playerEntities.Dispose();
                playerQuery.Dispose();
            }
            else
            {
                Debug.LogError("gun not exist");
            }

            damageEntities.Dispose();
            gunDatabaseQuery.Dispose();

            return;
        }

        Debug.LogError($"gunName: {gunName} not exist");

    }
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

        colorTablesQ.Dispose();
        entities.Dispose();

    }

    [Command("set-health-regen")]
    public static void SetPlayerRegen(float interval)
    {

        var _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerStatsComponent) });

        if (colorTablesQ.CalculateEntityCount() == 0)
            return;



        var entities = colorTablesQ.ToEntityArray(Allocator.Temp);

        var stats = _entityManager.GetComponentData<PlayerStatsComponent>(entities[0]);

        stats.hpRegenInterval = interval;

        _entityManager.SetComponentData(entities[0], stats);


        entities.Dispose();

        colorTablesQ.Dispose();
        UpdateStats();
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
        colorTablesQ.Dispose();
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

        colorTablesQ.Dispose();
        maxHealth.Dispose();
        entities.Dispose();

    }

    [Command("start-client-server")]
    public async static void StartClientServer()
    {
        await RelayConnection.Instance.HostServerAndClient();

        var handle = SceneManager.UnloadSceneAsync(1);

        handle.completed += (res) =>
        {
            SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        };



    }
    [Command("join-server")]
    public async static void JoinServer(string relayCode)
    {
        await RelayConnection.Instance.JoinAsClient(relayCode);

        var handle = SceneManager.UnloadSceneAsync(1);

        handle.completed += (res) =>
        {
            SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        };
    }

    [Command("leave-server")]
    public static void LeaveServer()
    {
        RelayConnection.Instance.LeaveGame();

        var handle = SceneManager.UnloadSceneAsync(2);

        handle.completed += (res) =>
        {
            SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        };
    }

    [Command("set-name")]
    public static void SetUserServer(string name)
    {
        LocalPlayerData.Player.DisplayName.Value = name;
    }
    [Command("start-local-server")]
    public static void StartLocalServerAndClient(ushort port)
    {
        if(port <= 0)
            RelayConnection.Instance.StartClientServerLocal();
        else RelayConnection.Instance.StartClientServerLocal(port);

        var handle = SceneManager.UnloadSceneAsync(1);

        handle.completed += (res) =>
        {
            SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        };
    }

    [Command("join-local")]
    public static void JoinLocalServer(ushort port)
    {
        RelayConnection.Instance.ConnectToServer(port: port);
        var handle = SceneManager.UnloadSceneAsync(1);

        handle.completed += (res) =>
        {
            SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        };
    }

    [Command("join-with-ip")]
    public static void JoinWithIp(string ip,  ushort port)
    {
        RelayConnection.Instance.ConnectToServer(ip, port);
        var handle = SceneManager.UnloadSceneAsync(1);

        handle.completed += (res) =>
        {
            SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        };
    }
}
