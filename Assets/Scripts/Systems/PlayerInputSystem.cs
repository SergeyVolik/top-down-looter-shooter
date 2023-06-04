using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace SV.ECS
{
    public partial class PlayerInputSystem : SystemBase
    {
        private PlayerControlls input;

        protected override void OnCreate()
        {
            base.OnCreate();
            input = new PlayerControlls();
            input.Enable();

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            input.Disable();
        }
        protected override void OnUpdate()
        {
            
            var moveInput = input.Controlls.Move.ReadValue<Vector2>();

            Entities.ForEach((ref CharacterMoveInputComponent moveInputComp) =>
            {

                moveInputComp.value = moveInput;

            }).WithAll<ReadPlayerInputComponent, PlayerComponent>().Run();
        }
    }

   

   


   

   
}

