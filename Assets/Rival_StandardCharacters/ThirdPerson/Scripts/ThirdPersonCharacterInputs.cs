using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct ThirdPersonCharacterInputs : IInputComponentData
{
    [GhostField(Quantization = 1000)] public float3 MoveVector;
    [GhostField] public InputEvent JumpRequested;
    [GhostField] public bool sprint;
}


