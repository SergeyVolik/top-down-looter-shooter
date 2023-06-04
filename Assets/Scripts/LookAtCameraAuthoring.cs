using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public class LookAtCameraAuthoring : MonoBehaviour
    {

    }

    public struct LookAtCameraComponent : IComponentData
    {

    }

    public partial class LookAtCameraSystem : SystemBase
    {
        private Camera camera;
        protected override void OnUpdate()
        {
            if (camera == null)
                camera = Camera.main;

            if (camera == null)
                return;

            float3 camPos = camera.transform.position;
            foreach (var lt in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<LookAtCameraComponent>())
            {
                var vector = lt.ValueRO.Position - camPos;
                lt.ValueRW.Rotation = quaternion.LookRotation(math.normalize(vector), math.up());
            }

        }
    }

    public class LookAtCameraBaker : Baker<LookAtCameraAuthoring>
    {
        public override void Bake(LookAtCameraAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new LookAtCameraComponent());



        }
    }
}
