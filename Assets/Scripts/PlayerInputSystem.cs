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



        }
        protected override void OnUpdate()
        {

            var moveInput = input.Controlls.Move.ReadValue<Vector2>();
            var y = Input.GetAxis("Vertical");
            var x = Input.GetAxis("Horizontal");

            moveInput = new Vector2(x, y);

            Entities.ForEach((ref CharacterMoveInputComponent moveInputComp) =>
            {

                moveInputComp.value = moveInput;

            }).WithAll<ReadPlayerInputComponent, PlayerComponent>().Run();
        }
    }

   

   


   

   
}

