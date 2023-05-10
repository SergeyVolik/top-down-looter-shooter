using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    [UpdateAfter(typeof(LocalToWorldSystem))]
    public partial class MoveToPlayerSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();

        }

        protected override void OnUpdate()
        {
            var ltwLookUp = SystemAPI.GetComponentLookup<LocalToWorld>();
            var player = SystemAPI.GetSingletonEntity<PlayerComponent>();
            float3 playerPos = default;
            if (ltwLookUp.TryGetComponent(player, out var ltw))
            {
                playerPos = ltw.Position;
            }
            Entities.ForEach((ref Entity e, ref CharacterMoveInputComponent input, in MoveToPlayerComponent moveToTarget) =>
            {


                var selfPos = ltwLookUp.GetRefRO(e).ValueRO.Position;
                var vecotr = (playerPos - selfPos);
                vecotr = math.normalize(vecotr);

                var dist = math.distance(selfPos, playerPos);

                var vector = new Vector2(vecotr.x, vecotr.z);

                if (dist < moveToTarget.stopDistance + 1f && dist > moveToTarget.stopDistance - 1f)
                    vector = Vector2.zero;

                else if (dist < moveToTarget.stopDistance)
                    vector *= -1;
               
                input.value = vector;

            }).Schedule();
        }

    }
}