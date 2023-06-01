using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class SpawnedObjects : MonoBehaviour
{
    private EntityManager _entityManager;
    private EntityQuery colorTablesQ;

    private EntityQuery KilledEnemiesStat;
    private EntityQuery _collectedStat;
    private TMP_Text text;

    private void Awake()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(Spawner) });
        KilledEnemiesStat = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(KilledEnemies) });
        _collectedStat = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(CollectedItems) });
        text = GetComponent<TMPro.TMP_Text>();
        text.text = $"spawned: 0";
    }
    private void Update()
    {
        if (colorTablesQ.CalculateChunkCount() == 0)
            return;

        var health = colorTablesQ.ToComponentDataArray<Spawner>(Allocator.Temp);
        var killed = KilledEnemiesStat.ToComponentDataArray<KilledEnemies>(Allocator.Temp);
        var collected = _collectedStat.ToComponentDataArray<CollectedItems>(Allocator.Temp);
        var spawner = health[0];

        var spawned = spawner.gridSize.x * spawner.gridSize.y * spawner.gridSize.z;
        text.text = $"spawned: {spawned * spawner.cycleCount} killed enemies: {killed[0].value} collected: {collected[0].value}";

        collected.Dispose();
        health.Dispose();
        killed.Dispose();
    }
}
