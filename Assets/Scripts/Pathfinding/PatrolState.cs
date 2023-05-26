using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PatrolState : MonoBehaviour
{
   
    public bool useLocalPatrolData;
}

public struct PatrolStateComponent : IComponentData, IEnableableComponent
{
    public int partolIndex;
    public bool rndExecuted;
   
}

public class PatrolStateBaker : Baker<PatrolState>
{
    public override void Bake(PatrolState authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new PatrolStateComponent { });
    }
}


