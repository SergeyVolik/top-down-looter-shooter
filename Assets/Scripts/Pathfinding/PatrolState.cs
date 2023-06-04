using Unity.Entities;
using UnityEngine;

public class PatrolState : MonoBehaviour
{
   
    public bool useLocalPatrolData;

    public void OnEnable()
    {
        
    }
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
        if (!authoring.enabled)
            return;

        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new PatrolStateComponent { });
    }
}


