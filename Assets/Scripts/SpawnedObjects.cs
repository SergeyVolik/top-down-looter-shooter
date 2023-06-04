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
 

    private EntityQuery KilledEnemiesStat;
    private EntityQuery _collectedStat;
    private TMP_Text text;

    private void Awake()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
       
        KilledEnemiesStat = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(KilledEnemies) });
        _collectedStat = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(CollectedItems) });
        text = GetComponent<TMPro.TMP_Text>();
        text.text = $"spawned: 0";
    }
    private void Update()
    {
        
       
        var killed = KilledEnemiesStat.ToComponentDataArray<KilledEnemies>(Allocator.Temp);
        var collected = _collectedStat.ToComponentDataArray<CollectedItems>(Allocator.Temp);

      

      
        text.text = $"killed enemies: {killed[0].value} collected: {collected[0].value}";

        collected.Dispose();
     
        killed.Dispose();
    }
}
