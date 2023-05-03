using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public partial class LookWithCharacterMoveInputSystem : SystemBase
    {


        protected override void OnUpdate()
        {


            Entities.ForEach((ref LocalTransform transform, in CharacterMoveInputComponent moveInput) =>
            {


                var moveValue = moveInput.value;


                if (moveValue != Vector2.zero)
                {
                    var lookPos = transform.Position;
                    var lookPos2 = lookPos;
                    lookPos2.x += moveInput.value.x;
                    lookPos2.z += moveInput.value.y;

                    Debug.DrawLine(lookPos, lookPos2, Color.red, 0.5f);

                    var vector = lookPos2 - lookPos;


                    transform.Rotation = quaternion.LookRotation(vector, math.up());
                }

            }).Run();
        }

    }
}