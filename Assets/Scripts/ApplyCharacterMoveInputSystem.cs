using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public partial class ApplyCharacterMoveInputSystem : SystemBase
    {


        protected override void OnUpdate()
        {


            Entities.ForEach((ref PhysicsVelocity vel, in CharacterMoveInputComponent moveInput, in CharacterControllerComponent cc) =>
            {

                var linerVel = vel.Linear;
                var moveValue = moveInput.value.normalized * cc.speed;
                vel.Linear = new Unity.Mathematics.float3(moveValue.x, linerVel.y, moveValue.y);




            }).Run();
        }

    }
}