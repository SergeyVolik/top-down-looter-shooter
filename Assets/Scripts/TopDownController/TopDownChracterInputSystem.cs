using SV.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Services.Lobbies.Models;
using UnityEngine;




[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class TopDownChracterInputSystem : SystemBase
{
    public FixedUpdateTickSystem FixedUpdateTickSystem;

    private PlayerControlls input;

    protected override void OnCreate()
    {
        base.OnCreate();
        input = new PlayerControlls();
        input.Enable();
        FixedUpdateTickSystem = World.GetOrCreateSystemManaged<FixedUpdateTickSystem>();
    }


    protected override void OnUpdate()
    {
        uint fixedTick = FixedUpdateTickSystem.FixedTick;

        // Gather raw input
        float2 moveInput = input.Controlls.Move.ReadValue<Vector2>();

        var worldname = World.Unmanaged.Name;


        bool jumpInput = input.Controlls.Jump.triggered;

        //Debug.Log($"Update TopDownChracterInputSystem");

        foreach (var characterInputs in SystemAPI.Query<RefRW<TopDownCharacterInputs>>().WithAll<GhostOwnerIsLocal>())
        {
            characterInputs.ValueRW.moveX = moveInput.x;
            characterInputs.ValueRW.moveY = moveInput.y;
         
            if (jumpInput)
            {
                characterInputs.ValueRW.JumpRequested = jumpInput;
            }

            characterInputs.ValueRW.LastInputsProcessingTick = fixedTick;
        }



    }
}
