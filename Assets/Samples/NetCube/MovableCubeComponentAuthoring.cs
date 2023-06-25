using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct MovableCubeComponent : IComponentData
{
}

[DisallowMultipleComponent]
public class MovableCubeComponentAuthoring : MonoBehaviour
{
    private void OnEnable()
    {
        
    }
    class MovableCubeComponentBaker : Baker<MovableCubeComponentAuthoring>
    {
        public override void Bake(MovableCubeComponentAuthoring authoring)
        {
            if (!authoring.enabled)
                return;
            MovableCubeComponent component = default(MovableCubeComponent);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
